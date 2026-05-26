using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Attach to a mushroom collider. When the player lands on top of it,
    /// launches them upward with a configurable boost force — like a spring.
    ///
    /// Uses IEntityContact so it works with the Entity contact pipeline.
    /// Only fires when the player is coming from above (downward or zero vertical velocity).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Adventure Multiplayer/Mushroom Bounce")]
    public class MushroomBounce : MonoBehaviour, IEntityContact
    {
        [SerializeField] private float bounceForce = 15f;

        public void OnEntityContact(Entity entity)
        {
            if (entity is not Player player) return;
            if (!player.isAlive) return;

            // Only bounce when the player is on top: their centre must be above
            // the mushroom's centre, and they must be moving downward (or standing).
            if (player.position.y < transform.position.y) return;
            if (player.verticalVelocity.y > 1f) return;    // already moving strongly upward — skip

            player.verticalVelocity = Vector3.up * bounceForce;
            player.states.Change<FallPlayerState>();

            Debug.Log($"[MushroomBounce] Launched '{player.name}' with force {bounceForce}");
        }
    }
}
