using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Persistent singleton. Stores upgrade levels (Lv1-3) and coin balance per power-up.
    /// Levels are saved to PlayerPrefs; effects are all Lv1 until the upgrade UI is built.
    ///
    /// Call RegisterRaceResult(finishPosition, beatTargetTime) at race end to award coins.
    /// </summary>
    [AddComponentMenu("Rush Champions/Power-Up Upgrade Manager")]
    public class PowerUpUpgradeManager : MonoBehaviour
    {
        public static PowerUpUpgradeManager Instance { get; private set; }

        // ── FTUE ─────────────────────────────────────────────────────────────
        private const int    FtueRaceCount   = 3;
        private const string PrefFtueRaces   = "RC_FtueRacesLeft";

        // ── Coins ─────────────────────────────────────────────────────────────
        private const string PrefCoins = "RC_Coins";

        // ── Upgrades ──────────────────────────────────────────────────────────
        private const string PrefUpgradePrefix = "RC_Up_";

        // Coin rewards by finish position (1-indexed)
        private static readonly int[] PositionCoins = { 0, 100, 70, 50, 30 };
        private const int TimeBonusCoins = 25;

        // ── Lv2 upgrade costs ─────────────────────────────────────────────────
        private const int CostLv2 = 200;
        private const int CostLv3 = 500;

        // ── Runtime state ─────────────────────────────────────────────────────
        public int CoinBalance { get; private set; }
        public int FtueRacesRemaining { get; private set; }
        public bool IsFtue => FtueRacesRemaining > 0;

        private readonly Dictionary<PowerUpType, int> m_Levels = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Decrement FTUE counter when entering a race scene
            if (scene.name.StartsWith("DeathRun") || scene.name.StartsWith("Obstacle"))
            {
                if (FtueRacesRemaining > 0)
                {
                    FtueRacesRemaining--;
                    Save();
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns effective level for gameplay. During FTUE all power-ups act as Lv2.
        /// Once levels are implemented in effects, this is what PlayerPowerUpInventory reads.
        /// </summary>
        public int GetEffectiveLevel(PowerUpType type)
        {
            int stored = m_Levels.TryGetValue(type, out var l) ? l : 1;
            if (IsFtue) return Mathf.Max(stored, 2);
            return stored;
        }

        public int GetStoredLevel(PowerUpType type) =>
            m_Levels.TryGetValue(type, out var l) ? l : 1;

        public bool CanUpgrade(PowerUpType type)
        {
            int current = GetStoredLevel(type);
            if (current >= 3) return false;
            int cost = current == 1 ? CostLv2 : CostLv3;
            return CoinBalance >= cost;
        }

        public bool TryUpgrade(PowerUpType type)
        {
            if (!CanUpgrade(type)) return false;
            int current = GetStoredLevel(type);
            int cost = current == 1 ? CostLv2 : CostLv3;
            CoinBalance -= cost;
            m_Levels[type] = current + 1;
            Save();
            Debug.Log($"[UpgradeManager] {type} upgraded to Lv{m_Levels[type]}. Coins: {CoinBalance}");
            return true;
        }

        /// <summary>Call at race end. finishPosition is 1-based.</summary>
        public static void RegisterRaceResult(int finishPosition, bool beatTargetTime = false)
        {
            if (Instance == null) return;
            int coins = finishPosition >= 1 && finishPosition <= 4
                ? PositionCoins[finishPosition] : 30;
            if (beatTargetTime) coins += TimeBonusCoins;
            Instance.CoinBalance += coins;
            Instance.Save();
            Debug.Log($"[UpgradeManager] Race result pos={finishPosition} → +{coins} coins (total {Instance.CoinBalance}).");
        }

        // ── Persistence ───────────────────────────────────────────────────────

        private void Load()
        {
            CoinBalance        = PlayerPrefs.GetInt(PrefCoins, 0);
            FtueRacesRemaining = PlayerPrefs.GetInt(PrefFtueRaces, FtueRaceCount);

            foreach (PowerUpType t in System.Enum.GetValues(typeof(PowerUpType)))
                m_Levels[t] = PlayerPrefs.GetInt(PrefUpgradePrefix + (int)t, 1);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PrefCoins,    CoinBalance);
            PlayerPrefs.SetInt(PrefFtueRaces, FtueRacesRemaining);
            foreach (var kvp in m_Levels)
                PlayerPrefs.SetInt(PrefUpgradePrefix + (int)kvp.Key, kvp.Value);
            PlayerPrefs.Save();
        }
    }
}
