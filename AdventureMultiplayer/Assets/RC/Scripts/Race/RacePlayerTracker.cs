using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Attach to the player prefab.
    /// Each owner-client sends their world position to the server at a fixed rate so
    /// the RaceManager always has accurate positions even when NetworkTransform is not
    /// configured with Owner authority.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Race Player Tracker")]
    public class RacePlayerTracker : NetworkBehaviour
    {
        [SerializeField] private float positionReportInterval = 0.05f; // 20 reports/second

        private float m_nextReportTime;

        public override void OnNetworkSpawn()
        {
            // AI bots register themselves via RaceBotBrain — skip this component on bot prefabs
            // so multiple server-owned bots (all OwnerClientId=0) don't overwrite each other.
            if (GetComponent<RaceBotBrain>() != null)
            {
                enabled = false;
                return;
            }

            if (!IsServer) return;

            if (RaceManager.Instance == null)
            {
                Debug.LogWarning("[RacePlayerTracker] RaceManager not found in scene.");
                return;
            }

            RaceManager.Instance.RegisterPlayerTransform(OwnerClientId, transform);
            Debug.Log($"[RacePlayerTracker] Registered client {OwnerClientId} with RaceManager.");
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (Time.time < m_nextReportTime) return;
            m_nextReportTime = Time.time + positionReportInterval;

            if (IsServer)
            {
                // Host: update directly without going through the network.
                RaceManager.Instance?.UpdatePlayerPosition(OwnerClientId, transform.position);
            }
            else
            {
                ReportPositionServerRpc(transform.position);
            }
        }

        [ServerRpc(RequireOwnership = true)]
        private void ReportPositionServerRpc(Vector3 position)
        {
            RaceManager.Instance?.UpdatePlayerPosition(OwnerClientId, position);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            RaceManager.Instance?.UnregisterPlayerTransform(OwnerClientId);
        }
    }
}
