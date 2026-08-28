using System.Collections.Generic;
using PLAYERTWO.PlatformerProject;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// A power-up box in the scene. Server is authoritative.
    ///
    /// When a player walks into the trigger:
    ///   1. Server picks a random PowerUpType from this box's type pool.
    ///   2. Server calls TryAddPowerUp on the player's PlayerPowerUpInventory.
    ///   3. Box hides (NetworkVariable) and starts a respawn timer.
    ///   4. After respawnDelay seconds the box becomes visible again.
    ///
    /// Box type determines which power-ups can drop:
    ///   Yellow — SpeedBoost, Banana, DecoyBox  (safe / positional)
    ///   Red    — Rocket, StunBolt, Swap, Freeze (offensive)
    ///   Green  — Shield, Invisible (defensive)
    ///
    /// Setup:
    ///   - Add NetworkObject + trigger Collider + this component.
    ///   - Assign the visual renderer so the box can hide/show.
    ///   - Register the prefab in NetworkManager's NetworkPrefabs list.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Rush Champions/Networked Power-Up Box")]
    public class NetworkedPowerUpBox : NetworkBehaviour
    {
        [SerializeField] private PowerUpType powerUpType   = PowerUpType.SpeedBoost;
        [SerializeField] private float       respawnDelay = 10f;
        [SerializeField] private Renderer    boxRenderer;
        [SerializeField] private Collider    boxCollider;

        // Synced to clients so all machines show/hide the box in sync
        private NetworkVariable<bool> m_Active = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Whether this box currently has a power-up to give (not hidden/on cooldown).</summary>
        public bool IsActive => m_Active.Value;

        /// <summary>
        /// Every spawned box in the scene, server + client. Lets bot AI (RaceBotBrain) find
        /// nearby boxes to steer toward without an expensive scene-wide search every frame.
        /// </summary>
        public static readonly List<NetworkedPowerUpBox> All = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            m_Active.OnValueChanged += OnActiveChanged;
            ApplyActiveState(m_Active.Value);
        }

        public override void OnNetworkDespawn()
        {
            All.Remove(this);
            m_Active.OnValueChanged -= OnActiveChanged;
        }

        // ── Trigger (server only) ─────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !m_Active.Value) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;
            if (other.GetComponentInParent<Player>() == null) return;

            // Look up the inventory directly on the colliding NetworkObject rather than
            // through PlayerPowerUpInventory.All by OwnerClientId — bots all default to
            // OwnerClientId 0 (server-owned), so a clientId-keyed lookup here would
            // route a bot's pickup into whichever inventory registered last under that
            // same key (often the human host's), instead of the bot's own.
            var inv = netObj.GetComponent<PlayerPowerUpInventory>();
            if (inv == null)
            {
                Debug.Log($"[PowerUpBox] '{netObj.name}' has no PlayerPowerUpInventory — skipping.");
                return;
            }

            bool added = inv.TryAddPowerUp(powerUpType);

            if (added)
            {
                Debug.Log($"[PowerUpBox] '{netObj.name}' (NetworkObjectId={netObj.NetworkObjectId}) got {powerUpType}.");
                m_Active.Value = false;
                RespawnAsync().Forget();
            }
        }

        // ── Respawn ───────────────────────────────────────────────────────────

        private async Cysharp.Threading.Tasks.UniTaskVoid RespawnAsync()
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(
                System.TimeSpan.FromSeconds(respawnDelay),
                cancellationToken: destroyCancellationToken);

            if (IsServer)
            {
                m_Active.Value = true;
                Debug.Log($"[PowerUpBox] {powerUpType} box respawned.");
            }
        }

        // ── Visual sync (all clients) ─────────────────────────────────────────

        private void OnActiveChanged(bool _, bool isActive) => ApplyActiveState(isActive);

        private void ApplyActiveState(bool isActive)
        {
            if (boxRenderer != null) boxRenderer.enabled = isActive;
            if (boxCollider != null) boxCollider.enabled = isActive;
        }

    }
}
