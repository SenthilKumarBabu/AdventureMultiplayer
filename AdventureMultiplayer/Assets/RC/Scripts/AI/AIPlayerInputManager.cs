using System;
using System.Collections.Generic;
using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Replaces PlayerInputManager for AI-controlled bots.
    /// AIBotBrain sets desiredMoveDirection and action flags each frame.
    /// Supports human-like simulation: reaction delays, direction smoothing, jitter.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/AI Player Input Manager")]
    public class AIPlayerInputManager : PlayerInputManager
    {
        // ── Raw inputs set by AIBotBrain ──────────────────────────────────────

        [HideInInspector] public Vector3 desiredMoveDirection;
        [HideInInspector] public bool jumpQueued;
        [HideInInspector] public bool runHeld;
        [HideInInspector] public bool diveHeld;
        [HideInInspector] public bool swimUpwardHeld;
        [HideInInspector] public bool glideHeld;
        [HideInInspector] public bool spinQueued;
        [HideInInspector] public bool stompQueued;
        [HideInInspector] public bool dashQueued;
        [HideInInspector] public bool airDiveQueued;
        [HideInInspector] public bool crouchHeld;
        [HideInInspector] public bool rollHeld;
        [HideInInspector] public bool pickAndDropQueued;
        [HideInInspector] public bool releaseLedgeQueued;

        // ── Human simulation parameters (set by AIBotBrain.ApplyDifficulty) ──

        /// <summary>Minimum seconds before a queued action actually fires.</summary>
        [HideInInspector] public float reactionTimeMin = 0f;
        /// <summary>Maximum seconds before a queued action actually fires.</summary>
        [HideInInspector] public float reactionTimeMax = 0f;
        /// <summary>How fast the output direction Lerps to the desired direction (deg/sec equivalent).</summary>
        [HideInInspector] public float turnSpeed = 9999f;
        /// <summary>Max random yaw offset (degrees) added to move direction each frame via Perlin noise.</summary>
        [HideInInspector] public float directionJitter = 0f;

        // ── Internal ──────────────────────────────────────────────────────────

        private Vector3 m_smoothedMoveDir;
        private Vector3 m_lastLookDir = Vector3.forward;

        private struct PendingAction
        {
            public Action fire;
            public float  fireAt;
        }
        private readonly List<PendingAction> m_pending = new List<PendingAction>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Queue an input action to fire after a human-like reaction delay.
        /// The cooldown that prevents double-queuing should be set by the caller BEFORE
        /// calling this, so it takes effect immediately.
        /// </summary>
        public void QueueDelayedAction(Action action)
        {
            float delay = UnityEngine.Random.Range(reactionTimeMin, reactionTimeMax);
            m_pending.Add(new PendingAction { fire = action, fireAt = Time.time + delay });
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        protected override void Update()
        {
            // Fire any pending delayed actions whose time has come.
            for (int i = m_pending.Count - 1; i >= 0; i--)
            {
                if (Time.time >= m_pending[i].fireAt)
                {
                    m_pending[i].fire();
                    m_pending.RemoveAt(i);
                }
            }

            // Smooth the move direction toward the brain's desired direction.
            if (turnSpeed >= 9999f)
            {
                m_smoothedMoveDir = desiredMoveDirection;
            }
            else
            {
                // Apply Perlin-noise jitter to the target direction before smoothing.
                Vector3 target = desiredMoveDirection;
                if (directionJitter > 0f && target.sqrMagnitude > 0.01f)
                {
                    // Use a unique noise offset per instance so bots don't all jitter in sync.
                    float noise = (Mathf.PerlinNoise(Time.time * 2.5f, GetInstanceID() * 0.01f) - 0.5f)
                                  * 2f * directionJitter;
                    target = Quaternion.Euler(0f, noise, 0f) * target;
                }
                m_smoothedMoveDir = Vector3.Lerp(m_smoothedMoveDir, target,
                    turnSpeed * Time.deltaTime);
            }
        }

        // ── PlayerInputManager overrides ──────────────────────────────────────

        public override Vector3 GetMovementDirection() => m_smoothedMoveDir;

        public override Vector3 GetMovementCameraDirection(out float magnitude, bool localSpace = true)
        {
            var dir = m_smoothedMoveDir;
            magnitude = Mathf.Clamp01(desiredMoveDirection.magnitude);
            if (dir.sqrMagnitude > 0f) dir = dir.normalized;
            return dir;
        }

        public override Vector3 GetLateralMovementCameraDirection(out float magnitude, bool localSpace = true)
            => GetMovementCameraDirection(out magnitude, localSpace);

        public override Vector3 GetHorizontalMovementCameraDirection(out float magnitude)
            => GetMovementCameraDirection(out magnitude);

        public override Vector3 GetLookDirection()
        {
            if (m_smoothedMoveDir.sqrMagnitude > 0.0001f)
                m_lastLookDir = m_smoothedMoveDir.normalized;
            return m_lastLookDir;
        }

        public override bool GetJumpDown()
        {
            if (!jumpQueued) return false;
            jumpQueued = false;
            return true;
        }

        public override bool GetJumpUp()        => false;
        public override bool GetRun()           => runHeld;
        public override bool GetRunUp()         => false;
        public override bool GetRoll()          => rollHeld;
        public override bool GetRollDown()      => rollHeld;
        public override bool GetCancelDown()    => false;
        public override bool GetRollCharge()    => false;
        public override bool GetSwimUpward()    => swimUpwardHeld;
        public override bool GetDive()          => diveHeld;
        public override bool GetSpinDown()      { if (!spinQueued)         return false; spinQueued         = false; return true; }
        public override bool GetPickAndDropDown(){ if (!pickAndDropQueued) return false; pickAndDropQueued  = false; return true; }
        public override bool GetCrouch()        => crouchHeld;
        public override bool GetCrouchDown()    => crouchHeld;
        public override bool GetAirDiveDown()   { if (!airDiveQueued)      return false; airDiveQueued      = false; return true; }
        public override bool GetStompDown()     { if (!stompQueued)        return false; stompQueued        = false; return true; }
        public override bool GetReleaseLedgeDown(){ if (!releaseLedgeQueued) return false; releaseLedgeQueued = false; return true; }
        public override bool GetGlide()         => glideHeld;
        public override bool GetDashDown()      { if (!dashQueued)         return false; dashQueued         = false; return true; }
        public override bool GetGrindBrake()    => false;
        public override bool GetPauseDown()     => false;
        public override bool GetHomingDashDown() => false;
        public override bool EscPressed()       => false;

        // Skip InputActionAsset initialization — no physical input for bots.
        protected override void Awake()   => InitializePlayer();
        protected override void Start()   { }
        protected override void OnEnable()  { }
        protected override void OnDisable() { }
    }
}
