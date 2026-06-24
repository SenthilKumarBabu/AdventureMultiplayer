using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-spawned trap. Player who placed it is immune.
    /// Any other player that walks into the trigger gets the StunEffect applied on their owner client.
    /// DecoyBox intentionally bypasses both Shield and Invincibility (active counter-play item).
    ///
    /// Requires: NetworkObject, trigger Collider, MeshRenderer.
    /// Register this prefab in NetworkManager's NetworkPrefabs list.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Rush Champions/Decoy Box Pickup")]
    public class DecoyBoxPickup : NetworkBehaviour
    {
        [SerializeField] private float lifetime = 20f;

        private ulong _ownerClientId;
        private float _stunDuration;
        private bool  _triggered;

        /// <summary>Called by PlayerPowerUpInventory right after Spawn().</summary>
        public void Init(ulong ownerClientId, float stunDuration)
        {
            _ownerClientId = ownerClientId;
            _stunDuration  = stunDuration;
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

            // Immune to own box
            if (hitterId == _ownerClientId) return;

            // Decoy bypasses Shield and Invincibility — no checks here.

            _triggered = true;
            Debug.Log($"[DecoyBox] Client {hitterId} stunned by decoy! ({_stunDuration}s)");

            if (PlayerPowerUpInventory.All.TryGetValue(hitterId, out var inv))
            {
                ClientRpcParams p = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { hitterId } }
                };
                inv.ApplyStunClientRpc(_stunDuration, p);
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
