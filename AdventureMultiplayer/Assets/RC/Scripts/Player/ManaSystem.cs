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
        [SerializeField] private float _instantCost = 40f;   // Dash / Roll / AirDive
        [SerializeField] private float _drainRate   = 35f;   // per second for Glide / Sprint

        public float CurrentMana    { get; private set; }
        public float MaxMana        => _maxMana;
        public float MinMana        => _minMana;
        public bool  CanUseAbility  => CurrentMana >= _minMana;

        private Player             _player;
        private PlayerInputManager _input;
        private PlayerStatsManager _statsManager;
        private PlayerStats        _noManaStats;
        private int                _noManaIndex;
        private int                _characterIndex;
        private float              _regenTimer;

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

            // Subscribe to instant-cost events
            switch (_characterIndex)
            {
                case 1: _player.playerEvents.OnDashStarted.AddListener(OnInstantAbility); break;
                case 3: _player.playerEvents.OnRollStarted.AddListener(OnInstantAbility); break;
                case 4: _player.playerEvents.OnAirDive.AddListener(OnInstantAbility);     break;
            }
        }

        private void OnDestroy()
        {
            if (_player == null) return;
            switch (_characterIndex)
            {
                case 1: _player.playerEvents.OnDashStarted.RemoveListener(OnInstantAbility); break;
                case 3: _player.playerEvents.OnRollStarted.RemoveListener(OnInstantAbility); break;
                case 4: _player.playerEvents.OnAirDive.RemoveListener(OnInstantAbility);     break;
            }
        }

        private void OnInstantAbility() => UseMana(_instantCost);

        private void Update()
        {
            // Hold abilities drain per second
            bool draining = (_characterIndex == 0 && _input.GetGlide())
                         || (_characterIndex == 2 && _input.GetRun());

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
                case 1: s.canAirDash     = false; s.canGroundDash = false; break; // Blaze
                case 2: s.canRun         = false; break;                   // Bolt
                case 3: s.canRollCharge  = false; break;                   // Bruno
                case 4: s.canAirDive     = false; break;                   // Spike
            }
        }
    }
}
