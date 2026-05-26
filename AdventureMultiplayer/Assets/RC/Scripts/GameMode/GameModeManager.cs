using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    public enum GameMode { None, AI, Multiplayer }

    /// <summary>
    /// Orchestrates scene setup based on the mode chosen in ModeSelectUI.
    /// humanPlayer stays active at scene start so PlayerCamera can initialise.
    /// aiBot starts disabled and is only activated when AI mode is chosen.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Game Mode Manager")]
    public class GameModeManager : MonoBehaviour
    {
        [SerializeField] private GameObject humanPlayer;
        [SerializeField] private GameObject aiBot;
        [SerializeField] private GameObject modeSelectCanvas;

        public static GameModeManager Instance { get; private set; }
        public GameMode CurrentMode { get; private set; } = GameMode.None;

        private void Awake()
        {
            Instance = this;

            // Auto-find humanPlayer: a Player without AIPlayerInputManager.
            if (humanPlayer == null)
            {
                foreach (var p in FindObjectsByType<Player>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (p.GetComponent<AIPlayerInputManager>() == null)
                    {
                        humanPlayer = p.gameObject;
                        break;
                    }
                }
            }

            // Auto-find aiBot: a Player WITH AIPlayerInputManager (avoids AIBotBrain type dependency).
            if (aiBot == null)
            {
                foreach (var p in FindObjectsByType<Player>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (p.GetComponent<AIPlayerInputManager>() != null)
                    {
                        aiBot = p.gameObject;
                        break;
                    }
                }
            }

            if (humanPlayer == null)
                Debug.LogError("[GameModeManager] Could not find human player.");
            if (aiBot == null)
                Debug.LogError("[GameModeManager] Could not find AI bot.");

            // Keep humanPlayer active so PlayerCamera can initialise on scene start.
            // Disable only the AI bot until a mode is chosen.
            aiBot?.SetActive(false);
        }

        /// <summary>Called by ModeSelectUI when the player picks AI mode.</summary>
        public void StartAIMode()
        {
            CurrentMode = GameMode.AI;
            modeSelectCanvas.SetActive(false);

            if (aiBot == null)
            {
                Debug.LogError("[GameModeManager] aiBot is null — cannot start AI mode.");
                return;
            }

            aiBot.SetActive(true);

            // Tell the Level singleton which Player is the human so the camera follows correctly.
            Level.instance.player = humanPlayer.GetComponent<Player>();
        }

        /// <summary>Called by ModeSelectUI when the player picks Multiplayer mode.</summary>
        public void StartMultiplayerMode()
        {
            CurrentMode = GameMode.Multiplayer;
            modeSelectCanvas.SetActive(false);

            // Disable local player — network-spawned players take over.
            humanPlayer?.SetActive(false);
        }

        /// <summary>Returns to the mode select screen (e.g. from the Back button in NetworkUI).</summary>
        public void ShowModeSelect()
        {
            CurrentMode = GameMode.None;
            modeSelectCanvas.SetActive(true);
        }
    }
}
