using Cysharp.Threading.Tasks;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Spike Cooldown")]
    public class SpikeCooldown : MonoBehaviour
    {
        [SerializeField] private float _cooldownSeconds = 2f;

        private Player _player;
        private PlayerStatsManager _statsManager;
        private bool _onCooldown;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _statsManager = GetComponent<PlayerStatsManager>();

            // Build a runtime cooldown variant from the base stats — no separate asset needed.
            var cooldown = UnityEngine.Object.Instantiate(_statsManager.stats[0]);
            cooldown.canAirDive = false;
            var arr = _statsManager.stats;
            System.Array.Resize(ref arr, 2);
            arr[1] = cooldown;
            _statsManager.stats = arr;
        }

        private void Start()
        {
            _player.playerEvents.OnAirDive.AddListener(OnAirDive);
        }

        private void OnDestroy()
        {
            _player.playerEvents.OnAirDive.RemoveListener(OnAirDive);
        }

        private void OnAirDive()
        {
            if (!_onCooldown)
                RunCooldown().Forget();
        }

        private async UniTaskVoid RunCooldown()
        {
            _onCooldown = true;
            _statsManager.Change(1); // switch to cooldown stats (canAirDive=false)
            await UniTask.Delay((int)(_cooldownSeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
            _statsManager.Change(0); // restore normal stats
            _onCooldown = false;
        }
    }
}
