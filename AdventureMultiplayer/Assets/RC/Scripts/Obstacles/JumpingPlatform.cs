using Cysharp.Threading.Tasks;
using DG.Tweening;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Adventure Multiplayer/Jumping Platform")]
    public class JumpingPlatform : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 18f;
        [SerializeField] private float detectionRadius = 1.2f;
        [SerializeField] private float squishScale = 0.6f;
        [SerializeField] private float squishDuration = 0.1f;
        [SerializeField] private float cooldown = 0.5f;

        private Collider _myCollider;
        private bool _isOnCooldown;
        private Vector3 _originalScale;

        private void Awake()
        {
            _myCollider = GetComponent<Collider>();
            _originalScale = transform.localScale;
        }

        private void Update()
        {
            if (_isOnCooldown) return;

            var top = _myCollider.bounds.center + Vector3.up * (_myCollider.bounds.extents.y + 0.1f);
            var hits = Physics.OverlapSphere(top, detectionRadius, ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in hits)
            {
                var player = col.GetComponentInParent<Player>();
                if (player == null) continue;
                if (!player.isAlive) continue;
                if (!player.isGrounded) continue;
                if (player.groundHit.collider != _myCollider) continue;

                player.verticalVelocity = Vector3.up * bounceForce;
                player.ResetJumps();
                player.states.Change<FallPlayerState>();
                LaunchAsync().Forget();
                break;
            }
        }

        private async UniTaskVoid LaunchAsync()
        {
            _isOnCooldown = true;

            await transform.DOScaleY(_originalScale.y * squishScale, squishDuration)
                           .SetEase(Ease.OutQuad)
                           .AsyncWaitForCompletion();

            transform.DOScaleY(_originalScale.y, squishDuration * 2f)
                     .SetEase(Ease.OutElastic);

            await UniTask.Delay((int)(cooldown * 1000));
            _isOnCooldown = false;
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
