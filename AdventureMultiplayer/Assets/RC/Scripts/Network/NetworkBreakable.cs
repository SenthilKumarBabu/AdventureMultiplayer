using System.Reflection;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative wrapper for the plugin's Breakable component.
    ///
    /// Coin direction sync:  The server reads the randomly-assigned CollectiblePhysics
    ///   velocity (via reflection) after Break() fires, and sends it to all clients so
    ///   every machine sees the same trajectory.
    ///
    /// Coin collection sync: Crate coins have no NetworkObject, so collection is tracked
    ///   by index.  Whichever client (or server) collects a coin first, it notifies
    ///   everyone else to remove it.
    ///
    /// Hold guard: When a player picks up the crate, _isHeld is set to true on the server
    ///   and replicated to all clients. Any break attempt (server-side EntityHitbox or
    ///   client RequestBreakServerRpc) is rejected and the local state is restored.
    /// </summary>
    [RequireComponent(typeof(Breakable))]
    [AddComponentMenu("Adventure Multiplayer/Network Breakable")]
    public class NetworkBreakable : NetworkBehaviour
    {
        private Breakable _breakable;
        private AudioSource _audio;
        private Collectible[] _cachedCollectibles;
        private bool[] _coinCollected;
        private bool _localBreakApplied;

        // Accesses CollectiblePhysics.m_velocity (protected field) without modifying the plugin.
        private static readonly FieldInfo VelocityField =
            typeof(CollectiblePhysics).GetField("m_velocity",
                BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly NetworkVariable<bool> _isBroken = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Replicated so clients can reject and revert local breaks while crate is carried.
        private readonly NetworkVariable<bool> _isHeld = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            _breakable = GetComponent<Breakable>();
            _audio = GetComponent<AudioSource>();
        }

        public override void OnNetworkSpawn()
        {
            _cachedCollectibles = _breakable.collectibles;
            _coinCollected = new bool[_cachedCollectibles.Length];

            if (IsServer)
            {
                _breakable.OnBreak.AddListener(OnBreakServer);

                if (TryGetComponent<Pickable>(out var pickable))
                {
                    pickable.onPicked.AddListener(() =>
                    {
                        _isHeld.Value = true;
                        Debug.Log($"[NetworkBreakable] '{name}' picked up — _isHeld=True");
                    });
                    pickable.onReleased.AddListener(() =>
                    {
                        _isHeld.Value = false;
                        Debug.Log($"[NetworkBreakable] '{name}' released — _isHeld=False");
                    });
                }
            }
            else
            {
                // Prevent plugin from calling SetActive on coins client-side.
                _breakable.collectibles = new Collectible[0];

                _breakable.OnBreak.AddListener(OnBreakClient);
                _isBroken.OnValueChanged += OnIsBrokenChanged;

                // Notify server when this client picks up or releases the crate.
                if (TryGetComponent<Pickable>(out var pickable))
                {
                    pickable.onPicked.AddListener(() =>
                    {
                        Debug.Log($"[NetworkBreakable] '{name}' client picked up — notifying server.");
                        SetHeldServerRpc(true);
                    });
                    pickable.onReleased.AddListener(() =>
                    {
                        Debug.Log($"[NetworkBreakable] '{name}' client released — notifying server.");
                        SetHeldServerRpc(false);
                    });
                }

                if (_isBroken.Value)
                    ApplyLocalBreak(null); // Late joiner
            }
        }

        public override void OnNetworkDespawn()
        {
            _breakable.OnBreak.RemoveListener(OnBreakServer);
            _breakable.OnBreak.RemoveListener(OnBreakClient);
            _isBroken.OnValueChanged -= OnIsBrokenChanged;
        }

        // ── Server ────────────────────────────────────────────────────────────

        private void OnBreakServer()
        {
            if (_isBroken.Value) return;

            // Break() has already run by the time OnBreak fires.
            // If the crate is held, restore it and abort.
            if (_isHeld.Value)
            {
                Debug.Log($"[NetworkBreakable] '{name}' break rejected (OnBreakServer) — crate is held. Restoring.");
                _breakable.Restore();
                return;
            }

            _isBroken.Value = true;

            var velocities = ReadCoinVelocities();
            SubscribeToCoinCollect();

            for (int i = 0; i < velocities.Length; i++)
                Debug.Log($"[NetworkBreakable] '{name}' coin[{i}] server velocity={velocities[i]}");

            SyncBreakClientRpc(velocities);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetHeldServerRpc(bool held)
        {
            _isHeld.Value = held;
            Debug.Log($"[NetworkBreakable] '{name}' SetHeldServerRpc — _isHeld={held}");
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestBreakServerRpc()
        {
            if (_breakable.broken) return;

            if (_isHeld.Value)
            {
                Debug.Log($"[NetworkBreakable] '{name}' break rejected (RequestBreakServerRpc) — crate is held.");
                return;
            }

            _breakable.ApplyDamage(_breakable.HP);
        }

        [ClientRpc]
        private void SyncBreakClientRpc(Vector3[] coinVelocities)
        {
            if (IsServer) return; // Host already handled via Breakable.Break()
            ApplyLocalBreak(coinVelocities);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SyncCoinCollectedServerRpc(int coinIdx)
        {
            Debug.Log($"[NetworkBreakable] '{name}' SyncCoinCollectedServerRpc coin[{coinIdx}]. already={_coinCollected[coinIdx]}");
            if (_coinCollected[coinIdx]) return;
            // Do NOT set _coinCollected here — let ClientRpc set it so the host processes it too.
            SyncCoinCollectedClientRpc(coinIdx);
        }

        [ClientRpc]
        private void SyncCoinCollectedClientRpc(int coinIdx)
        {
            Debug.Log($"[NetworkBreakable] '{name}' SyncCoinCollectedClientRpc coin[{coinIdx}]. already={_coinCollected[coinIdx]} IsServer={IsServer}");
            if (_coinCollected[coinIdx]) return;
            _coinCollected[coinIdx] = true;

            if (_cachedCollectibles == null || coinIdx >= _cachedCollectibles.Length) return;
            var c = _cachedCollectibles[coinIdx];
            if (c != null && c.gameObject.activeSelf)
            {
                c.gameObject.SetActive(false);
                Debug.Log($"[NetworkBreakable] '{name}' coin[{coinIdx}] removed on this machine.");
            }
        }

        // ── Client ────────────────────────────────────────────────────────────

        private void OnBreakClient()
        {
            // The client's EntityHitbox called Break() locally. If the crate is held,
            // revert the local visual break and do not ask the server to confirm it.
            if (_isHeld.Value)
            {
                Debug.Log($"[NetworkBreakable] '{name}' break rejected (OnBreakClient) — crate is held. Restoring local state.");
                _breakable.Restore();
                if (_audio != null) _audio.Stop();
                return;
            }

            RequestBreakServerRpc();
        }

        private void OnIsBrokenChanged(bool _, bool broken)
        {
            if (broken) ApplyLocalBreak(null);
        }

        private void ApplyLocalBreak(Vector3[] velocities)
        {
            if (_localBreakApplied) return;
            _localBreakApplied = true;

            if (!_breakable.broken)
                _breakable.ApplyDamage(_breakable.HP);

            for (int i = 0; i < _cachedCollectibles.Length; i++)
            {
                var c = _cachedCollectibles[i];
                if (c == null) continue;

                c.gameObject.SetActive(true);

                // Override the local random velocity with the server's value.
                if (velocities != null && i < velocities.Length)
                {
                    SetCoinVelocity(c, velocities[i]);
                    Debug.Log($"[NetworkBreakable] '{name}' coin[{i}] client velocity set to {velocities[i]}");
                }
            }

            SubscribeToCoinCollect();
        }

        // ── Collection sync ───────────────────────────────────────────────────

        private void SubscribeToCoinCollect()
        {
            for (int i = 0; i < _cachedCollectibles.Length; i++)
            {
                int idx = i;
                var c = _cachedCollectibles[i];
                if (c == null) continue;
                c.onCollect.AddListener(_ => OnCoinCollectedLocally(idx));
            }
        }

        private void OnCoinCollectedLocally(int idx)
        {
            if (_coinCollected[idx]) return;
            _coinCollected[idx] = true;

            Debug.Log($"[NetworkBreakable] '{name}' coin[{idx}] collected locally. IsServer={IsServer}");

            if (IsServer)
                SyncCoinCollectedClientRpc(idx);
            else
                SyncCoinCollectedServerRpc(idx);
        }

        // ── Velocity helpers (reflection) ─────────────────────────────────────

        private Vector3[] ReadCoinVelocities()
        {
            var velocities = new Vector3[_cachedCollectibles.Length];
            for (int i = 0; i < _cachedCollectibles.Length; i++)
            {
                var c = _cachedCollectibles[i];
                if (c == null || VelocityField == null) continue;
                var physics = c.GetComponent<CollectiblePhysics>();
                if (physics != null)
                    velocities[i] = (Vector3)VelocityField.GetValue(physics);
            }
            return velocities;
        }

        private void SetCoinVelocity(Collectible c, Vector3 velocity)
        {
            if (VelocityField == null) return;
            var physics = c.GetComponent<CollectiblePhysics>();
            if (physics != null)
                VelocityField.SetValue(physics, velocity);
        }
    }
}
