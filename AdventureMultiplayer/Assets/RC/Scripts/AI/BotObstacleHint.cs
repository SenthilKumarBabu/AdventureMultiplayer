using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drop this on an obstacle (or its parent) to tell <see cref="RaceBotBrain"/>
    /// exactly how to treat it, overriding the reactive raycast heuristics.
    ///
    /// The heuristics guess "wall / ramp / timed hazard / gap / bridge" from
    /// collider geometry and component types — good enough most of the time, but
    /// some obstacles read wrong (a rope bridge over water whose thin rail posts
    /// the side-rays only catch every other metre; a spinning disc that IS the
    /// path). This lets a level designer settle it per-obstacle in the Inspector.
    ///
    /// Placement: on any GameObject whose collider a bot's forward ray can hit, or
    /// an ancestor of it — RaceBotBrain checks self → parent → children.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/AI/Bot Obstacle Hint")]
    public class BotObstacleHint : MonoBehaviour
    {
        public enum Behavior
        {
            /// <summary>Fall back to the normal raycast heuristics.</summary>
            Auto,
            /// <summary>Static crossing (rope/plank bridge): walk straight down the
            /// middle, no weave, no jump, no detour, no gap-panic, full speed.</summary>
            Bridge,
            /// <summary>Moving/rotating platform the bot rides across a gap.</summary>
            RidePlatform,
            /// <summary>Swinging/rotating hazard: stop, watch, cross when it clears.</summary>
            TimeIt,
            /// <summary>Walk straight onto and across it like ground (rotating log).</summary>
            WalkThrough,
            /// <summary>Jump it on the approach.</summary>
            JumpOver,
            /// <summary>Always go around, never hop.</summary>
            Detour,
        }

        [Tooltip("How RaceBotBrain should handle this obstacle.")]
        public Behavior behavior = Behavior.Auto;

        [Header("Bridge crossing line (optional)")]
        [Tooltip("For Behavior.Bridge: two empty child transforms placed at the two ENDS " +
                 "of the walkable deck, on its centreline. When both are set the bot does " +
                 "pure-pursuit exactly along the line between them — dead reliable, no " +
                 "geometry guessing. Leave empty to fall back to the (less reliable) " +
                 "deck-scan heuristic.")]
        public Transform pathA;
        public Transform pathB;

        public bool HasCrossingLine => pathA != null && pathB != null;

        [Header("Stepping-disc crossing (optional)")]
        [Tooltip("For Behavior.RidePlatform: one START point (on the entry island edge) and " +
                 "one END point (on the exit island edge). RaceBotBrain builds the jump path " +
                 "automatically — start → the disc centres in between, ordered along start→end " +
                 "→ end — and the bot hops point-to-point along it. Leave empty to use the " +
                 "fully automatic disc-to-disc logic.")]
        public Transform stoneStart;
        public Transform stoneEnd;

        public bool HasStoneCrossing => stoneStart != null && stoneEnd != null;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (HasCrossingLine)
            {
                UnityEngine.Gizmos.color = new Color(0.3f, 0.8f, 1f, 1f);
                UnityEngine.Gizmos.DrawSphere(pathA.position, 0.35f);
                UnityEngine.Gizmos.DrawSphere(pathB.position, 0.35f);
                UnityEngine.Gizmos.DrawLine(pathA.position, pathB.position);
            }

            if (HasStoneCrossing)
            {
                UnityEngine.Gizmos.color = new Color(0.3f, 1f, 0.4f, 1f);
                UnityEngine.Gizmos.DrawSphere(stoneStart.position, 0.5f);
                UnityEngine.Gizmos.DrawSphere(stoneEnd.position, 0.5f);
                UnityEngine.Gizmos.DrawLine(stoneStart.position, stoneEnd.position);
            }
        }
#endif
    }
}
