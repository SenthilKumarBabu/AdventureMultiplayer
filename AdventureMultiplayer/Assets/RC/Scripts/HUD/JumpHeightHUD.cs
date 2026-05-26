using PLAYERTWO.PlatformerProject;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Shows a read-only vertical slider while the player is rising after a jump.
    /// Uses CanvasGroup alpha so this component keeps running even when hidden.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Jump Height HUD")]
    public class JumpHeightHUD : MonoBehaviour
    {
        [SerializeField] private Slider      slider;
        [SerializeField] private Image       minMarker;
        [SerializeField] private CanvasGroup canvasGroup;

        private Player _player;

        private void Start()
        {
            if (slider      != null) slider.interactable = false;
            if (canvasGroup != null) canvasGroup.alpha   = 0f;
        }

        private void Update()
        {
            if (_player == null)
                _player = FindFirstObjectByType<Player>();
            if (_player == null) return;

            float vy    = _player.verticalVelocity.y;
            var   stats = _player.stats.current;
            bool  rising = !_player.isGrounded && vy > 0f;

            if (canvasGroup != null)
                canvasGroup.alpha = rising ? 1f : 0f;

            if (!rising) return;

            float max = stats.maxJumpHeight;
            float min = stats.minJumpHeight;

            if (slider != null)
                slider.value = Mathf.InverseLerp(0f, max, Mathf.Clamp(vy, 0f, max));

            if (minMarker != null)
            {
                float ratio = Mathf.InverseLerp(0f, max, min);
                minMarker.rectTransform.anchorMin = new Vector2(ratio, 0f);
                minMarker.rectTransform.anchorMax = new Vector2(ratio, 1f);
            }
        }
    }
}
