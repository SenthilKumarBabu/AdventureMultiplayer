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
        private float _cooldownEndTime = -1f;

        public bool IsOnCooldown => _onCooldown;

        /// <summary>0 right after use (not ready), 1 once fully recharged. Always 1 when not on cooldown.</summary>
        public float CooldownProgress01
        {
            get
            {
                if (!_onCooldown) return 1f;
                float remaining = _cooldownEndTime - Time.time;
                return Mathf.Clamp01(1f - remaining / _cooldownSeconds);
            }
        }

        private void Awake()
        {
            _player = GetComponent<Player>();
            _statsManager = GetComponent<PlayerStatsManager>();

            // Build a runtime cooldown variant from the base stats — no separate asset needed.
            // Bruno's roll is actually triggered via Player.Roll() (gated by canRoll, while
            // moving fast enough on the ground) — Player.RollCharge() (gated by canRollCharge,
            // the crouch-charge mechanic) is a separate, unused entry path. Disabling only
            // canRollCharge left canRoll untouched, so the cooldown never actually blocked reuse.
            var cooldown = UnityEngine.Object.Instantiate(_statsManager.stats[0]);
            cooldown.canRoll = false;
            cooldown.canRollCharge = false;
            var arr = _statsManager.stats;
            System.Array.Resize(ref arr, 2);
            arr[1] = cooldown;
            _statsManager.stats = arr;
        }

        private void Start()
        {
            // Fires on press (roll start), not on completion — the cooldown bar should
            // reset and start refilling the instant the ability is used, not after its
            // animation/motion finishes playing out.
            _player.playerEvents.OnRollStarted.AddListener(OnRollStarted);
        }

        private void OnDestroy()
        {
            _player.playerEvents.OnRollStarted.RemoveListener(OnRollStarted);
        }

        private void OnRollStarted()
        {
            if (!_onCooldown)
                RunCooldown().Forget();
        }

        private async UniTaskVoid RunCooldown()
        {
            _onCooldown = true;
            _cooldownEndTime = Time.time + _cooldownSeconds;
            _statsManager.Change(1); // cooldown stats — canRoll=false
            await UniTask.Delay((int)(_cooldownSeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
            _statsManager.Change(0); // restore normal stats
            _onCooldown = false;
            _cooldownEndTime = -1f;
        }
    }
}
