using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Multiplayer-safe kill zone.
    ///
    /// Problem: the base KillZone calls player.Die() for every Player-tagged collider
    /// that enters the trigger. In multiplayer all player instances exist on every client,
    /// so the base class would kill ghost players too, corrupting other clients' state.
    ///
    /// Fix: only call Die() if the entering collider belongs to a NetworkObject we own
    /// (i.e. it is our local player). Ghosts are skipped entirely.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Kill Zone")]
    public class NetworkKillZone : KillZone
    {
        protected override void OnTriggerEnter(Collider other)
        {
            if (m_level != null && m_level.isFinished)
                return;

            if (!other.CompareTag(GameTags.Player))
                return;

            var netObj = other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner)
                return;

            var player = other.GetComponentInParent<Player>();
            if (player == null)
                return;

            Debug.Log($"[NetworkKillZone] Player '{player.name}' entered kill zone.");
            player.Die();
            player.states.Change<DiePlayerState>();
        }
    }
}
