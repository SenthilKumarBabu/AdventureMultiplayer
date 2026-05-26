using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Attach to the player prefab.
    /// Registers this player's transform with RaceManager on the server so
    /// real-time distance-based position calculations work between checkpoints.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Race Player Tracker")]
    public class RacePlayerTracker : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            // Only the server needs to track transforms for position calculation.
            if (!IsServer) return;

            if (RaceManager.Instance == null)
            {
                Debug.LogWarning("[RacePlayerTracker] RaceManager not found in scene.");
                return;
            }

            RaceManager.Instance.RegisterPlayerTransform(OwnerClientId, transform);
            Debug.Log($"[RacePlayerTracker] Registered client {OwnerClientId} with RaceManager.");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            RaceManager.Instance?.UnregisterPlayerTransform(OwnerClientId);
        }
    }
}
