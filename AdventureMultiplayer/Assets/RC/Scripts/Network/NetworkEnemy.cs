using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative enemy health and death sync.
    ///
    /// The server is the sole authority on enemy health. When a player's hitbox
    /// hits an enemy (via NetworkedEntityHitbox), the client calls DamageEnemyServerRpc.
    /// The server validates and applies the damage through the plugin's Enemy.ApplyDamage,
    /// then replicates state to all clients via ClientRpc.
    ///
    /// Enemy.ContactAttack damage to players is handled separately in NetworkedHealth,
    /// which listens to the Health component's onChange event and fires a ClientRpc
    /// for non-server clients when external damage is detected.
    ///
    /// Add to every enemy prefab alongside a NetworkObject.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Network Enemy")]
    public class NetworkEnemy : NetworkBehaviour
    {
        private Enemy _enemy;
        private bool _respawning;

        private readonly NetworkVariable<int> _health = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _dead = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Awake() => _enemy = GetComponent<Enemy>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _health.Value = _enemy.health.current;
                _enemy.health.onChange.AddListener(OnServerHealthChanged);
                _enemy.enemyEvents.OnRespawn.AddListener(OnServerRespawn);
            }
            else
            {
                _dead.OnValueChanged += OnDeadChanged;
                // Apply initial dead state for late joiners.
                if (_dead.Value)
                    ApplyDeathLocally();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _enemy.health.onChange.RemoveListener(OnServerHealthChanged);
                _enemy.enemyEvents.OnRespawn.RemoveListener(OnServerRespawn);
            }
            _dead.OnValueChanged -= OnDeadChanged;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Called by NetworkedEntityHitbox when a player's hitbox contacts this enemy.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void DamageEnemyServerRpc(int amount, Vector3 origin)
        {
            if (_dead.Value) return;
            _enemy.ApplyDamage(amount, origin);
            // OnServerHealthChanged fires via health.onChange after ApplyDamage.
        }

        // ── Server listeners ─────────────────────────────────────────────────────

        private void OnServerHealthChanged()
        {
            if (_respawning) return;

            int curr = _enemy.health.current;
            bool wasAlive = !_dead.Value;
            bool nowDead = curr == 0;

            _health.Value = curr;
            _dead.Value = nowDead;

            // Only replicate damage events (not health increases from respawn).
            if (wasAlive)
                SyncEnemyDamageClientRpc(curr, nowDead);
        }

        private void OnServerRespawn()
        {
            _respawning = true;
            _health.Value = _enemy.health.current;
            _dead.Value = false;
            _respawning = false;

            SyncEnemyRespawnClientRpc(transform.position, transform.rotation);
            Debug.Log($"[NetworkEnemy] '{name}' respawned on server.");
        }

        // ── ClientRpcs ────────────────────────────────────────────────────────────

        [ClientRpc]
        private void SyncEnemyDamageClientRpc(int newHealth, bool died)
        {
            if (IsServer) return; // Server already processed via ApplyDamage.

            _enemy.health.Set(newHealth);
            _enemy.enemyEvents.OnDamage?.Invoke();

            if (died)
                ApplyDeathLocally();

            Debug.Log($"[NetworkEnemy] '{name}' client synced — health={newHealth} died={died}.");
        }

        [ClientRpc]
        private void SyncEnemyRespawnClientRpc(Vector3 position, Quaternion rotation)
        {
            if (IsServer) return;

            transform.SetPositionAndRotation(position, rotation);
            _enemy.velocity = Vector3.zero;
            _enemy.health.ResetHealth();
            _enemy.controller.enabled = true;
            _enemy.states.Reset();
            _enemy.enemyEvents.OnRespawn.Invoke();
            Debug.Log($"[NetworkEnemy] '{name}' client respawned.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void ApplyDeathLocally()
        {
            _enemy.health.Set(0);
            _enemy.controller.enabled = false;
            _enemy.enemyEvents.OnDie?.Invoke();
        }

        private void OnDeadChanged(bool _, bool isDead)
        {
            // Late joiners: apply current dead state immediately.
            if (isDead)
                ApplyDeathLocally();
        }
    }
}
