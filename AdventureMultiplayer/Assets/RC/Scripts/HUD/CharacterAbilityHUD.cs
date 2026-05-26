using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Configures the single Ability OnScreenButton at runtime based on the
    /// character the local player selected in the Lobby.
    ///
    /// Each character's unique ability maps to a specific Gamepad control path
    /// that the PLAYER TWO input actions are already bound to.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Character Ability HUD")]
    public class CharacterAbilityHUD : MonoBehaviour
    {
        [SerializeField] private OnScreenButton     abilityButton;
        [SerializeField] private TextMeshProUGUI    abilityLabel;

        private static readonly string[] ControlPaths =
        {
            "<Gamepad>/rightShoulder", // 0 Gale  — Glide
            "<Gamepad>/leftShoulder",  // 1 Blaze — Dash
            "<Gamepad>/buttonWest",    // 2 Bolt  — Sprint (Run)
            "<Gamepad>/buttonWest",    // 3 Bruno — Roll
            "<Gamepad>/leftTrigger",   // 4 Spike — Air Dive
        };

        private static readonly string[] AbilityNames =
        {
            "Glide",    // 0 Lily
            "Dash",     // 1 Blaze
            "Sprint",   // 2 Bolt
            "Roll",     // 3 Bruno
            "Air Dive", // 4 Spike
        };

        private void Start()
        {
            int index = CharacterPicker.Instance != null
                ? CharacterPicker.Instance.LocalSelectedIndex
                : 0;

            index = Mathf.Clamp(index, 0, ControlPaths.Length - 1);

            if (abilityButton != null)
            {
                // Disable first so the property setter doesn't call SetupInputControl
                // on top of an already-initialized control (causes Input System corruption).
                abilityButton.enabled = false;
                abilityButton.controlPath = ControlPaths[index];
                abilityButton.enabled = true;
            }

            if (abilityLabel != null)
                abilityLabel.text = AbilityNames[index];
        }
    }
}
