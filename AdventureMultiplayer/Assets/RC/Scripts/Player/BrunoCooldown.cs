using Cysharp.Threading.Tasks;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Bruno Cooldown")]
    public class BrunoCooldown : MonoBehaviour
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
            cooldown.canRollCharge = false;
            var arr = _statsManager.stats;
            System.Array.Resize(ref arr, 2);
            arr[1] = cooldown;
            _statsManager.stats = arr;
        }

        private void Start()
        {
            _player.playerEvents.OnRollEnded.AddListener(OnRollEnded);
        }

        private void OnDestroy()
        {
            _player.playerEvents.OnRollEnded.RemoveListener(OnRollEnded);
        }

        private void OnRollEnded()
        {
            if (!_onCooldown)
                RunCooldown().Forget();
        }

        private async UniTaskVoid RunCooldown()
        {
            _onCooldown = true;
            _statsManager.Change(1); // cooldown stats — canRoll=false
            await UniTask.Delay((int)(_cooldownSeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
            _statsManager.Change(0); // restore normal stats
            _onCooldown = false;
        }
    }
}
