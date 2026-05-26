using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative ItemBox.
    ///
    /// Root problem: Entity.InvokeContacts calls collider.GetComponents(buffer) which
    /// returns ALL components regardless of MonoBehaviour.enabled, so setting
    /// m_itemBox.enabled=false does NOT stop ItemBox.OnEntityContact from firing.
    /// ItemBox.Collect() checks the internal protected bool m_enabled, not MonoBehaviour.enabled.
    ///
    /// Fix:
    ///   1. Use reflection to set m_enabled=false on the ItemBox so its local Collect() is
    ///      a no-op whenever the server hasn't authorised a collection.
    ///   2. Intercept the same IEntityContact on this NetworkBehaviour.
    ///   3. Send CollectServerRpc → server sets m_enabled=true, calls Collect(), tracks
    ///      dispensed count, sends CollectClientRpc so all clients mirror the result.
    ///   4. Keep m_enabled=false between hits so local physics never auto-collects.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(ItemBox))]
    [AddComponentMenu("Adventure Multiplayer/Networked Item Box")]
    public class NetworkedItemBox : NetworkBehaviour, IEntityContact
    {
        private ItemBox m_itemBox;
        private bool m_pendingCollect;  // debounce: true while waiting for ClientRpc
        private int  m_dispensedCount;  // how many items have been server-authorised

        // Reflection handle for the protected ItemBox.m_enabled field.
        private static readonly System.Reflection.FieldInfo s_mEnabledField =
            typeof(ItemBox).GetField("m_enabled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private void SetBoxEnabled(bool value) => s_mEnabledField?.SetValue(m_itemBox, value);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            m_itemBox = GetComponent<ItemBox>();

            // Disable MonoBehaviour lifecycle (safe: ItemBox has no Update loop).
            m_itemBox.enabled = false;

            // Block ItemBox.Collect via the internal flag so local contact never collects.
            SetBoxEnabled(false);

            Debug.Log($"[NetworkedItemBox] OnNetworkSpawn '{name}' — reflectionField={s_mEnabledField != null} collectibles={m_itemBox?.collectibles?.Length} local collection blocked.");
        }

        // ── IEntityContact — fires on every client when EntityController hits this box ──

        public void OnEntityContact(Entity entity)
        {
            // Log every contact so we can see if this fires at all during a jump.
            Debug.Log($"[NetworkedItemBox] Contact '{name}' entity={entity.name} vertVel={entity.verticalVelocity.y:F2} isPlayer={entity is Player} IsSpawned={IsSpawned} pending={m_pendingCollect}");

            if (!IsSpawned) return;
            if (m_pendingCollect) return;
            if (m_dispensedCount >= m_itemBox.collectibles.Length) return;
            if (entity is not Player player) return;

            var offset = entity.height * 0.5f - entity.radius;
            var head   = entity.position + entity.transform.up * (offset - Physics.defaultContactOffset);
            var col    = GetComponent<BoxCollider>();
            bool isAirborne = !entity.isGrounded;
            bool abovePoint = BoundsHelper.IsAbovePoint(col, head);

            Debug.Log($"[NetworkedItemBox] Check '{name}' isAirborne={isAirborne} abovePoint={abovePoint} vertVel={entity.verticalVelocity.y:F2} headY={head.y:F2} boxBottomY={col.bounds.min.y:F2}");

            if (!isAirborne || !abovePoint) return;

            var netObj = player.GetComponent<NetworkObject>();
            if (netObj == null) { Debug.LogWarning($"[NetworkedItemBox] No NetworkObject on player"); return; }

            m_pendingCollect = true;
            Debug.Log($"[NetworkedItemBox] '{name}' SENDING CollectServerRpc clientId={netObj.OwnerClientId}");
            CollectServerRpc(netObj.OwnerClientId);
        }

        // ── Server ────────────────────────────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        private void CollectServerRpc(ulong playerClientId)
        {
            // Guard: box may be exhausted before RPC arrived.
            if (m_dispensedCount >= m_itemBox.collectibles.Length)
            {
                Debug.LogWarning($"[NetworkedItemBox] CollectServerRpc ignored — box exhausted ({m_dispensedCount}/{m_itemBox.collectibles.Length}).");
                CollectClientRpc(playerClientId, false);
                return;
            }

            // Temporarily re-enable so Collect() can run its logic.
            SetBoxEnabled(true);

            var player = NetworkUtils.FindPlayerByClientId(playerClientId);

            bool collected = false;
            if (player != null)
            {
                m_itemBox.Collect(player);
                player.verticalVelocity = Vector3.zero;
                m_dispensedCount++;
                collected = true;
                Debug.Log($"[NetworkedItemBox] Server collected item {m_dispensedCount}/{m_itemBox.collectibles.Length} for clientId={playerClientId}.");
            }

            // Block local collection again.
            SetBoxEnabled(false);

            CollectClientRpc(playerClientId, collected);
        }

        // ── Clients ───────────────────────────────────────────────────────────

        [ClientRpc]
        private void CollectClientRpc(ulong playerClientId, bool collected)
        {
            m_pendingCollect = false;

            if (!collected) return;

            // Server already ran Collect; mirror on non-server clients.
            if (!IsServer)
            {
                m_dispensedCount++;

                SetBoxEnabled(true);

                var player = NetworkUtils.FindPlayerByClientId(playerClientId);

                if (player != null)
                    m_itemBox.Collect(player);

                SetBoxEnabled(false);

                Debug.Log($"[NetworkedItemBox] Client mirrored collection {m_dispensedCount}/{m_itemBox.collectibles.Length}.");
            }
        }
    }
}
