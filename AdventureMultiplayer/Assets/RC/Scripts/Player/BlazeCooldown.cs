using Cysharp.Threading.Tasks;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Dedicated per-use cooldown for Blaze's Dash, mirroring BrunoCooldown/SpikeCooldown.
    /// Replaces the mana-pool threshold as the sole gate on reuse, so one dash allows
    /// exactly one dash — the next is only available once the cooldown bar (see
    /// CooldownProgress01, read by ManaHUD) has filled back to 100%.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Blaze Cooldown")]
    public class BlazeCooldown : MonoBehaviour
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
            // Dash has a single unified entry gate (Player.Dash()), so both flags cover it.
            var cooldown = UnityEngine.Object.Instantiate(_statsManager.stats[0]);
            cooldown.canAirDash = false;
            cooldown.canGroundDash = false;
            var arr = _statsManager.stats;
            System.Array.Resize(ref arr, 2);
            arr[1] = cooldown;
            _statsManager.stats = arr;
        }

        private void Start()
        {
            // Fires on press (dash start), not on completion — the cooldown bar should
            // reset and start refilling the instant the ability is used, not after its
            // animation/motion finishes playing out.
            _player.playerEvents.OnDashStarted.AddListener(OnDashStarted);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.playerEvents.OnDashStarted.RemoveListener(OnDashStarted);
        }

        private void OnDashStarted()
        {
            if (!_onCooldown)
                RunCooldown().Forget();
        }

        private async UniTaskVoid RunCooldown()
        {
            _onCooldown = true;
            _cooldownEndTime = Time.time + _cooldownSeconds;
            _statsManager.Change(1); // cooldown stats — canAirDash/canGroundDash=false
            await UniTask.Delay((int)(_cooldownSeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
            _statsManager.Change(0); // restore normal stats
            _onCooldown = false;
            _cooldownEndTime = -1f;
        }
    }
}
