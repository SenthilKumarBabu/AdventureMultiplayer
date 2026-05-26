using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Replaces RollingPlayerState on Bruno.
    /// One button press = one fixed-distance roll at constant speed.
    /// The charge duration no longer affects roll distance or speed.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Bruno Rolling State")]
    public class BrunoRollingState : RollingPlayerState
    {
        [Tooltip("Horizontal distance of one full roll.")]
        [SerializeField] private float fixedRollDistance = 8f;
        [Tooltip("Constant speed during the roll.")]
        [SerializeField] private float rollSpeed = 12f;

        private Vector3 _startPos;

        protected override void OnEnter(Player player)
        {
            base.OnEnter(player); // fires OnRollStarted, resizes collider
            _startPos = player.position;

            // Use current facing or lateral velocity direction, then lock speed
            var dir = player.lateralVelocity.sqrMagnitude > 0.001f
                ? player.lateralVelocity.normalized
                : player.localForward;
            player.lateralVelocity = dir * rollSpeed;
        }

        // No friction — speed must stay constant for the fixed distance to be exact
        protected override void HandleFriction(Player player) { }

        // No input deceleration — direction is locked for the duration of the roll
        protected override void HandleDeceleration(
            Player player, Vector3 inputDirection, float inputMagnitude, float forwardDot) { }

        // No steering — this is a committed, fixed-direction roll
        protected override void HandleGroundTurning(
            Player player, Vector3 inputDirection, float inputMagnitude, float forwardDot) { }

        protected override void HandleUncurl(Player player)
        {
            // Measure horizontal distance covered since roll started
            float dist = Vector3.Distance(
                new Vector3(player.position.x, 0f, player.position.z),
                new Vector3(_startPos.x, 0f, _startPos.z)
            );

            if (player.isGrounded && dist >= fixedRollDistance)
            {
                player.lateralVelocity = Vector3.zero;
                player.states.Change<WalkPlayerState>();
                return;
            }

            // If the player goes off a ledge and starts falling, exit the roll
            if (!player.isGrounded && player.verticalVelocity.y < 0f)
                player.states.Change<FallPlayerState>();
        }
    }
}
