using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Replaces AirDivePlayerState on Spike.
    /// Adds an upward burst on activation so the player rockets higher instead of
    /// diving toward the ground. Landing is handled like a normal jump (immediate
    /// transition to IdlePlayerState on contact) rather than AirDivePlayerState's
    /// friction-slide + bounce-hop, which was tuned for the dive and looked wrong
    /// on a jump landing. The Animator Controller reuses the normal Jump/Fall clips
    /// for this state (blended on Vertical Speed) rather than a dedicated clip —
    /// see MixamoPlayer.controller's Any State transitions for State==12.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Spike High Jump State")]
    public class SpikeHighJumpState : AirDivePlayerState
    {
        [Tooltip("Multiplier applied to Spike's normal jump height (maxJumpHeight) for the burst. " +
            "Reads the stat rather than a flat force so normal jump stays completely untouched.")]
        [SerializeField] private float highJumpMultiplier = 1f;

        [Tooltip("Extra upward speed granted for one jump while a SuperCharge bonus is active.")]
        [SerializeField] private float superChargeBonusHeight = 12f;

        private ManaSystem _manaSystem;

        protected override void OnEnter(Player player)
        {
            base.OnEnter(player); // sets vertical=0, lateral=forward*airDiveForwardForce

            if (_manaSystem == null) _manaSystem = player.GetComponent<ManaSystem>();
            bool superChargeActive = _manaSystem != null && _manaSystem.TryConsumeSuperCharge();

            float verticalForce = player.stats.current.maxJumpHeight * highJumpMultiplier
                + (superChargeActive ? superChargeBonusHeight : 0f);
            player.verticalVelocity = Vector3.up * verticalForce;
        }

        // Reimplements the step loop instead of inheriting AirDivePlayerState.OnStep —
        // that base version slides on landing with dive friction/rotation and then
        // bounces back up via airDiveGroundLeapHeight, which reads as a broken double
        // jump for what's now a plain (if bigger) jump. This mirrors FallPlayerState's
        // landing instead: normal air control, land on contact, no slide, no bounce.
        protected override void OnStep(Player player)
        {
            player.Gravity();
            player.SnapToGround();
            player.FaceDirectionSmooth(player.lateralVelocity);
            player.AccelerateToInputDirection();
            player.Jump();

            if (player.isGrounded)
                player.states.Change<IdlePlayerState>();
        }
    }
}
