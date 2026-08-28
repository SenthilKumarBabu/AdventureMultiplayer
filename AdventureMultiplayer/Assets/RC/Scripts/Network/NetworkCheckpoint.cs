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

            // AI bots are handled by RaceBotBrain directly (see RaceCheckpoint) — skip them
            // here. Bots default to OwnerClientId 0 (server-owned), which is the SAME ID the
            // Host uses, so letting a bot through this trigger would broadcast an activation
            // that the Host's own client also matches, corrupting the Host's respawn point
            // with the bot's checkpoint position.
            if (other.GetComponentInParent<RaceBotBrain>() != null) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;

            ulong clientId = netObj.OwnerClientId;
            if (_activatedBy.Contains(clientId)) return;
            _activatedBy.Add(clientId);

            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
            Debug.Log($"[NetworkCheckpoint] '{name}' activated by clientId={clientId} at {pos}");

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            };
            ActivateClientRpc(pos, rpcParams);
        }

        [ClientRpc]
        private void ActivateClientRpc(Vector3 respawnPos, ClientRpcParams _ = default)
        {
            // Play sound.
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);

            // Show HUD notification.
            CheckpointHUD.Instance?.Show();

            // Store the new respawn position on OUR OWN player object specifically — not
            // "whichever spawned object happens to satisfy IsOwner first", which on the Host
            // could just as easily be a bot (bots share the Host's OwnerClientId/IsOwner
            // status). PlayerObject is NGO's own "the object spawned via SpawnAsPlayerObject
            // for this client" — bots are never spawned that way, so this can never resolve
            // to a bot's respawner.
            var localObj  = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            var respawner = localObj != null ? localObj.GetComponent<NetworkRespawner>() : null;
            if (respawner != null)
            {
                respawner.SetRespawnPoint(respawnPos);
                Debug.Log($"[NetworkCheckpoint] '{name}' respawn point set to {respawnPos}.");
            }
            else
            {
                Debug.LogWarning($"[NetworkCheckpoint] '{name}' could not find local player's NetworkRespawner.");
            }
        }
    }
}
