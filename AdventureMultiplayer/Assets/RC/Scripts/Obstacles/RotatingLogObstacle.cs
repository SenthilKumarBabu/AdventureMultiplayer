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
            var center = _myCollider.bounds.center;
            float searchRadius = detectionRadius + _myCollider.bounds.extents.y + 1.5f;
            var hits = Physics.OverlapSphere(center, searchRadius, ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                var player = col.GetComponentInParent<Player>();
                if (player == null) continue;
                if (!player.isAlive) continue;
                if (!player.isGrounded) continue;

                var groundCol = player.groundHit.collider;
                bool onThisLog = groundCol != null &&
                    (groundCol == _myCollider || groundCol.transform.IsChildOf(transform));
                if (!onThisLog) continue;

                ApplyRotationPush(player);
                break;
            }
        }

        private void ApplyRotationPush(Player player)
        {
            var localAxis = _rotationScript.rotationAxis switch
            {
                RotationScript.RotationAxis.X => Vector3.right,
                RotationScript.RotationAxis.Y => Vector3.up,
                _                             => Vector3.forward
            };

            var worldAxis  = transform.TransformDirection(localAxis);
            var omega      = worldAxis * (_rotationScript.rotationSpeed * Mathf.Deg2Rad);
            var r          = player.position - transform.position;
            var tangential = Vector3.Cross(omega, r) * pushMultiplier;

            // Add rotation push on top of player's own input — don't wipe input direction.
            player.lateralVelocity += new Vector3(tangential.x, 0f, tangential.z);
        }
    }
}
