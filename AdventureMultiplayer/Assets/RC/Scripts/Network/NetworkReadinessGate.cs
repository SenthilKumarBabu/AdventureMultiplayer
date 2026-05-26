using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Prevents PLAYER TWO camera and UI systems from crashing when Level.instance.player
    /// is null at scene Start() (which happens in multiplayer because the player spawns
    /// after the scene has loaded).
    ///
    /// How it works:
    ///   - Runs its own Start() before other scripts (DefaultExecutionOrder = -100).
    ///   - At that point all Awake() calls are done, so Level.instance is valid,
    ///     but Level.instance.player is still null (player hasn't spawned yet).
    ///   - Disables PlayerCameraManager and all UIHomingTarget components so their
    ///     own Start() is deferred.
    ///   - Subscribes to Level.instance.onPlayerChanged.
    ///   - Re-enables the components when the player is assigned; Unity then calls
    ///     their Start() on the next frame, at which point the player is available.
    ///
    /// Setup: add this component to any persistent scene GameObject (e.g. GameManager).
    /// No other Inspector changes are needed.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Adventure Multiplayer/Network Readiness Gate")]
    public class NetworkReadinessGate : MonoBehaviour
    {
        private PlayerCameraManager m_cameraManager;
        private UIHomingTarget[]    m_homingTargets;

        private void Start()
        {
            if (Level.instance == null)
            {
                Debug.LogWarning("[NetworkReadinessGate] Level.instance is null — cannot defer init.");
                return;
            }

            // If a player is already present (e.g. single-player / offline mode), do nothing.
            if (Level.instance.player != null)
                return;

            Debug.Log("[NetworkReadinessGate] No player yet — deferring camera and UI init.");

            // Disable PlayerCameraManager before its Start() runs.
            m_cameraManager = FindAnyObjectByType<PlayerCameraManager>();
            if (m_cameraManager != null)
                m_cameraManager.enabled = false;

            // Disable every UIHomingTarget before their Start() runs.
            m_homingTargets = FindObjectsByType<UIHomingTarget>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in m_homingTargets)
                t.enabled = false;

            Level.instance.onPlayerChanged.AddListener(OnPlayerReady);
        }

        private void OnPlayerReady(Player p)
        {
            Level.instance.onPlayerChanged.RemoveListener(OnPlayerReady);
            Debug.Log("[NetworkReadinessGate] Player ready — re-enabling camera and UI systems.");

            if (m_cameraManager != null)
                m_cameraManager.enabled = true;

            if (m_homingTargets != null)
                foreach (var t in m_homingTargets)
                    if (t != null) t.enabled = true;
        }

        private void OnDestroy()
        {
            if (Level.instance != null)
                Level.instance.onPlayerChanged.RemoveListener(OnPlayerReady);
        }
    }
}
