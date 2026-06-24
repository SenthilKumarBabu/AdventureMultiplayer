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

        /// <summary>Server + client registry: clientId → inventory. Populated in OnNetworkSpawn.</summary>
        public static readonly Dictionary<ulong, PlayerPowerUpInventory> All = new();

        // ── Inspector ─────────────────────────────────────────────────────────

        [SerializeField] private NetworkObject bananaPeelPrefab;
        [SerializeField] private NetworkObject decoyBoxPrefab;

        // ── Networked slots ───────────────────────────────────────────────────

        // 3 slots; each int is (int)PowerUpType, or -1 for empty.
        private NetworkList<int> m_Slots;

        // ── Lv1 effect constants ──────────────────────────────────────────────

        private const float SpeedBoostSpeedMult = 1.4f;
        private const float SpeedBoostAccelMult = 1.2f;
        private const float SpeedBoostDur       = 5f;

        private const float RocketSpeedMult = 2.0f;
        private const float RocketAccelMult = 2.0f;
        private const float RocketDur       = 3f;

        private const float ShieldMaxDur  = 30f;
        private const float InvincDur     = 2f;
        private const float StunBoltDur   = 3f;
        private const float FreezeDur     = 1f;
        private const float BananaSlipDur = 2.5f;
        private const float DecoyStunDur  = 2f;

        // ── Component refs ────────────────────────────────────────────────────

        private Player              _player;
        private PlayerStatsManager  _statsManager;
        private CharacterController _cc;
        private NetworkedHealth     _health;
        private StunEffect          _stun;
        private SlipEffect          _slip;

        // ── Timed-effect cancellation (owner client) ──────────────────────────

        private CancellationTokenSource _speedBoostCts;
        private CancellationTokenSource _rocketCts;

        // ── Timed-effect cancellation (server) ────────────────────────────────

        private CancellationTokenSource _shieldCts;
        private CancellationTokenSource _invincCts;

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

            // Replace SO asset references with per-instance copies so SpeedBoost /
            // Rocket can safely mutate values without affecting other players.
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
            All[OwnerClientId] = this;
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(OwnerClientId);
            _speedBoostCts?.Cancel();
            _rocketCts?.Cancel();
            _shieldCts?.Cancel();
            _invincCts?.Cancel();
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
            DispatchEffect(type);
        }

        // ── Effect dispatch ───────────────────────────────────────────────────

        private void DispatchEffect(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.SpeedBoost:    DispatchSpeedBoost();           break;
                case PowerUpType.Rocket:        DispatchRocket();               break;
                case PowerUpType.Shield:        DispatchShieldAsync().Forget(); break;
                case PowerUpType.Invincibility: DispatchInvincAsync().Forget(); break;
                case PowerUpType.StunBolt:      DispatchStunBolt();             break;
                case PowerUpType.Swap:          DispatchSwap();                 break;
                case PowerUpType.Freeze:        DispatchFreeze();               break;
                case PowerUpType.Banana:        DispatchBanana();               break;
                case PowerUpType.DecoyBox:      DispatchDecoyBox();             break;
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
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Rocket) ?? 1;
            Debug.Log($"[PowerUpInventory] Rocket Lv{lvl} → client {OwnerClientId}.");
            BeginRocketClientRpc(RocketSpeedMult, RocketAccelMult, RocketDur, OwnerOnly());
        }

        [ClientRpc]
        private void BeginRocketClientRpc(float sMult, float aMult, float dur, ClientRpcParams _ = default)
        {
            RocketAsync(sMult, aMult, dur).Forget();
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
            Debug.Log($"[PowerUpInventory] SpeedBoost active x{sMult} for {dur}s.");

            try   { await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token); }
            catch (OperationCanceledException) { }
            finally
            {
                if (_statsManager != null) ScaleAllStats(_statsManager, 1f / sMult, 1f / aMult);
                Debug.Log("[PowerUpInventory] SpeedBoost ended.");
            }
        }

        private async UniTaskVoid RocketAsync(float sMult, float aMult, float dur)
        {
            if (_statsManager == null) return;
            _rocketCts?.Cancel();
            _rocketCts = new CancellationTokenSource();
            var token = CancellationTokenSource
                .CreateLinkedTokenSource(_rocketCts.Token, destroyCancellationToken).Token;

            ScaleAllStats(_statsManager, sMult, aMult);
            Debug.Log($"[PowerUpInventory] Rocket active x{sMult} for {dur}s.");

            try   { await UniTask.Delay(TimeSpan.FromSeconds(dur), cancellationToken: token); }
            catch (OperationCanceledException) { }
            finally
            {
                if (_statsManager != null) ScaleAllStats(_statsManager, 1f / sMult, 1f / aMult);
                Debug.Log("[PowerUpInventory] Rocket ended.");
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
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(ShieldMaxDur), cancellationToken: token);
                _health.SetShield(false);
                Debug.Log($"[PowerUpInventory] Shield expired for client {OwnerClientId}.");
            }
            catch (OperationCanceledException) { }
        }

        // ── Invincibility ─────────────────────────────────────────────────────

        private async UniTaskVoid DispatchInvincAsync()
        {
            if (_health == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Invincibility) ?? 1;
            Debug.Log($"[PowerUpInventory] Invincibility Lv{lvl} for client {OwnerClientId}.");

            _invincCts?.Cancel();
            _invincCts = new CancellationTokenSource();
            var token = CancellationTokenSource
                .CreateLinkedTokenSource(_invincCts.Token, destroyCancellationToken).Token;

            _health.SetInvincible(true);
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(InvincDur), cancellationToken: token);
                _health.SetInvincible(false);
                Debug.Log($"[PowerUpInventory] Invincibility ended for client {OwnerClientId}.");
            }
            catch (OperationCanceledException)
            {
                _health?.SetInvincible(false);
            }
        }

        // ── StunBolt ──────────────────────────────────────────────────────────

        private void DispatchStunBolt()
        {
            if (RaceManager.Instance == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.StunBolt) ?? 1;

            ulong targetId = RaceManager.Instance.GetPlayerAhead(OwnerClientId);
            if (targetId == ulong.MaxValue) { Debug.Log("[PowerUpInventory] StunBolt: no target ahead."); return; }
            if (!All.TryGetValue(targetId, out var target)) { Debug.Log($"[PowerUpInventory] StunBolt: target {targetId} not in registry."); return; }

            // Shield absorbs StunBolt
            if (target._health != null && target._health.IsShielded)
            {
                target._health.SetShield(false);
                Debug.Log($"[PowerUpInventory] StunBolt Lv{lvl} blocked by shield on client {targetId}.");
                return;
            }

            Debug.Log($"[PowerUpInventory] StunBolt Lv{lvl} → client {targetId} stunned {StunBoltDur}s.");
            target.ApplyStunClientRpc(StunBoltDur, TargetOnly(targetId));
        }

        // ── Swap ──────────────────────────────────────────────────────────────

        private void DispatchSwap()
        {
            if (RaceManager.Instance == null) return;

            ulong targetId = RaceManager.Instance.GetPlayerAhead(OwnerClientId);
            if (targetId == ulong.MaxValue) { Debug.Log("[PowerUpInventory] Swap: no target ahead."); return; }
            if (!All.TryGetValue(targetId, out var target)) { Debug.Log($"[PowerUpInventory] Swap: target {targetId} not found."); return; }

            // Shield counters Swap
            if (target._health != null && target._health.IsShielded)
            {
                target._health.SetShield(false);
                Debug.Log($"[PowerUpInventory] Swap blocked by shield on client {targetId}.");
                return;
            }

            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Swap) ?? 1;
            Vector3 myPos     = transform.position;
            Vector3 targetPos = target.transform.position;

            Debug.Log($"[PowerUpInventory] Swap Lv{lvl}: client {OwnerClientId} ↔ {targetId}.");
            TeleportClientRpc(targetPos, OwnerOnly());
            target.TeleportClientRpc(myPos, TargetOnly(targetId));
        }

        // ── Freeze ────────────────────────────────────────────────────────────

        private void DispatchFreeze()
        {
            if (RaceManager.Instance == null) return;
            int lvl = PowerUpUpgradeManager.Instance?.GetEffectiveLevel(PowerUpType.Freeze) ?? 1;

            var targets = RaceManager.Instance.GetPlayersBehind(OwnerClientId);
            if (targets.Count == 0) { Debug.Log("[PowerUpInventory] Freeze: no targets behind."); return; }

            Debug.Log($"[PowerUpInventory] Freeze Lv{lvl} — {targets.Count} targets for {FreezeDur}s.");

            foreach (ulong id in targets)
            {
                if (!All.TryGetValue(id, out var target)) continue;

                // Shield absorbs Freeze
                if (target._health != null && target._health.IsShielded)
                {
                    target._health.SetShield(false);
                    Debug.Log($"[PowerUpInventory] Freeze blocked by shield on client {id}.");
                    continue;
                }

                target.ApplyStunClientRpc(FreezeDur, TargetOnly(id));
            }
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
                peel.Init(OwnerClientId, BananaSlipDur);

            Debug.Log($"[PowerUpInventory] Banana Lv{lvl} spawned by client {OwnerClientId}.");
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
                db.Init(OwnerClientId, DecoyStunDur);

            Debug.Log($"[PowerUpInventory] DecoyBox Lv{lvl} spawned by client {OwnerClientId}.");
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
