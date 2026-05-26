using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Syncs discrete particle events (land, stomp, dash, hurt) via RPCs with baked-in
    /// world position so smoke always appears at the correct location on all clients.
    ///
    /// Owner: subscribes to PLAYER TWO events, sends ServerRpc when they fire.
    /// All clients: receive ClientRpc, move the particle system to the given world
    ///              position and play it — no position guessing from interpolation.
    ///
    /// Add to the player prefab alongside NetworkedMovementSync.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Particle Sync")]
    public class NetworkedParticleSync : NetworkBehaviour
    {
        private Player               m_player;
        private PlayerParticles      m_particles;
        private NetworkedMovementSync m_movementSync;

        public override void OnNetworkSpawn()
        {
            m_player       = GetComponent<Player>();
            m_particles    = GetComponent<PlayerParticles>();
            m_movementSync = GetComponent<NetworkedMovementSync>();

            if (IsOwner && m_player != null)
            {
                m_player.entityEvents.OnGroundEnter.AddListener(OnOwnerLanded);
                m_player.playerEvents.OnStompLanding.AddListener(OnOwnerStompLanded);
                m_player.playerEvents.OnDashStarted.AddListener(OnOwnerDashStarted);
                m_player.playerEvents.OnDashEnded.AddListener(OnOwnerDashEnded);
                m_player.playerEvents.OnHurt.AddListener(OnOwnerHurt);
            }

            if (!IsOwner && m_movementSync != null)
                m_movementSync.OnRemoteLanded += OnRemoteLandedFallback;
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && m_player != null)
            {
                m_player.entityEvents.OnGroundEnter.RemoveListener(OnOwnerLanded);
                m_player.playerEvents.OnStompLanding.RemoveListener(OnOwnerStompLanded);
                m_player.playerEvents.OnDashStarted.RemoveListener(OnOwnerDashStarted);
                m_player.playerEvents.OnDashEnded.RemoveListener(OnOwnerDashEnded);
                m_player.playerEvents.OnHurt.RemoveListener(OnOwnerHurt);
            }

            if (!IsOwner && m_movementSync != null)
                m_movementSync.OnRemoteLanded -= OnRemoteLandedFallback;
        }

        // ── Owner event handlers ──────────────────────────────────────────────

        private void OnOwnerLanded()
        {
            if (m_particles == null || m_particles.landDust == null) return;
            float speed = m_player != null ? Mathf.Abs(m_player.velocity.y) : 0f;
            if (speed >= m_particles.landingParticleMinSpeed)
                BroadcastLandDustServerRpc(transform.position);
        }

        // Stomp landing always plays dust regardless of speed threshold.
        private void OnOwnerStompLanded() => BroadcastLandDustServerRpc(transform.position);

        private void OnOwnerDashStarted() => BroadcastDashStartServerRpc();
        private void OnOwnerDashEnded()   => BroadcastDashEndServerRpc();
        private void OnOwnerHurt()        => BroadcastHurtServerRpc();

        // ── ServerRpcs — relay to all clients ─────────────────────────────────

        [ServerRpc]
        private void BroadcastLandDustServerRpc(Vector3 position) => PlayLandDustClientRpc(position);

        [ServerRpc]
        private void BroadcastDashStartServerRpc() => PlayDashStartClientRpc();

        [ServerRpc]
        private void BroadcastDashEndServerRpc() => PlayDashEndClientRpc();

        [ServerRpc]
        private void BroadcastHurtServerRpc() => PlayHurtClientRpc();

        // ── ClientRpcs — play on all clients at the given world position ──────

        [ClientRpc]
        private void PlayLandDustClientRpc(Vector3 position)
        {
            if (m_particles == null || m_particles.landDust == null) return;
            m_particles.landDust.transform.position = position;
            m_particles.Play(m_particles.landDust);
        }

        [ClientRpc]
        private void PlayDashStartClientRpc()
        {
            if (m_particles == null) return;
            if (m_particles.dashDust   != null) m_particles.Play(m_particles.dashDust);
            if (m_particles.speedTrails != null) m_particles.Play(m_particles.speedTrails);
        }

        [ClientRpc]
        private void PlayDashEndClientRpc()
        {
            if (m_particles?.speedTrails != null)
                m_particles.Stop(m_particles.speedTrails, true);
        }

        [ClientRpc]
        private void PlayHurtClientRpc()
        {
            if (m_particles?.hurtDust != null)
                m_particles.Play(m_particles.hurtDust);
        }

        // ── Fallback: if RPC hasn't arrived, use movement sync landing event ──

        private void OnRemoteLandedFallback(Vector3 position, float fallSpeed)
        {
            if (m_particles == null || m_particles.landDust == null) return;
            if (fallSpeed < m_particles.landingParticleMinSpeed) return;
            m_particles.landDust.transform.position = position;
            m_particles.Play(m_particles.landDust);
        }
    }
}
