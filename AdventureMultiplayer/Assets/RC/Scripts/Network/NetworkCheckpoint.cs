using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Multiplayer respawn checkpoint.
    ///
    /// When the owner of a player NetworkObject walks through this trigger, the server
    /// records it and tells that client to update their NetworkRespawner's respawn
    /// position to <see cref="respawnPoint"/>.
    ///
    /// No dependency on PLAYER TWO Checkpoint, LevelCheckpoint, or Level.instance.
    ///
    /// Setup:
    ///   - Add to any GO with a trigger BoxCollider.
    ///   - Assign respawnPoint (the "Respawn" child Transform).
    ///   - Optionally assign a clip for the activation sound.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Checkpoint")]
    public class NetworkCheckpoint : NetworkBehaviour
    {
        [SerializeField] private Transform  respawnPoint;
        [SerializeField] private AudioClip  clip;
        [SerializeField] private AudioSource audioSource;

        // Per-client activation tracking (server only).
        private readonly HashSet<ulong> _activatedBy = new();

        private void OnTriggerStay(Collider other)
        {
            if (!IsSpawned || !IsServer) return;
            if (!GameTags.IsPlayer(other)) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;

            ulong clientId = netObj.OwnerClientId;
            if (_activatedBy.Contains(clientId)) return;
            _activatedBy.Add(clientId);

            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
            Debug.Log($"[NetworkCheckpoint] '{name}' activated by clientId={clientId} at {pos}");
            ActivateClientRpc(clientId, pos);
        }

        [ClientRpc]
        private void ActivateClientRpc(ulong clientId, Vector3 respawnPos)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;

            // Play sound.
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);

            // Show HUD notification.
            CheckpointHUD.Instance?.Show();

            // Find our own NetworkRespawner and store the new respawn position.
            var respawner = FindLocalPlayerRespawner();
            if (respawner != null)
            {
                respawner.SetRespawnPoint(respawnPos);
                Debug.Log($"[NetworkCheckpoint] '{name}' respawn point set to {respawnPos} for clientId={clientId}");
            }
            else
            {
                Debug.LogWarning($"[NetworkCheckpoint] '{name}' could not find NetworkRespawner for clientId={clientId}");
            }
        }

        private static NetworkRespawner FindLocalPlayerRespawner()
        {
            foreach (var no in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (!no.IsOwner) continue;
                var r = no.GetComponent<NetworkRespawner>();
                if (r != null) return r;
            }
            return null;
        }
    }
}
