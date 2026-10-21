using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// What kind of thing the bot has just detected in front of it.
    ///
    /// This is the vocabulary the whole bot obstacle system is built on: the sensor finds a
    /// Collider, <see cref="BotObstacleClassifier"/> turns it into one of these, and (from
    /// Phase 2 onward) a single handler per value decides how to deal with it. Adding support
    /// for a new obstacle should mean adding a value here plus a classifier rule — never a new
    /// bespoke "mode" threaded through RaceBotBrain.
    /// </summary>
    public enum BotObstacleType
    {
        /// <summary>Nothing relevant ahead.</summary>
        None = 0,

        /// <summary>Another racer (human or bot). Never terrain — push through, don't detour.</summary>
        Racer,

        /// <summary>Rope/plank crossing. Walk the deck centreline, never jump.</summary>
        Bridge,

        /// <summary>Rotating stepping disc — a surface to ride/hop across, NOT a hazard to wait out.</summary>
        SpinningDisc,

        /// <summary>Non-spinning moving platform. Step on and be carried.</summary>
        RideablePlatform,

        /// <summary>Trampoline / mushroom. Walk on and ride the launch.</summary>
        BouncePad,

        /// <summary>A log you walk along rather than avoid.</summary>
        WalkableLog,

        /// <summary>Swinging, spinning or falling hazard on a cycle — time the crossing.</summary>
        TimedHazard,

        /// <summary>Solid static geometry. Jump it if low, go around if not.</summary>
        Wall,
    }

    /// <summary>
    /// Turns a detected Collider into a <see cref="BotObstacleType"/>.
    ///
    /// Identification is automatic: it reads the components actually present on the object
    /// (and its parents/children), so a newly placed obstacle is understood with no tagging.
    /// A <see cref="BotObstacleHint"/> is honoured first where one exists, because a designer
    /// saying "this is a bridge" always beats a guess — and because the bridge hint is where
    /// the deck-end markers live.
    ///
    /// Pure and stateless: no allocation, no side effects, safe to call every frame.
    /// </summary>
    public static class BotObstacleClassifier
    {
        /// <summary>
        /// Classify <paramref name="col"/>. <paramref name="self"/> is the bot's own Player,
        /// used only to tell "another racer" apart from "me".
        /// </summary>
        public static BotObstacleType Classify(Collider col, Player self)
        {
            if (col == null) return BotObstacleType.None;

            // ── 0. Another racer ──────────────────────────────────────────────
            // Checked first: a player capsule can carry no other marker component at all, and
            // treating one as terrain is what made two bots detour off each other forever.
            var player = col.GetComponentInParent<Player>();
            if (player != null) return player == self ? BotObstacleType.None : BotObstacleType.Racer;

            // ── 1. Designer hint wins ─────────────────────────────────────────
            var hint = FindHint(col);
            if (hint != null)
            {
                switch (hint.behavior)
                {
                    case BotObstacleHint.Behavior.Bridge:       return BotObstacleType.Bridge;
                    case BotObstacleHint.Behavior.RidePlatform: return BotObstacleType.SpinningDisc;
                    case BotObstacleHint.Behavior.WalkThrough:  return BotObstacleType.None;
                    case BotObstacleHint.Behavior.TimeIt:       return BotObstacleType.TimedHazard;
                    case BotObstacleHint.Behavior.JumpOver:     return BotObstacleType.Wall;
                    // Auto / Detour fall through to component detection below.
                }
            }

            // ── 2. Launch pads ────────────────────────────────────────────────
            // Before the platform check: a trampoline is technically stood on, but riding the
            // bounce is a completely different behaviour from being carried.
            if (Has<JumpingPlatform>(col) || Has<MushroomBounce>(col))
                return BotObstacleType.BouncePad;

            // ── 3. Rideable surfaces ──────────────────────────────────────────
            // PLAYER TWO's Platform is the base of DynamicPlatform / MovingPlatform. A platform
            // that also spins is a stepping disc — a RIDE, never a hazard to wait out. This
            // ordering is load-bearing: classifying the disc bridges as hazards is what used to
            // leave bots standing at the water's edge waiting for a "gap" that never comes.
            if (col.GetComponentInParent<Platform>() != null)
                return HasMotionComponent(col) ? BotObstacleType.SpinningDisc
                                               : BotObstacleType.RideablePlatform;

            // ── 4. Logs you walk along ────────────────────────────────────────
            if (Has<RollingLogObstacle>(col) || Has<RotatingLogObstacle>(col))
                return BotObstacleType.WalkableLog;

            // ── 5. Knockback hazards ──────────────────────────────────────────
            var knock = Find<ObstacleKnockback>(col);
            if (knock != null)
            {
                // RotatingLog is the one knockback type you cross by walking it, not timing it.
                if (knock.Type == ObstacleType.RotatingLog)
                    return BotObstacleType.WalkableLog;
                return BotObstacleType.TimedHazard;
            }

            // ── 6. Anything else that moves on a cycle ────────────────────────
            if (HasMotionComponent(col)) return BotObstacleType.TimedHazard;

            // ── 7. Static geometry ────────────────────────────────────────────
            return BotObstacleType.Wall;
        }

        /// <summary>
        /// True if THIS collider actually moves. See <see cref="Moves{T}"/> for the exact rule —
        /// in short, motion on the collider or an ancestor always counts, while motion found only
        /// in a child counts solely when that child has no collider of its own.
        ///
        /// That distinction is the whole point. DeathRunL1's pendulum is the shape that exposed
        /// it:
        ///
        ///     pendulum       (obstacle_11_001)  static MeshCollider — the walkable board
        ///     └── pendulum_ball (obstacle_11_002)  OscillateRotation 45 deg / 1.5 s + own collider
        ///
        /// The ball hangs off the board, so a plainly child-inclusive search found the ball's
        /// OscillateRotation from the BOARD's collider and classified the walkable board as a
        /// swinging hazard. Bots then refused to commit to the very platform they had to cross,
        /// hesitating and detouring at its edge. Same family as the spinning stepping discs,
        /// where "a platform that also moves is a ride, not a hazard to wait out".
        ///
        /// Both the original ithappy namespace and the RC working copies are checked: the
        /// project's own rotating hazards use <c>ithappy.rc</c>, and a single-namespace check
        /// silently treated every one of them as a static wall.
        /// </summary>
        public static bool HasMotionComponent(Collider col)
        {
            return Moves<ithappy.RotationScript>(col)
                || Moves<ithappy.rc.RotationScript>(col)
                || Moves<ithappy.OscillateRotation>(col)
                || Moves<ithappy.rc.OscillateRotation>(col)
                || Moves<ithappy.rc.OscillatePosition>(col)
                || Moves<RandomStoneFall>(col);
        }

        /// <summary>
        /// Does motion of type <typeparamref name="T"/> actually apply to THIS collider?
        ///
        /// Self or an ancestor: yes — a child of something that swings swings with it.
        /// Only in a child: only when that child has no collider of its own. A moving child that
        /// carries its own collider is separately detectable as a hazard, so this collider is
        /// merely the static mount it hangs from. That is the pendulum board, and keeping the
        /// no-collider case preserves obstacles whose motion script and collider live on
        /// different objects.
        /// </summary>
        private static bool Moves<T>(Collider col) where T : Component
        {
            if (col.GetComponent<T>() != null) return true;
            if (col.GetComponentInParent<T>() != null) return true;

            var moving = col.GetComponentInChildren<T>();
            return moving != null && moving.GetComponentInChildren<Collider>() == null;
        }

        /// <summary>
        /// Nearest <see cref="BotObstacleHint"/> on the collider, its parents, or its children.
        /// The hint and the collider rarely sit on the same GameObject.
        /// </summary>
        public static BotObstacleHint FindHint(Collider col) => Find<BotObstacleHint>(col);

        // Self → parent → child, the same walk order the rest of the bot code uses. A hazard's
        // motion script and its collider are routinely on different objects in these prefabs.
        private static T Find<T>(Collider col) where T : Component
        {
            var c = col.GetComponent<T>();
            if (c != null) return c;
            c = col.GetComponentInParent<T>();
            if (c != null) return c;
            return col.GetComponentInChildren<T>();
        }

        private static bool Has<T>(Collider col) where T : Component => Find<T>(col) != null;
    }
}
