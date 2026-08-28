using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Tracks ability mana. When mana drops below the minimum threshold the ability is
    /// disabled via a runtime stats clone, so PLAYER TWO's state machine refuses the input.
    /// Mana regenerates after a short delay following any use.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Mana System")]
    public class ManaSystem : MonoBehaviour
    {
        [SerializeField] private float _maxMana     = 100f;
        [SerializeField] private float _minMana     = 50f;   // must have this much to use ability
        [SerializeField] private float _regenRate   = 20f;   // per second
        [SerializeField] private float _regenDelay  = 1.5f;  // seconds before regen starts
        [SerializeField] private float _drainRate   = 35f;   // per second for Glide / Sprint

        public float CurrentMana    { get; private set; }
        public float MaxMana        => _maxMana;
        public float MinMana        => _minMana;
        public bool  CanUseAbility  => CurrentMana >= _minMana;

        // ── SuperCharge power-up ─────────────────────────────────────────────────
        // Armed by the SuperCharge power-up (PlayerPowerUpInventory, owner-only RPC).
        // Consumed the next time this player's ability actually activates, giving
        // that single activation +SuperChargeBonusSeconds of duration.

        public const float SuperChargeBonusSeconds = 1f;
        private bool _superChargeArmed;

        private Player             _player;
        private PlayerInputManager _input;
        private PlayerStatsManager _statsManager;
        private PlayerStats        _noManaStats;
        private int                _noManaIndex;
        private int                _characterIndex;
        private float              _regenTimer;
        private bool               _wasDraining;
        private float              _dashDurationBeforeSuperCharge = -1f;

        private void Awake()
        {
            _player       = GetComponent<Player>();
            _input        = GetComponent<PlayerInputManager>();
            _statsManager = GetComponent<PlayerStatsManager>();
            CurrentMana   = _maxMana;
        }

        private void Start()
        {
            _characterIndex = CharacterPicker.Instance != null
                ? CharacterPicker.Instance.LocalSelectedIndex : 0;

            // Build a no-mana stats clone with the ability flag disabled
            _noManaStats = Object.Instantiate(_statsManager.stats[0]);
            DisableAbilityOn(_noManaStats);

            var arr = _statsManager.stats;
            _noManaIndex = arr.Length;
            System.Array.Resize(ref arr, arr.Length + 1);
            arr[_noManaIndex] = _noManaStats;
            _statsManager.stats = arr;

            // Subscribe to instant-cost events.
            // Bruno/Roll, Blaze/Dash and Spike/High Jump are intentionally NOT gated by
            // mana here — BrunoCooldown/BlazeCooldown/SpikeCooldown each own a dedicated
            // per-use timer (reset-to-0%-then-refill, shown directly by ManaHUD), and
            // having mana ALSO gate them let players bank ~2-3 uses off the 100-mana pool
            // (100/40 cost) before that timer's own lockout was ever felt, and made the
            // HUD slider desync from the timer that actually gated reuse. Blaze still
            // listens for SuperCharge's dash duration doubling — that's independent of
            // the mana cost being removed.
            switch (_characterIndex)
            {
                case 1:
                    _player.playerEvents.OnDashStarted.AddListener(OnDashStartedSuperCharge);
                    _player.playerEvents.OnDashEnded.AddListener(OnDashEndedSuperCharge);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_player == null) return;
            switch (_characterIndex)
            {
                case 1:
                    _player.playerEvents.OnDashStarted.RemoveListener(OnDashStartedSuperCharge);
                    _player.playerEvents.OnDashEnded.RemoveListener(OnDashEndedSuperCharge);
                    break;
            }
        }

        // Blaze/Dash has a real timed duration (PlayerStats.dashDuration), unlike
        // Roll/AirDive whose exit is distance/physics-driven. dashForce is constant
        // for the whole dash (no friction/deceleration mid-dash), so distance scales
        // 1:1 with duration — doubling dashDuration gives exactly a 2x dash distance,
        // rather than the flat +SuperChargeBonusSeconds used by the other abilities
        // (which on the ~0.3s base duration would be a much larger multiple).
        //
        // Dash isn't mana-gated (BlazeCooldown owns reuse instead — see Start()). Both
        // ManaSystem and BlazeCooldown now listen to OnDashStarted (cooldown resets on
        // press, not on completion), and Unity doesn't guarantee listener order between
        // components without an explicit DefaultExecutionOrder — if BlazeCooldown's
        // listener runs first, it swaps _statsManager.current to its own cooldown clone
        // before this one fires, and capturing/reverting only that single object would
        // silently double (or fail to revert) the wrong clone depending on ordering. So
        // this mutates dashDuration on every clone in the stats array uniformly instead
        // of going through ".current" at all — correct regardless of which one ends up active.
        private const float SuperChargeDashDistanceMultiplier = 2f;

        private void OnDashStartedSuperCharge()
        {
            if (!TryConsumeSuperCharge()) return;
            if (_statsManager?.current == null || _statsManager.stats == null) return;

            _dashDurationBeforeSuperCharge = _statsManager.current.dashDuration;
            float doubled = _dashDurationBeforeSuperCharge * SuperChargeDashDistanceMultiplier;

            foreach (var s in _statsManager.stats)
                if (s != null) s.dashDuration = doubled;

            Debug.Log($"[ManaSystem] SuperCharge: Dash distance doubled ({_dashDurationBeforeSuperCharge:0.00}s -> {doubled:0.00}s).");
        }

        private void OnDashEndedSuperCharge()
        {
            if (_dashDurationBeforeSuperCharge < 0f) return;
            if (_statsManager?.stats != null)
                foreach (var s in _statsManager.stats)
                    if (s != null) s.dashDuration = _dashDurationBeforeSuperCharge;
            _dashDurationBeforeSuperCharge = -1f;
        }

        // ── SuperCharge public API ──────────────────────────────────────────────

        /// <summary>Arms a one-time +SuperChargeBonusSeconds bonus for this player's next ability activation.</summary>
        public void ArmSuperCharge()
        {
            _superChargeArmed = true;
            Debug.Log("[ManaSystem] SuperCharge armed for next ability use.");
        }

        /// <summary>
        /// Consumes the armed SuperCharge bonus if present. Used by ability states (Roll/AirDive)
        /// whose duration isn't mana-driven, so they extend themselves directly.
        /// </summary>
        public bool TryConsumeSuperCharge()
        {
            if (!_superChargeArmed) return false;
            _superChargeArmed = false;
            Debug.Log("[ManaSystem] SuperCharge consumed.");
            return true;
        }

        private void Update()
        {
            // Hold abilities drain per second.
            // Glide: only drain while actually in GlidingPlayerState (not on floor button press).
            bool draining = (_characterIndex == 0 && _player.states.current is GlidingPlayerState)
                         || (_characterIndex == 2 && _input.GetRun());

            // SuperCharge: on the rising edge of a fresh hold-ability activation, grant a
            // one-time bank of extra mana equal to SuperChargeBonusSeconds of drain — this
            // lets the ability run 2s longer than usual without touching the drain formula.
            if (draining && !_wasDraining && _superChargeArmed && CanUseAbility)
            {
                CurrentMana += _drainRate * SuperChargeBonusSeconds;
                _superChargeArmed = false;
                Debug.Log($"[ManaSystem] SuperCharge: hold ability bonus applied (+{SuperChargeBonusSeconds:0.0}s).");
            }
            _wasDraining = draining;

            if (draining && CurrentMana > 0f)
                UseMana(_drainRate * Time.deltaTime);

            // Force-stop hold abilities at 0 mana.
            // GlidingPlayerState only exits on button release — must kick it manually.
            // Sprint (running property) stops via canRun=false in _noManaStats,
            // but we also push to WalkPlayerState so the transition is instant.
            if (CurrentMana <= 0f)
            {
                if (_characterIndex == 0 && _player.states.IsCurrentOfType(typeof(GlidingPlayerState)))
                    _player.states.Change<FallPlayerState>();

                if (_characterIndex == 2 && _player.running)
                    _player.states.Change<WalkPlayerState>();
            }

            // Regen after delay
            if (_regenTimer > 0f)
                _regenTimer -= Time.deltaTime;
            else if (CurrentMana < _maxMana)
                CurrentMana = Mathf.Min(_maxMana, CurrentMana + _regenRate * Time.deltaTime);

            // Hold abilities (Glide/Sprint) stop at 0; instant abilities block below _minMana.
            bool isHoldAbility = _characterIndex == 0 || _characterIndex == 2;
            bool hasEnough = isHoldAbility ? CurrentMana > 0f : CurrentMana >= _minMana;

            if (!hasEnough && _statsManager.current != _noManaStats)
                _statsManager.Change(_noManaIndex);
            else if (hasEnough && _statsManager.current == _noManaStats)
                _statsManager.Change(0);
        }

        private void UseMana(float amount)
        {
            CurrentMana = Mathf.Max(0f, CurrentMana - amount);
            _regenTimer = _regenDelay;
        }

        private void DisableAbilityOn(PlayerStats s)
        {
            switch (_characterIndex)
            {
                case 0: s.canGlide       = false; break;                   // Gale
                // Blaze (1), Bruno (3) and Spike (4) intentionally omitted —
                // Dash/Roll/High Jump aren't mana-gated, see Start().
                case 2: s.canRun         = false; break;                   // Bolt
            }
        }
    }
}
