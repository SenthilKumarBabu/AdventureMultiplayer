using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Applied to a player when they are hit by StunBolt, Freeze, or DecoyBox.
    /// Zeros lateral and vertical velocity each LateUpdate for the stun duration.
    /// Also clears AI input if an AIPlayerInputManager is present.
    /// Add to every player prefab.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Rush Champions/Stun Effect")]
    public class StunEffect : MonoBehaviour
    {
        public bool IsStunned => _stunned;

        private Player               _player;
        private AIPlayerInputManager _aiInput;
        private bool                 _stunned;
        private float                _endTime;

        private void Awake()
        {
            _player  = GetComponent<Player>();
            _aiInput = GetComponent<AIPlayerInputManager>();
        }

        /// <summary>Activate stun for <paramref name="duration"/> seconds. Safe to call multiple times — refreshes.</summary>
        public void Apply(float duration)
        {
            _stunned = true;
            _endTime = Time.time + duration;
            Debug.Log($"[StunEffect] {name} stunned for {duration}s.");
        }

        private void LateUpdate()
        {
            if (!_stunned) return;

            if (Time.time >= _endTime)
            {
                _stunned = false;
                return;
            }

            if (_player != null && _player.enabled)
            {
                _player.lateralVelocity = Vector3.zero;
                if (_player.isGrounded)
                    _player.verticalVelocity = Vector3.zero;
                else if (_player.verticalVelocity.y > 0f)
                    _player.verticalVelocity = Vector3.zero;
            }

            if (_aiInput != null)
            {
                _aiInput.desiredMoveDirection = Vector3.zero;
                _aiInput.jumpQueued           = false;
                _aiInput.dashQueued           = false;
                _aiInput.spinQueued           = false;
                _aiInput.stompQueued          = false;
            }
        }
    }
}
