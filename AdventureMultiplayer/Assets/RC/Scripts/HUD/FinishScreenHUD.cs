using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Displays the race results leaderboard and a Home button.
    /// Starts inactive — FinishScreenActivator enables it when all players finish.
    ///
    /// Setup:
    ///   - titleText    → "RACE RESULTS" header label
    ///   - resultsText  → multi-line label listing all players by rank
    ///   - homeButton   → Button that returns to lobby
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Finish Screen HUD")]
    public class FinishScreenHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI resultsText;
        [SerializeField] private Button          homeButton;
        [SerializeField] private float           slideInDuration = 0.5f;
        [SerializeField] private string          lobbySceneName  = "Lobby";

        private static readonly string[] k_ordinals =
            { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th" };

        private void OnEnable()
        {
            if (titleText != null)
                titleText.text = "RACE RESULTS";

            BuildResults();
            WireHomeButton();
            SlideIn();
        }

        private void BuildResults()
        {
            if (resultsText == null || RaceManager.Instance == null) return;

            var rm = RaceManager.Instance;
            var sorted = new System.Collections.Generic.List<RaceEntry>();
            for (int i = 0; i < rm.RaceEntries.Count; i++)
                sorted.Add(rm.RaceEntries[i]);
            sorted.Sort((a, b) => a.RacePosition.CompareTo(b.RacePosition));

            ulong localId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : ulong.MaxValue;

            var sb = new System.Text.StringBuilder();
            foreach (var entry in sorted)
            {
                string ordinal = entry.RacePosition >= 1 && entry.RacePosition <= k_ordinals.Length
                    ? k_ordinals[entry.RacePosition - 1]
                    : $"{entry.RacePosition}th";

                bool   isLocal = entry.ClientId == localId;
                string status  = entry.Finished ? ordinal : "DNF";
                string name    = isLocal ? "You" : $"Player {entry.ClientId}";

                if (isLocal)
                    sb.AppendLine($"<color=#FFD91A><b>{status}  {name}</b></color>");
                else
                    sb.AppendLine($"{status}  {name}");
            }

            resultsText.text = sb.ToString().TrimEnd();
        }

        private void WireHomeButton()
        {
            if (homeButton == null) return;
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(GoHome);
        }

        private void GoHome()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(lobbySceneName);
        }

        private void SlideIn()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -Screen.height);
            rt.DOAnchorPosY(0f, slideInDuration).SetEase(Ease.OutBack);
        }
    }
}
