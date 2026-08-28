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

        /// <summary>Ordered checkpoint array — read by RaceBotBrain for navigation.</summary>
        public RaceCheckpoint[] Checkpoints => checkpoints;

        /// <summary>Read-only on clients — one entry per connected player.</summary>
        public NetworkList<RaceEntry> RaceEntries { get; private set; }

        /// <summary>
        /// One record per player who has crossed the finish line (or been marked DNF).
        /// Uses Add — never [i]=value — so clients see records immediately with no NGO
        /// NetworkList staging delay. Use this for authoritative finish status display.
        /// </summary>
        public NetworkList<FinishRecord> FinishRecords { get; private set; }

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
        private bool  m_checkpointDebugLogged;
        private float m_nextScoreLogTime;

        // Server-only: clientId → player transform (registration/cleanup) and
        // clientId → world position (updated by owner-client RPC every 100 ms).
        private readonly Dictionary<ulong, Transform> m_playerTransforms = new();
        private readonly Dictionary<ulong, Vector3>   m_playerPositions  = new();

        // Server-only: tracks finished clients without going through NGO NetworkList.
        // NGO NetworkList[i]=value writes are staged and NOT immediately visible in the same
        // frame — reading RaceEntries[i].Finished right after writing always returns the old
        // value. These collections are updated synchronously so CheckAllFinished and the HUD
        // can rely on them.
        private readonly HashSet<ulong>           m_serverFinishedClients  = new();
        private readonly Dictionary<ulong, float> m_serverFinishTimes      = new();
        private readonly Dictionary<ulong, int>   m_serverFinishOrders     = new();
        // Same staging delay affects CheckpointIndex and RacePosition — keep authoritative server-side copies.
        private readonly Dictionary<ulong, int>   m_serverCheckpointIndex  = new();
        private readonly Dictionary<ulong, int>   m_serverRacePositions    = new();
        // Debounce: minimum seconds between position changes to prevent HUD flip-flop when
        // players are neck-and-neck and the client's 50 ms position RPC causes score oscillation.
        private readonly Dictionary<ulong, float> m_lastPositionChangeTime = new();
        private const float kPositionChangeCooldown = 0.3f;

        private void Awake()
        {
            Instance      = this;
            RaceEntries   = new NetworkList<RaceEntry>();
            FinishRecords = new NetworkList<FinishRecord>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            var ids = new List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
            Debug.Log($"[RaceManager] OnNetworkSpawn — ConnectedClientsIds count={ids.Count}: [{string.Join(",", ids)}]");

            foreach (var clientId in ids)
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
            m_playerPositions[clientId]  = playerTransform.position; // seed with spawn position
            Debug.Log($"[RaceManager] Registered transform for client {clientId}.");
        }

        /// <summary>Called by RacePlayerTracker's owner-side RPC every 100 ms.</summary>
        public void UpdatePlayerPosition(ulong clientId, Vector3 position)
        {
            m_playerPositions[clientId] = position;
        }

        public void UnregisterPlayerTransform(ulong clientId)
        {
            m_playerTransforms.Remove(clientId);
            m_playerPositions.Remove(clientId);
        }

        /// <summary>
        /// The registered Transform for a race participant ID (an NGO client ID for a
        /// human player, or a RaceBotBrain.BotId for a bot). Null if not registered.
        /// Lets power-up targeting (PlayerPowerUpInventory) resolve a race-position-based
        /// target (e.g. "player ahead") to an actual player/bot instance without needing
        /// its own separate, bot-incompatible ID scheme.
        /// </summary>
        public Transform GetPlayerTransform(ulong id) =>
            m_playerTransforms.TryGetValue(id, out var t) ? t : null;

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

            // Use the authoritative server-side dictionary (not the staged NetworkList value)
            // so we never accept a lower-index checkpoint after a higher one was recorded.
            int current = m_serverCheckpointIndex.TryGetValue(clientId, out int ci) ? ci : -1;
            if (checkpointIndex <= current) return;

            m_serverCheckpointIndex[clientId] = checkpointIndex; // update immediately

            var entry = RaceEntries[idx];
            entry.CheckpointIndex = checkpointIndex;
            RaceEntries[idx]      = entry;

            Debug.Log($"[RaceManager] Client {clientId} reached checkpoint {checkpointIndex}.");
        }

        /// <summary>Call when a player crosses the finish line (server only).</summary>
        public void PlayerFinished(ulong clientId)
        {
            if (!IsServer) return;

            Debug.Log($"[RaceManager] PlayerFinished({clientId}) — total entries={RaceEntries.Count}  m_raceStartTime={m_raceStartTime}  Time.time={Time.time}");

            int idx = FindEntryIndex(clientId);
            if (idx < 0)
            {
                Debug.LogWarning($"[RaceManager] PlayerFinished({clientId}) — no entry found! Entries: {DumpEntries()}");
                return;
            }

            var entry    = RaceEntries[idx];
            if (entry.Finished)
            {
                Debug.Log($"[RaceManager] PlayerFinished({clientId}) — already finished, skipped.");
                return;
            }
            entry.Finished          = true;
            entry.FinishOrder       = ++m_finishCounter;
            entry.FinishTimeSeconds = Time.time - m_raceStartTime;
            RaceEntries[idx]        = entry;
            m_serverFinishedClients.Add(clientId);
            m_serverFinishTimes[clientId]  = entry.FinishTimeSeconds;
            m_serverFinishOrders[clientId] = entry.FinishOrder;
            // Add to FinishRecords using Add (not [i]=value) — no NGO staging delay,
            // clients see this immediately when the network delivers it.
            FinishRecords.Add(new FinishRecord
            {
                ClientId          = clientId,
                FinishTimeSeconds = entry.FinishTimeSeconds,
                FinishOrder       = entry.FinishOrder
            });
            Debug.Log($"[RaceManager] PlayerFinished({clientId}) — set Finished=true  FinishTimeSeconds={entry.FinishTimeSeconds:F2}  entry idx={idx}");

            // NGO NetworkList writes are staged — the backing store is not updated immediately.
            // Write the correct state again after a few frames so clients receive the real data
            // regardless of whether RecalculatePositions runs (requires RaceStarted=true).
            HealEntryAsync(idx, clientId).Forget();

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
            // Read unique client IDs from the list (ClientId is set at Add time and never changes).
            // Use m_serverFinishedClients for the done-check — NOT RaceEntries[i].Finished —
            // because NGO NetworkList [i]=value writes are staged and return the old value
            // when read back in the same frame.
            var allClients = new HashSet<ulong>();
            for (int i = 0; i < RaceEntries.Count; i++)
                allClients.Add(RaceEntries[i].ClientId);

            Debug.Log($"[RaceManager] CheckAllFinished — uniqueClients={allClients.Count}  serverFinished={m_serverFinishedClients.Count}  detail={DumpEntries()}");

            if (allClients.Count > 0 && allClients.IsSubsetOf(m_serverFinishedClients))
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
                // Clients who actually finished — use the server-side HashSet (not the NetworkList
                // which may still show stale Finished=false due to NGO staging).
                var reallyFinished = new HashSet<ulong>(m_serverFinishedClients);

                for (int i = 0; i < RaceEntries.Count; i++)
                {
                    var entry = RaceEntries[i];
                    if (!entry.Finished && !reallyFinished.Contains(entry.ClientId))
                    {
                        entry.Finished    = true;
                        entry.FinishOrder = ++m_finishCounter;
                        RaceEntries[i]    = entry;
                        m_serverFinishedClients.Add(entry.ClientId);
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
            // One-time: log all checkpoint positions so we can verify Inspector order.
            if (!m_checkpointDebugLogged)
            {
                m_checkpointDebugLogged = true;
                if (checkpoints == null || checkpoints.Length == 0)
                    Debug.LogWarning("[RaceManager] checkpoints array is EMPTY — distance scoring disabled. Assign checkpoints in Inspector.");
                else
                    for (int ci = 0; ci < checkpoints.Length; ci++)
                        Debug.Log($"[RaceManager] checkpoints[{ci}] pos={( checkpoints[ci] != null ? checkpoints[ci].transform.position.ToString("F1") : "null")}");
            }

            var scored = new List<(ulong clientId, float score)>();

            for (int i = 0; i < RaceEntries.Count; i++)
            {
                var entry = RaceEntries[i];
                // Use server-side state — NetworkList may still show stale Finished=false.
                bool finished  = entry.Finished || m_serverFinishedClients.Contains(entry.ClientId);
                int  finOrder  = m_serverFinishOrders.TryGetValue(entry.ClientId, out int fo)
                                 ? fo : entry.FinishOrder;

                // Read checkpoint progress from the authoritative server-side dictionary —
                // NOT entry.CheckpointIndex, which reflects the staged NetworkList value and
                // may lag one frame behind the actual update (same staging bug as Finished).
                int cpIdx = m_serverCheckpointIndex.TryGetValue(entry.ClientId, out int sIdx)
                            ? sIdx : entry.CheckpointIndex;

                // cpIdx starts at -1 (no checkpoint crossed yet).
                // nextIdx = cpIdx + 1 → -1+1=0, so fresh players target checkpoints[0].
                // Do NOT clamp to checkpoints.Length-1: once past the last checkpoint give
                // a fixed maximum bonus so advancing players are never penalised.
                float score = cpIdx * 10000f;

                // Use owner-reported position (updated via RPC every 100 ms) so the server
                // always has the client's real world position, not a stale transform copy.
                if (!finished
                    && m_playerPositions.TryGetValue(entry.ClientId, out var pos)
                    && checkpoints != null && checkpoints.Length > 0)
                {
                    int nextIdx = cpIdx + 1; // -1+1=0 for fresh players
                    if (nextIdx < 0) nextIdx = 0; // safety floor

                    if (nextIdx < checkpoints.Length)
                    {
                        var nextCp = checkpoints[nextIdx];
                        if (nextCp != null)
                        {
                            float dist = Vector3.Distance(pos, nextCp.transform.position);
                            score += Mathf.Clamp(10000f - dist * 10f, 0f, 9999f);
                        }
                    }
                    else
                    {
                        // Past the last tracked checkpoint — award max distance bonus so
                        // these players always outrank anyone still approaching that checkpoint.
                        score += 9999f;
                    }
                }

                if (finished) score = 100_000_000f - finOrder * 1_000f;

                scored.Add((entry.ClientId, score));
            }

            // Throttled diagnostic log — remove once positions are confirmed correct.
            if (Time.time >= m_nextScoreLogTime)
            {
                m_nextScoreLogTime = Time.time + 2f;
                var sb = new System.Text.StringBuilder("[RaceManager] Scores: ");
                foreach (var (cid, sc) in scored)
                {
                    bool hasPos = m_playerPositions.TryGetValue(cid, out var dbgPos);
                    int  cpI    = m_serverCheckpointIndex.TryGetValue(cid, out int ci2) ? ci2 : -99;
                    sb.Append($"C{cid}=score:{sc:F0} cp:{cpI} pos:{(hasPos ? dbgPos.ToString("F0") : "NO_POS")}  ");
                }
                Debug.Log(sb.ToString());

                // Chasing detection: log when two adjacent players are within 2000 score of each other.
                for (int si = 0; si + 1 < scored.Count; si++)
                {
                    float gap = scored[si].score - scored[si + 1].score;
                    if (gap < 2000f && gap >= 0f)
                        Debug.Log($"[RaceManager] CHASING: C{scored[si + 1].clientId} is chasing C{scored[si].clientId} — score gap={gap:F0} (~{gap / 10f:F0} units apart)");
                }
            }

            scored.Sort((a, b) => b.score.CompareTo(a.score));

            bool anyChanged = false;
            for (int rank = 0; rank < scored.Count; rank++)
            {
                int idx = FindEntryIndex(scored[rank].clientId);
                if (idx < 0) continue;

                var entry  = RaceEntries[idx];
                int newPos = rank + 1;

                // Heal stale finish state — the NetworkList backing store may still show
                // Finished=false due to NGO staging. Without this write, clients receive
                // Finished=false and show the finished player as DNF.
                bool needsHeal = !entry.Finished && m_serverFinishedClients.Contains(entry.ClientId);
                if (needsHeal)
                {
                    entry.Finished = true;
                    if (m_serverFinishTimes.TryGetValue(entry.ClientId, out float ft))
                        entry.FinishTimeSeconds = ft;
                    if (m_serverFinishOrders.TryGetValue(entry.ClientId, out int fo2))
                        entry.FinishOrder = fo2;
                }

                // Compare against the authoritative server-side dictionary — NOT entry.RacePosition
                // from the NetworkList, which has the same staging delay as CheckpointIndex and
                // Finished. Using the stale NetworkList value causes phantom writes every frame.
                int prevServerPos = m_serverRacePositions.TryGetValue(entry.ClientId, out int sp) ? sp : 0;
                bool posChanged   = prevServerPos != newPos;
                if (posChanged)
                {
                    // Debounce: ignore the change if one already fired within the cooldown window.
                    // This prevents HUD flip-flop when players are neck-and-neck and the 50 ms
                    // client position-RPC causes one-frame score oscillations.
                    float lastChange = m_lastPositionChangeTime.TryGetValue(entry.ClientId, out float lct) ? lct : -10f;
                    if (Time.time - lastChange < kPositionChangeCooldown)
                    {
                        posChanged = false; // within cooldown — skip this change
                    }
                    else
                    {
                        m_serverRacePositions[entry.ClientId] = newPos;
                        m_lastPositionChangeTime[entry.ClientId] = Time.time;
                        Debug.Log($"[RaceManager] POSITION CHANGE: C{entry.ClientId} moved from {prevServerPos} → {newPos}");
                    }
                }

                if (!needsHeal && !posChanged) continue;

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
            int existing = FindEntryIndex(clientId);
            if (existing >= 0)
            {
                Debug.Log($"[RaceManager] AddEntry({clientId}) SKIPPED — already exists at index {existing}. Total entries={RaceEntries.Count}");
                return;
            }
            RaceEntries.Add(new RaceEntry
            {
                ClientId        = clientId,
                CheckpointIndex = -1, // -1 = no checkpoint crossed; 0 = crossed checkpoint 0
                RacePosition    = RaceEntries.Count + 1,
                Finished        = false
            });
            m_serverCheckpointIndex[clientId] = -1;
            m_serverRacePositions[clientId]   = RaceEntries.Count; // matches initial RacePosition
            Debug.Log($"[RaceManager] AddEntry({clientId}) ADDED. Total entries={RaceEntries.Count}");
        }

        /// <summary>
        /// Called by RaceBotBrain on the server to register an AI bot in the race.
        /// Bot IDs start at 10000 and are unique per bot instance.
        /// </summary>
        public void AddBotEntry(ulong botId)
        {
            if (!IsServer) return;
            AddEntry(botId);
        }

        private async UniTaskVoid HealEntryAsync(int originalIdx, ulong clientId)
        {
            // Wait a few frames so the initial staging write has a chance to commit.
            // Then re-write with the authoritative state so clients definitely receive it.
            await UniTask.DelayFrame(5, cancellationToken: destroyCancellationToken);

            int idx = FindEntryIndex(clientId);
            if (idx < 0) return;

            var entry = RaceEntries[idx];
            if (entry.Finished && entry.FinishTimeSeconds > 0f) return; // already committed correctly

            entry.Finished = true;
            if (m_serverFinishTimes.TryGetValue(clientId, out float ft))  entry.FinishTimeSeconds = ft;
            if (m_serverFinishOrders.TryGetValue(clientId, out int fo))   entry.FinishOrder = fo;
            RaceEntries[idx] = entry;
            Debug.Log($"[RaceManager] HealEntry({clientId}) — re-wrote Finished=true time={entry.FinishTimeSeconds:F2} to push correct state to clients.");
        }

        private string DumpEntries()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < RaceEntries.Count; i++)
            {
                var e = RaceEntries[i];
                sb.Append($"[{i}]client={e.ClientId} fin={e.Finished} time={e.FinishTimeSeconds:F2} pos={e.RacePosition} ");
            }
            return sb.Length > 0 ? sb.ToString() : "(empty)";
        }

        private int FindEntryIndex(ulong clientId)
        {
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].ClientId == clientId) return i;
            return -1;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            Debug.Log($"[RaceManager] OnClientConnected({clientId}) — entries before={RaceEntries.Count}");
            AddEntry(clientId);
        }
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
                    m_serverFinishedClients.Add(clientId);
                    Debug.Log($"[RaceManager] Client {clientId} disconnected — marked DNF (order {entry.FinishOrder}).");
                }
            }

            m_playerTransforms.Remove(clientId);
            m_playerPositions.Remove(clientId);
            m_serverCheckpointIndex.Remove(clientId);
            m_serverRacePositions.Remove(clientId);
            m_lastPositionChangeTime.Remove(clientId);

            if (RaceStarted.Value)
                CheckAllFinished();
        }

        /// <summary>Returns this client's current race position (1-based). 0 if not found.</summary>
        /// <summary>
        /// Returns true if the client finished with a real time (not DNF).
        /// Reads from the server-side HashSet rather than the NGO NetworkList
        /// so the result is accurate in the same frame the finish was recorded.
        /// </summary>
        public bool TryGetServerFinishTime(ulong clientId, out float seconds)
            => m_serverFinishTimes.TryGetValue(clientId, out seconds);

        public int GetLocalRacePosition()
        {
            if (NetworkManager.Singleton == null) return 0;
            ulong localId = NetworkManager.Singleton.LocalClientId;

            // On the server the NetworkList has a staging delay — reading RaceEntries[i].RacePosition
            // back in the same frame we wrote it returns the old value, so the host HUD would
            // always show a stale position. Use the authoritative server-side dictionary instead.
            if (IsServer && m_serverRacePositions.TryGetValue(localId, out int serverPos))
                return serverPos;

            // Clients receive NetworkList deltas from the server with no staging issue.
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].ClientId == localId)
                    return RaceEntries[i].RacePosition;
            return 0;
        }

        // ── Power-up targeting helpers (server-only) ──────────────────────────

        /// <summary>Race position (1-based) of a client. Returns int.MaxValue if not found.</summary>
        public int GetRacePosition(ulong clientId)
        {
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].ClientId == clientId)
                    return RaceEntries[i].RacePosition;
            return int.MaxValue;
        }

        // A finished racer (human or bot) gets a huge fixed score bonus (see RecalculatePositions)
        // so it always ranks above everyone still actually racing — including a bot standing idle
        // at the finish line. GetPlayerInFirst/Ahead/Behind/NAhead used to work directly off that
        // absolute RacePosition, so as soon as ANYONE finished, every power-up that targets "the
        // leader" or "the racer ahead/behind" (Rocket, Swap, StunBolt) could resolve to that
        // finished, no-longer-competing racer instead of an actual rival — confirmed as the cause
        // of Swap targeting a bot that had already finished and was just waiting at the line.
        // GetPlayersBehind already excluded Finished entries; these didn't. Routing all of them
        // through this same "racing only" list fixes every targeting helper at once.
        private List<(ulong id, int pos)> GetRacingOnly()
        {
            var list = new List<(ulong id, int pos)>();
            for (int i = 0; i < RaceEntries.Count; i++)
                if (!RaceEntries[i].Finished)
                    list.Add((RaceEntries[i].ClientId, RaceEntries[i].RacePosition));
            list.Sort((a, b) => a.pos.CompareTo(b.pos));
            return list;
        }

        /// <summary>Client in 1st place AMONG THOSE STILL RACING. Returns ulong.MaxValue if nobody is still racing.</summary>
        public ulong GetPlayerInFirst()
        {
            var racing = GetRacingOnly();
            return racing.Count > 0 ? racing[0].id : ulong.MaxValue;
        }

        /// <summary>Still-racing client directly ahead. Returns ulong.MaxValue if caster is already
        /// 1st among racers, has finished, or isn't racing.</summary>
        public ulong GetPlayerAhead(ulong casterId)
        {
            var racing = GetRacingOnly();
            int idx = racing.FindIndex(r => r.id == casterId);
            if (idx <= 0) return ulong.MaxValue;
            return racing[idx - 1].id;
        }

        /// <summary>Still-racing client N positions ahead of caster. Clamps to 1st place among racers.</summary>
        public ulong GetPlayerNAhead(ulong casterId, int n)
        {
            var racing = GetRacingOnly();
            int idx = racing.FindIndex(r => r.id == casterId);
            if (idx < 0) return ulong.MaxValue;
            int target = Mathf.Max(0, idx - n);
            if (target == idx) return ulong.MaxValue;
            return racing[target].id;
        }

        /// <summary>
        /// Exchanges the CheckpointIndex of two players so race position recalculates
        /// correctly after a Swap teleport. Must be called on the server.
        /// </summary>
        public void SwapCheckpoints(ulong clientA, ulong clientB)
        {
            if (!IsServer) return;
            int idxA = FindEntryIndex(clientA);
            int idxB = FindEntryIndex(clientB);
            if (idxA < 0 || idxB < 0) return;

            var entryA = RaceEntries[idxA];
            var entryB = RaceEntries[idxB];

            int tmp = entryA.CheckpointIndex;
            entryA.CheckpointIndex = entryB.CheckpointIndex;
            entryB.CheckpointIndex = tmp;

            RaceEntries[idxA] = entryA;
            RaceEntries[idxB] = entryB;

            // Mirror the swap in the authoritative server-side dictionary.
            int tmpIdx = m_serverCheckpointIndex.TryGetValue(clientA, out int ia) ? ia : entryA.CheckpointIndex;
            int tmpIdxB = m_serverCheckpointIndex.TryGetValue(clientB, out int ib) ? ib : entryB.CheckpointIndex;
            m_serverCheckpointIndex[clientA] = tmpIdxB;
            m_serverCheckpointIndex[clientB] = tmpIdx;

            // Whichever of the two just got rewound to a LOWER checkpoint index needs to
            // physically re-cross the checkpoints between their new and old index to advance
            // again — but each RaceCheckpoint's own m_passedOwners dedup guard already marked
            // them as having passed those from their original, forward crossing, so re-entering
            // the same trigger would otherwise be silently swallowed and their tracked position
            // would never recover. Un-dedupe both directions unconditionally rather than
            // figuring out which one actually needs it — harmless no-op for whichever side
            // doesn't (RegisterCheckpoint's own monotonic guard still no-ops a redundant re-fire).
            ClearPassedBeyond(clientA, entryA.CheckpointIndex);
            ClearPassedBeyond(clientB, entryB.CheckpointIndex);

            Debug.Log($"[RaceManager] SwapCheckpoints: client {clientA} ↔ {clientB}.");
        }

        // See ClearPassed's doc comment on RaceCheckpoint for why this exists.
        private void ClearPassedBeyond(ulong clientId, int keepUpToIndex)
        {
            if (checkpoints == null) return;
            foreach (var cp in checkpoints)
                if (cp != null && cp.index > keepUpToIndex)
                    cp.ClearPassed(clientId);
        }

        /// <summary>Still-racing client directly behind. Returns ulong.MaxValue if caster is last
        /// among racers, has finished, or isn't racing.</summary>
        public ulong GetPlayerBehind(ulong casterId)
        {
            var racing = GetRacingOnly();
            int idx = racing.FindIndex(r => r.id == casterId);
            if (idx < 0 || idx >= racing.Count - 1) return ulong.MaxValue;
            return racing[idx + 1].id;
        }

        /// <summary>All clients with a higher position number (further behind) than the caster.</summary>
        public List<ulong> GetPlayersBehind(ulong casterId)
        {
            int myPos  = GetRacePosition(casterId);
            var result = new List<ulong>();
            for (int i = 0; i < RaceEntries.Count; i++)
                if (RaceEntries[i].RacePosition > myPos && !RaceEntries[i].Finished)
                    result.Add(RaceEntries[i].ClientId);
            return result;
        }

    }
}
