using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Sits on an always-active GameObject (e.g. the HUD Canvas root).
    /// Enables the FinishPanel when all players finish so that FinishScreenHUD
    /// can run its OnEnable setup — the panel itself starts inactive in the scene.
    ///
    /// Setup: assign finishPanel to the inactive FinishPanel GameObject.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Finish Screen Activator")]
    public class FinishScreenActivator : MonoBehaviour
    {
        [SerializeField] private GameObject finishPanel;

        private bool m_subscribed;

        private void Start() => TrySubscribe();

        private void Update()
        {
            if (!m_subscribed) TrySubscribe();
        }

        private void OnDestroy()
        {
            if (m_subscribed && RaceManager.Instance != null)
                RaceManager.Instance.AllPlayersFinished.OnValueChanged -= OnAllFinished;
        }

        private void TrySubscribe()
        {
            if (RaceManager.Instance == null) return;
            RaceManager.Instance.AllPlayersFinished.OnValueChanged += OnAllFinished;
            m_subscribed = true;

            if (RaceManager.Instance.AllPlayersFinished.Value)
                Activate();
        }

        private void OnAllFinished(bool _, bool current)
        {
            if (current) Activate();
        }

        private void Activate()
        {
            if (finishPanel != null)
                finishPanel.SetActive(true);
        }
    }
}
