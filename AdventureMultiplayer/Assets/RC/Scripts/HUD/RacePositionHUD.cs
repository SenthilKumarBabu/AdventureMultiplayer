using TMPro;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Top-right placement badge — the local player's current race position over the racer count
    /// (e.g. "3rd" / "/4"). The badge visual stays hidden until the race is actually running.
    ///
    /// Scene hierarchy (auto-resolved by name — see PositionBadge.prefab):
    ///   PositionBadge  — this component (always active)
    ///   └── Badge      — the visual, toggled by race state
    ///       ├── OrdinalText   "3rd"
    ///       ├── TotalText     "/4"
    ///       └── Outline
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Race Position HUD")]
    public class RacePositionHUD : MonoBehaviour
    {
        [SerializeField] private GameObject      badge;        // visual, hidden until racing
        [SerializeField] private TextMeshProUGUI ordinalText;  // "3rd"
        [SerializeField] private TextMeshProUGUI totalText;    // "/4"

        private static readonly string[] k_suffixes = { "", "st", "nd", "rd" };

        private void Awake()
        {
            if (badge == null)
            {
                Transform b = transform.Find("Badge");
                badge = b != null ? b.gameObject : null;
            }
            if (ordinalText == null) ordinalText = FindTmp("Badge/OrdinalText");
            if (totalText   == null) totalText   = FindTmp("Badge/TotalText");
            if (badge != null) badge.SetActive(false);
        }

        private TextMeshProUGUI FindTmp(string path)
        {
            Transform t = transform.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private void Update()
        {
            bool racing = RaceManager.Instance != null && RaceManager.Instance.RaceStarted.Value;
            int  pos    = racing ? RaceManager.Instance.GetLocalRacePosition() : 0;
            int  total  = racing ? RaceManager.Instance.RaceEntries.Count      : 0;

            bool show = racing && pos > 0;
            if (badge != null && badge.activeSelf != show) badge.SetActive(show);
            if (!show) return;

            string suffix = pos <= 3 ? k_suffixes[pos] : "th";
            if (ordinalText != null) ordinalText.text = $"{pos}<size=55%>{suffix}</size>";
            if (totalText   != null) totalText.text   = total > 0 ? $"/{total}" : string.Empty;
        }
    }
}
