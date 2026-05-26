using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Replaces AirDivePlayerState on Spike.
    /// Adds an immediate downward velocity on dive entry so the player
    /// descends toward the ground instead of gliding forward at height.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Spike Air Dive State")]
    public class SpikeAirDiveState : AirDivePlayerState
    {
        [Tooltip("Downward speed applied when the dive starts.")]
        [SerializeField] private float diveDownwardForce = 20f;

        protected override void OnEnter(Player player)
        {
            base.OnEnter(player); // sets vertical=0, lateral=forward*airDiveForwardForce
            player.verticalVelocity = Vector3.down * diveDownwardForce;
        }
    }
}
