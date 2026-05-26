using TMPro;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Shows the local player's current race position (1st / 2nd / 3rd …).
    ///
    /// Setup:
    ///   - Add to a Canvas GameObject.
    ///   - Assign positionText (a TextMeshProUGUI).
    ///   - Reads RaceManager.Instance each frame — safe if RaceManager is null
    ///     (text stays hidden until the race starts).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Race Position HUD")]
    public class RacePositionHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI positionText;

        private static readonly string[] k_suffixes = { "", "st", "nd", "rd" };

        private void Update()
        {
            if (positionText == null) return;

            if (RaceManager.Instance == null || !RaceManager.Instance.RaceStarted.Value)
            {
                positionText.text = string.Empty;
                return;
            }

            int pos = RaceManager.Instance.GetLocalRacePosition();
            if (pos <= 0)
            {
                positionText.text = string.Empty;
                return;
            }

            string suffix = pos <= 3 ? k_suffixes[pos] : "th";
            positionText.text = $"{pos}<size=60%>{suffix}</size>";
        }
    }
}
