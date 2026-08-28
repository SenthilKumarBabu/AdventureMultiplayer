using Cysharp.Threading.Tasks;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    // Gates Spike's double jump. Spike no longer has a dedicated Ability button —
    // the extra jump is the plugin's own multi-jump (stats.multiJumps, driven by the
    // normal Jump button in Player.Jump()). This component only adds a per-use
    // cooldown on top of it: after the second (double) jump, multiJumps is dropped to 1
    // via a runtime stats clone so the extra air jump is unavailable for a few seconds,
    // while the plain grounded jump keeps working.
    [AddComponentMenu("Adventure Multiplayer/Spike Cooldown")]
    public class SpikeCooldown : MonoBehaviour
    {
        [SerializeField] private float _cooldownSeconds = 3f;

        private Player _player;
        private PlayerStatsManager _statsManager;
        private int _baseMultiJumps;
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
            _baseMultiJumps = _statsManager.stats[0].multiJumps;

            // Build a runtime cooldown variant from the base stats — no separate asset needed.
            var cooldown = UnityEngine.Object.Instantiate(_statsManager.stats[0]);
            cooldown.multiJumps = 1;
            var arr = _statsManager.stats;
            System.Array.Resize(ref arr, 2);
            arr[1] = cooldown;
            _statsManager.stats = arr;
        }

        private void Start()
        {
            _player.playerEvents.OnJump.AddListener(OnJump);
        }

        private void OnDestroy()
        {
            _player.playerEvents.OnJump.RemoveListener(OnJump);
        }

        private void OnJump()
        {
            // jumpCounter reaching the base multiJumps means this was the extra (double)
            // jump, not the first grounded one — that's the only case that should cool down.
            if (!_onCooldown && _baseMultiJumps > 1 && _player.jumpCounter >= _baseMultiJumps)
                RunCooldown().Forget();
        }

        private async UniTaskVoid RunCooldown()
        {
            _onCooldown = true;
            _cooldownEndTime = Time.time + _cooldownSeconds;
            _statsManager.Change(1); // switch to cooldown stats (multiJumps=1)
            await UniTask.Delay((int)(_cooldownSeconds * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
            _statsManager.Change(0); // restore normal stats
            _onCooldown = false;
            _cooldownEndTime = -1f;
        }
    }
}
