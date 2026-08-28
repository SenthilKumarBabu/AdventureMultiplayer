using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Singleton that survives the Lobby → gameplay scene load and carries the
    /// host's bot on/off choice and difficulty (set via the Lobby UI) to BotSpawner.
    /// Bot COUNT is not carried here — BotSpawner calculates it from the room size and
    /// connected human count so the room always ends up balanced.
    ///
    /// Lives on the same GameObject as CharacterPicker ("NetworkManager" in the
    /// Lobby scene), which already calls DontDestroyOnLoad — this component
    /// rides along automatically.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Bot Settings")]
    public class BotSettings : MonoBehaviour
    {
        public static BotSettings Instance { get; private set; }

        public bool          BotsEnabled { get; private set; } = true;
        public BotDifficulty Difficulty  { get; private set; } = BotDifficulty.Medium;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void SetBotsEnabled(bool value) => BotsEnabled = value;

        public void SetDifficulty(BotDifficulty value) => Difficulty = value;
    }
}
