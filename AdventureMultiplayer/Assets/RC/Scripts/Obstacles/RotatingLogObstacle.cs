using ithappy;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Reads the RotationScript on this log and applies the resulting tangential
    /// surface velocity to any player grounded on it, pushing them off naturally.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(RotationScript))]
    [AddComponentMenu("Adventure Multiplayer/Rotating Log Obstacle")]
    public class RotatingLogObstacle : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 1.5f;
        [SerializeField] private float pushMultiplier = 1.5f;

        private Collider _myCollider;
        private RotationScript _rotationScript;

        private void Awake()
        {
            _myCollider = GetComponent<Collider>();
            _rotationScript = GetComponent<RotationScript>();
        }

        private void Update()
        {
            var top = _myCollider.bounds.center + Vector3.up * (_myCollider.bounds.extents.y + 0.1f);
            var hits = Physics.OverlapSphere(top, detectionRadius, ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                var player = col.GetComponentInParent<Player>();
                if (player == null) continue;
                if (!player.isAlive) continue;
                if (!player.isGrounded) continue;
                if (player.groundHit.collider != _myCollider) continue;

                ApplyRotationPush(player);
                break;
            }
        }

        private void ApplyRotationPush(Player player)
        {
            // Build angular velocity vector in world space (deg/s → rad/s)
            var localAxis = _rotationScript.rotationAxis switch
            {
                RotationScript.RotationAxis.X => Vector3.right,
                RotationScript.RotationAxis.Y => Vector3.up,
                _                             => Vector3.forward
            };

            var worldAxis  = transform.TransformDirection(localAxis);
            var omega      = worldAxis * (_rotationScript.rotationSpeed * Mathf.Deg2Rad);

            // Tangential velocity at the player's position: v = ω × r
            var r               = player.position - transform.position;
            var tangentialVelocity = Vector3.Cross(omega, r) * pushMultiplier;

            // Apply as lateral velocity and kick into fall state
            player.lateralVelocity  = new Vector3(tangentialVelocity.x, 0f, tangentialVelocity.z);
            player.verticalVelocity = Vector3.down * 2f;
            player.states.Change<FallPlayerState>();
        }
    }
}
