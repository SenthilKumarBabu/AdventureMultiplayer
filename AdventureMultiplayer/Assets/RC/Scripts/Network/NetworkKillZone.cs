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

            // A checkpoint/spawn point can sit close enough to a kill zone that respawning
            // teleports the player straight back into this trigger, re-firing OnTriggerEnter
            // and killing them again before they can move — an infinite death loop that looks
            // like the player/bot is permanently "stuck". The brief post-respawn grace window
            // gives them a chance to step clear first.
            var respawner = other.GetComponentInParent<NetworkRespawner>();
            if (respawner != null && respawner.IsRespawnProtected)
            {
                Debug.Log($"[NetworkKillZone] '{player.name}' is respawn-protected — kill zone ignored.");
                return;
            }

            Debug.Log($"[NetworkKillZone] Player '{player.name}' entered kill zone.");
            player.Die();
            player.states.Change<DiePlayerState>();
        }
    }
}
