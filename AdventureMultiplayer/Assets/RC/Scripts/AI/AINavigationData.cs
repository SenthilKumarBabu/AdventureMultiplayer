using UnityEngine;

namespace AdventureMultiplayer
{
    public enum LauncherType
    {
        Spring,
        Pole,
        Rail,
        Portal,
        MovingPlatform,
        ForceField,     // upward-push trigger volume
        SpeedBooster,   // directional velocity booster
    }

    /// <summary>
    /// Cached data about a launcher — entry/exit points.
    /// Built once at Start() by scanning the scene.
    /// </summary>
    public class LauncherInfo
    {
        public LauncherType type;

        /// <summary>NavMesh-sampled point the bot walks to in order to use this launcher.</summary>
        public Vector3 entryPoint;

        /// <summary>Approximate world position where the bot ends up after the launcher.</summary>
        public Vector3 exitPoint;

        /// <summary>Springs only: highest point of the launch trajectory (apex).</summary>
        public Vector3 apexPoint;

        /// <summary>The actual component (Spring / Pole / SplineContainer / Portal / MovingPlatform).</summary>
        public Component source;

        // Moving-platform specific
        public Transform boardingWaypoint;
        public Transform exitWaypoint;
    }
}
