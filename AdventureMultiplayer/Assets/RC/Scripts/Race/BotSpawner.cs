using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-side bot spawner for race scenes.
    ///
    /// Attach to the same GameObject as NetworkGameManager.
    /// Reads the same spawnPoints array that NetworkGameManager uses.
    /// Human players take slots 0…(playerCount-1); bots fill the remaining slots
    /// so no two characters share the exact same spawn position.
    ///
    /// Setup:
    ///   1. Run Tools > Adventure Multiplayer > Setup Bot Prefabs (All Characters) once.
    ///   2. Drag every "&lt;Name&gt;Bot" prefab into Bot Prefabs.
    ///   3. Assign the SAME Spawn Points array that NetworkGameManager uses.
    ///
    /// Bot count is not configured here — it's calculated at spawn time from
    /// spawnPoints.Length minus the number of connected humans, so the room always
    /// ends up full (see OnSceneLoadCompleted).
    ///
    /// Each spawned bot gets a random (non-repeating, until exhausted) character
    /// from Bot Prefabs, so multiple bots don't all look identical.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Bot Spawner")]
    public class BotSpawner : MonoBehaviour
    {
        [Header("Bot Configuration")]
        [Tooltip("One entry per bot-playable character — each must have NetworkObject and RaceBotBrain.")]
        [SerializeField] private GameObject[] botPrefabs;

        [Header("Spawn Points")]
        [Tooltip("The same Transform array used by NetworkGameManager. " +
                 "Bots are placed at index (humanPlayerCount + botIndex) so they " +
                 "never share a position with a human.")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Spawn Safety")]
        [Tooltip("Spawn point markers are hand-placed and can end up slightly embedded in the " +
                 "ground mesh (or sit over a seam/gap in the collider). Each bot is raycast down " +
                 "onto the ground from above its marker and snapped onto the actual surface so it " +
                 "never spawns under/inside geometry.")]
        [SerializeField] private float groundProbeHeight = 5f;
        [SerializeField] private float groundProbeDepth = 15f;
        [SerializeField] private LayerMask groundLayer = Physics.DefaultRaycastLayers;

        /// <summary>Scene-local singleton so RaceBotBrain can reach GetVerifiedSafeGroundPosition().</summary>
        public static BotSpawner Instance { get; private set; }

        private readonly List<NetworkObject> m_spawnedBots = new();
        private int m_humanSlotsTaken;

        private void OnEnable()
        {
            Instance = this;
            if (NetworkManager.Singleton?.SceneManager == null) return;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (NetworkManager.Singleton?.SceneManager == null) return;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }

        /// <summary>
        /// Re-runs the same ground raycast used for initial bot placement, but at an arbitrary
        /// XZ position rather than one of the assigned spawnPoints entries — used by
        /// RaceBotBrain's embed watchdog to re-verify ground near a struggling bot's OWN marker
        /// (nudged a little to one side) when its first recovery attempt didn't hold.
        ///
        /// This used to instead return a FIXED, shared spawnPoints slot (first spawnPoints[0] —
        /// the human's own spot; then, after that was reported spawning bots on top of the
        /// player, spawnPoints[m_humanSlotsTaken] — the first BOT slot). Both versions had the
        /// same underlying flaw: every struggling bot gets relocated to the SAME single point, so
        /// if more than one bot needs this in the same race, they all end up clustered on top of
        /// EACH OTHER at that one shared spot — still visibly "spawning at the wrong place / on
        /// another character," just a different character each time depending on which slot was
        /// hardcoded. Taking the caller's own nudged position instead means every bot always
        /// stays near its OWN assigned marker — never another slot, human or bot.
        /// </summary>
        public Vector3? ResolveGroundedPositionAt(Vector3 xzBasePosition) =>
            ResolveGroundedPosition(xzBasePosition);

        private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (botPrefabs == null || botPrefabs.Length == 0) return;

            // The Lobby's bot toggle (set by the host) overrides the inspector default when
            // present; falls back to the serialized value otherwise (e.g. testing a gameplay
            // scene directly without going through Lobby).
            bool botsEnabled = BotSettings.Instance == null || BotSettings.Instance.BotsEnabled;
            if (!botsEnabled) return;

            // Human players occupy spawn indices 0 … (clientsCompleted.Count - 1).
            m_humanSlotsTaken = clientsCompleted.Count;

            // Bot count is calculated, not configured: bots fill whatever slots the humans
            // left empty so the room is always balanced (e.g. 1 human -> 3 bots, 4 humans ->
            // 0 bots). spawnPoints.Length is the room's total capacity, so this can never
            // wrap a bot onto a human's (or another bot's) slot index.
            int roomSize   = spawnPoints != null ? spawnPoints.Length : 0;
            int spawnCount = Mathf.Max(0, roomSize - m_humanSlotsTaken);
            if (spawnCount <= 0) return;

            Debug.Log($"[BotSpawner] {clientsCompleted.Count} human(s) took slots 0–{m_humanSlotsTaken - 1}. " +
                      $"Spawning {spawnCount} bot(s) to fill the {roomSize}-slot room, from slot {m_humanSlotsTaken}.");
            SpawnBots(spawnCount);
        }

        private void SpawnBots(int count)
        {
            var order = ShuffledPrefabOrder(count);

            for (int i = 0; i < count; i++)
            {
                var prefab = botPrefabs[order[i]];
                if (prefab == null) continue;

                var pos = GetSpawnPos(m_humanSlotsTaken + i);
                var go  = Instantiate(prefab, pos, Quaternion.identity);
                var net = go.GetComponent<NetworkObject>();

                if (net == null)
                {
                    Debug.LogError($"[BotSpawner] Bot prefab '{prefab.name}' is missing NetworkObject — " +
                                   "run 'Tools > Adventure Multiplayer > Setup Bot Prefabs (All Characters)' first.");
                    Destroy(go);
                    continue;
                }

                var bot = go.GetComponent<RaceBotBrain>();

                // Must happen BEFORE Spawn — OnNetworkSpawn reads the difficulty field
                // immediately (via ApplyDifficulty) to set up speed/weave/braking/reaction-time,
                // so setting it after spawning would be too late to have any effect.
                if (bot != null && BotSettings.Instance != null)
                    bot.SetDifficulty(BotSettings.Instance.Difficulty);

                net.Spawn(true);
                m_spawnedBots.Add(net);

                // botPrefabs is wired in the same [Gale/Lily, Blaze, Bolt, Bruno, Spike] order as
                // NetworkGameManager.characterPrefabs (see WireBotSpawnerPrefabs.cs), so order[i] IS
                // the right character index. Without this, CharacterPicker.GetSelection(botId) falls
                // back to 0 for every bot — the results screen would show every bot as "Gale".
                if (bot != null)
                    CharacterPicker.Instance?.RegisterBotSelection(bot.BotId, order[i]);

                Debug.Log($"[BotSpawner] Bot {i} ('{prefab.name}') spawned at {pos} (slot {m_humanSlotsTaken + i}, " +
                          $"NetworkObjectId={net.NetworkObjectId}, charIndex={order[i]}).");
            }
        }

        // Fisher-Yates shuffle of botPrefabs indices, repeated/cycled if count > botPrefabs.Length,
        // so bots get distinct characters before any character repeats.
        private List<int> ShuffledPrefabOrder(int count)
        {
            var order = new List<int>(count);
            while (order.Count < count)
            {
                var batch = new List<int>(botPrefabs.Length);
                for (int i = 0; i < botPrefabs.Length; i++) batch.Add(i);
                for (int i = batch.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (batch[i], batch[j]) = (batch[j], batch[i]);
                }
                order.AddRange(batch);
            }
            order.RemoveRange(count, order.Count - count);
            return order;
        }

        private Vector3 GetSpawnPos(int slotIndex)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[BotSpawner] No spawn points assigned — cannot safely place a bot. " +
                                "Assign the same Spawn Points array used by NetworkGameManager.");
                return Vector3.zero;
            }

            var t = spawnPoints[slotIndex % spawnPoints.Length];
            if (t == null)
            {
                Debug.LogError($"[BotSpawner] spawnPoints[{slotIndex % spawnPoints.Length}] is null — cannot safely place a bot.");
                return Vector3.zero;
            }

            // Each slot already has its own dedicated marker (spaced apart by design in
            // the scene) — place the bot there as-is, only correcting for ground height.
            return ResolveGroundedPosition(t.position);
        }

        // Spawn markers are hand-placed and can end up slightly embedded in the ground
        // mesh (or sit over a seam/gap in the collider) — raycasting down from above the
        // marker and snapping onto the actual surface avoids bots spawning under/inside
        // geometry when the raw marker position is off.
        private Vector3 ResolveGroundedPosition(Vector3 markerPos)
        {
            Vector3 rayOrigin = markerPos + Vector3.up * groundProbeHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    groundProbeHeight + groundProbeDepth, groundLayer, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.1f;
            }

            Debug.LogWarning($"[BotSpawner] No ground found under spawn marker at {markerPos} " +
                              "(probe range too short, or marker is off the track) — using marker position as-is.");
            return markerPos;
        }

        // ── Editor gizmos ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        [Tooltip("Editor-only preview of how many humans are expected (actual count comes from " +
                 "connected clients at runtime). Matches real slot assignment: humans take the " +
                 "low indices, bots fill the rest — opposite of how this used to be drawn.")]
        [SerializeField] private int gizmoPreviewHumanCount = 1;

        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null) continue;
                var p = spawnPoints[i].position;
                bool isHumanSlot = i < gizmoPreviewHumanCount;
                // Human slots are dim; bot slots (the rest) are cyan.
                Gizmos.color = isHumanSlot
                    ? new Color(0.4f, 0.4f, 0.4f, 0.4f)
                    : new Color(0f, 0.8f, 1f, 0.9f);
                Gizmos.DrawWireSphere(p, 0.5f);
                UnityEditor.Handles.Label(p + Vector3.up * 0.9f, isHumanSlot ? $"Human {i}" : $"Bot {i}");
            }
        }
#endif
    }
}
