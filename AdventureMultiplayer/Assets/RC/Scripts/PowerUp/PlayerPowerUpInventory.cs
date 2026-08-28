using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Three-slot power-up inventory attached to every player prefab.
    ///
    /// Server responsibilities:
    ///   - TryAddPowerUp: called by NetworkedPowerUpBox when a player picks up a box.
    ///   - UseSlotServerRpc: called by the HUD when the player taps a slot.
    ///   - All effect logic runs server-side then pushes to clients via targeted ClientRpcs.
    ///
    /// Owner-client responsibilities:
    ///   - Stats manipulation (SpeedBoost / Rocket) via BeginBoost* ClientRpcs.
    ///   - Teleportation (Swap) via TeleportClientRpc.
    ///   - Stun / Slip application via ApplyStun / ApplySlip ClientRpcs on the target's NetworkBehaviour.
    ///
    /// Add to every player prefab. Assign bananaPeelPrefab + decoyBoxPrefab in Inspector.
    /// </summary>
    [DefaultExecutionOrder(-200)] // instance stats SOs before ManaSystem.Start()
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Rush Champions/Player Power-Up Inventory")]
    public class PlayerPowerUpInventory : NetworkBehaviour
    {
        // ── Static registry ───────────────────────────────────────────────────

        /// <summary>
        /// Server + client registry: NetworkObjectId → inventory. Populated in OnNetworkSpawn.
        /// Keyed by NetworkObjectId (not OwnerClientId) because OwnerClientId is NOT unique
        /// for bots — BotSpawner spawns them with default (server) ownership, so every bot
        /// (and the Host's own player) shares OwnerClientId 0.
        /// </summary>
        public static readonly Dictionary<ulong, PlayerPowerUpInventory> All = new();

        /// <summary>
        /// This instance's race-participant ID: a human player's real NGO OwnerClientId,
        /// or a bot's RaceBotBrain.BotId (10000+). Use this — never OwnerClientId directly —
        /// wherever "who is this, for race-position purposes" matters (self-checks,
        /// RaceManager.GetPlayerAhead/Behind/etc.), since RaceManager's own entries are
        /// keyed the same way (see RaceBotBrain.OnNetworkSpawn → AddBotEntry).
        /// </summary>
        internal ulong RaceId => TryGetComponent<RaceBotBrain>(out var bot) ? bot.BotId : OwnerClientId;

        // ── Inspector ─────────────────────────────────────────────────────────

        [SerializeField] private NetworkObject bananaPeelPrefab;
        [SerializeField] private NetworkObject decoyBoxPrefab;
        [SerializeField] private NetworkObject rocketProjectilePrefab;

        // ── Networked slots ───────────────────────────────────────────────────

        // 3 slots; each int is (int)PowerUpType, or -1 for empty.
        private NetworkList<int> m_Slots;

        // ── Lv1 effect constants ──────────────────────────────────────────────

        private const float SpeedBoostSpeedMult = 1.4f;
        private const float SpeedBoostAccelMult = 1.2f;
        private const float SpeedBoostDur       = 5f;

        private const float ShieldMaxDur  = 30f;
        private const float InvisibleDur  = 5f;
        private const float StunBoltDur   = 3f;
        [SerializeField] private float freezeDuration = 10f;
        [SerializeField] private float bananaSlipDuration = 2f;
        private const float DecoyStunDur  = 2f;

        // ── Component refs ────────────────────────────────────────────────────

        private Player              _player;
        private PlayerStatsManager  _statsManager;
        private CharacterController _cc;
        private NetworkedHealth     _health;
        private StunEffect          _stun;
        private SlipEffect          _slip;
        private InvisibleEffect     _invisible;
        private ManaSystem          _manaSystem;
        private PlayerBodyCollider  _bodyCollider;

        // ── Timed-effect cancellation (owner client) ──────────────────────────

        private CancellationTokenSource _speedBoostCts;

        // ── Timed-effect cancellation (server) ────────────────────────────────

        private CancellationTokenSource _shieldCts;
        private CancellationTokenSource _invisibleCts;

        // ── Timed-effect end times (Time.time-based, -1 = not active) ─────────
        // Used by SuperCharge to extend a currently-running effect mid-flight.
        // SpeedBoost's timer lives on the owner client (that's where the async
        // loop runs); Shield/Invisible live on the server.

        private float _speedBoostEndTime = -1f; // owner-client only
        private float _shieldEndTime     = -1f; // server only
        private float _invisibleEndTime  = -1f; // server only

        // ── Invisible dynamic pass-through (per client, local only) ────────────
        // Obstacle colliders come from many different unrelated scripts/assets (some
        // with no AdventureMultiplayer script at all — plain MeshColliders off an
        // imported FBX). Rather than chase every obstacle type individually, continuously
        // ignore whatever solid collider is physically near this player while invisible.
        private bool _invisiblePassthroughActive;
        private readonly HashSet<Collider> _dynamicIgnoredColliders = new();
        private static readonly Collider[] _passthroughOverlapBuffer = new Collider[64];

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            m_Slots = new NetworkList<int>(new[] { -1, -1, -1 });
        }

        private void Start()
        {
            _player       = GetComponent<Player>();
            _statsManager = GetComponent<PlayerStatsManager>();
            _cc           = GetComponent<CharacterController>();
            _health       = GetComponent<NetworkedHealth>();
            _stun         = GetComponent<StunEffect>();
            _slip         = GetComponent<SlipEffect>();
            _invisible    = GetComponent<InvisibleEffect>();
            _manaSystem   = GetComponent<ManaSystem>();
            _bodyCollider = GetComponent<PlayerBodyCollider>();

            // Replace SO asset references with per-instance copies so SpeedBoost
            // can safely mutate values without affecting other players.
            if (_statsManager != null && _statsManager.stats != null)
            {
                var arr = _statsManager.stats;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] != null)
                        arr[i] = UnityEngine.Object.Instantiate(arr[i]);
                _statsManager.stats = arr;
            }
        }

        public override void OnNetworkSpawn()
        {
            All[NetworkObjectId] = this;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(NetworkObjectId);
            _speedBoostCts?.Cancel();
            _shieldCts?.Cancel();
            _invisibleCts?.Cancel();
            ClearDynamicPassthrough();
        }

        private void Update()
        {
            if (_invisiblePassthroughActive)
                RefreshDynamicPassthrough();
        }

        // Ignores every solid collider within reach of this player right now, whatever
        // script (if any) it belongs to. Runs every frame while invisible so obstacles
        // get ignored just before contact as the player moves, not only at activation.
        // The overlap sphere naturally reaches the ground under the player's feet too —
        // there's no separate ground/obstacle layer to filter by in this project — so
        // whatever Player itself currently reports as ground is always excluded/restored,
        // otherwise the player falls through the floor the moment Invisible activates.
        private void RefreshDynamicPassthrough()
        {
            var controller = _player != null ? _player.controller : null;
            if (controller == null) return;

            var groundCollider = _player.isGrounded ? _player.groundHit.collider : null;

            float radius = controller.radius + 3f;
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, _passthroughOverlapBuffer,
                controller.collisionLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var col = _passthroughOverlapBuffer[i];
                if (col == null || col.transform.IsChildOf(transform)) continue;
                if (col == groundCollider) continue;
                if (_dynamicIgnoredColliders.Add(col))
                    controller.IgnoreCollider(col, true);
            }

            // Restore support immediately if the player walked onto something that got
            // ignored earlier while it was still just a nearby obstacle, not yet the ground.
            if (groundCollider != null && _dynamicIgnoredColliders.Remove(groundCollider))
                controller.IgnoreCollider(groundCollider, false);
        }

        private void ClearDynamicPassthrough()
        {
            var controller = _player != null ? _player.controller : null;
            if (controller != null)
            {
                foreach (var col in _dynamicIgnoredColliders)
                    if (col != null) controller.IgnoreCollider(col, false);
            }
            _dynamicIgnoredColliders.Clear();
        }

        // ── Public API (slots) ────────────────────────────────────────────────

        /// <summary>Returns the PowerUpType in a slot, or -1 if empty.</summary>
        public int GetSlot(int i) => m_Slots != null && i >= 0 && i < 3 ? m_Slots[i] : -1;

        /// <summary>Subscribe to NetworkList callbacks for HUD updates.</summary>
        public NetworkList<int> Slots => m_Slots;

        // ── Server API ────────────────────────────────────────────────────────

        /// <summary>Called by NetworkedPowerUpBox on the server when a player collides with a box.</summary>
        public bool TryAddPowerUp(PowerUpType type)
        {
            if (!IsServer) return false;
            for (int i = 0; i < 3; i++)
            {
                if (m_Slots[i] == -1)
                {
                    m_Slots[i] = (int)type;
                    Debug.Log($"[PowerUpInventory] Client {OwnerClientId} picked up {type} → slot {i}.");
                    return true;
                }
            }
            Debug.Log($"[PowerUpInventory] Client {OwnerClientId} inventory full — {type} discarded.");
            return false;
        }

        // ── ServerRpc: HUD tap ────────────────────────────────────────────────

        [ServerRpc]
        public void UseSlotServerRpc(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3 || m_Slots[slotIndex] == -1) return;

            var type = (PowerUpType)m_Slots[slotIndex];
            m_Slots[slotIndex] = -1;

            Debug.Log($"[PowerUpInventory] Client {OwnerClientId} used slot {slotIndex}: {type}");
            NotifyPowerUpUsedClientRpc((int)type, OwnerOnly());

            // Self-buffs (no opponent) still belong in the global attack feed — attack-style
            // power-ups instead broadcast from NotifyGlobalAttackClientRpc once they resolve
            // (hit/blocked/dodged), so they're deliberately not duplicated here.
            if (type is PowerUpType.SpeedBoost or PowerUpType.Shield or PowerUpType.Invisible or PowerUpType.SuperCharge)
                NotifyGlobalSelfUseClientRpc(RaceId, (int)type);

            DispatchEffect(type);
        }

        // ── Client-side notification (HUD text) ─────────────────────────────────

        /// <summary>Raised on the owning client only, right when a power-up is used. Drives on-screen feedback text.</summary>
        public event System.Action<PowerUpType> OnPowerUpUsed;

        [ClientRpc]
        private void NotifyPowerUpUsedClientRpc(int typeInt, ClientRpcParams _ = default)
        {
            OnPowerUpUsed?.Invoke((PowerUpType)typeInt);
        }

        /// <summary>Raised on the RECEIVING client only, right when another player's power-up
        /// reaches them — hit, or blocked by Shield/Invisible. Drives on-screen "Frozen!"-style
        /// feedback text so a player understands what just happened to them, not just that their
        /// own movement suddenly stopped.</summary>
        public event System.Action<PowerUpType, PowerUpAffectOutcome> OnPowerUpAffected;

        [ClientRpc]
        internal void NotifyPowerUpAffectedClientRpc(int typeInt, int outcomeInt, ClientRpcParams _ = default)
        {
            OnPowerUpAffected?.Invoke((PowerUpType)typeInt, (PowerUpAffectOutcome)outcomeInt);
        }

        /// <summary>Broadcast to EVERY client (no target filter) whenever an attack power-up
        /// is used on someone — hit, or blocked/dodged by Shield/Invisible — drives the global
        /// left-side kill/attack feed. Unlike OnPowerUpUsed/OnPowerUpAffected above, this reaches
        /// spectators of the attack, not just the two participants.</summary>
        [ClientRpc]
        internal void NotifyGlobalAttackClientRpc(ulong attackerRaceId, ulong targetRaceId, int typeInt, int outcomeInt)
        {
            PowerUpFeedHUD.Instance?.ShowAttack(attackerRaceId, targetRaceId, (PowerUpType)typeInt, (PowerUpAffectOutcome)outcomeInt);
        }

        /// <summary>Broadcast to EVERY client whenever a self-buff (SpeedBoost/Shield/Invisible/
        /// SuperCharge) is used — same global feed as NotifyGlobalAttackClientRpc, but with no
        /// opponent to name.</summary>
        [ClientRpc]
        internal void NotifyGlobalSelfUseClientRpc(ulong userRaceId, int typeInt)
        {
            PowerUpFeedHUD.Instance?.ShowSelfUse(userRaceId, (PowerUpType)typeInt);
        }

        // ── Effect dispatch ───────────────────────────────────────────────────

        private void DispatchEffect(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.SpeedBoost: DispatchSpeedBoost();             break;
                case PowerUpType.Rocket:     DispatchRocket();                 break;
                case PowerUpType.Shield:     DispatchShieldAsync().Forget();   break;
                case PowerUpType.StunBolt:   DispatchStunBolt();               break;
                case PowerUpType.Swap:       DispatchSwap();                   break;
                case PowerUpType.Freeze:     DispatchFreeze();                 break;
                case PowerUpType.Banana:     DispatchBanana();                 break;
                case PowerUpType.DecoyBox:   DispatchDecoyBox();               break;
                case PowerUpType.Invisible:   DispatchInvisibleAsync().Forget(); break;
                case PowerUpType.SuperCharge: DispatchSuperCharge();             break;
            }
        }

        // ── SpeedBoost ────────────────────────────────────────────────────────

        private void DispatchSpeedBoost()
        {
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.SpeedBoost) ?? 1;
            Debug.Log($"[PowerUpInventory] SpeedBoost Lv{lvl} → client {OwnerClientId}.");
            BeginSpeedBoostClientRpc(SpeedBoostSpeedMult, SpeedBoostAccelMult, SpeedBoostDur, OwnerOnly());
        }

        [ClientRpc]
        private void BeginSpeedBoostClientRpc(float sMult, float aMult, float dur, ClientRpcParams _ = default)
        {
            SpeedBoostAsync(sMult, aMult, dur).Forget();
        }

        // ── Rocket ────────────────────────────────────────────────────────────

        private void DispatchRocket()
        {
            if (rocketProjectilePrefab == null)
            {
                Debug.LogWarning("[PowerUpInventory] Rocket projectile prefab not assigned — assign it in the Inspector.");
                return;
            }

            if (RaceManager.Instance == null) return;

            // Always target the player in 1st place. If we ARE 1st, do nothing.
            ulong targetId = RaceManager.Instance.GetPlayerInFirst();
            if (targetId == ulong.MaxValue || targetId == RaceId)
            {
                Debug.Log("[PowerUpInventory] Rocket: caster is in 1st place — no effect.");
                return;
            }

            // Resolve via RaceManager's transform registry (bot-aware) rather than the
            // OwnerClientId-based ClientRpc routing — targetId here is a race ID, which
            // for a bot is NOT a real NGO client ID.
            var targetTransform = RaceManager.Instance.GetPlayerTransform(targetId);
            var targetInv = targetTransform != null ? targetTransform.GetComponent<PlayerPowerUpInventory>() : null;
            if (targetInv == null)
            {
                Debug.Log($"[PowerUpInventory] Rocket: target raceId={targetId} has no registered inventory — aborting.");
                return;
            }

            int     lvl      = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Rocket) ?? 1;
            Vector3 spawnPos = transform.position + transform.forward * 1f + Vector3.up * 0.5f;

            // Spawn already facing the target so the rocket doesn't spin to acquire.
            Quaternion spawnRot = transform.rotation;
            Vector3 toTarget = targetInv.transform.position - spawnPos;
            if (toTarget.sqrMagnitude > 0.01f)
                spawnRot = Quaternion.LookRotation(toTarget);

            var obj = Instantiate(rocketProjectilePrefab, spawnPos, spawnRot);
            obj.Spawn();
            if (obj.TryGetComponent<RocketProjectile>(out var rocket))
                rocket.Init(NetworkObjectId, targetInv.NetworkObjectId);

            Debug.Log($"[PowerUpInventory] Rocket Lv{lvl} launched by raceId {RaceId} → targeting raceId {targetId}.");
        }

        // ── Stats boost async (owner client) ──────────────────────────────────

        // Multiplies topSpeed, acceleration and airSpeed across every entry in the stats
        // array, then divides them back after the duration.  Touching all entries means
        // ManaSystem can freely switch between slots without cancelling the boost.

        private async UniTaskVoid SpeedBoostAsync(float sMult, float aMult, float dur)
        {
            if (_statsManager == null) return;
            _speedBoostCts?.Cancel();
            _speedBoostCts = new CancellationTokenSource();
            var token = CancellationTokenSource
                .CreateLinkedTokenSource(_speedBoostCts.Token, destroyCancellationToken).Token;

            ScaleAllStats(_statsManager, sMult, aMult);
            _speedBoostEndTime = Time.time + dur;
            Debug.Log($"[PowerUpInventory] SpeedBoost active x{sMult} for {dur}s.");

            try   { await WaitUntilEndTimeAsync(() => _speedBoostEndTime, token); }
            catch (OperationCanceledException) { }
            finally
            {
                if (_statsManager != null) ScaleAllStats(_statsManager, 1f / sMult, 1f / aMult);
                _speedBoostEndTime = -1f;
                Debug.Log("[PowerUpInventory] SpeedBoost ended.");
            }
        }

        private static void ScaleAllStats(PlayerStatsManager mgr, float sMult, float aMult)
        {
            if (mgr.stats == null) return;
            foreach (var s in mgr.stats)
            {
                if (s == null) continue;
                s.topSpeed        *= sMult;
                s.acceleration    *= aMult;
                s.airAcceleration *= aMult;
            }
        }

        // ── Shield ────────────────────────────────────────────────────────────

        private async UniTaskVoid DispatchShieldAsync()
        {
            if (_health == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Shield) ?? 1;
            Debug.Log($"[PowerUpInventory] Shield Lv{lvl} activated for client {OwnerClientId}.");

            _shieldCts?.Cancel();
            _shieldCts = new CancellationTokenSource();
            var token = CancellationTokenSource
                .CreateLinkedTokenSource(_shieldCts.Token, destroyCancellationToken).Token;

            _health.SetShield(true);
            _shieldEndTime = Time.time + ShieldMaxDur;
            try
            {
                await WaitUntilEndTimeAsync(() => _shieldEndTime, token);
                _health.SetShield(false);
                Debug.Log($"[PowerUpInventory] Shield expired for client {OwnerClientId}.");
            }
            catch (OperationCanceledException) { }
            finally { _shieldEndTime = -1f; }
        }

        // ── Invisible ─────────────────────────────────────────────────────────

        // Merges the old Invincibility (full damage immunity) and Invisibility (physical
        // pass-through) power-ups into one: ignores obstacles/enemy damage (via
        // NetworkedHealth.IsInvisible, checked by TreadmillObstacle/BananaPeel/
        // DecoyBoxPickup/ObstacleKnockback/RocketProjectile), plus actual physical
        // pass-through of obstacles and other players (via EntityController.IgnoreCollider
        // below — Physics.IgnoreCollision has no effect on EntityController's manual
        // sweep/overlap collision, which is what actually blocks movement), plus a
        // semi-transparent glass look.
        private async UniTaskVoid DispatchInvisibleAsync()
        {
            if (_health == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Invisible) ?? 1;
            Debug.Log($"[PowerUpInventory] Invisible Lv{lvl} for client {OwnerClientId}.");

            _invisibleCts?.Cancel();
            _invisibleCts = new CancellationTokenSource();
            var token = CancellationTokenSource
                .CreateLinkedTokenSource(_invisibleCts.Token, destroyCancellationToken).Token;

            _health.SetInvisible(true);
            SetInvisibleClientRpc(true);
            _invisibleEndTime = Time.time + InvisibleDur;
            try
            {
                await WaitUntilEndTimeAsync(() => _invisibleEndTime, token);
                _health.SetInvisible(false);
                SetInvisibleClientRpc(false);
                Debug.Log($"[PowerUpInventory] Invisible ended for client {OwnerClientId}.");
            }
            catch (OperationCanceledException)
            {
                _health?.SetInvisible(false);
                SetInvisibleClientRpc(false);
            }
            finally { _invisibleEndTime = -1f; }
        }

        // Broadcast (not owner-only): every client renders every player, so each one needs
        // to both show the glass look and ignore collision against its own local copy of
        // the invisible player — via EntityController.IgnoreCollider, the API that actually
        // drives movement blocking for this player (Physics.IgnoreCollision does not affect
        // EntityController's manual sweep/overlap system).
        [ClientRpc]
        private void SetInvisibleClientRpc(bool active)
        {
            _invisible?.SetActive(active);

            var myController = _player != null ? _player.controller : null;
            if (myController == null) return;

            // Pass through obstacles: obstacle colliders come from many unrelated
            // scripts/assets (some with no AdventureMultiplayer script at all), so instead
            // of enumerating every known type, continuously ignore whatever's physically
            // near this player for the duration of Invisible (see Update/RefreshDynamicPassthrough).
            _invisiblePassthroughActive = active;
            if (active)
                RefreshDynamicPassthrough();
            else
                ClearDynamicPassthrough();

            // TEMP diagnostic — confirms the dynamic ignore-list isn't empty when this fires.
            // Remove once pass-through is confirmed working in-game.
            Debug.Log($"[PowerUpInventory] Invisible pass-through {(active ? "ON" : "OFF")}: " +
                $"{_dynamicIgnoredColliders.Count} nearby colliders ignored.");

            // Pass through every other player, both directions: this player must ignore
            // their solid Body collider, and they must ignore this player's Body collider.
            var myBody = _bodyCollider != null ? _bodyCollider.BodyCollider : null;
            foreach (var kvp in All)
            {
                if (kvp.Value == this || kvp.Value == null) continue;

                var otherBody       = kvp.Value._bodyCollider != null ? kvp.Value._bodyCollider.BodyCollider : null;
                var otherController = kvp.Value._player != null ? kvp.Value._player.controller : null;

                if (otherBody != null)
                    myController.IgnoreCollider(otherBody, active);
                if (myBody != null && otherController != null)
                    otherController.IgnoreCollider(myBody, active);
            }
        }

        // ── StunBolt ──────────────────────────────────────────────────────────

        private void DispatchStunBolt()
        {
            if (RaceManager.Instance == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.StunBolt) ?? 1;

            // Target the player ahead; if in 1st place, stun the player directly behind instead.
            ulong targetId = RaceManager.Instance.GetPlayerAhead(RaceId);
            if (targetId == ulong.MaxValue)
                targetId = RaceManager.Instance.GetPlayerBehind(RaceId);
            if (targetId == ulong.MaxValue) { Debug.Log("[PowerUpInventory] StunBolt: no other player found."); return; }
            var target = ResolveTarget(targetId);
            if (target == null) { Debug.Log($"[PowerUpInventory] StunBolt: target raceId={targetId} not in registry."); return; }

            // Shield absorbs StunBolt (consumed); Invisible blocks it (not consumed).
            if (target._health != null && target._health.IsShielded)
            {
                target._health.SetShield(false);
                Debug.Log($"[PowerUpInventory] StunBolt Lv{lvl} blocked by shield on raceId {targetId}.");
                target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.StunBolt, (int)PowerUpAffectOutcome.ShieldBlocked, TargetOnly(target.OwnerClientId));
                target.NotifyGlobalAttackClientRpc(RaceId, targetId, (int)PowerUpType.StunBolt, (int)PowerUpAffectOutcome.ShieldBlocked);
                return;
            }
            if (target._health != null && target._health.IsInvisible)
            {
                Debug.Log($"[PowerUpInventory] StunBolt Lv{lvl} blocked by Invisible on raceId {targetId}.");
                target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.StunBolt, (int)PowerUpAffectOutcome.InvisibleDodged, TargetOnly(target.OwnerClientId));
                target.NotifyGlobalAttackClientRpc(RaceId, targetId, (int)PowerUpType.StunBolt, (int)PowerUpAffectOutcome.InvisibleDodged);
                return;
            }

            Debug.Log($"[PowerUpInventory] StunBolt Lv{lvl} → raceId {targetId} stunned {StunBoltDur}s.");
            target.ApplyStunClientRpc(StunBoltDur, TargetOnly(target.OwnerClientId));
            target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.StunBolt, (int)PowerUpAffectOutcome.Hit, TargetOnly(target.OwnerClientId));
            target.NotifyGlobalAttackClientRpc(RaceId, targetId, (int)PowerUpType.StunBolt, (int)PowerUpAffectOutcome.Hit);
        }

        // ── Swap ──────────────────────────────────────────────────────────────

        private void DispatchSwap()
        {
            if (RaceManager.Instance == null) return;

            // Only swap with the player directly ahead. Do nothing if already in 1st.
            ulong targetId = RaceManager.Instance.GetPlayerAhead(RaceId);
            if (targetId == ulong.MaxValue) { Debug.Log("[PowerUpInventory] Swap: already in 1st place — no effect."); return; }
            var target = ResolveTarget(targetId);
            if (target == null) { Debug.Log($"[PowerUpInventory] Swap: target raceId={targetId} not found."); return; }

            // Shield counters Swap (consumed); Invisible blocks it (not consumed).
            if (target._health != null && target._health.IsShielded)
            {
                target._health.SetShield(false);
                Debug.Log($"[PowerUpInventory] Swap blocked by shield on raceId {targetId}.");
                target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Swap, (int)PowerUpAffectOutcome.ShieldBlocked, TargetOnly(target.OwnerClientId));
                target.NotifyGlobalAttackClientRpc(RaceId, targetId, (int)PowerUpType.Swap, (int)PowerUpAffectOutcome.ShieldBlocked);
                return;
            }
            if (target._health != null && target._health.IsInvisible)
            {
                Debug.Log($"[PowerUpInventory] Swap blocked by Invisible on raceId {targetId}.");
                target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Swap, (int)PowerUpAffectOutcome.InvisibleDodged, TargetOnly(target.OwnerClientId));
                target.NotifyGlobalAttackClientRpc(RaceId, targetId, (int)PowerUpType.Swap, (int)PowerUpAffectOutcome.InvisibleDodged);
                return;
            }

            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Swap) ?? 1;
            Vector3 myPos     = transform.position;
            Vector3 targetPos = target.transform.position;

            Debug.Log($"[PowerUpInventory] Swap Lv{lvl}: raceId {RaceId} ↔ {targetId}.");
            TeleportClientRpc(targetPos, OwnerOnly());
            target.TeleportClientRpc(myPos, TargetOnly(target.OwnerClientId));
            target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Swap, (int)PowerUpAffectOutcome.Hit, TargetOnly(target.OwnerClientId));
            target.NotifyGlobalAttackClientRpc(RaceId, targetId, (int)PowerUpType.Swap, (int)PowerUpAffectOutcome.Hit);

            // Swap checkpoint indices so RecalculatePositions ranks them correctly
            // after the teleport. Without this, the higher-checkpoint player keeps
            // outscoring the other even when physically behind.
            RaceManager.Instance.SwapCheckpoints(RaceId, targetId);
        }

        // ── Freeze ────────────────────────────────────────────────────────────

        private void DispatchFreeze()
        {
            if (RaceManager.Instance == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Freeze) ?? 1;

            var targets = RaceManager.Instance.GetPlayersBehind(RaceId);
            if (targets.Count == 0) { Debug.Log("[PowerUpInventory] Freeze: no targets behind."); return; }

            Debug.Log($"[PowerUpInventory] Freeze Lv{lvl} — {targets.Count} targets for {freezeDuration}s.");

            foreach (ulong id in targets)
            {
                var target = ResolveTarget(id);
                if (target == null) continue;

                // Shield absorbs Freeze (consumed); Invisible blocks it (not consumed).
                if (target._health != null && target._health.IsShielded)
                {
                    target._health.SetShield(false);
                    Debug.Log($"[PowerUpInventory] Freeze blocked by shield on raceId {id}.");
                    target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Freeze, (int)PowerUpAffectOutcome.ShieldBlocked, TargetOnly(target.OwnerClientId));
                    target.NotifyGlobalAttackClientRpc(RaceId, id, (int)PowerUpType.Freeze, (int)PowerUpAffectOutcome.ShieldBlocked);
                    continue;
                }
                if (target._health != null && target._health.IsInvisible)
                {
                    Debug.Log($"[PowerUpInventory] Freeze blocked by Invisible on raceId {id}.");
                    target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Freeze, (int)PowerUpAffectOutcome.InvisibleDodged, TargetOnly(target.OwnerClientId));
                    target.NotifyGlobalAttackClientRpc(RaceId, id, (int)PowerUpType.Freeze, (int)PowerUpAffectOutcome.InvisibleDodged);
                    continue;
                }

                target.ApplyStunClientRpc(freezeDuration, TargetOnly(target.OwnerClientId));
                target.NotifyPowerUpAffectedClientRpc((int)PowerUpType.Freeze, (int)PowerUpAffectOutcome.Hit, TargetOnly(target.OwnerClientId));
                target.NotifyGlobalAttackClientRpc(RaceId, id, (int)PowerUpType.Freeze, (int)PowerUpAffectOutcome.Hit);
            }
        }

        // Resolves a RaceManager race-participant ID (NGO clientId for a human,
        // RaceBotBrain.BotId for a bot) to that player's/bot's PlayerPowerUpInventory,
        // via RaceManager's transform registry — NOT the All dictionary, which is keyed
        // by NetworkObjectId and has no knowledge of race IDs.
        private static PlayerPowerUpInventory ResolveTarget(ulong raceId)
        {
            var t = RaceManager.Instance?.GetPlayerTransform(raceId);
            return t != null ? t.GetComponent<PlayerPowerUpInventory>() : null;
        }

        // ── Banana ────────────────────────────────────────────────────────────

        private void DispatchBanana()
        {
            if (bananaPeelPrefab == null) { Debug.LogWarning("[PowerUpInventory] BananaPeel prefab not assigned!"); return; }

            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Banana) ?? 1;
            Vector3 spawnPos = transform.position - transform.forward * 1.2f;

            var obj = Instantiate(bananaPeelPrefab, spawnPos, Quaternion.identity);
            obj.Spawn();
            if (obj.TryGetComponent<BananaPeel>(out var peel))
                peel.Init(NetworkObjectId, bananaSlipDuration);

            Debug.Log($"[PowerUpInventory] Banana Lv{lvl} spawned by NetworkObjectId {NetworkObjectId}.");
        }

        // ── DecoyBox ─────────────────────────────────────────────────────────

        private void DispatchDecoyBox()
        {
            if (decoyBoxPrefab == null) { Debug.LogWarning("[PowerUpInventory] DecoyBox prefab not assigned!"); return; }

            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.DecoyBox) ?? 1;
            Vector3 spawnPos = transform.position + transform.forward * 3f + Vector3.up * 0.5f;

            var obj = Instantiate(decoyBoxPrefab, spawnPos, Quaternion.identity);
            obj.Spawn();
            if (obj.TryGetComponent<DecoyBoxPickup>(out var db))
                db.Init(NetworkObjectId, DecoyStunDur);

            Debug.Log($"[PowerUpInventory] DecoyBox Lv{lvl} spawned by NetworkObjectId {NetworkObjectId}.");
        }

        // ── SuperCharge ───────────────────────────────────────────────────────

        // Arms a one-time +1s bonus on the owner's character ability (Glide/Sprint/
        // Dash/Roll/AirDive — see ManaSystem). Consumed on that player's next ability
        // activation; if never used, it stays armed. Owner-only — never touches other players.
        private void DispatchSuperCharge()
        {
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.SuperCharge) ?? 1;
            Debug.Log($"[PowerUpInventory] SuperCharge Lv{lvl} armed for client {OwnerClientId} — next ability use gets a bonus.");
            ArmSuperChargeClientRpc(OwnerOnly());
        }

        [ClientRpc]
        private void ArmSuperChargeClientRpc(ClientRpcParams _ = default)
        {
            _manaSystem?.ArmSuperCharge();
        }

        // ── ClientRpcs ────────────────────────────────────────────────────────

        [ClientRpc]
        internal void ApplyStunClientRpc(float duration, ClientRpcParams _ = default)
        {
            _stun?.Apply(duration);
        }

        [ClientRpc]
        internal void ApplySlipClientRpc(float duration, ClientRpcParams _ = default)
        {
            _slip?.Apply(duration);
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 newPos, ClientRpcParams _ = default)
        {
            bool wasEnabled = _cc != null && _cc.enabled;
            if (_cc != null) _cc.enabled = false;
            transform.position = newPos;
            if (_cc != null) _cc.enabled = wasEnabled;

            if (_player != null)
            {
                _player.lateralVelocity  = Vector3.zero;
                _player.verticalVelocity = Vector3.zero;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Waits until Time.time reaches the value returned by endTimeGetter, re-reading
        // it after every delay so an in-flight extension (SuperCharge) is picked up without
        // needing to cancel/restart the running effect.
        private static async UniTask WaitUntilEndTimeAsync(Func<float> endTimeGetter, CancellationToken token)
        {
            while (true)
            {
                float remaining = endTimeGetter() - Time.time;
                if (remaining <= 0f) return;
                await UniTask.Delay(TimeSpan.FromSeconds(remaining), cancellationToken: token);
            }
        }

        private ClientRpcParams OwnerOnly() => new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };

        private static ClientRpcParams TargetOnly(ulong clientId) => new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
    }
}
