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

        private void OnTriggerEnter(Collider other)
        {
            // Collision detection is server-authoritative.
            if (RaceManager.Instance == null || !RaceManager.Instance.IsServer) return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null) return;

            // Ignore non-player objects (enemies, collectibles, etc.).
            if (other.GetComponentInParent<Player>() == null) return;

            ulong clientId = netObj.OwnerClientId;

            if (isFinishLine)
                RaceManager.Instance.PlayerFinished(clientId);
            else
                RaceManager.Instance.RegisterCheckpoint(clientId, index);
        }
    }
}
