using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Network-aware replacement for EntityHitbox.
    ///
    /// When a hitbox on a networked entity hits an enemy or another player, the
    /// damage request is routed through the appropriate ServerRpc instead of being
    /// applied locally:
    ///
    ///   • Enemy hit  → NetworkEnemy.DamageEnemyServerRpc
    ///   • Player hit → NetworkedHealth.TakeDamageServerRpc
    ///
    /// Replace EntityHitbox with this component on all hitbox children of the
    /// player prefab (and optionally on enemy hitbox children).
    /// All other EntityHitbox behaviour (breakables, rebound, push-back) is
    /// inherited unchanged.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Networked Entity Hitbox")]
    public class NetworkedEntityHitbox : EntityHitbox
    {
        protected override void HandleEntityAttack(Entity target)
        {
            if (target.TryGetComponent<NetworkEnemy>(out var netEnemy))
            {
                netEnemy.DamageEnemyServerRpc(damage, transform.position);
                return;
            }

            if (target.TryGetComponent<NetworkedHealth>(out var netHealth))
            {
                netHealth.TakeDamageServerRpc(damage, transform.position);
                return;
            }

            // Fallback: no network component found, apply locally (single-player / offline).
            base.HandleEntityAttack(target);
        }
    }
}
