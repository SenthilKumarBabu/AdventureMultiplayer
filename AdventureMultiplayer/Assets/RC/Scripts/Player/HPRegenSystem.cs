using Cysharp.Threading.Tasks;
using PLAYERTWO.PlatformerProject;
using System.Threading;
using UnityEngine;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/HP Regen System")]
    public class HPRegenSystem : MonoBehaviour
    {
        [SerializeField] private float _regenDelay    = 3f;   // seconds after damage before regen
        [SerializeField] private int   _regenAmount   = 1;
        [SerializeField] private float _regenInterval = 1.5f; // seconds between regen ticks

        private Health                 _health;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void Start()
        {
            _health.onDamage.AddListener(OnDamaged);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.onDamage.RemoveListener(OnDamaged);
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void OnDamaged()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            RegenLoopAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid RegenLoopAsync(CancellationToken ct)
        {
            await UniTask.Delay((int)(_regenDelay * 1000), cancellationToken: ct);

            while (_health != null && _health.current < _health.max)
            {
                if (ct.IsCancellationRequested) return;
                _health.Increase(_regenAmount);
                await UniTask.Delay((int)(_regenInterval * 1000), cancellationToken: ct);
            }
        }
    }
}
