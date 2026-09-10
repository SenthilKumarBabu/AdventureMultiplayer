using PLAYERTWO.PlatformerProject;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Top-left player nameplate: circular character avatar (colour + initials), the character
    /// name, and a health bar with a "current/max" readout. Health drives the bar every frame;
    /// the avatar/name are set once the local player + character selection resolve.
    ///
    /// Scene hierarchy (auto-resolved by name — see Nameplate.prefab):
    ///   Nameplate — this component
    ///   ├── Avatar/Disc      Image, tinted per character
    ///   ├── Avatar/Initial   TMP, "Ga" / "Bl" / …
    ///   └── Info
    ///       ├── NameText     TMP, coloured character name
    ///       └── HPBar        Slider (fill = HPBar/Fill Area/Fill) + HPText overlay
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Player Nameplate HUD")]
    public class HPHUD : MonoBehaviour
    {
        [SerializeField] private Slider          slider;
        [SerializeField] private Image           fill;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image           avatarDisc;
        [SerializeField] private TextMeshProUGUI initialText;

        private static readonly Color ColorFull = new(0.24f, 0.82f, 0.30f, 0.95f);
        private static readonly Color ColorLow  = new(0.85f, 0.22f, 0.22f, 0.95f);

        private static readonly string[] k_names    = { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };
        private static readonly string[] k_initials = { "Ga",   "Bl",    "Bo",   "Br",    "Sp"    };
        private static readonly Color[]  k_colors   =
        {
            new(0.31f, 0.83f, 1.00f), // Gale
            new(1.00f, 0.42f, 0.21f), // Blaze
            new(1.00f, 0.92f, 0.00f), // Bolt
            new(0.30f, 0.85f, 0.39f), // Bruno
            new(0.78f, 0.49f, 1.00f), // Spike
        };

        private Player _player;
        private bool   _identitySet;

        private void Start()
        {
            if (slider != null) slider.interactable = false;
            ApplyIdentity(CharacterPicker.Instance != null ? CharacterPicker.Instance.LocalSelectedIndex : 0);
        }

        private void Update()
        {
            if (!_identitySet && CharacterPicker.Instance != null)
                ApplyIdentity(CharacterPicker.Instance.LocalSelectedIndex);

            if (_player == null)
            {
                // The NetworkObject that belongs to THIS client — not FindFirstObjectByType,
                // which always returns the first player spawned on every machine.
                var localObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
                if (localObj != null) _player = localObj.GetComponent<Player>();
            }

            if (_player == null) return;

            var health = _player.health;
            float t = health.max > 0 ? (float)health.current / health.max : 0f;

            if (slider != null) slider.value = t;
            if (fill != null)   fill.color = Color.Lerp(ColorLow, ColorFull, t);
            if (hpText != null)  hpText.text = $"{Mathf.Max(0, health.current)}/{health.max}";
        }

        private void ApplyIdentity(int index)
        {
            if (index < 0 || index >= k_names.Length) index = 0;

            if (nameText != null)
                nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(k_colors[index])}>{k_names[index]}</color>";
            if (initialText != null) initialText.text = k_initials[index];
            if (avatarDisc != null)  avatarDisc.color = k_colors[index];

            _identitySet = CharacterPicker.Instance != null;
        }
    }
}
