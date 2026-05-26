using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Checkpoint HUD")]
    public class CheckpointHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup     canvasGroup;
        [SerializeField] private float           fadeInDuration  = 0.3f;
        [SerializeField] private float           holdDuration    = 1.5f;
        [SerializeField] private float           fadeOutDuration = 0.5f;

        public static CheckpointHUD Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string message = "Checkpoint Reached!")
        {
            if (label != null) label.text = message;
            ShowAsync().Forget();
        }

        private async UniTaskVoid ShowAsync()
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;

            await canvasGroup.DOFade(1f, fadeInDuration).AsyncWaitForCompletion();
            await UniTask.Delay((int)(holdDuration * 1000));
            await canvasGroup.DOFade(0f, fadeOutDuration).AsyncWaitForCompletion();
        }
    }
}
