using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// One point on a <see cref="BotRoute"/>. The <see cref="action"/> says what the bot does
    /// when it LEAVES this point, heading for the next one:
    ///   Walk       — normal running, bots spread across <see cref="laneWidth"/>.
    ///   Careful    — narrow ground (bridge, plank, disc): single file, no run, never jumps.
    ///   Jump       — take off from here toward the next waypoint (sprint in, single file).
    ///   DoubleJump — as Jump, plus a second jump near the top of the arc.
    ///   Wait       — stop here until the hazard zone ahead is clear (or, with
    ///                <see cref="waitFor"/> = Footing, until the next waypoint's moving
    ///                ground — a swinging plank — is lined up), then go.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/AI/Bot Waypoint")]
    public class BotWaypoint : MonoBehaviour
    {
        public enum Action { Walk, Careful, Jump, DoubleJump, Wait }

        /// <summary>What a Wait waypoint waits for.</summary>
        public enum WaitFor
        {
            HazardClear, // the watch zone has no moving hazard in it
            Footing,     // the next waypoint has ground under it at its own height (swinging plank lined up)
        }

        [SerializeField] public Action action = Action.Walk;

        [Tooltip("How close (horizontally) a bot must get to count this waypoint as reached.")]
        [SerializeField] public float radius = 1.2f;

        [Tooltip("Total side-to-side spread of bots on the way TO this waypoint. 0 = single file.")]
        [SerializeField] public float laneWidth = 2f;

        [Header("Wait only")]
        [SerializeField] public WaitFor waitFor = WaitFor.HazardClear;
        [Tooltip("Centre of the zone that must be free of hazards before the bot goes. " +
                 "Empty = halfway to the next waypoint.")]
        [SerializeField] public Transform watchPoint;
        [SerializeField] public float watchRadius = 2f;
        [Tooltip("Give up waiting and go anyway after this many seconds.")]
        [SerializeField] public float maxWait = 6f;

        public bool IsJump => action == Action.Jump || action == Action.DoubleJump;
    }
}
