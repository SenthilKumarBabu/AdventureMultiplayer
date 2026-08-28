using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Applied when a player hits a Banana Peel.
    ///
    /// On activation the player is immediately forced into CrouchPlayerState
    /// (which triggers PLAYER TWO's own slide animation and collider resize).
    /// SlipAwarePlayerInputManager keeps them there by returning GetCrouch()=true
    /// and GetMovementDirection()=zero for the duration.
    /// This script fights CrouchPlayerState's deceleration every frame so the
    /// player glides forward at a constant speed instead of stopping.
    /// Sets the animator bool "Is Sliding" so the controller transitions to the
    /// Slide state (Saphy|RailGrind / Lily|Grind), set up via SlideAnimationSetup.
    ///
    /// Requires SlipAwarePlayerInputManager on the same prefab.
    /// </summary>
    [AddComponentMenu("Rush Champions/Slip Effect")]
    public class SlipEffect : MonoBehaviour
    {
        [Tooltip("Forward speed held constant during the slide (units/sec). " +
                 "Must be >= PlayerStats.minSpeedToSlide or the slide animation won't trigger.")]
        [SerializeField] private float slideSpeed = 10f;

        public bool IsSlipping => _slipping;

        private Player               _player;
        private AIPlayerInputManager _aiInput;
        private Animator             _animator;
        private bool                 _slipping;
        private float                _endTime;

        private static readonly int IsSlidingHash = Animator.StringToHash("Is Sliding");

        private void Awake()
        {
            _player  = GetComponent<Player>();
            _aiInput = GetComponent<AIPlayerInputManager>();

            // PlayerAnimator exposes the model's Animator; fall back to a child search.
            var pa = GetComponent<PlayerAnimator>();
            _animator = pa != null ? pa.animator : GetComponentInChildren<Animator>();
        }

        /// <summary>Activate the banana-peel slide for <paramref name="duration"/> seconds.</summary>
        public void Apply(float duration)
        {
            _slipping = true;
            _endTime  = Time.time + duration;

            if (_player != null && _player.enabled)
            {
                // Set velocity first so OnEnter's HandleSlideStart sees speed >= minSpeedToSlide
                // and fires the slide animation (instead of zeroing velocity).
                _player.lateralVelocity = _player.localForward * slideSpeed;

                // Force PLAYER TWO into CrouchPlayerState — shrinks collider, handles ground snap.
                _player.states.Change<CrouchPlayerState>();
            }

            // Tell the animator to show the RailGrind/Slide animation.
            if (_animator != null)
                _animator.SetBool(IsSlidingHash, true);

            Debug.Log($"[SlipEffect] {name} sliding for {duration}s at {slideSpeed} u/s.");
        }

        private void Update()
        {
            if (!_slipping) return;

            if (Time.time >= _endTime)
            {
                _slipping = false;
                if (_animator != null)
                    _animator.SetBool(IsSlidingHash, false);
                return;
            }

            if (_player != null && _player.enabled)
            {
                // CrouchPlayerState calls Decelerate(crouchFriction) every step.
                // Override the result so the player glides at constant speed for the full duration.
                if (_player.isGrounded)
                    _player.lateralVelocity = _player.localForward * slideSpeed;

                // Suppress upward velocity while airborne to prevent jumping during slide.
                if (_player.verticalVelocity.y > 0f && !_player.isGrounded)
                    _player.verticalVelocity = Vector3.zero;
            }

            if (_aiInput != null)
                _aiInput.jumpQueued = false;
        }
    }
}
