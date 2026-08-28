using System;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-driven homing rocket spawned by the Rocket power-up.
    ///
    /// The server moves and steers the rocket every FixedUpdate using a kinematic
    /// Rigidbody (MovePosition / MoveRotation). A NetworkTransform on the prefab
    /// replicates the position/rotation to all clients so the visual is visible
    /// everywhere.
    ///
    /// On trigger contact with the target player the rocket applies a StunEffect
    /// via ApplyStunClientRpc, then despawns itself.
    /// Shield absorbs the hit (consumed). Invisible deflects it (not consumed).
    ///
    /// Prefab requirements:
    ///   - NetworkObject
    ///   - NetworkTransform  (Server authority)
    ///   - Rigidbody         (isKinematic = true, useGravity = false)
    ///   - Collider          (isTrigger = true  — SphereCollider recommended)
    ///   - RocketProjectile  (this script)
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Adventure Multiplayer/Rocket Projectile")]
    public class RocketProjectile : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed   = 20f;   // units/sec
        [SerializeField] private float turnSpeed   = 180f;  // degrees/sec
        [SerializeField] private float maxLifetime = 10f;   // auto-despawn after this

        // NetworkObjectIds, not OwnerClientIds — bots all default to OwnerClientId 0
        // (server-owned), so matching hits by OwnerClientId would never correctly hit
        // (or correctly exclude) a bot. NetworkObjectId is always unique.
        private ulong     _ownerNetworkObjectId;
        private ulong     _targetNetworkObjectId;
        private Rigidbody _rb;
        private bool      _hit;

        /// <summary>Called by PlayerPowerUpInventory immediately after Spawn().</summary>
        public void Init(ulong ownerNetworkObjectId, ulong targetNetworkObjectId)
        {
            _ownerNetworkObjectId  = ownerNetworkObjectId;
            _targetNetworkObjectId = targetNetworkObjectId;
        }

        public override void OnNetworkSpawn()
        {
            _rb             = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity  = false;

            if (IsServer)
                DespawnAfterLifetimeAsync().Forget();
        }

        private void FixedUpdate()
        {
            if (!IsServer || _hit) return;

            Vector3   dir    = GetTargetDirection();
            Quaternion newRot = transform.rotation;

            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                newRot = Quaternion.RotateTowards(
                    transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
                _rb.MoveRotation(newRot);
            }

            // Use newRot * forward — transform.forward is stale until the physics step commits MoveRotation.
            _rb.MovePosition(transform.position + newRot * Vector3.forward * moveSpeed * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || _hit) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;
            // Never hit the firer, even if the rocket circles back.
            if (netObj.NetworkObjectId == _ownerNetworkObjectId) return;
            if (netObj.NetworkObjectId != _targetNetworkObjectId) return;

            _hit = true;

            // Search up from the triggered collider — handles child-collider setups where
            // netObj.GetComponent would miss a root-level NetworkedHealth.
            var health = other.GetComponentInParent<NetworkedHealth>();
            var inv    = netObj.GetComponent<PlayerPowerUpInventory>();
            ClientRpcParams targetOnly = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { netObj.OwnerClientId } }
            };

            if (health != null && health.IsShielded)
            {
                health.SetShield(false);
                Debug.Log($"[RocketProjectile] Blocked by shield on '{netObj.name}'.");
                inv?.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Rocket, (int)PowerUpAffectOutcome.ShieldBlocked, targetOnly);
                if (inv != null && PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var blockedAttackerInv))
                    inv.NotifyGlobalAttackClientRpc(blockedAttackerInv.RaceId, inv.RaceId, (int)PowerUpType.Rocket, (int)PowerUpAffectOutcome.ShieldBlocked);
                Despawn();
                return;
            }

            if (health != null && health.IsInvisible)
            {
                Debug.Log($"[RocketProjectile] Blocked by Invisible on '{netObj.name}'.");
                inv?.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Rocket, (int)PowerUpAffectOutcome.InvisibleDodged, targetOnly);
                if (inv != null && PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var dodgedAttackerInv))
                    inv.NotifyGlobalAttackClientRpc(dodgedAttackerInv.RaceId, inv.RaceId, (int)PowerUpType.Rocket, (int)PowerUpAffectOutcome.InvisibleDodged);
                Despawn();
                return;
            }

            // Kill shot: deal full max health so the target dies and respawns.
            int damage = health != null ? health.MaxHealth : 100;
            Debug.Log($"[RocketProjectile] Kill shot on '{netObj.name}' — damage {damage}.");

            if (health != null)
                health.ApplyDamageFromPowerUp(damage, transform.position);
            inv?.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Rocket, (int)PowerUpAffectOutcome.Hit, targetOnly);

            if (inv != null && PlayerPowerUpInventory.All.TryGetValue(_ownerNetworkObjectId, out var attackerInv))
                inv.NotifyGlobalAttackClientRpc(attackerInv.RaceId, inv.RaceId, (int)PowerUpType.Rocket, (int)PowerUpAffectOutcome.Hit);

            Despawn();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector3 GetTargetDirection()
        {
            if (!PlayerPowerUpInventory.All.TryGetValue(_targetNetworkObjectId, out var inv))
                return transform.forward; // target gone — fly straight

            Vector3 toTarget = inv.transform.position - transform.position;
            return toTarget.magnitude > 0.01f ? toTarget.normalized : transform.forward;
        }

        private void Despawn()
        {
            if (IsSpawned) NetworkObject.Despawn();
        }

        private async UniTaskVoid DespawnAfterLifetimeAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(maxLifetime),
                cancellationToken: destroyCancellationToken);
            if (!_hit) Despawn();
        }
    }
}
