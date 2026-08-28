using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Overrides PLAYER TWO's MobileRig, which only shows the on-screen
    /// joystick/Jump/Ability buttons when compiled with UNITY_IOS or UNITY_ANDROID
    /// defined. That made VirtualGamepad appear on an Android-target Editor Play
    /// session but disappear on a Windows Standalone build — not a per-client bug,
    /// just two different compiled targets. This project wants the touch controls
    /// mouse-clickable during PC testing too, so this always enables the rig.
    /// </summary>
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    [AddComponentMenu("Adventure Multiplayer/Always-On Mobile Rig")]
    public class AlwaysOnMobileRig : MobileRig
    {
        protected override void CheckEnable()
        {
            EnableRig(true);
        }
    }
}
