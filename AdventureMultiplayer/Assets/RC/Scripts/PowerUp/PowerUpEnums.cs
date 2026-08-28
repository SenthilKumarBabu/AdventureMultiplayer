using System;
using UnityEngine;

namespace AdventureMultiplayer
{
    public enum PowerUpType
    {
        SpeedBoost   = 0,
        Rocket       = 1,
        Shield       = 2,
        StunBolt     = 4,
        Swap         = 5,
        Freeze       = 6,
        Banana       = 7,
        DecoyBox     = 8,
        // Invisible merges the old Invincibility (damage immunity) and Invisibility
        // (physical pass-through) power-ups into one. 3 and 9 are retired, not reused,
        // so any stale serialized references fail loudly instead of silently remapping.
        Invisible    = 10,
        // Arms a one-time +1s bonus on the collecting player's own character ability
        // (Glide/Sprint/Dash/Roll/AirDive). Consumed on that player's next ability use.
        SuperCharge  = 11,
    }

    public enum PowerUpBoxType { Yellow, Red, Green }

    /// <summary>What happened to the RECEIVING end of an incoming power-up — drives the
    /// "affected by" on-screen feedback text (see PlayerPowerUpInventory.OnPowerUpAffected).</summary>
    public enum PowerUpAffectOutcome
    {
        Hit,             // the effect actually landed
        ShieldBlocked,   // Shield absorbed it (and was consumed)
        InvisibleDodged, // Invisible let it pass through untouched
    }

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
