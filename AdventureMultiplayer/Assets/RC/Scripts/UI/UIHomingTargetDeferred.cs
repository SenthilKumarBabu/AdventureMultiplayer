using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Companion script for UIHomingTarget.
    /// Disables UIHomingTarget before its Start() runs (preventing a NullReferenceException
    /// when Level.instance.player is not yet set), then re-enables it once the player spawns.
    ///
    /// Add this component to the same GameObject as UIHomingTarget.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/UI Homing Target Deferred")]
    public class UIHomingTargetDeferred : MonoBehaviour
    {
        private UIHomingTarget m_homingTarget;

        private void Awake()
        {
            m_homingTarget = GetComponent<UIHomingTarget>();

            if (m_homingTarget == null)
            {
                Debug.LogWarning("[UIHomingTargetDeferred] No UIHomingTarget found on this GameObject.");
                return;
            }

            // Disable before Start() runs so it doesn't crash on null player.
            m_homingTarget.enabled = false;

            if (Level.instance != null)
                Level.instance.onPlayerChanged.AddListener(OnPlayerReady);
            else
                Debug.LogWarning("[UIHomingTargetDeferred] Level.instance is null in Awake.");
        }

        private void OnPlayerReady(Player p)
        {
            Level.instance.onPlayerChanged.RemoveListener(OnPlayerReady);

            if (m_homingTarget != null)
            {
                Debug.Log("[UIHomingTargetDeferred] Player ready — enabling UIHomingTarget.");
                m_homingTarget.enabled = true;
            }
        }

        private void OnDestroy()
        {
            if (Level.instance != null)
                Level.instance.onPlayerChanged.RemoveListener(OnPlayerReady);
        }
    }
}
