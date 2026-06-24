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
    ///   Green  — Shield, Invincibility          (defensive)
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
        [SerializeField] private PowerUpBoxType boxType      = PowerUpBoxType.Yellow;
        [SerializeField] private float          respawnDelay = 10f;
        [SerializeField] private Renderer       boxRenderer;
        [SerializeField] private Collider       boxCollider;

        // Synced to clients so all machines show/hide the box in sync
        private NetworkVariable<bool> m_Active = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // ── Pool per box type ─────────────────────────────────────────────────

        private static readonly PowerUpType[] YellowPool =
        {
            PowerUpType.SpeedBoost,
            PowerUpType.Banana,
            PowerUpType.DecoyBox,
        };

        private static readonly PowerUpType[] RedPool =
        {
            PowerUpType.Rocket,
            PowerUpType.StunBolt,
            PowerUpType.Swap,
            PowerUpType.Freeze,
        };

        private static readonly PowerUpType[] GreenPool =
        {
            PowerUpType.Shield,
            PowerUpType.Invincibility,
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            m_Active.OnValueChanged += OnActiveChanged;
            ApplyActiveState(m_Active.Value);
        }

        public override void OnNetworkDespawn()
        {
            m_Active.OnValueChanged -= OnActiveChanged;
        }

        // ── Trigger (server only) ─────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !m_Active.Value) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;
            if (other.GetComponentInParent<Player>() == null) return;

            ulong clientId = netObj.OwnerClientId;

            if (!PlayerPowerUpInventory.All.TryGetValue(clientId, out var inv))
            {
                Debug.Log($"[PowerUpBox] Client {clientId} has no inventory — skipping.");
                return;
            }

            PowerUpType type = PickRandom();
            bool added = inv.TryAddPowerUp(type);

            if (added)
            {
                Debug.Log($"[PowerUpBox] Client {clientId} got {type} from {boxType} box.");
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
                Debug.Log($"[PowerUpBox] {boxType} box respawned.");
            }
        }

        // ── Visual sync (all clients) ─────────────────────────────────────────

        private void OnActiveChanged(bool _, bool isActive) => ApplyActiveState(isActive);

        private void ApplyActiveState(bool isActive)
        {
            if (boxRenderer != null) boxRenderer.enabled = isActive;
            if (boxCollider != null) boxCollider.enabled = isActive;
        }

        // ── Random pick ───────────────────────────────────────────────────────

        private PowerUpType PickRandom()
        {
            PowerUpType[] pool = boxType switch
            {
                PowerUpBoxType.Red   => RedPool,
                PowerUpBoxType.Green => GreenPool,
                _                   => YellowPool,
            };
            return pool[Random.Range(0, pool.Length)];
        }
    }
}
