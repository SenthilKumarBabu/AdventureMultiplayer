using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using Cysharp.Threading.Tasks;

namespace AdventureMultiplayer
{
    public enum PowerUpType
    {
        SpeedBoost,   // temporary top-speed increase
        Shield,       // absorb one hit
        StunBolt,     // stun the nearest player ahead
        Swap,         // swap position with the race leader
        Magnet        // auto-collect nearby coins
    }

    /// <summary>
    /// Server-authoritative power-up pickup.
    ///
    /// Place as a NetworkObject in the scene (or spawn dynamically).
    /// The server detects which player touches it, despawns it, and broadcasts the
    /// effect to all clients via ClientRpc so every machine applies it consistently.
    ///
    /// Setup:
    ///   - Add a trigger Collider and a NetworkObject to the GameObject.
    ///   - Set the PowerUpType in the Inspector.
    ///   - Register the prefab in NetworkManager's NetworkPrefabs list.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Power Up")]
    public class NetworkedPowerUp : NetworkBehaviour
    {
        [SerializeField] private PowerUpType type;
        [SerializeField] private float       speedBoostMultiplier  = 1.5f;
        [SerializeField] private float       speedBoostDuration    = 3f;
        [SerializeField] private float       magnetDuration        = 5f;
        [SerializeField] private float       stunDuration          = 2f;

        private bool m_collected;

        private void OnTriggerEnter(Collider other)
        {
            // Only the server processes pickups.
            if (!IsServer || m_collected) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;
            if (other.GetComponentInParent<Player>() == null) return;

            m_collected = true;
            ulong collectingClientId = netObj.OwnerClientId;

            ApplyEffectClientRpc(collectingClientId);
            GetComponent<NetworkObject>().Despawn();
        }

        [ClientRpc]
        private void ApplyEffectClientRpc(ulong targetClientId)
        {
            // Find the local player that owns this client.
            var localPlayer = FindLocalPlayer(targetClientId);
            if (localPlayer == null) return;

            switch (type)
            {
                case PowerUpType.SpeedBoost:
                    ApplySpeedBoost(localPlayer).Forget();
                    break;

                case PowerUpType.Shield:
                    ApplyShield(localPlayer);
                    break;

                case PowerUpType.Magnet:
                    ApplyMagnet(localPlayer).Forget();
                    break;

                case PowerUpType.StunBolt:
                case PowerUpType.Swap:
                    // These affect other players — handled server-side in a future pass.
                    Debug.Log($"[NetworkedPowerUp] {type} collected by client {targetClientId}.");
                    break;
            }
        }

        // ── Effect implementations ────────────────────────────────────────────

        private async UniTaskVoid ApplySpeedBoost(Player player)
        {
            var stats = player.stats.current;
            float original = stats.topSpeed;
            stats.topSpeed *= speedBoostMultiplier;
            Debug.Log($"[NetworkedPowerUp] SpeedBoost active ({speedBoostDuration}s).");
            await UniTask.Delay(System.TimeSpan.FromSeconds(speedBoostDuration));
            stats.topSpeed = original;
            Debug.Log("[NetworkedPowerUp] SpeedBoost expired.");
        }

        private void ApplyShield(Player player)
        {
            // Shield flag is read by NetworkedHealth.TakeDamageServerRpc.
            var health = player.GetComponent<NetworkedHealth>();
            if (health != null)
            {
                health.SetShield(true);
                Debug.Log("[NetworkedPowerUp] Shield activated.");
            }
        }

        private async UniTaskVoid ApplyMagnet(Player player)
        {
            // TODO: integrate with collectible system to auto-collect nearby coins.
            Debug.Log($"[NetworkedPowerUp] Magnet active ({magnetDuration}s).");
            await UniTask.Delay(System.TimeSpan.FromSeconds(magnetDuration));
            Debug.Log("[NetworkedPowerUp] Magnet expired.");
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static Player FindLocalPlayer(ulong targetClientId)
        {
            foreach (var obj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
                if (obj.OwnerClientId == targetClientId && obj.IsPlayerObject)
                    return obj.GetComponent<Player>();
            return null;
        }
    }
}
