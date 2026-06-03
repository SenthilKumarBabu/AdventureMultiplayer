using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative race manager.
    ///
    /// Tracks each player's checkpoint progress and calculates race positions in
    /// real-time by combining checkpoint index with distance to the next checkpoint.
    ///
    /// Setup:
    ///   - Add to the GameManager GameObject.
    ///   - Assign all RaceCheckpoint components in scene order (index 0 = first, last = finish).
    ///   - Add RacePlayerTracker to the player prefab — it self-registers here on spawn.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Race Manager")]
    public class RaceManager : NetworkBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [SerializeField] private RaceCheckpoint[] checkpoints;

        /// <summary>Read-only on clients — one entry per connected player.</summary>
        public NetworkList<RaceEntry> RaceEntries { get; private set; }

        public NetworkVariable<bool> RaceStarted { get; private set; } =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> RaceEnded { get; private set; } =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<ulong> WinnerClientId { get; private set; } =
            new(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<bool> AllPlayersFinished { get; private set; } =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        /// <summary>Server network time when the 30-second countdown ends (0 = not started).</summary>
        public NetworkVariable<double> CountdownEndTime { get; private set; } =
            new(0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // How long after the first finisher before results are forced (in case someone never finishes).
        [SerializeField] private float resultsTimeoutSeconds = 30f;

        private bool  m_timeoutStarted;
        private int   m_finishCounter;   // monotonically increases each time a player finishes or DNFs
        private float m_raceStartTime;   // Time.time when race began (server only)

        // Server-only: clientId → player transform for real-time distance calculations.
        private readonly Dictionary<ulong, Transform> m_playerTransforms = new();

        private void Awake()
        {
            Instance    = this;
            RaceEntries = new NetworkList<RaceEntry>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                AddEntry(clientId);

            NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // ── Real-time position update ─────────────────────────────────────────

        private void Update()
        {
            if (!IsServer || !RaceStarted.Value) return;
            RecalculatePositions();
        }

        // ── Player registration (called by RacePlayerTracker) ─────────────────

        public void RegisterPlayerTransform(ulong clientId, Transform playerTransform)
        {
            m_playerTransforms[clientId] = playerTransform;
            Debug.Log($"[RaceManager] Registered transform for client {clientId}.");
        }

        public void UnregisterPlayerTransform(ulong clientId)
        {
            m_playerTransforms.Remove(clientId);
        }

        // ── Server API (called by RaceCheckpoint) ─────────────────────────────

        /// <summary>Called on the owning client when they cross a checkpoint; relayed to server.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void ReportCheckpointServerRpc(int checkpointIndex, bool isFinishLine, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[RaceManager] ReportCheckpointServerRpc from client {clientId}  index={checkpointIndex}  isFinishLine={isFinishLine}");
            if (isFinishLine)
                PlayerFinished(clientId);
            else
                RegisterCheckpoint(clientId, checkpointIndex);
        }

        public void StartRace()
        {
            if (!IsServer) return;
            m_raceStartTime   = Time.time;
            RaceStarted.Value = true;
            Debug.Log("[RaceManager] Race started.");
        }

        /// <summary>Call when a player crosses a checkpoint (server only).</summary>
        public void RegisterCheckpoint(ulong clientId, int checkpointIndex)
        {
            if (!IsServer) return;

            int idx = FindEntryIndex(clientId);
            if (idx < 0) return;

            var entry = RaceEntries[idx];
            if (checkpointIndex <= entry.CheckpointIndex) return;

            entry.CheckpointIndex = checkpointIndex;
            RaceEntries[idx]      = entry;

            Debug.Log($"[RaceManager] Client {clientId} reached checkpoint {checkpointIndex}.");
        }

        /// <summary>Call when a player crosses the finish line (server only).</summary>
        public void PlayerFinished(ulong clientId)
        {
            if (!IsServer) return;

            int idx = FindEntryIndex(clientId);
            if (idx < 0) return;

            var entry    = RaceEntries[idx];
            if (entry.Finished) return;
            entry.Finished          = true;
            entry.FinishOrder       = ++m_finishCounter;
            entry.FinishTimeSeconds = Time.time - m_raceStartTime;
            RaceEntries[idx]        = entry;

            if (WinnerClientId.Value == ulong.MaxValue)
            {
                WinnerClientId.Value = clientId;
                RaceEnded.Value      = true;
                Debug.Log($"[RaceManager] Client {clientId} won the race!");

                if (!m_timeoutStarted)
                {
                    m_timeoutStarted = true;
                    WaitForAllOrTimeoutAsync().Forget();
                }
            }

            CheckAllFinished();
        }

        private void CheckAllFinished()
        {
            if (AllPlayersFinished.Value) return;
            int done = 0;
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].Finished) done++;
            if (done >= RaceEntries.Count)
            {
                AllPlayersFinished.Value = true;
                Debug.Log("[RaceManager] All players finished.");
            }
        }

        private async UniTaskVoid WaitForAllOrTimeoutAsync()
        {
            CountdownEndTime.Value = NetworkManager.Singleton.ServerTime.Time + resultsTimeoutSeconds;
            Debug.Log($"[RaceManager] Countdown started — results in {resultsTimeoutSeconds}s.");

            await UniTask.Delay(System.TimeSpan.FromSeconds(resultsTimeoutSeconds),
                cancellationToken: destroyCancellationToken);

            if (!AllPlayersFinished.Value)
            {
                // Mark every player who hasn't finished yet as DNF.
                for (int i = 0; i < RaceEntries.Count; i++)
                {
                    var entry = RaceEntries[i];
                    if (!entry.Finished)
                    {
                        entry.Finished    = true;
                        entry.FinishOrder = ++m_finishCounter;
                        RaceEntries[i]    = entry;
                        Debug.Log($"[RaceManager] Client {entry.ClientId} marked DNF after timeout.");
                    }
                }

                AllPlayersFinished.Value = true;
                Debug.Log("[RaceManager] Results timeout — showing results.");
            }
        }

        // ── Position calculation ──────────────────────────────────────────────

        private void RecalculatePositions()
        {
            var scored = new List<(ulong clientId, float score)>();

            for (int i = 0; i < RaceEntries.Count; i++)
            {
                var entry = RaceEntries[i];
                float score = entry.CheckpointIndex * 10000f;

                // Add distance-based sub-score: closer to next checkpoint = higher score.
                if (!entry.Finished
                    && m_playerTransforms.TryGetValue(entry.ClientId, out var t)
                    && t != null
                    && checkpoints != null && checkpoints.Length > 0)
                {
                    int nextIdx = Mathf.Min(entry.CheckpointIndex + 1, checkpoints.Length - 1);
                    var nextCp  = checkpoints[nextIdx];
                    if (nextCp != null)
                    {
                        float dist = Vector3.Distance(t.position, nextCp.transform.position);
                        score += Mathf.Clamp(10000f - dist * 10f, 0f, 9999f);
                    }
                }

                // Finished players are always ahead; earlier finishers score higher.
                if (entry.Finished) score = 100_000_000f - entry.FinishOrder * 1_000f;

                scored.Add((entry.ClientId, score));
            }

            // Sort descending: highest score = furthest ahead.
            scored.Sort((a, b) => b.score.CompareTo(a.score));

            bool anyChanged = false;
            for (int rank = 0; rank < scored.Count; rank++)
            {
                int idx = FindEntryIndex(scored[rank].clientId);
                if (idx < 0) continue;

                var entry = RaceEntries[idx];
                int newPos = rank + 1;
                if (entry.RacePosition == newPos) continue;

                entry.RacePosition = newPos;
                RaceEntries[idx]   = entry;
                anyChanged = true;
            }

            if (anyChanged)
                Debug.Log("[RaceManager] Positions recalculated.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void AddEntry(ulong clientId)
        {
            RaceEntries.Add(new RaceEntry
            {
                ClientId        = clientId,
                CheckpointIndex = 0,
                RacePosition    = RaceEntries.Count + 1,
                Finished        = false
            });
        }

        private int FindEntryIndex(ulong clientId)
        {
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].ClientId == clientId) return i;
            return -1;
        }

        private void OnClientConnected(ulong clientId)    { if (IsServer) AddEntry(clientId); }
        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            int idx = FindEntryIndex(clientId);
            if (idx >= 0)
            {
                var entry = RaceEntries[idx];
                if (!entry.Finished)
                {
                    // Mark as DNF: keep in list so results HUD can show the player.
                    entry.Finished    = true;
                    entry.FinishOrder = ++m_finishCounter;
                    RaceEntries[idx]  = entry;
                    Debug.Log($"[RaceManager] Client {clientId} disconnected — marked DNF (order {entry.FinishOrder}).");
                }
            }

            m_playerTransforms.Remove(clientId);

            if (RaceStarted.Value)
                CheckAllFinished();
        }

        /// <summary>Returns this client's current race position (1-based). 0 if not found.</summary>
        public int GetLocalRacePosition()
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].ClientId == localId)
                    return RaceEntries[i].RacePosition;
            return 0;
        }
    }
}
