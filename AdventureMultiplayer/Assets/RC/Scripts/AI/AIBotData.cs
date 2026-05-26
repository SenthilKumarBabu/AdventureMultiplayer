using System;

namespace AdventureMultiplayer
{
    public enum AIState
    {
        Idle,               // scanning for a target
        WalkToCollectible,  // navigating directly to the target
        WalkToLauncher,     // walking to a launcher's entry point
        UseSpring,          // walking into spring, waiting for launch, landing
        ClimbPole,          // grabbing and climbing a pole, jumping off at the top
        UseRail,            // entering a rail grind, riding to exit, jumping off
        UsePortal,          // walking into portal A, detecting teleport to B
        WaitForPlatform,    // standing at boarding waypoint until platform arrives
        BoardPlatform,      // stepping onto the moving platform
        RidePlatform,       // riding platform to the exit waypoint
        UseForceField,      // standing inside upward push volume until high enough
        UseSpeedBooster,    // boost trigger already fired; transition to WalkToCollectible
    }

    public enum AIGoalMode
    {
        Balanced,    // default: value / (dist × costMul)
        Survivor,    // hunts hearts/lives when health is critical
        Greedy,      // maximises value, reduced distance penalty
        Sprinter,    // always picks the nearest collectible
        Rival,       // prioritises items that rival players are nearby (denial)
        Tactical,    // avoids launchers; strongly prefers direct walks
    }

    public enum AILogType
    {
        Info,
        Warning,
        Error,
        StateChange,
        TargetChange,
        Ability,     // spin, stomp, dash, glide, dive, roll, jump etc.
        Movement,    // navigation decisions (wall suppressed, conveyor, hazard avoid)
    }

    [Serializable]
    public struct AILogEntry
    {
        public float     time;
        public AILogType type;
        public string    message;
    }

    /// <summary>One row in the per-scan scoring table shown in the AI Bot Debugger.</summary>
    public struct CollectibleScanEntry
    {
        public string name;
        public float  dist;
        public float  hDiff;
        public string navStatus;    // "Full" / "Part" / "None"
        public bool   heightOk;
        public float  walkScore;    // float.MinValue = not scored
        public float  launchScore;  // float.MinValue = no launcher beat walk
        public string launchType;   // e.g. "Spring"
        public bool   isWinner;
        public bool   isBlacklisted;
    }
}
