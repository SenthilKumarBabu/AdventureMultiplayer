using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Add this alongside Collectible + NetworkObject to any standalone scene collectible
    /// (coin, star, heart, etc.) to make collection server-authoritative.
    ///
    /// Problem: Collectible.OnTriggerStay fires locally on whichever client's physics engine
    /// detects the player contact. The other machine never sees it, so LevelScore diverges.
    ///
    /// Fix: set collectOnContact=false so the base class never auto-collects, then intercept
    /// OnTriggerEnter here and route through a ServerRpc so every machine calls Collect()
    /// exactly once via CollectClientRpc.
    ///
    /// Do NOT add this to collectibles that live inside an ItemBox — those are already
    /// handled by NetworkedItemBox which calls ItemBox.Collect() server-side.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collectible))]
    [AddComponentMenu("Adventure Multiplayer/Networked Collectible")]
    public class NetworkedCollectible : NetworkBehaviour
    {
        private Collectible m_collectible;

        // Local guard: prevents spamming ServerRpc while waiting for CollectClientRpc confirmation.
        private bool m_pendingCollect;

        public override void OnNetworkSpawn()
        {
            m_collectible = GetComponent<Collectible>();

            // Block the base class OnTriggerStay auto-collection.
            // We intercept the trigger ourselves and route through the server.
            m_collectible.collectOnContact = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_pendingCollect) return;
            if (!IsSpawned) return;
            if (!GameTags.IsPlayer(other)) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogWarning($"[NetworkedCollectible] '{name}': player collider has no NetworkObject in parent — cannot identify owner.");
                return;
            }

            Debug.Log($"[NetworkedCollectible] '{name}': trigger entered by clientId={netObj.OwnerClientId}. Sending ServerRpc.");
            m_pendingCollect = true;
            CollectServerRpc(netObj.OwnerClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void CollectServerRpc(ulong playerClientId)
        {
            Debug.Log($"[NetworkedCollectible] '{name}': CollectServerRpc from clientId={playerClientId}. colliderEnabled={m_collectible?.triggerCollider.enabled}");
            if (m_collectible == null || !m_collectible.triggerCollider.enabled) return;

            var player = NetworkUtils.FindPlayerByClientId(playerClientId);
            if (player != null)
                m_collectible.Collect(player);

            CollectClientRpc(playerClientId);
        }

        [ClientRpc]
        private void CollectClientRpc(ulong playerClientId)
        {
            if (IsServer) return;

            Debug.Log($"[NetworkedCollectible] '{name}': CollectClientRpc for clientId={playerClientId}.");
            m_pendingCollect = true;

            var player = NetworkUtils.FindPlayerByClientId(playerClientId);
            if (player != null)
                m_collectible.Collect(player);
        }
    }
}
