using System.Collections.Generic;
using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Add to any obstacle to launch the player away on contact.
    /// Knockback values are defined per ObstacleType in an ObstacleKnockbackDatabase
    /// ScriptableObject — they are not editable directly on this component.
    ///
    /// Two detection paths:
    ///   1. IEntityContact — fires when the player's CapsuleCast hits this collider.
    ///   2. OnTriggerStay — fires when a trigger collider overlaps the player
    ///      (spinning/moving obstacle rotates into a stationary player).
    ///
    /// For moving obstacles add a trigger CapsuleCollider alongside the solid collider
    /// so OnTriggerStay fires reliably.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Adventure Multiplayer/Obstacle Knockback")]
    public class ObstacleKnockback : MonoBehaviour, IEntityContact
    {
        [SerializeField] private ObstacleType              obstacleType      = ObstacleType.Triangle;
        [SerializeField] private ObstacleKnockbackDatabase database;

        /// <summary>Read-only access for external systems (e.g. RaceBotBrain) that need to know
        /// what kind of hazard this is without triggering the knockback itself.</summary>
        public ObstacleType Type => obstacleType;

        // When true, ignores auto-computed direction and uses customDirection instead.
        // Use for HorizontalPusher, Laser, and any obstacle whose push direction
        // cannot be derived from surface velocity or pivot→player.
        [SerializeField] private bool    overrideDirection = false;
        [SerializeField] private Vector3 customDirection   = Vector3.right;

        private Rigidbody _rb;

        // Per-player cooldown keyed by NGO OwnerClientId (0 = single-player / no NetworkObject).
        private readonly Dictionary<ulong, float> _lastKnockbackTime = new();

        private void Awake()
        {
            _rb = GetComponentInParent<Rigidbody>();

            if (database == null)
                Debug.LogWarning($"[ObstacleKnockback] '{name}': no ObstacleKnockbackDatabase assigned — knockback will use fallback values.");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // Auto-assign database if missing.
            if (database == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:ObstacleKnockbackDatabase");
                if (guids.Length > 0)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    database = UnityEditor.AssetDatabase.LoadAssetAtPath<ObstacleKnockbackDatabase>(path);
                    Debug.Log($"[ObstacleKnockback] '{name}': auto-assigned database from '{path}'.");
                }
            }

            // Auto-detect obstacle type from mesh name.
            // Search self → parents → children so it works regardless of where
            // ObstacleKnockback is placed in the hierarchy.
            if (database != null)
            {
                var mf = GetComponent<MeshFilter>()
                      ?? GetComponentInParent<MeshFilter>()
                      ?? GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var detected = database.GetTypeFromMesh(mf.sharedMesh.name);
                    if (detected.HasValue && detected.Value != obstacleType)
                    {
                        obstacleType = detected.Value;
                        Debug.Log($"[ObstacleKnockback] '{name}': auto-detected type '{obstacleType}' from mesh '{mf.sharedMesh.name}'.");
                    }
                }
            }
        }
#endif

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

            if (player.TryGetComponent<NetworkedHealth>(out var health) && health.IsInvisible)
            {
                Debug.Log($"[ObstacleKnockback] {name} ({source}): player Invisible — passed through");
                return;
            }

            // Read config early so we know pureKnockback before the recovering check.
            var  config        = database?.GetConfig(obstacleType);
            bool pureKnockback = config?.pureKnockback ?? false;

            // Recovering guard only applies to damage hits — pure knockback always fires.
            if (!pureKnockback && player.health.recovering)
            {
                Debug.Log($"[ObstacleKnockback] {name} ({source}): health recovering ({player.health.coolDown}s cd) — skipped");
                return;
            }

            // Per-player cooldown using NGO client ID.
            float cooldown = config?.knockbackCooldown ?? 1f;
            var   netObj   = player.GetComponent<NetworkObject>();
            ulong clientId = netObj != null ? netObj.OwnerClientId : 0ul;
            _lastKnockbackTime.TryGetValue(clientId, out float lastTime);

            if (Time.time - lastTime < cooldown)
            {
                Debug.Log($"[ObstacleKnockback] {name} ({source}): cooldown for client {clientId} ({cooldown - (Time.time - lastTime):F2}s left) — skipped");
                return;
            }

            _lastKnockbackTime[clientId] = Time.time;

            // Direction resolution — three paths in priority order:
            //  1. Manual override (customDirection) for obstacles like HorizontalPusher/Laser.
            //  2. Rigidbody surface velocity for spinning/moving obstacles.
            //  3. Pivot→player fallback for static obstacles.
            Vector3 dir;
            if (overrideDirection)
            {
                dir = customDirection.sqrMagnitude > 0.001f ? customDirection.normalized : Vector3.forward;
            }
            else if (_rb != null)
            {
                var surfaceVel = _rb.GetPointVelocity(player.position);
                var flatVel    = new Vector3(surfaceVel.x, 0f, surfaceVel.z);
                dir = flatVel.sqrMagnitude >= 0.01f
                    ? flatVel.normalized
                    : new Vector3(player.position.x - transform.position.x, 0f, player.position.z - transform.position.z).normalized;
            }
            else
            {
                var rawDir = player.position - transform.position;
                dir = new Vector3(rawDir.x, 0f, rawDir.z);
            }

            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir = dir.normalized;

            float lateralForce = config?.lateralForce ?? 20f;
            float upwardForce  = config?.upwardForce  ?? 15f;
            int   damage       = config?.damage       ?? 1;

            Debug.Log($"[ObstacleKnockback] {name} ({source}): type={obstacleType} client={clientId} dir={dir:F2} | state={player.states.current?.GetType().Name ?? "null"} | HP={player.health.current}/{player.health.max}");

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
                Debug.Log($"[ObstacleKnockback] {name} ({source}): DAMAGE dmg={damage} HP={player.health.current} | lat={player.lateralVelocity:F2} vert={player.verticalVelocity:F2} state={player.states.current?.GetType().Name ?? "null"}");
            }
        }
    }
}
