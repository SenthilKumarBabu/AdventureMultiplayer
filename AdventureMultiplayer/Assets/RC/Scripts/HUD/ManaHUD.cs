using PLAYERTWO.PlatformerProject;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Mana HUD")]
    public class ManaHUD : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image  fill;

        private static readonly Color ColorReady = new Color(0.4f, 0.4f, 1f,  0.95f); // blue — enough mana
        private static readonly Color ColorLow   = new Color(0.6f, 0.2f, 0.2f, 0.8f); // red  — below threshold

        private ManaSystem _mana;

        private void Start()
        {
            if (slider != null) slider.interactable = false;
        }

        private void Update()
        {
            if (_mana == null)
            {
                var player = FindFirstObjectByType<Player>();
                if (player != null) _mana = player.GetComponent<ManaSystem>();
            }
            if (_mana == null || slider == null) return;

            float t = _mana.CurrentMana / _mana.MaxMana;
            slider.value = t;

            if (fill != null)
                fill.color = _mana.CanUseAbility ? ColorReady : ColorLow;
        }
    }
}
