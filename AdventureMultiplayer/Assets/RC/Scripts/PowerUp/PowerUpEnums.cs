using System;
using UnityEngine;

namespace AdventureMultiplayer
{
    public enum PowerUpType
    {
        SpeedBoost   = 0,
        Rocket       = 1,
        Shield       = 2,
        Invincibility = 3,
        StunBolt     = 4,
        Swap         = 5,
        Freeze       = 6,
        Banana       = 7,
        DecoyBox     = 8,
    }

    public enum PowerUpBoxType { Yellow, Red, Green }

    [Serializable]
    public class PowerUpLevelData
    {
        [Tooltip("Speed multiplier (SpeedBoost / Rocket)")]
        public float speedMultiplier = 1f;
        [Tooltip("Duration in seconds for timed effects")]
        public float duration = 1.5f;
        [Tooltip("Number of hits absorbed (Shield)")]
        public int   blockCount = 1;
        [Tooltip("How many targets are affected (StunBolt, Freeze, Swap)")]
        public int   targetCount = 1;
        [Tooltip("Stun duration applied to targets")]
        public float stunDuration = 1f;
        [Tooltip("Number of objects spawned (Banana peels, Decoy boxes)")]
        public int   spawnCount = 1;
    }
}
