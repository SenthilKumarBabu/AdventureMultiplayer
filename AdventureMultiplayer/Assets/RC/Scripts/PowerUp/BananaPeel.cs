using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-spawned trap. Player who placed it is immune.
    /// Any other player that walks through the trigger collider gets the SlipEffect applied
    /// on their owner client via PlayerPowerUpInventory.ApplySlipClientRpc.
    /// Invincibility blocks the slip.
    ///
    /// Requires: NetworkObject, trigger Collider, MeshRenderer.
    /// Register this prefab in NetworkManager's NetworkPrefabs list.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Rush Champions/Banana Peel")]
    public class BananaPeel : NetworkBehaviour
    {
        [SerializeField] private float lifetime = 15f;

        private ulong _ownerClientId;
        private float _slipDuration;
        private bool  _triggered;

        /// <summary>Called by PlayerPowerUpInventory right after Spawn().</summary>
        public void Init(ulong ownerClientId, float slipDuration)
        {
            _ownerClientId = ownerClientId;
            _slipDuration  = slipDuration;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                DespawnAfterLifetimeAsync().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid DespawnAfterLifetimeAsync()
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(
                System.TimeSpan.FromSeconds(lifetime),
                cancellationToken: destroyCancellationToken);

            if (IsSpawned)
                GetComponent<NetworkObject>().Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || _triggered) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || other.GetComponentInParent<Player>() == null) return;

            ulong hitterId = netObj.OwnerClientId;

            // Immune to own peel
            if (hitterId == _ownerClientId) return;

            // Invincibility blocks slip
            var health = netObj.GetComponent<NetworkedHealth>();
            if (health != null && health.IsInvincible)
            {
                Debug.Log($"[BananaPeel] Slip blocked by invincibility on client {hitterId}.");
                return;
            }

            _triggered = true;
            Debug.Log($"[BananaPeel] Client {hitterId} slipped! ({_slipDuration}s)");

            if (PlayerPowerUpInventory.All.TryGetValue(hitterId, out var inv))
            {
                ClientRpcParams p = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { hitterId } }
                };
                inv.ApplySlipClientRpc(_slipDuration, p);
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
