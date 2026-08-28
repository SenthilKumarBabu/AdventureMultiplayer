using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drop-in replacement for PlayerInputManager that drives the banana-peel
    /// slide through PLAYER TWO's own CrouchPlayerState.
    ///
    /// While IsSlipping:
    ///   GetCrouch()             → true  (keeps player in CrouchPlayerState)
    ///   GetMovementDirection()  → zero  (prevents CrouchState → CrawlingState transition)
    ///   GetMovementCameraDir()  → forward (fallback if player somehow exits crouch)
    ///
    /// Normal gameplay: all overrides pass straight through to the base class.
    ///
    /// Setup: run Tools > Adventure Multiplayer > Setup Slip Input Manager once
    /// to replace PlayerInputManager with this on every player prefab.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Slip Aware Input Manager")]
    public class SlipAwarePlayerInputManager : PlayerInputManager
    {
        private SlipEffect _slip;

        protected override void Awake()
        {
            // Bot prefabs have no InputActionAsset — skip CacheActions entirely.
            // Bots use AIPlayerInputManager; these components should not be on bot prefabs,
            // but guard here so a forgotten instance doesn't crash on instantiation.
            if (actions == null) return;
            base.Awake();
        }

        protected override void InitializePlayer()
        {
            base.InitializePlayer();
            _slip = GetComponent<SlipEffect>();
        }

        // Keep the player locked in CrouchPlayerState for the full slide duration.
        public override bool GetCrouch()     => (_slip != null && _slip.IsSlipping) || base.GetCrouch();
        public override bool GetCrouchDown() => (_slip != null && _slip.IsSlipping) || base.GetCrouchDown();

        // Return zero movement direction while slipping so CrouchPlayerState's
        // canCrawl branch never fires and the player stays in the slide animation.
        public override Vector3 GetMovementDirection()
        {
            if (_slip != null && _slip.IsSlipping)
                return Vector3.zero;
            return base.GetMovementDirection();
        }

        // Fallback: if the player is somehow in a state that reads camera direction
        // (e.g. briefly after the state change), drive them forward.
        public override Vector3 GetMovementCameraDirection(
            out float magnitude, bool localSpace = true)
        {
            var dir = base.GetMovementCameraDirection(out magnitude, localSpace);

            if (_slip == null || !_slip.IsSlipping)
                return dir;

            magnitude = 1f;
            return m_player.localForward;
        }
    }
}
