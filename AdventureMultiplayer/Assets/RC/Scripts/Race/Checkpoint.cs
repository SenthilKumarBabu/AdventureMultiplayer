using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Place on a trigger collider in the scene to mark a race checkpoint or the finish line.
    ///
    /// The server detects when a player passes through and notifies RaceManager.
    /// Index 0 = first checkpoint after start. Finish line uses isFinishLine = true.
    ///
    /// Setup:
    ///   - Add a Box/Sphere Collider set to Is Trigger.
    ///   - Set the index in order (0, 1, 2 … N).
    ///   - Tick Is Finish Line on the last checkpoint.
    ///   - Drag all checkpoints into RaceManager.checkpoints in order.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Race Checkpoint")]
    public class RaceCheckpoint : MonoBehaviour
    {
        [SerializeField] public int  index;
        [SerializeField] public bool isFinishLine;

        // Tracks owners already processed this crossing so multiple colliders on the
        // same player (Body, Stomp Hitbox, root) don't send duplicate RPCs.
        private readonly System.Collections.Generic.HashSet<ulong> m_passedOwners = new();

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Checkpoint {index}] OnTriggerEnter: '{other.gameObject.name}' tag={other.tag}");

            if (RaceManager.Instance == null)
            {
                Debug.LogWarning($"[Checkpoint {index}] RaceManager.Instance is null — skipped.");
                return;
            }

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null)
            {
                Debug.Log($"[Checkpoint {index}] No NetworkObject in parent of '{other.gameObject.name}' — skipped.");
                return;
            }

            if (!netObj.IsOwner)
            {
                Debug.Log($"[Checkpoint {index}] Not owner (OwnerClientId={netObj.OwnerClientId}) — skipped.");
                return;
            }

            if (other.GetComponentInParent<Player>() == null)
            {
                Debug.Log($"[Checkpoint {index}] No Player component in parent — skipped.");
                return;
            }

            // Deduplicate: each owner is processed only once per checkpoint.
            if (!m_passedOwners.Add(netObj.OwnerClientId))
            {
                Debug.Log($"[Checkpoint {index}] Owner {netObj.OwnerClientId} already processed — skipped.");
                return;
            }

            Debug.Log($"[Checkpoint {index}] Owner {netObj.OwnerClientId} crossed  isFinishLine={isFinishLine} — sending RPC.");
            RaceManager.Instance.ReportCheckpointServerRpc(index, isFinishLine);

            // Update the owner's respawn point to this checkpoint position.
            var respawner = other.GetComponentInParent<NetworkRespawner>();
            if (respawner != null)
                respawner.SetRespawnPoint(transform.position);
            else
                Debug.LogWarning($"[Checkpoint {index}] No NetworkRespawner found on player — respawn point not updated.");

            // Show local HUD immediately — don't wait for the server round-trip.
            if (!isFinishLine)
                CheckpointHUD.Instance?.Show($"Checkpoint {index}!");
        }
    }
}
