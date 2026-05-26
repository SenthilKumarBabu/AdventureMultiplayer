using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Synchronises Pickable grab / throw across all clients.
    ///
    /// Does NOT use TrySetParent — parenting a crate NetworkObject to a player
    /// NetworkObject interferes with NGO's transform synchronisation on the player.
    ///
    /// Carry:   m_holderClientId NetworkVariable tracks who holds the crate.
    ///          LateUpdate on every client drives crate position to the holder's
    ///          grabber slot. The holding client also writes the exact world position
    ///          into m_heldPosition each frame so others don't depend on ghost lag.
    /// Release: ThrowServerRpc clears m_holderClientId. ThrowClientRpc sends the
    ///          authoritative release position + velocity to all non-throwers.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkPickable))]
    [AddComponentMenu("Adventure Multiplayer/Networked Pickable")]
    public class NetworkedPickable : NetworkBehaviour
    {
        private NetworkPickable m_networkPickable;
        private Pickable        m_pickable;
        private Rigidbody       m_rigidbody;
        private Collider        m_collider;

        private readonly NetworkVariable<ulong> m_holderClientId = new(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Authoritative hold position written by the holding client every frame.
        private readonly NetworkVariable<Vector3> m_heldPosition = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<Quaternion> m_heldRotation = new(
            Quaternion.identity,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            m_networkPickable = GetComponent<NetworkPickable>();
            m_pickable        = GetComponent<Pickable>();
            m_rigidbody       = GetComponent<Rigidbody>();
            m_collider        = GetComponent<Collider>();

            m_pickable.onPicked.AddListener(OnPickedLocally);
            m_pickable.onReleased.AddListener(OnReleasedLocally);
            m_holderClientId.OnValueChanged += OnHolderChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (m_pickable != null)
            {
                m_pickable.onPicked.RemoveListener(OnPickedLocally);
                m_pickable.onReleased.RemoveListener(OnReleasedLocally);
            }
            m_holderClientId.OnValueChanged -= OnHolderChanged;
        }

        // ── LateUpdate ────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (!IsSpawned) return;
            var holder = m_holderClientId.Value;
            if (holder == ulong.MaxValue) return;

            bool isLocalHolder = holder == NetworkManager.Singleton.LocalClientId;

            if (isLocalHolder)
            {
                var slot = FindGrabberSlot(holder);
                if (slot == null) return;
                var pos = slot.position + slot.rotation * m_pickable.offset;
                var rot = slot.rotation;
                transform.SetPositionAndRotation(pos, rot);
                // Publish exact position for other clients.
                UpdateHeldPositionServerRpc(pos, rot);
            }
            else
            {
                // Use the authoritative position published by the holder.
                transform.SetPositionAndRotation(m_heldPosition.Value, m_heldRotation.Value);
            }
        }

        // ── Local callbacks ───────────────────────────────────────────────────

        private void OnPickedLocally()
        {
            if (!IsSpawned) return;
            GrabServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        private void OnReleasedLocally()
        {
            if (!IsSpawned) return;
            var dir   = m_networkPickable != null ? m_networkPickable.LastThrowDirection : Vector3.zero;
            var force = m_networkPickable != null ? m_networkPickable.LastThrowForce     : 0f;
            ThrowServerRpc(dir, force, NetworkManager.Singleton.LocalClientId,
                           transform.position, transform.rotation);
        }

        // ── ServerRpcs ────────────────────────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        private void UpdateHeldPositionServerRpc(Vector3 pos, Quaternion rot)
        {
            m_heldPosition.Value = pos;
            m_heldRotation.Value = rot;
        }

        [ServerRpc(RequireOwnership = false)]
        private void GrabServerRpc(ulong grabberClientId)
        {
            m_holderClientId.Value = grabberClientId;
            if (m_rigidbody != null) m_rigidbody.isKinematic = true;
            if (m_collider  != null) m_collider.isTrigger    = true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void ThrowServerRpc(Vector3 direction, float force, ulong throwerClientId,
                                    Vector3 releasePos, Quaternion releaseRot)
        {
            m_holderClientId.Value = ulong.MaxValue;
            transform.SetPositionAndRotation(releasePos, releaseRot);
            ThrowClientRpc(direction, force, throwerClientId, releasePos, releaseRot);
        }

        // ── ClientRpcs ────────────────────────────────────────────────────────

        [ClientRpc]
        private void ThrowClientRpc(Vector3 direction, float force, ulong throwerClientId,
                                    Vector3 releasePos, Quaternion releaseRot)
        {
            if (NetworkManager.Singleton.LocalClientId == throwerClientId) return;

            transform.SetPositionAndRotation(releasePos, releaseRot);

            if (m_rigidbody != null)
            {
                m_rigidbody.isKinematic = false;
#if UNITY_6000_0_OR_NEWER
                m_rigidbody.linearVelocity = direction * force;
#else
                m_rigidbody.velocity = direction * force;
#endif
            }
            if (m_collider != null) m_collider.isTrigger = false;
        }

        // ── NetworkVariable callback ──────────────────────────────────────────

        private void OnHolderChanged(ulong oldHolder, ulong newHolder)
        {
            bool nowHeld = newHolder != ulong.MaxValue;
            if (nowHeld)
            {
                if (m_rigidbody != null) m_rigidbody.isKinematic = true;
                if (m_collider  != null) m_collider.isTrigger    = true;
            }
            // Release handled by ThrowClientRpc / NetworkPickable.Release.
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Transform FindGrabberSlot(ulong clientId)
        {
            var netObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (netObj == null) return null;
            if (!netObj.TryGetComponent<PlayerObjectGrabber>(out var grabber)) return null;
            return grabber.grabberSlot;
        }
    }
}
