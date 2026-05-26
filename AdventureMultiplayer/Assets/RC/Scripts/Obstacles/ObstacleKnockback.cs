using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Add to any obstacle to launch the player away on contact.
    ///
    /// Two detection paths:
    ///   1. IEntityContact — fires when the player's CapsuleCast hits this collider
    ///      (player walks into obstacle).
    ///   2. OnTriggerStay — fires when a trigger collider on this GameObject overlaps the player
    ///      (spinning/moving obstacle rotates into a stationary player).
    ///
    /// For moving obstacles (e.g. spinning drums) add a trigger CapsuleCollider
    /// alongside the solid collider so OnTriggerStay fires reliably.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Adventure Multiplayer/Obstacle Knockback")]
    public class ObstacleKnockback : MonoBehaviour, IEntityContact
    {
        [SerializeField] private float lateralForce   = 20f;
        [SerializeField] private float upwardForce    = 15f;
        [SerializeField] private int   damage         = 1;
        [Tooltip("Skips ApplyDamage — no hurt animation, just velocity.")]
        [SerializeField] private bool  pureKnockback  = false;
        [Tooltip("Seconds between knockback hits (used for pure knockback; damage path uses health.coolDown).")]
        [SerializeField] private float knockbackCooldown = 1f;

        private float _lastKnockbackTime = float.MinValue;

        // Path 1: player moves into obstacle
        public void OnEntityContact(Entity entity)
        {
            if (entity is not Player player)
            {
                Debug.Log($"[ObstacleKnockback] {name} (EntityContact): non-Player '{entity.name}' — skipped");
                return;
            }
            HandleContact(player, "EntityContact");
        }

        // Path 2: spinning/moving obstacle overlaps player via trigger collider
        private void OnTriggerStay(Collider other)
        {
            var player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();
            if (player == null) return;
            HandleContact(player, "TriggerStay");
        }

        private void HandleContact(Player player, string source)
        {
            if (!player.isAlive)
            {
                Debug.Log($"[ObstacleKnockback] {name} ({source}): player not alive — skipped");
                return;
            }

            if (player.health.recovering)
            {
                Debug.Log($"[ObstacleKnockback] {name} ({source}): health recovering ({player.health.coolDown}s cd) — skipped");
                return;
            }

            if (Time.time - _lastKnockbackTime < knockbackCooldown)
            {
                Debug.Log($"[ObstacleKnockback] {name} ({source}): knockback on cooldown ({knockbackCooldown - (Time.time - _lastKnockbackTime):F2}s left) — skipped");
                return;
            }

            _lastKnockbackTime = Time.time;

            var rawDir = player.position - transform.position;
            var dir    = new Vector3(rawDir.x, 0f, rawDir.z);
            bool usedFallback = dir.sqrMagnitude < 0.001f;
            if (usedFallback) dir = Vector3.forward;
            dir = dir.normalized;

            Debug.Log($"[ObstacleKnockback] {name} ({source}): dir={dir:F2} fallback={usedFallback} | state={player.states.current.GetType().Name} | HP={player.health.current}/{player.health.max}");

            if (pureKnockback)
            {
                player.lateralVelocity  = dir * lateralForce;
                player.verticalVelocity = Vector3.up * upwardForce;
                player.states.Change<FallPlayerState>();
                Debug.Log($"[ObstacleKnockback] {name} ({source}): PURE — lat={player.lateralVelocity:F2} vert={player.verticalVelocity:F2}");
            }
            else
            {
                player.ApplyDamage(damage, transform.position);
                player.lateralVelocity  = dir * lateralForce;
                player.verticalVelocity = Vector3.up * upwardForce;
                Debug.Log($"[ObstacleKnockback] {name} ({source}): DAMAGE dmg={damage} HP={player.health.current} | lat={player.lateralVelocity:F2} vert={player.verticalVelocity:F2} state={player.states.current.GetType().Name}");
            }
        }
    }
}
