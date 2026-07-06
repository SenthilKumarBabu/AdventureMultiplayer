using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Displays the race results and a Home button.
    /// Starts inactive — FinishScreenActivator enables it when the local player finishes.
    ///
    /// While other players are still racing the Home button is locked and a live
    /// countdown ("Waiting for others... 28s") is shown. Once AllPlayersFinished
    /// fires (everyone finished or timed out) the button unlocks and results refresh.
    ///
    /// Setup (Inspector):
    ///   titleText   → "RACE RESULTS" header
    ///   timeText    → large label for the local player's finish time
    ///   waitingText → small label shown while others are still racing
    ///   resultsText → multi-line leaderboard
    ///   homeButton  → returns to lobby (locked until all finish)
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Finish Screen HUD")]
    public class FinishScreenHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI waitingText;
        [SerializeField] private TextMeshProUGUI resultsText;
        [SerializeField] private Button          homeButton;
        [SerializeField] private float           slideInDuration = 0.5f;
        [SerializeField] private string          lobbySceneName  = "Lobby";

        private static readonly string[] k_ordinals =
            { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th" };

        private static readonly string[] k_characterNames =
            { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };

        private bool m_allFinished;

        private void OnEnable()
        {
            if (titleText != null) titleText.text = "RACE RESULTS";

            m_allFinished = RaceManager.Instance != null && RaceManager.Instance.AllPlayersFinished.Value;

            // Ensure HUD is hidden and input is disabled even for DNF players who
            // never entered spectator mode (SpectatorController guards against double-calls).
            SpectatorController.Instance?.HideRacingHUD();
            SpectatorController.Instance?.DisableLocalInput();

            BuildResults();
            RefreshWaitingState();
            WireHomeButton();
            SlideIn();

            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.AllPlayersFinished.OnValueChanged += OnAllPlayersFinished;
                RaceManager.Instance.RaceEntries.OnListChanged         += OnRaceEntriesChanged;
                RaceManager.Instance.FinishRecords.OnListChanged       += OnFinishRecordsChanged;
            }

            // On pure clients, the NetworkList backing store may still be stale when the
            // panel first opens. Rebuild every frame for a few seconds so any late-arriving
            // NGO updates (healed Finished=true entries) are reflected without user interaction.
            RebuildUntilStableAsync().Forget();
        }

        private async UniTaskVoid RebuildUntilStableAsync()
        {
            float deadline = Time.time + 3f;
            while (Time.time < deadline)
            {
                await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
                if (RaceManager.Instance == null) break;
                m_allFinished = RaceManager.Instance.AllPlayersFinished.Value;
                BuildResults();
                RefreshWaitingState();
            }
        }

        private void OnDisable()
        {
            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.AllPlayersFinished.OnValueChanged -= OnAllPlayersFinished;
                RaceManager.Instance.RaceEntries.OnListChanged         -= OnRaceEntriesChanged;
                RaceManager.Instance.FinishRecords.OnListChanged       -= OnFinishRecordsChanged;
            }
        }

        private void Update()
        {
            if (m_allFinished || RaceManager.Instance == null) return;

            double endTime = RaceManager.Instance.CountdownEndTime.Value;
            if (endTime <= 0 || NetworkManager.Singleton == null) return;

            double remaining = endTime - NetworkManager.Singleton.ServerTime.Time;
            int secs = Mathf.Max(0, (int)remaining);

            if (waitingText != null)
                waitingText.text = $"Waiting for others...  {secs}s";
        }

        private void OnAllPlayersFinished(bool _, bool current)
        {
            if (!current) return;
            m_allFinished = true;
            BuildResults();
            RefreshWaitingState();
        }

        private void OnRaceEntriesChanged(NetworkListEvent<RaceEntry> _)   => BuildResults();
        private void OnFinishRecordsChanged(NetworkListEvent<FinishRecord> _) => BuildResults();

        // ── UI ────────────────────────────────────────────────────────────────

        private void RefreshWaitingState()
        {
            if (homeButton  != null) homeButton.interactable         = m_allFinished;
            if (waitingText != null) waitingText.gameObject.SetActive(!m_allFinished);
        }

        private void BuildResults()
        {
            if (RaceManager.Instance == null) return;

            var rm      = RaceManager.Instance;
            ulong localId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : ulong.MaxValue;

            // Build a lookup from FinishRecords — this uses Add (never [i]=value) so it has
            // no NGO staging delay and is correct on both host and remote clients.
            var finishLookup = new System.Collections.Generic.Dictionary<ulong, FinishRecord>();
            for (int i = 0; i < rm.FinishRecords.Count; i++)
                finishLookup[rm.FinishRecords[i].ClientId] = rm.FinishRecords[i];

            // Local player's own time.
            if (timeText != null)
            {
                timeText.text = "";
                if (finishLookup.TryGetValue(localId, out var localRec) && localRec.FinishTimeSeconds > 0f)
                    timeText.text = $"YOUR TIME\n{FormatTime(localRec.FinishTimeSeconds)}";
                else if (rm.TryGetServerFinishTime(localId, out float serverTime) && serverTime > 0f)
                    timeText.text = $"YOUR TIME\n{FormatTime(serverTime)}";
            }

            if (resultsText == null) return;

            // Deduplicate by ClientId: keep only the best entry per player.
            // Priority: finished with real time > finished (DNF/timeout) > still racing.
            var bestByClient = new System.Collections.Generic.Dictionary<ulong, RaceEntry>();
            for (int i = 0; i < rm.RaceEntries.Count; i++)
            {
                var e = rm.RaceEntries[i];
                if (!bestByClient.TryGetValue(e.ClientId, out var existing) || EntryScore(e) > EntryScore(existing))
                    bestByClient[e.ClientId] = e;
            }

            var sorted = new System.Collections.Generic.List<RaceEntry>(bestByClient.Values);
            sorted.Sort((a, b) => a.RacePosition.CompareTo(b.RacePosition));

            var sb = new System.Text.StringBuilder();
            foreach (var entry in sorted)
            {
                string ordinal = entry.RacePosition >= 1 && entry.RacePosition <= k_ordinals.Length
                    ? k_ordinals[entry.RacePosition - 1]
                    : $"{entry.RacePosition}th";

                bool   isLocal = entry.ClientId == localId;
                int    charIdx = CharacterPicker.Instance != null
                    ? CharacterPicker.Instance.GetSelection(entry.ClientId) : 0;
                string charName = charIdx >= 0 && charIdx < k_characterNames.Length
                    ? k_characterNames[charIdx] : "Player";
                string name    = isLocal ? $"You ({charName})" : charName;

                // FinishRecords is the authoritative source — works on both host and remote clients.
                string status;
                if (finishLookup.TryGetValue(entry.ClientId, out var rec) && rec.FinishTimeSeconds > 0f)
                    status = $"Complete  {ordinal}  {FormatTime(rec.FinishTimeSeconds)}";
                else if (!entry.Finished && !finishLookup.ContainsKey(entry.ClientId))
                    status = m_allFinished ? "DNF" : "Racing";
                else
                    status = "DNF";

                if (isLocal)
                    sb.AppendLine($"<color=#FFD91A><b>{name}  —  {status}</b></color>");
                else
                    sb.AppendLine($"{name}  —  {status}");
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

        // Higher = better entry for the same client (finished with time > finished DNF > still racing).
        private static int EntryScore(RaceEntry e)
        {
            if (e.Finished && e.FinishTimeSeconds > 0f) return 2;
            if (e.Finished) return 1;
            return 0;
        }

        private static string FormatTime(float seconds)
        {
            int   m = (int)(seconds / 60);
            float s = seconds % 60;
            return $"{m}:{s:00.00}";
        }
    }
}
