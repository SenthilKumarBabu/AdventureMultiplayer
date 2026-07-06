using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drop-in replacement for PlayerInputManager that restricts movement to
    /// forward-only while a SlipEffect is active.
    ///
    /// PLAYER TWO reads GetMovementCameraDirection() inside AccelerateToInputDirection()
    /// every frame — before controller.Move — so filtering here prevents any sideways
    /// or backward velocity from ever being computed by the state machine.
    ///
    /// Setup: replace PlayerInputManager with this component on every player prefab
    /// (run Tools > Adventure Multiplayer > Setup Slip Input Manager).
    /// The `actions` InputActionAsset must be assigned in the Inspector.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Slip Aware Input Manager")]
    public class SlipAwarePlayerInputManager : PlayerInputManager
    {
        private SlipEffect _slip;

        protected override void InitializePlayer()
        {
            base.InitializePlayer();
            _slip = GetComponent<SlipEffect>();
        }

        public override Vector3 GetMovementCameraDirection(
            out float magnitude, bool localSpace = true)
        {
            var dir = base.GetMovementCameraDirection(out magnitude, localSpace);

            if (_slip == null || !_slip.IsSlipping || dir.sqrMagnitude < 0.001f)
                return dir;

            // Project the raw camera-space direction onto the player's local forward.
            // Any sideways or backward component is zeroed so the state machine only
            // ever computes forward (or zero) velocity while slipping.
            var forward = m_player.localForward;
            float dot   = Vector3.Dot(dir, forward);

            if (dot > 0f)
            {
                dir = forward * dot;   // forward component only, preserves magnitude scale
            }
            else
            {
                dir       = Vector3.zero;
                magnitude = 0f;
            }

            return dir;
        }
    }
}
