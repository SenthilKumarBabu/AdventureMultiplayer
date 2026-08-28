using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
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

        private ManaSystem    _mana;
        private BrunoCooldown _brunoCooldown;
        private BlazeCooldown _blazeCooldown;
        private SpikeCooldown _spikeCooldown;

        private void Start()
        {
            if (slider != null) slider.interactable = false;
        }

        private void Update()
        {
            if (_mana == null)
            {
                // Use the NetworkObject that belongs to THIS client — not FindFirstObjectByType
                // which always returns the first player spawned (host) on every machine.
                var localObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
                if (localObj != null)
                {
                    _mana          = localObj.GetComponent<ManaSystem>();
                    _brunoCooldown = localObj.GetComponent<BrunoCooldown>();
                    _blazeCooldown = localObj.GetComponent<BlazeCooldown>();
                    _spikeCooldown = localObj.GetComponent<SpikeCooldown>();
                }
            }
            if (_mana == null || slider == null) return;

            // Bruno's Roll, Blaze's Dash and Spike's High Jump aren't mana-gated
            // (BrunoCooldown/BlazeCooldown/SpikeCooldown each own a dedicated per-use
            // timer) — show that cooldown's 0%->100% refill here instead of the
            // (unused) mana pool.
            if (_brunoCooldown != null)
            {
                slider.value = _brunoCooldown.CooldownProgress01;
                if (fill != null)
                    fill.color = _brunoCooldown.IsOnCooldown ? ColorLow : ColorReady;
                return;
            }

            if (_blazeCooldown != null)
            {
                slider.value = _blazeCooldown.CooldownProgress01;
                if (fill != null)
                    fill.color = _blazeCooldown.IsOnCooldown ? ColorLow : ColorReady;
                return;
            }

            if (_spikeCooldown != null)
            {
                slider.value = _spikeCooldown.CooldownProgress01;
                if (fill != null)
                    fill.color = _spikeCooldown.IsOnCooldown ? ColorLow : ColorReady;
                return;
            }

            float t = _mana.CurrentMana / _mana.MaxMana;
            slider.value = t;

            if (fill != null)
                fill.color = _mana.CanUseAbility ? ColorReady : ColorLow;
        }
    }
}
