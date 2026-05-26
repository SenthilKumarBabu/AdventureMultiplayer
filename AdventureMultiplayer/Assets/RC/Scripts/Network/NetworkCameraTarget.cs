using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// On owner spawn, finds the scene's NetworkCameraFollow and tells it
    /// to follow this player's Transform.
    ///
    /// Add to the HumanPlayer prefab alongside NetworkedPlayerSync.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Network Camera Target")]
    public class NetworkCameraTarget : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            var player = GetComponent<Player>();
            if (player == null)
            {
                Debug.LogWarning("[NetworkCameraTarget] No Player component found.");
                return;
            }

            var cam = FindFirstObjectByType<NetworkCameraFollow>();
            if (cam == null)
            {
                Debug.LogWarning("[NetworkCameraTarget] No NetworkCameraFollow found in scene. Add it to the Main Camera.");
                return;
            }

            cam.SetTarget(player.transform);
            Debug.Log($"[NetworkCameraTarget] Camera assigned to '{player.name}' (clientId={OwnerClientId})");
        }
    }
}
