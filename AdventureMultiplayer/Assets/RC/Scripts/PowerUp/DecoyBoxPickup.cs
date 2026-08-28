using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-spawned trap. Player who placed it is immune.
    /// Any other player that walks into the trigger gets the StunEffect applied on their owner client.
    /// DecoyBox intentionally bypasses Shield (active counter-play item), but Invisible still phases through it.
    ///
    /// Requires: NetworkObject, trigger Collider, MeshRenderer.
    /// Register this prefab in NetworkManager's NetworkPrefabs list.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Rush Champions/Decoy Box Pickup")]
    public class DecoyBoxPickup : NetworkBehaviour
    {
        [SerializeField] private float lifetime = 20f;

        // Caster's NetworkObjectId — NOT OwnerClientId, which is not unique for bots
        // (every server-owned bot defaults to OwnerClientId 0).
        private ulong _ownerNetworkObjectId;
        private float _stunDuration;
        private bool  _triggered;

        /// <summary>Called by PlayerPowerUpInventory right after Spawn().</summary>
        public void Init(ulong ownerNetworkObjectId, float stunDuration)
        {
            _ownerNetworkObjectId = ownerNetworkObjectId;
            _stunDuration         = stunDuration;
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

            // Immune to own box — compare by NetworkObjectId, not OwnerClientId (bots
            // all share OwnerClientId 0, which would make every bot immune to every
            // other bot's decoy box too).
            if (netObj.NetworkObjectId == _ownerNetworkObjectId) return;

            // Decoy is designed as a counter to Shield, but Invisible's physical
            // pass-through still dodges the trap like any other obstacle.
            var health = netObj.GetComponent<NetworkedHealth>();
            var inv    = netObj.GetComponent<PlayerPowerUpInventory>();
            if (health != null && health.IsInvisible)
            {
                Debug.Log($"[DecoyBox] '{netObj.name}' phased through the decoy box (Invisible).");
                if (inv != null)
                {
                    ClientRpcParams pDodge = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { netObj.OwnerClientId } }
                    };
                    inv.NotifyPowerUpAffectedClientRpc((int)PowerUpType.DecoyBox, (int)PowerUpAffectOutcome.InvisibleDodged, pDodge);

                    if (PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var dodgeAttackerInv))
                        inv.NotifyGlobalAttackClientRpc(dodgeAttackerInv.RaceId, inv.RaceId, (int)PowerUpType.DecoyBox, (int)PowerUpAffectOutcome.InvisibleDodged);
                }
                return;
            }

            _triggered = true;
            Debug.Log($"[DecoyBox] '{netObj.name}' stunned by decoy! ({_stunDuration}s)");

            if (inv != null)
            {
                ClientRpcParams p = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { netObj.OwnerClientId } }
                };
                inv.ApplyStunClientRpc(_stunDuration, p);
                inv.NotifyPowerUpAffectedClientRpc((int)PowerUpType.DecoyBox, (int)PowerUpAffectOutcome.Hit, p);

                if (PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var attackerInv))
                    inv.NotifyGlobalAttackClientRpc(attackerInv.RaceId, inv.RaceId, (int)PowerUpType.DecoyBox, (int)PowerUpAffectOutcome.Hit);
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
