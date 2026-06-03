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

            // PlayerInputManager.Start() calls Camera.main which can be null if the player
            // spawns before the scene camera initialises on the client. Inject it directly.
            var inputManager = GetComponent<PlayerInputManager>();
            if (inputManager != null)
            {
                var field = typeof(PlayerInputManager).GetField("m_camera",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null && field.GetValue(inputManager) == null)
                {
                    field.SetValue(inputManager, cam.GetComponent<Camera>());
                    Debug.Log($"[NetworkCameraTarget] Injected m_camera into PlayerInputManager for '{player.name}'");
                }
            }
        }
    }
}
