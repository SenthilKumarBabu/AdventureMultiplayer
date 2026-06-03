using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Sits on an always-active GameObject (e.g. the HUD Canvas root).
    ///
    /// When the local player finishes:
    ///   1. Shows "Finished!" via CheckpointHUD for 3 seconds.
    ///   2. Enters spectator mode so the player can watch others race.
    ///
    /// When ALL players finish (or the 30-second timeout expires):
    ///   Cancels any pending spectator transition, exits spectator mode,
    ///   hides the racing HUD, and shows the result panel.
    ///
    /// Setup: assign finishPanel to the inactive FinishPanel GameObject.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Finish Screen Activator")]
    public class FinishScreenActivator : MonoBehaviour
    {
        [SerializeField] private GameObject finishPanel;
        [SerializeField] private float      spectatorDelay = 3f;

        private bool                   m_subscribed;
        private bool                   m_localFinished;
        private CancellationTokenSource m_spectatorCts;

        private void Start()  => TrySubscribe();
        private void Update() { if (!m_subscribed) TrySubscribe(); }

        private void OnDestroy()
        {
            m_spectatorCts?.Cancel();
            m_spectatorCts?.Dispose();
            if (!m_subscribed || RaceManager.Instance == null) return;
            RaceManager.Instance.AllPlayersFinished.OnValueChanged -= OnAllFinished;
            RaceManager.Instance.RaceEntries.OnListChanged         -= OnRaceEntriesChanged;
        }

        private void TrySubscribe()
        {
            if (RaceManager.Instance == null) return;
            RaceManager.Instance.AllPlayersFinished.OnValueChanged += OnAllFinished;
            RaceManager.Instance.RaceEntries.OnListChanged         += OnRaceEntriesChanged;
            m_subscribed = true;

            if (RaceManager.Instance.AllPlayersFinished.Value)
                ShowResults();
            else
                CheckLocalPlayerFinished();
        }

        private void OnAllFinished(bool _, bool current)
        {
            if (current) ShowResults();
        }

        private void OnRaceEntriesChanged(NetworkListEvent<RaceEntry> _)
        {
            if (!m_localFinished)
                CheckLocalPlayerFinished();
        }

        private void CheckLocalPlayerFinished()
        {
            if (m_localFinished) return;
            if (NetworkManager.Singleton == null) return;

            ulong localId = NetworkManager.Singleton.LocalClientId;
            var rm = RaceManager.Instance;
            for (int i = 0; i < rm.RaceEntries.Count; i++)
            {
                var entry = rm.RaceEntries[i];
                if (entry.ClientId == localId && entry.Finished && entry.FinishTimeSeconds > 0f)
                {
                    m_localFinished = true;
                    EnterSpectatorAfterDelayAsync().Forget();
                    return;
                }
            }
        }

        private async UniTaskVoid EnterSpectatorAfterDelayAsync()
        {
            m_spectatorCts?.Cancel();
            m_spectatorCts = new CancellationTokenSource();

            // Show "Finished!" the same way as a checkpoint notification.
            CheckpointHUD.Instance?.Show("Finished!");
            Debug.Log($"[FinishScreenActivator] Finished! Entering spectator in {spectatorDelay}s.");

            try
            {
                await UniTask.Delay(
                    (int)(spectatorDelay * 1000),
                    cancellationToken: m_spectatorCts.Token);
            }
            catch (OperationCanceledException)
            {
                // ShowResults() fired before the delay expired — result screen is already showing.
                return;
            }

            var spectator = SpectatorController.Instance
                ?? FindFirstObjectByType<SpectatorController>();

            if (spectator != null)
                spectator.EnterSpectatorMode();
            else
                Debug.LogWarning("[FinishScreenActivator] SpectatorController not found — check that its GameObject is active on start.");
        }

        private void ShowResults()
        {
            // Cancel any pending spectator transition (player finished within the delay window).
            m_spectatorCts?.Cancel();

            var spectator = SpectatorController.Instance
                ?? FindFirstObjectByType<SpectatorController>();

            spectator?.ExitSpectatorMode();
            spectator?.HideRacingHUD();
            spectator?.DisableLocalInput();

            if (finishPanel != null)
                finishPanel.SetActive(true);
        }
    }
}
