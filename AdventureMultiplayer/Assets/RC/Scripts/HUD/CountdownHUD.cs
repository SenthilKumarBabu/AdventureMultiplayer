using TMPro;
using DG.Tweening;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Displays the pre-race countdown (3 / 2 / 1 / GO!) on a TextMeshPro label.
    ///
    /// Setup:
    ///   - Add to a Canvas GameObject.
    ///   - Assign countdownText (a TextMeshProUGUI on this Canvas).
    ///   - The panel auto-hides after Go fades out.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Countdown HUD")]
    public class CountdownHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private float punchScale   = 0.4f;
        [SerializeField] private float holdDuration = 0.6f;
        [SerializeField] private float fadeDuration = 0.3f;

        private void OnEnable()
        {
            RaceCountdown.OnCountdownTick += HandleTick;
        }

        private void OnDisable()
        {
            RaceCountdown.OnCountdownTick -= HandleTick;
        }

        private void HandleTick(int value)
        {
            if (countdownText == null) return;

            countdownText.text  = value == 0 ? "GO!" : value.ToString();
            countdownText.color = new Color(countdownText.color.r,
                                            countdownText.color.g,
                                            countdownText.color.b, 1f);

            DOTween.Kill(countdownText.transform);
            countdownText.transform.localScale = Vector3.one;

            countdownText.transform
                .DOPunchScale(Vector3.one * punchScale, holdDuration, vibrato: 0)
                .SetEase(Ease.OutBack);

            if (value == 0)
            {
                // Fade out the "GO!" text then hide the panel.
                DOTween.ToAlpha(
                    () => countdownText.color,
                    c  => countdownText.color = c,
                    0f, fadeDuration)
                    .SetDelay(holdDuration)
                    .OnComplete(() => gameObject.SetActive(false));
            }
        }
    }
}
