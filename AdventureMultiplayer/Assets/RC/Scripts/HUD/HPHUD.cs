using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/HP HUD")]
    public class HPHUD : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image  fill;

        private static readonly Color ColorFull = new Color(0.2f, 0.8f, 0.2f, 0.95f);
        private static readonly Color ColorLow  = new Color(0.8f, 0.2f, 0.2f, 0.95f);

        private Player _player;

        private void Start()
        {
            if (slider != null) slider.interactable = false;
        }

        private void Update()
        {
            if (_player == null)
            {
                // Use the NetworkObject that belongs to THIS client — not FindFirstObjectByType
                // which always returns the first player spawned (C0) on every machine.
                var localObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;
                if (localObj != null)
                    _player = localObj.GetComponent<Player>();
            }

            if (_player == null || slider == null) return;

            var health = _player.health;
            float t = health.max > 0 ? (float)health.current / health.max : 0f;
            slider.value = t;

            if (fill != null)
                fill.color = Color.Lerp(ColorLow, ColorFull, t);
        }
    }
}
