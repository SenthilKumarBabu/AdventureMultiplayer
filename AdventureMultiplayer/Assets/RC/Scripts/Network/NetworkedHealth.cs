using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative health and damage.
    ///
    /// The server is the single source of truth for health values. When damage is
    /// requested (via ServerRpc), the server validates it (shield check, Invisible),
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
        public NetworkVariable<int>  Health          { get; private set; } =
            new(0,     NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Replicated so every client can observe shield/invincibility for VFX or HUD.
        public NetworkVariable<bool> ShieldActive    { get; private set; } =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Invisible merges the old Invincibility (damage immunity) and Invisibility
        // (physical pass-through) power-ups into a single state.
        public NetworkVariable<bool> InvisibleActive { get; private set; } =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Health m_health;

        // Read directly from the NetworkVariables (not a mirrored local bool) so the
        // correct value is visible on every peer, not just the server that writes it.
        public bool IsShielded  => ShieldActive.Value;
        public bool IsInvisible => InvisibleActive.Value;
        public int  MaxHealth   => m_health != null ? m_health.max : 100;

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
            if (InvisibleActive.Value)
            {
                Debug.Log($"[NetworkedHealth] Invisible blocked hit for client {OwnerClientId}.");
                return;
            }

            // Same reasoning as NetworkKillZone: a hazard sitting close to a checkpoint/spawn
            // point can otherwise re-kill the player the instant they respawn, looping forever.
            var respawner = GetComponent<NetworkRespawner>();
            if (respawner != null && respawner.IsRespawnProtected)
            {
                Debug.Log($"[NetworkedHealth] Respawn-protected — hit ignored for client {OwnerClientId}.");
                return;
            }

            if (ShieldActive.Value)
            {
                SetShield(false);
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

        /// <summary>
        /// Called directly from server-side power-up code (e.g. RocketProjectile).
        /// Bypasses PLAYER TWO's recovery-frame cooldown by using Health.Set() instead of Damage().
        /// Shield / invincibility must be validated by the caller before invoking this.
        /// </summary>
        public void ApplyDamageFromPowerUp(int damage, Vector3 origin)
        {
            if (!IsServer) return;

            int newHealth = Mathf.Max(0, Health.Value - damage);
            Health.Value  = newHealth;

            // Broadcast new health to all clients so HUD / health bar update everywhere.
            ForceSetHealthClientRpc(newHealth, origin);

            // If this is a kill shot, tell ONLY the owning client to enter DiePlayerState.
            // The owner has the NetworkRespawner listener; forcing die on all clients
            // would leave non-owners stuck in death with nobody to call Respawn().
            if (newHealth <= 0)
                TriggerOwnerDeathClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                });

            Debug.Log($"[NetworkedHealth] Power-up damage {damage} → client {OwnerClientId} HP {newHealth}");
        }

        [ClientRpc]
        private void ForceSetHealthClientRpc(int value, Vector3 origin)
        {
            if (m_health != null)
                m_health.Set(value);

            Debug.Log($"[NetworkedHealth] ForceSetHealth {value} on client {OwnerClientId}");
        }

        /// <summary>
        /// Sent only to the owner client when killed by a power-up.
        /// player.Die() fires playerEvents.OnDie → NetworkRespawner starts the respawn timer.
        /// DiePlayerState is then forced to freeze the player during the delay.
        /// NOTE: DiePlayerState.OnEnter is empty in PLAYER TWO — it does NOT fire OnDie itself.
        /// </summary>
        [ClientRpc]
        private void TriggerOwnerDeathClientRpc(ClientRpcParams _ = default)
        {
            var player = GetComponent<Player>();
            if (player == null) return;

            player.Die();                              // fires playerEvents.OnDie → NetworkRespawner
            player.states.Change<DiePlayerState>();    // freeze visually during respawn delay

            Debug.Log($"[NetworkedHealth] TriggerOwnerDeath on client {OwnerClientId}");
        }

        /// <summary>
        /// Called by NetworkRespawner on the owner after player.Respawn() restores health locally.
        /// Calling m_health.Set() on the server fires onChange → OnHealthComponentChanged which
        /// updates Health.Value, keeping the server state consistent. ForceSetHealthClientRpc
        /// then propagates the restored value to every client's HUD.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncRespawnHealthServerRpc()
        {
            if (!IsServer || m_health == null) return;
            // Set on server first so OnHealthComponentChanged sees curr==max (no spurious damage RPC).
            m_health.Set(m_health.max);
            // Explicitly sync to all clients so their HUD updates immediately.
            ForceSetHealthClientRpc(m_health.max, transform.position);
            Debug.Log($"[NetworkedHealth] Respawn health sync: {m_health.max} for client {OwnerClientId}");
        }

        /// <summary>Activate/deactivate shield. Replicates state to all clients via ShieldActive NetworkVariable.</summary>
        public void SetShield(bool active)
        {
            if (IsServer) ShieldActive.Value = active;
        }

        /// <summary>Activate/deactivate Invisible. Replicates state to all clients via InvisibleActive NetworkVariable.</summary>
        public void SetInvisible(bool active)
        {
            if (IsServer) InvisibleActive.Value = active;
        }

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

            // Enemy.ContactAttack and ObstacleKnockback call player.ApplyDamage directly,
            // bypassing TakeDamageServerRpc entirely, so Invisible can't be checked
            // before the hit lands. Undo it here instead — same net effect, one frame late.
            if (curr < prev && InvisibleActive.Value)
            {
                m_health.Set(prev);
                Debug.Log($"[NetworkedHealth] Invisible reverted external damage for client {OwnerClientId}.");
                return;
            }

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
