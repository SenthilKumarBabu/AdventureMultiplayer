using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Live top-right leaderboard listing every active racer (human + bot), ordered by current
    /// race position. Rebuilds only when RaceManager reports an actual position change
    /// (overtake / finish), so rows reorder in real time without per-frame churn.
    ///
    /// One cloned row per racer — rank · coloured character name (+ "(You)"/"(Bot)") · a derived
    /// score (checkpoints reached × pointsPerCheckpoint; the game is a race, there is no real
    /// score system). The local player's row shows a highlight band.
    ///
    /// Scene hierarchy this expects (auto-resolved by name — see LeaderboardPanel.prefab):
    ///   LeaderboardPanel  — Image bg, VerticalLayoutGroup, ContentSizeFitter, this component
    ///   ├── Header        — crown icon + "LIVE LEADERBOARD"
    ///   └── Body          — VerticalLayoutGroup + ContentSizeFitter  (== rowsContainer)
    ///       └── RowTemplate — inactive; HLG row with children RankText / NameText / ScoreText
    ///                          and an Image on the root used as the "(You)" highlight band
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Leaderboard HUD")]
    public class LeaderboardHUD : MonoBehaviour
    {
        [SerializeField] private RectTransform rowsContainer;          // "Body"
        [SerializeField] private RectTransform rowTemplate;            // inactive row under Body
        [SerializeField] private int           pointsPerCheckpoint = 100;
        [SerializeField] private Color         youHighlight = new(0.30f, 0.78f, 1f, 0.16f);

        private static readonly string[] k_characterNames =
            { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };

        // Per-character colors so each racer's name is visually distinct — must stay index-aligned
        // with k_characterNames and match PowerUpFeedHUD's palette.
        private static readonly string[] k_characterColors =
            { "#4FD3FF", "#FF6B35", "#FFEA00", "#4CD964", "#C77DFF" };

        private const ulong k_firstBotId = 10000UL;

        private RaceManager m_raceManager;
        private readonly List<RaceEntry>  m_sorted = new();
        private readonly List<GameObject> m_rows   = new();

        private void Awake()
        {
            if (rowsContainer == null) rowsContainer = transform.Find("Body") as RectTransform;
            if (rowsContainer == null) rowsContainer = transform as RectTransform;
            if (rowTemplate == null && rowsContainer != null)
                rowTemplate = rowsContainer.Find("RowTemplate") as RectTransform;
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        }

        private void OnEnable() => WaitForRaceManagerAsync().Forget();

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
            if (rowsContainer == null || rowTemplate == null || m_raceManager == null) return;

            ulong localId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : ulong.MaxValue;

            var entries = m_raceManager.RaceEntries;
            m_sorted.Clear();
            for (int i = 0; i < entries.Count; i++) m_sorted.Add(entries[i]);
            m_sorted.Sort((a, b) => a.RacePosition.CompareTo(b.RacePosition));

            EnsureRows(m_sorted.Count);

            for (int i = 0; i < m_sorted.Count; i++)
            {
                RaceEntry e = m_sorted[i];

                bool isLocal = e.ClientId == localId;
                bool isBot   = e.ClientId >= k_firstBotId;

                int    charIdx  = CharacterPicker.Instance != null
                    ? CharacterPicker.Instance.GetSelection(e.ClientId) : 0;
                string charName = charIdx >= 0 && charIdx < k_characterNames.Length
                    ? k_characterNames[charIdx] : "Player";
                string color = charIdx >= 0 && charIdx < k_characterColors.Length
                    ? k_characterColors[charIdx] : "#FFFFFF";

                string tag  = isLocal ? "  <size=75%>(You)</size>"
                            : isBot   ? "  <size=75%>(Bot)</size>" : "";
                string name = $"<color={color}>{charName}</color>{tag}";
                if (e.Finished) name += "  ✓";

                int score = Mathf.Max(0, (e.CheckpointIndex + 1) * pointsPerCheckpoint);

                FillRow(m_rows[i], e.RacePosition.ToString(), name, score.ToString(), isLocal);
            }

            for (int i = m_sorted.Count; i < m_rows.Count; i++)
                m_rows[i].SetActive(false);
        }

        private void EnsureRows(int count)
        {
            while (m_rows.Count < count)
            {
                GameObject go = Instantiate(rowTemplate.gameObject, rowsContainer);
                go.name = $"Row{m_rows.Count}";
                m_rows.Add(go);
            }
        }

        private void FillRow(GameObject row, string rank, string name, string score, bool isLocal)
        {
            row.SetActive(true);

            SetText(row, "RankText",  rank);
            SetText(row, "NameText",  name);
            SetText(row, "ScoreText", score);

            if (row.TryGetComponent(out Image band))
            {
                band.enabled = isLocal;
                if (isLocal) band.color = youHighlight;
            }
        }

        private static void SetText(GameObject row, string child, string value)
        {
            Transform t = row.transform.Find(child);
            if (t != null && t.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = value;
        }
    }
}
