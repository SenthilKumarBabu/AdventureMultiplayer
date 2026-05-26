using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Makes this player physically solid to other players.
    ///
    /// The EntityController uses QueryTriggerInteraction.Ignore on all its sweeps,
    /// so the built-in trigger CapsuleCollider is invisible to other players.
    /// This script holds a reference to a second, solid (non-trigger) CapsuleCollider
    /// that other players' EntityControllers CAN detect — enabling player-to-player collision.
    ///
    /// Setup:
    ///   1. Add a child GameObject called "Body" to the player prefab.
    ///   2. Add a CapsuleCollider to it (non-trigger, sized to match EntityController).
    ///   3. Assign it to the Body Collider field on this component.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Player Body Collider")]
    public class PlayerBodyCollider : MonoBehaviour
    {
        [Tooltip("The solid CapsuleCollider child that other players collide against.")]
        [SerializeField] private CapsuleCollider bodyCollider;

        private void Start()
        {
            if (bodyCollider == null)
            {
                Debug.LogWarning("[PlayerBodyCollider] No body collider assigned.", this);
                return;
            }

            // The player's own EntityController must ignore its own solid body,
            // otherwise it would push itself away from the ground.
            var controller = GetComponent<EntityController>();
            if (controller != null)
                controller.IgnoreCollider(bodyCollider);

            Debug.Log($"[PlayerBodyCollider] Solid body collider registered on '{name}'.");
        }
    }
}
