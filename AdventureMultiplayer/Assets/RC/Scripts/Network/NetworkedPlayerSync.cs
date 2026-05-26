using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Disables the PLAYER TWO Player component on ghost instances so that
    /// PLAYER TWO's FindFirstObjectByType searches never accidentally pick up
    /// a remote player as the "local" player.
    ///
    /// Camera assignment is handled by NetworkCameraTarget.
    /// Respawn is handled by NetworkRespawner.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Player Sync")]
    public class NetworkedPlayerSync : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                // Disable the Player component so PLAYER TWO state machines and
                // singleton lookups don't touch remote ghosts.
                var player = GetComponent<Player>();
                if (player != null) player.enabled = false;
                Debug.Log($"[NetworkedPlayerSync] Ghost spawned (ownerClientId={OwnerClientId}). Player component disabled.");
            }
            else
            {
                Debug.Log($"[NetworkedPlayerSync] Owner spawned (clientId={OwnerClientId}).");
            }
        }
    }
}
