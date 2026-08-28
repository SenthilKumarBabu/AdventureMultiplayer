using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Live top-right leaderboard listing every active racer (human + bot), ordered by
    /// current race position. Rebuilds only when RaceManager reports an actual position
    /// change (overtake / finish), so rows reorder in real time without per-frame churn.
    ///
    /// Setup:
    ///   - Add to a Canvas GameObject (e.g. alongside RacePositionHUD on the RaceHUD panel).
    ///   - Assign listText (a TextMeshProUGUI).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Leaderboard HUD")]
    public class LeaderboardHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI listText;

        private static readonly string[] k_ordinals =
            { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th" };

        private static readonly string[] k_characterNames =
            { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };

        // Per-character colors so each racer's name is visually distinct on the leaderboard —
        // must stay index-aligned with k_characterNames and match PowerUpFeedHUD's palette.
        private static readonly string[] k_characterColors =
            { "#4FD3FF", "#FF6B35", "#FFEA00", "#4CD964", "#C77DFF" };

        private const ulong k_firstBotId = 10000UL;

        private RaceManager m_raceManager;
        private readonly List<RaceEntry> m_sorted = new();

        private void Awake()
        {
            if (listText == null) listText = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (listText != null) listText.text = string.Empty;
            WaitForRaceManagerAsync().Forget();
        }

        private void OnDisable()
        {
            if (m_raceManager != null)
                m_raceManager.RaceEntries.OnListChanged -= OnRaceEntriesChanged;
            m_raceManager = null;
        }

        private async UniTaskVoid WaitForRaceManagerAsync()
        {
            await UniTask.WaitUntil(() => RaceManager.Instance != null,
                cancellationToken: destroyCancellationToken);

            m_raceManager = RaceManager.Instance;
            m_raceManager.RaceEntries.OnListChanged += OnRaceEntriesChanged;
            Rebuild();
        }

        private void OnRaceEntriesChanged(NetworkListEvent<RaceEntry> _) => Rebuild();

        private void Rebuild()
        {
            if (listText == null || m_raceManager == null) return;

            ulong localId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : ulong.MaxValue;

            var entries = m_raceManager.RaceEntries;
            m_sorted.Clear();
            for (int i = 0; i < entries.Count; i++)
                m_sorted.Add(entries[i]);
            m_sorted.Sort((a, b) => a.RacePosition.CompareTo(b.RacePosition));

            var sb = new StringBuilder();
            foreach (var entry in m_sorted)
            {
                string ordinal = entry.RacePosition >= 1 && entry.RacePosition <= k_ordinals.Length
                    ? k_ordinals[entry.RacePosition - 1]
                    : $"{entry.RacePosition}th";

                bool isLocal = entry.ClientId == localId;
                bool isBot   = entry.ClientId >= k_firstBotId;

                int    charIdx  = CharacterPicker.Instance != null
                    ? CharacterPicker.Instance.GetSelection(entry.ClientId) : 0;
                string charName = charIdx >= 0 && charIdx < k_characterNames.Length
                    ? k_characterNames[charIdx] : "Player";
                string color = charIdx >= 0 && charIdx < k_characterColors.Length
                    ? k_characterColors[charIdx] : "#FFFFFF";
                string coloredName = $"<color={color}>{charName}</color>";

                string name = isLocal ? $"{coloredName} (You)" : isBot ? $"{coloredName} (Bot)" : coloredName;
                if (entry.Finished) name += "  ✓";

                if (isLocal)
                    sb.AppendLine($"<b>{ordinal}  {name}</b>");
                else
                    sb.AppendLine($"{ordinal}  {name}");
            }

            listText.text = sb.ToString().TrimEnd();
        }
    }
}
