using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Top-center stopwatch badge showing elapsed race time (mm:ss). Hidden until the race
    /// starts; once the local player finishes it freezes at their finish time.
    ///
    /// Elapsed time is derived on every client from RaceManager.RaceStartServerTime (a synced
    /// NetworkVariable) minus the current NGO ServerTime, so it stays in step across machines.
    ///
    /// Scene hierarchy (auto-resolved by name — see RaceTimerBadge.prefab):
    ///   RaceTimerBadge — this component (always active)
    ///   └── Badge      — the visual, toggled by race state
    ///       ├── Icon
    ///       ├── TimeText  "01:23"
    ///       └── Outline
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Race Timer HUD")]
    public class RaceTimerHUD : MonoBehaviour
    {
        [SerializeField] private GameObject      badge;
        [SerializeField] private TextMeshProUGUI timeText;

        private void Awake()
        {
            if (badge == null)
            {
                Transform b = transform.Find("Badge");
                badge = b != null ? b.gameObject : null;
            }
            if (timeText == null)
            {
                Transform t = transform.Find("Badge/TimeText");
                if (t != null) timeText = t.GetComponent<TextMeshProUGUI>();
            }
            if (badge != null) badge.SetActive(false);
        }

        private void Update()
        {
            RaceManager rm = RaceManager.Instance;
            bool running = rm != null && rm.RaceStarted.Value
                           && rm.RaceStartServerTime.Value > 0d
                           && NetworkManager.Singleton != null;

            if (badge != null && badge.activeSelf != running) badge.SetActive(running);
            if (!running || timeText == null) return;

            double elapsed = TryGetLocalFinishTime(rm, out float finished)
                ? finished
                : NetworkManager.Singleton.ServerTime.Time - rm.RaceStartServerTime.Value;

            if (elapsed < 0d) elapsed = 0d;
            int secs = (int)elapsed;
            timeText.text = $"{secs / 60:00}:{secs % 60:00}";
        }

        private static bool TryGetLocalFinishTime(RaceManager rm, out float seconds)
        {
            seconds = 0f;
            if (NetworkManager.Singleton == null) return false;
            ulong localId = NetworkManager.Singleton.LocalClientId;
            for (int i = 0; i < rm.RaceEntries.Count; i++)
            {
                RaceEntry e = rm.RaceEntries[i];
                if (e.ClientId == localId && e.Finished)
                {
                    seconds = e.FinishTimeSeconds;
                    return true;
                }
            }
            return false;
        }
    }
}
