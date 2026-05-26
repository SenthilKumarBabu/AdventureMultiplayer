using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative health and damage.
    ///
    /// The server is the single source of truth for health values. When damage is
    /// requested (via ServerRpc), the server validates it (shield check, invincibility),
    /// updates the NetworkVariable, then tells all clients to apply it to their local
    /// PLAYER TWO Health component so animations, particles and sounds fire everywhere.
    ///
    /// External damage (e.g. enemy ContactAttack calling player.ApplyDamage directly
    /// on the server) is detected via OnHealthComponentChanged: if m_health.current
    /// drops below Health.Value the server fires ExternalDamageClientRpc so non-host
    /// clients also enter the hurt/death state.
    ///
    /// Add to the player prefab.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Health")]
    public class NetworkedHealth : NetworkBehaviour
    {
        public NetworkVariable<int> Health { get; private set; } =
            new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Health m_health;
        private bool   m_shieldActive;

        public override void OnNetworkSpawn()
        {
            m_health = GetComponent<Health>();

            if (IsServer && m_health != null)
            {
                Health.Value = m_health.current;
                m_health.onChange.AddListener(OnHealthComponentChanged);
            }

            Health.OnValueChanged += OnNetHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            Health.OnValueChanged -= OnNetHealthChanged;

            if (IsServer && m_health != null)
                m_health.onChange.RemoveListener(OnHealthComponentChanged);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Request damage from any client (e.g. hit by obstacle or power-up).
        /// The server validates and applies it.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(int damage, Vector3 origin)
        {
            if (m_shieldActive)
            {
                m_shieldActive = false;
                Debug.Log($"[NetworkedHealth] Shield absorbed hit for client {OwnerClientId}.");
                return;
            }

            // Respect the plugin's own invincibility cooldown.
            if (m_health != null && m_health.recovering) return;

            int newHealth = Mathf.Max(0, Health.Value - damage);
            Health.Value  = newHealth;

            // Tell all clients to run the PLAYER TWO hurt/death logic.
            ApplyDamageClientRpc(damage, origin);
        }

        /// <summary>Activate shield (called by NetworkedPowerUp).</summary>
        public void SetShield(bool active) => m_shieldActive = active;

        // ── ClientRpcs ────────────────────────────────────────────────────────

        [ClientRpc]
        private void ApplyDamageClientRpc(int damage, Vector3 origin)
        {
            ApplyDamageLocally(damage, origin);
        }

        /// <summary>
        /// Fired for non-server clients when external damage (e.g. enemy ContactAttack)
        /// is detected via OnHealthComponentChanged. The server already processed it.
        /// </summary>
        [ClientRpc]
        private void ExternalDamageClientRpc(int damage, Vector3 origin)
        {
            if (IsServer) return;
            ApplyDamageLocally(damage, origin);
        }

        // ── Server-side health component listener ─────────────────────────────

        // Keeps the NetworkVariable in sync when Health is modified outside
        // TakeDamageServerRpc (e.g. enemy ContactAttack, hazards, healing collectibles).
        // When health drops externally, fires ExternalDamageClientRpc so non-server
        // clients enter the correct hurt/death state.
        //
        // The curr == prev guard handles the normal TakeDamageServerRpc path: by the
        // time ApplyDamageClientRpc runs on the host and calls health.Damage(), the
        // NetworkVariable is already set to the same value, so no extra RPC fires.
        private void OnHealthComponentChanged()
        {
            if (!IsServer || m_health == null) return;

            int prev = Health.Value;
            int curr = m_health.current;

            if (curr == prev) return;

            Health.Value = curr;

            if (curr < prev)
            {
                ExternalDamageClientRpc(prev - curr, transform.position);
                Debug.Log($"[NetworkedHealth] External damage on server for client {OwnerClientId}: {prev} → {curr}.");
            }
        }

        // ── All clients: mirror NetworkVariable → local Health component ──────

        private void OnNetHealthChanged(int _, int newValue)
        {
            // ApplyDamageClientRpc / ExternalDamageClientRpc handle state and animations.
            // This callback exists for HUD/UI that observes Health.Value directly.
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ApplyDamageLocally(int damage, Vector3 origin)
        {
            var player = GetComponent<Player>();
            if (player != null)
                player.ApplyDamage(damage, origin);
            else if (m_health != null)
                m_health.Damage(damage);
        }
    }
}
