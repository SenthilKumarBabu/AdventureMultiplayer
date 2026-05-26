using Cysharp.Threading.Tasks;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Owner-only respawn handler for multiplayer.
    ///
    /// Replaces the PLAYER TWO LevelRespawner singleton so that each player
    /// manages their own respawn independently.
    ///
    /// Flow:
    ///   1. On owner spawn, stores the current position as the default respawn point.
    ///   2. NetworkCheckpoint calls SetRespawnPoint() whenever a checkpoint is touched.
    ///   3. When the PLAYER TWO Player dies (playerEvents.OnDie), waits for
    ///      <see cref="respawnDelay"/> seconds then teleports the player back and
    ///      calls player.Respawn() to reset health and state.
    ///
    /// Add this to the HumanPlayer prefab alongside NetworkedPlayerSync.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Network Respawner")]
    public class NetworkRespawner : NetworkBehaviour
    {
        [SerializeField] private float respawnDelay = 2f;

        private Vector3    m_respawnPoint;
        private Player     m_player;
        private bool       m_respawning;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            m_player = GetComponent<Player>();
            m_respawnPoint = transform.position;

            if (m_player != null)
                m_player.playerEvents.OnDie.AddListener(OnPlayerDied);

            Debug.Log($"[NetworkRespawner] Initialised. Default respawn at {m_respawnPoint}");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            if (m_player != null)
                m_player.playerEvents.OnDie.RemoveListener(OnPlayerDied);
        }

        /// <summary>Called by NetworkCheckpoint when this owner touches a checkpoint.</summary>
        public void SetRespawnPoint(Vector3 position)
        {
            m_respawnPoint = position;
            Debug.Log($"[NetworkRespawner] Respawn point updated to {position}");
        }

        private void OnPlayerDied()
        {
            if (!m_respawning)
                RespawnAfterDelay().Forget();
        }

        private async UniTaskVoid RespawnAfterDelay()
        {
            m_respawning = true;
            Debug.Log($"[NetworkRespawner] Player died. Respawning in {respawnDelay}s at {m_respawnPoint}");

            await UniTask.WaitForSeconds(respawnDelay,
                cancellationToken: this.GetCancellationTokenOnDestroy());

            if (m_player != null)
            {
                m_player.SetRespawn(m_respawnPoint, Quaternion.identity);
                m_player.Respawn();
                Debug.Log($"[NetworkRespawner] Respawned at {m_respawnPoint}");
            }

            m_respawning = false;
        }
    }
}
