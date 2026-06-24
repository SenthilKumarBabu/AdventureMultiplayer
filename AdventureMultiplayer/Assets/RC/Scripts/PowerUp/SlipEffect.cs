using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Applied when a player hits a Banana peel.
    /// Clamps lateral movement to forward-only (can stop or go straight — no turning, no jumping).
    /// Add to every player prefab.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Rush Champions/Slip Effect")]
    public class SlipEffect : MonoBehaviour
    {
        public bool IsSlipping => _slipping;

        private Player               _player;
        private AIPlayerInputManager _aiInput;
        private bool                 _slipping;
        private float                _endTime;

        private void Awake()
        {
            _player  = GetComponent<Player>();
            _aiInput = GetComponent<AIPlayerInputManager>();
        }

        /// <summary>Activate slip for <paramref name="duration"/> seconds. Refreshes if already active.</summary>
        public void Apply(float duration)
        {
            _slipping = true;
            _endTime  = Time.time + duration;
            Debug.Log($"[SlipEffect] {name} slipping for {duration}s.");
        }

        private void LateUpdate()
        {
            if (!_slipping) return;

            if (Time.time >= _endTime)
            {
                _slipping = false;
                return;
            }

            if (_player != null && _player.enabled)
            {
                var forward = _player.localForward;
                float dot   = Vector3.Dot(_player.lateralVelocity, forward);

                // Allow forward motion only; zero out lateral and side components
                _player.lateralVelocity = dot > 0f ? forward * dot : Vector3.zero;

                // Cancel any jump (vertical upward velocity)
                if (_player.verticalVelocity.y > 0f && !_player.isGrounded)
                    _player.verticalVelocity = Vector3.zero;
            }

            if (_aiInput != null)
            {
                // Clamp AI move direction to forward only
                var fwd = transform.forward;
                float dot = Vector3.Dot(_aiInput.desiredMoveDirection, fwd);
                _aiInput.desiredMoveDirection = dot > 0f ? fwd * dot : Vector3.zero;
                _aiInput.jumpQueued = false;
            }
        }
    }
}
