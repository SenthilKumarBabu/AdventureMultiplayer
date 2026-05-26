using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Hooks mode-select buttons to GameModeManager.
    /// Attach to the ModeSelectCanvas root. Wire buttons in Inspector.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Mode Select UI")]
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] private Button playWithAIButton;
        [SerializeField] private Button multiplayerButton;

        private void Awake()
        {
            playWithAIButton.onClick.AddListener(() => GameModeManager.Instance.StartAIMode());
            multiplayerButton.onClick.AddListener(() => GameModeManager.Instance.StartMultiplayerMode());
        }
    }
}
