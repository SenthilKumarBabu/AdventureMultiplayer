using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Applied when a player hits a Banana Peel.
    ///
    /// Movement restriction works in two layers:
    ///   1. SlipAwarePlayerInputManager (required on every player prefab) filters the raw
    ///      input direction to forward-only before PLAYER TWO's state machine computes
    ///      velocity — this is the primary fix.
    ///   2. Apply() snaps any existing sideways/backward velocity to zero immediately so
    ///      the player doesn't coast sideways on existing momentum.
    ///
    /// Add to every player prefab alongside SlipAwarePlayerInputManager.
    /// </summary>
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

            // Immediately snap any sideways / backward velocity to zero so the player
            // doesn't coast in the wrong direction while the input filter takes over.
            if (_player != null && _player.enabled)
            {
                var forward = _player.localForward;
                float dot   = Vector3.Dot(_player.lateralVelocity, forward);
                _player.lateralVelocity = dot > 0f ? forward * dot : Vector3.zero;
            }

            Debug.Log($"[SlipEffect] {name} slipping for {duration}s.");
        }

        private void Update()
        {
            if (!_slipping) return;

            if (Time.time >= _endTime)
            {
                _slipping = false;
                return;
            }

            // Cancel upward velocity while airborne — prevents jumping during slip.
            if (_player != null && _player.enabled &&
                _player.verticalVelocity.y > 0f && !_player.isGrounded)
            {
                _player.verticalVelocity = Vector3.zero;
            }

            if (_aiInput != null)
                _aiInput.jumpQueued = false;
        }
    }
}
