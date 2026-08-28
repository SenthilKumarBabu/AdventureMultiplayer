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
        [Tooltip("Seconds the player must be continuously airborne+falling before the roll exits to FallPlayerState. Filters out 1-2 frame ground-contact blips from terrain bumps so a real roll isn't cut short.")]
        [SerializeField] private float airborneExitGracePeriod = 0.15f;

        private Vector3 _startPos;
        private ManaSystem _manaSystem;
        private float _airborneTime;

        // SuperCharge power-up: bonus travel distance equivalent to +SuperChargeBonusSeconds
        // of roll time, consumed once per roll activation.
        private float _superChargeBonusDistance;

        protected override void OnEnter(Player player)
        {
            base.OnEnter(player); // fires OnRollStarted, resizes collider
            _startPos = player.position;
            _airborneTime = 0f;

            if (_manaSystem == null) _manaSystem = player.GetComponent<ManaSystem>();
            _superChargeBonusDistance = _manaSystem != null && _manaSystem.TryConsumeSuperCharge()
                ? rollSpeed * ManaSystem.SuperChargeBonusSeconds
                : 0f;
            Debug.Log($"[BrunoRollingState] Roll started. superChargeBonusDistance={_superChargeBonusDistance} " +
                      $"targetDistance={fixedRollDistance + _superChargeBonusDistance}");

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

        // Diagnostic: catches ANY exit from this state, even one that bypasses both
        // branches in HandleUncurl below (e.g. something external forcing a transition).
        protected override void OnExit(Player player)
        {
            float dist = Vector3.Distance(
                new Vector3(player.position.x, 0f, player.position.z),
                new Vector3(_startPos.x, 0f, _startPos.z)
            );
            Debug.Log($"[BrunoRollingState] OnExit. dist={dist:0.00} " +
                      $"target={fixedRollDistance + _superChargeBonusDistance:0.00} newState={player.states.index}");
            base.OnExit(player);
        }

        protected override void HandleUncurl(Player player)
        {
            // Measure horizontal distance covered since roll started
            float dist = Vector3.Distance(
                new Vector3(player.position.x, 0f, player.position.z),
                new Vector3(_startPos.x, 0f, _startPos.z)
            );

            if (player.isGrounded && dist >= fixedRollDistance + _superChargeBonusDistance)
            {
                Debug.Log($"[BrunoRollingState] Roll completed normally. dist={dist:0.00} " +
                          $"target={fixedRollDistance + _superChargeBonusDistance:0.00}");
                player.lateralVelocity = Vector3.zero;
                player.states.Change<WalkPlayerState>();
                return;
            }

            // If the player goes off a ledge and starts falling, exit the roll — but only
            // after a short sustained window, so a 1-2 frame ground-contact blip from a
            // terrain bump doesn't cut the roll short (SnapToGround recovers those on its own).
            if (!player.isGrounded && player.verticalVelocity.y < 0f)
            {
                _airborneTime += Time.deltaTime;
                if (_airborneTime >= airborneExitGracePeriod)
                {
                    Debug.Log($"[BrunoRollingState] Roll interrupted — airborne+falling. dist={dist:0.00} " +
                              $"target={fixedRollDistance + _superChargeBonusDistance:0.00}");
                    player.states.Change<FallPlayerState>();
                }
            }
            else
            {
                _airborneTime = 0f;
            }
        }
    }
}
