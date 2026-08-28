using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-spawned trap. Player who placed it is immune.
    /// Any other player that walks through the trigger collider gets the SlipEffect applied
    /// on their owner client via PlayerPowerUpInventory.ApplySlipClientRpc.
    /// Invisible blocks the slip.
    ///
    /// Requires: NetworkObject, trigger Collider, MeshRenderer.
    /// Register this prefab in NetworkManager's NetworkPrefabs list.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Rush Champions/Banana Peel")]
    public class BananaPeel : NetworkBehaviour
    {
        [SerializeField] private float lifetime = 15f;

        // Caster's NetworkObjectId — NOT OwnerClientId, which is not unique for bots
        // (every server-owned bot defaults to OwnerClientId 0).
        private ulong _ownerNetworkObjectId;
        private float _slipDuration;
        private bool  _triggered;

        /// <summary>Called by PlayerPowerUpInventory right after Spawn().</summary>
        public void Init(ulong ownerNetworkObjectId, float slipDuration)
        {
            _ownerNetworkObjectId = ownerNetworkObjectId;
            _slipDuration         = slipDuration;
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

            // Immune to own peel — compare by NetworkObjectId, not OwnerClientId (bots
            // all share OwnerClientId 0, which would make every bot immune to every
            // other bot's peel too).
            if (netObj.NetworkObjectId == _ownerNetworkObjectId) return;

            // Invisible blocks the slip (both the damage-immunity and pass-through halves apply).
            var health = netObj.GetComponent<NetworkedHealth>();
            var inv    = netObj.GetComponent<PlayerPowerUpInventory>();
            if (health != null && health.IsInvisible)
            {
                Debug.Log($"[BananaPeel] '{netObj.name}' phased through the banana peel (Invisible).");
                if (inv != null)
                {
                    ClientRpcParams pDodge = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { netObj.OwnerClientId } }
                    };
                    inv.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Banana, (int)PowerUpAffectOutcome.InvisibleDodged, pDodge);

                    if (PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var dodgeAttackerInv))
                        inv.NotifyGlobalAttackClientRpc(dodgeAttackerInv.RaceId, inv.RaceId, (int)PowerUpType.Banana, (int)PowerUpAffectOutcome.InvisibleDodged);
                }
                return;
            }

            _triggered = true;
            Debug.Log($"[BananaPeel] '{netObj.name}' slipped! ({_slipDuration}s)");

            if (inv != null)
            {
                ClientRpcParams p = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { netObj.OwnerClientId } }
                };
                inv.ApplySlipClientRpc(_slipDuration, p);
                inv.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Banana, (int)PowerUpAffectOutcome.Hit, p);

                if (PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var attackerInv))
                    inv.NotifyGlobalAttackClientRpc(attackerInv.RaceId, inv.RaceId, (int)PowerUpType.Banana, (int)PowerUpAffectOutcome.Hit);
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
