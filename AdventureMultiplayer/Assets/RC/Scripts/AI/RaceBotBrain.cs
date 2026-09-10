using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using ithappy;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Checkpoint-steering race AI.
    ///
    /// Drives AIPlayerInputManager to navigate the race track in checkpoint order.
    /// Runs only on the server (bots are server-owned NetworkObjects).
    ///
    /// Key movement behaviours:
    ///   - Sinusoidal lateral weave so the bot never travels in a perfectly straight line.
    ///   - Smooth speed ramp (MoveTowards) for natural acceleration and deceleration.
    ///   - Speed drops when the bot needs to turn sharply (braking into corners).
    ///   - Look-ahead blending toward the NEXT checkpoint starts early, creating smooth arcs.
    ///   - Three forward raycasts for wall/gap avoidance; TryJump for low obstacles and gaps.
    ///   - Stuck detector escalates: jump → nudge → back-up → checkpoint respawn.
    ///   - Registers with RaceManager via unique bot ID (10000+) bypassing the trigger path.
    /// </summary>
    [RequireComponent(typeof(AIPlayerInputManager))]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Race Bot Brain")]
    public class RaceBotBrain : NetworkBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Difficulty")]
        [SerializeField] private BotDifficulty difficulty = BotDifficulty.Medium;

        [Header("Navigation")]
        [Tooltip("Distance from a checkpoint centre counted as 'reached'. Generous on purpose — " +
                 "this is only for ordinary waypoint steering, so the AI doesn't need to hit an " +
                 "exact spot mid-course. NOT used for the finish line — see HasReachedFinishLine.")]
        [SerializeField] private float arrivalRadius = 3.5f;
        [Tooltip("Fallback distance-to-centre for the finish line ONLY if it has no Collider to " +
                 "test bounds against. Much tighter than arrivalRadius — a human must physically " +
                 "enter the finish trigger, so a bot finishing from several metres away (which " +
                 "arrivalRadius would allow) reads as an obvious bug.")]
        [SerializeField] private float finishArrivalRadius = 1f;
        [Tooltip("Raycast distance for wall/obstacle detection ahead.")]
        [SerializeField] private float obstacleCheckDist = 2.5f;
        [Tooltip("Angle (degrees) of the left/right side rays from forward.")]
        [SerializeField] private float sideRayAngle = 35f;
        [Tooltip("Obstacle hit below this height (relative to bot feet) is jumped over. This is " +
                 "only a FALLBACK default — OnNetworkSpawn overwrites it with a value derived from " +
                 "this bot's own character stats (CalibrateJumpHeightFromStats), since the real " +
                 "jump height varies a lot per character (confirmed: Bolt's real max jump clears " +
                 "roughly 2.4 units, Spike's roughly 5.7, versus this flat 1.5 default) — leaving " +
                 "every bot capped at a low flat number regardless of character was rejecting jumps " +
                 "over obstacles they could easily clear.")]
        [SerializeField] private float wallJumpHeight = 1.5f;
        [Tooltip("Horizontal distance ahead to probe for ground when detecting gaps.")]
        [SerializeField] private float gapProbeDistance = 1.8f;
        [Tooltip("Max downward probe depth for gap detection.")]
        [SerializeField] private float gapProbeDepth = 3.5f;
        [Tooltip("Layers considered solid for obstacle and gap raycasts.")]
        [SerializeField] private LayerMask obstacleLayer = Physics.DefaultRaycastLayers;

        [Header("Stuck Recovery")]
        [SerializeField] private float stuckCheckInterval  = 0.6f;
        [SerializeField] private float stuckMoveThreshold  = 0.25f;
        [SerializeField] private float backUpDuration      = 0.55f;

        [Header("Progress Tracking")]
        [Tooltip("How often (seconds) to log this bot's position/speed/checkpoint progress " +
                 "while racing — used to verify movement stays smooth and continuous.")]
        [SerializeField] private float progressLogInterval = 3f;

        [Header("Avoidance Feel")]
        [Tooltip("Once the bot commits to steering around an obstacle on one side, it " +
                 "keeps steering that way for at least this long before re-evaluating — " +
                 "stops left/right flicker when both sides are borderline-clear.")]
        [SerializeField] private float avoidCommitDuration = 0.35f;
        [Tooltip("Every navigation distance/angle threshold is randomised per-bot within " +
                 "+/- this fraction at spawn (e.g. 0.15 = up to 15% off the base value), so " +
                 "bots of the same difficulty don't all notice and react to the same obstacle " +
                 "at the exact same instant — real racers don't share one reflex.")]
        [SerializeField, Range(0f, 0.4f)] private float perBotVariance = 0.15f;

        [Header("Hazard Avoidance")]
        [Tooltip("How far ahead (in the direction the bot is about to move) to check for a " +
                 "lethal KillZone — water, lava, pits. Every other obstacle/gap raycast below " +
                 "uses QueryTriggerInteraction.Ignore, so it's physically blind to a trigger-based " +
                 "hazard sitting on otherwise walkable-looking ground; this check exists " +
                 "specifically to catch that case.")]
        [SerializeField] private float hazardCheckDistance = 3f;
        [Tooltip("Radius of the hazard probe — wide enough to catch a hazard volume the probe " +
                 "point doesn't land exactly inside of.")]
        [SerializeField] private float hazardCheckRadius = 0.6f;
        [Tooltip("How far straight down to look for a lethal hazard below a probe point — " +
                 "catches a hazard (water, lava) sitting well below an ELEVATED crossing (a " +
                 "bridge deck several metres above the water it spans), which hazardCheckRadius " +
                 "alone can never reach since it only checks a small sphere at the probe's own " +
                 "height.")]
        [SerializeField] private float hazardBelowProbeDepth = 15f;

        [Header("Obstacle Type Handling")]
        [Tooltip("Ground steeper than this (degrees from flat) is treated as a wall to jump/detour " +
                 "instead of a walkable slope. Defaults to the same value as the character's own " +
                 "EntityController.slopeLimit when available, so the AI agrees with the physics " +
                 "on what counts as 'just a ramp' vs 'an actual obstacle'.")]
        [SerializeField] private float slopeLimitFallback = 45f;
        [Tooltip("How far to each side to check for railings/walls that mark a narrow crossing " +
                 "(a bridge). When found on both sides, the bot actively steers toward the " +
                 "midpoint between them instead of drifting toward either edge.")]
        [SerializeField] private float bridgeRailCheckDistance = 2f;

        [Header("Power-Ups")]
        [Tooltip("A power-up box within this range is noticed and steered toward — like a " +
                 "human naturally drifting over to grab one without abandoning their line.")]
        [SerializeField] private float powerUpDetectionRadius = 12f;
        [Tooltip("Max steering pull (0-1) toward a power-up box right on top of the bot, " +
                 "blended with the racing line — never fully overrides checkpoint navigation.")]
        [SerializeField] private float powerUpSeekStrength = 0.5f;
        [Tooltip("How often (seconds) the bot re-evaluates whether to use a held power-up.")]
        [SerializeField] private float powerUpDecisionInterval = 1.5f;
        [Tooltip("Max distance to a rival for an offensive/trap power-up to be considered " +
                 "worth using right now — a human doesn't fire a rocket at someone half the " +
                 "track away either.")]
        [SerializeField] private float powerUpTargetRange = 20f;

        // ── Bot ID counter ────────────────────────────────────────────────────

        private static ulong s_nextBotId = 10000UL;

        // ── References ────────────────────────────────────────────────────────

        private AIPlayerInputManager   m_input;
        private Player                 m_player;
        private NetworkRespawner       m_respawner;
        private PlayerPowerUpInventory m_inventory;
        private StunEffect             m_stunEffect;
        private SlipEffect             m_slipEffect;

        // ── Race state ────────────────────────────────────────────────────────

        private ulong m_botId;
        private bool  m_botIdAssigned;

        /// <summary>
        /// Unique race-participant ID for this bot (10000+) — shared with RaceManager
        /// and PlayerPowerUpInventory as the bot equivalent of a human player's NGO
        /// OwnerClientId (which is NOT unique for bots: BotSpawner.Spawn(true) defaults
        /// every bot's ownership to the server, so all bots — and the Host's own player —
        /// share OwnerClientId 0).
        ///
        /// Lazily self-assigns on first access so it's safe to read regardless of which
        /// NetworkBehaviour's OnNetworkSpawn runs first on this object.
        /// </summary>
        public ulong BotId
        {
            get
            {
                if (!m_botIdAssigned) { m_botId = s_nextBotId++; m_botIdAssigned = true; }
                return m_botId;
            }
        }

        private RaceCheckpoint[] m_checkpoints;
        private int              m_nextCpIdx;
        private Collider         m_finishCollider; // cached Collider on the current finish-line checkpoint
        private Collider         m_cpCollider;     // cached trigger Collider on the checkpoint at m_cpColliderIdx
        private int              m_cpColliderIdx = -1;

        // ── Stuck recovery ────────────────────────────────────────────────────

        private float   m_nextStuckCheck;
        private Vector3 m_lastStuckPos;
        private int     m_stuckLevel;
        private bool    m_backingUp;
        private float   m_backUpEndTime;

        // ── Incapacitation (stun / slip) ────────────────────────────────────────
        // StunEffect/SlipEffect already drive the character directly (velocity, animator,
        // AI-input fields) and work correctly on a bot with no changes — but RaceBotBrain
        // itself was never told to back off, so it kept calling Navigate() every frame and
        // re-writing desiredMoveDirection right underneath the effect, and CheckStuck()
        // would misread "frozen by a stun" as "stuck" and eventually force a mid-stun
        // respawn. Skipping AI steering entirely while incapacitated — and resetting the
        // stuck baseline the moment control returns — lets the effect fully own the
        // character, exactly like it already does for a human.
        private bool m_wasIncapacitated;

        // ── Power-ups ─────────────────────────────────────────────────────────
        private float m_nextPowerUpDecisionTime;

        // ── Pre-race embed watchdog ─────────────────────────────────────────────
        // BotSpawner ground-snaps the spawn position with a raycast, but a bot can still
        // silently sink through a collider gap (a mesh seam onto a lower interior surface —
        // Physics still reports grounded=True there, so nothing else catches it) during the
        // few seconds between spawning and the race actually starting, while it's supposed
        // to be perfectly still. Catch that specific case and snap it back up before "GO".

        private float m_expectedGroundY;
        private bool  m_embedCorrected;
        private bool  m_embedRecoveryPending;
        private float m_embedRecoveryCheckTime;
        private const float k_embedDropThreshold = 1f;

        // ── Progress / interruption tracking (diagnostic — verifies bots complete races cleanly) ──

        private float m_spawnTime;
        private float m_raceStartLogTime;   // Time.time this bot first saw RaceStarted — 0 until then
        private float m_nextProgressLogTime;
        private int   m_stuckEscalations;   // times CheckStuck progressed past level 1 this life
        private int   m_forcedRespawns;     // times the stuck detector gave up and force-respawned
        private int   m_deaths;             // times playerEvents.OnDie fired (kill zones, hazards, etc.)
        private int   m_obstaclesJumped;
        private int   m_obstaclesDetoured;
        private Collider m_lastLoggedObstacle; // edge-trigger: only log a NEW obstacle encounter

        // ── Jump limiter ──────────────────────────────────────────────────────

        private float       m_nextJumpTime;
        private const float k_jumpCooldown = 0.55f;

        // Coyote-style window for the gap-probe recovery jump below: "was grounded very
        // recently" (bumpy terrain making isGrounded flicker false for a frame or two) vs.
        // "genuinely airborne for a while" (already mid-jump-arc, or actually falling).
        // Without this distinction, removing the flat isGrounded gate made the gap probe
        // re-fire TryJump() on cooldown for the ENTIRE time a bot was airborne — including
        // the completely normal case of already being mid-air over a gap it correctly jumped —
        // which looked like continuous bunny-hopping instead of one clean jump.
        private float       m_lastGroundedTime;
        private const float k_recentGroundedWindow = 0.25f;

        // ── Obstacle avoidance state ──────────────────────────────────────────
        // "Sticky" side commitment: once the bot picks left/right to go around an
        // obstacle, it holds that choice for avoidCommitDuration instead of
        // re-picking every frame — real players don't zig-zag when both sides
        // are roughly equally clear.

        private int   m_avoidSide;        // -1 = left, 0 = none, 1 = right
        private float m_avoidCommitUntil;
        // Separate sticky state for the hazard (kill-zone) detour below — it used to re-pick
        // left/right fresh every single frame with no commitment at all, which reads as
        // continuous side-to-side jumping/hopping on any crossing where the hazard trigger's
        // bounds vary slightly along the path (a kill zone that isn't perfectly rectangular
        // relative to the walkable line makes which angles are safe flip frame to frame even while
        // standing still). Kept separate from m_avoidSide/m_avoidCommitUntil (physical obstacles)
        // rather than shared, since a hazard detour and a wall detour are different situations
        // that can immediately follow one another in the same frame.
        private int   m_hazardAvoidSide;
        private float m_hazardAvoidAngle; // degrees actually used to find m_hazardAvoidSide — needed to rebuild the exact committed direction on later frames, since it's no longer always sideRayAngle (see FindSafeHazardDirection)
        private float m_hazardAvoidCommitUntil;
        private float m_obstacleHopBias;  // 0-1: chance to hop a jumpable obstacle instead of detouring around it
        private int   m_preferredSide;    // -1 or 1, randomised per bot — tie-breaker when both sides are equally clear
        private Collider m_lastLoggedHazard; // edge-trigger: only log a NEW hazard encounter, same pattern as obstacles
        private float m_nextHazardWarnTime;  // throttles the "no safe detour" warning while boxed in
        private Collider m_lastLoggedTimedHazard; // edge-trigger for the "timing past moving hazard" log
        private bool     m_wasOnBridge; // edge-trigger for the "on a narrow crossing" log

        // Sticky "crossing a hinted bridge" state — see Update(). Held k_bridgeModeHold
        // seconds past the last detection so a brief hop or a missed rail-ray can't drop
        // the bot out of it and let stuck-recovery / the gap-probe jump it off the deck.
        private bool  m_bridgeMode;
        private bool  m_bridgeModePrev;      // edge-detect entering bridge mode
        private float m_bridgeModeUntil;
        private float m_bridgeSuppressUntil; // after a death: block bridge mode from re-arming while the respawn teleport completes
        private BotObstacleHint m_bridgeHint; // the hint driving the current crossing (for its pathA/pathB)
        private Vector3 m_bridgeExitPos;     // LATCHED at entry: the deck end the bot walks toward (farther marker)
        private bool    m_bridgeExitValid;   // false = no crossing line, fall back to fallback scan
        private float   m_bridgeLateralOff;  // signed distance from the crossing line (for speed scaling in Navigate)
        private bool    m_bridgeAligned;     // true once the bot has reached the entry mouth lined up — then it commits to the crossing
        private float   m_nextBridgeApproachScan;
        private Vector3 m_bridgeApproachMouth;
        private bool    m_hasBridgeApproach; // a hinted bridge is ahead and the bot should route to its mouth first
        private Vector3 m_bridgeAxis;        // last-frame chosen heading (fallback scan) — seed to stop flip-flop
        private Vector3 m_bridgeEntryPos;
        private float m_bridgeAirborneSince;  // Time.time the bot went airborne while on the bridge (0 = grounded)
        private float m_bridgeStalledSince;   // Time.time forward progress stalled while crossing (0 = moving)
        private Vector3 m_bridgeLastProgressPos;
        private float m_nextBridgeLog;
        private const float k_bridgeModeHold = 1.5f;
        private static readonly Collider[] s_bridgeBuffer = new Collider[32];

        // Sticky "hopping across a field of rotating stepping-disc platforms" state — the
        // DeathRunL1 moving_platform cluster. Each disc carries a BotObstacleHint(RidePlatform);
        // the bot hops disc-to-disc toward the checkpoint instead of trusting the reactive
        // gap/hazard heuristics (which had bots clipping disc edges into the water).
        private bool    m_stoneMode;
        private bool    m_stoneModePrev;
        private float   m_stoneModeUntil;
        private float   m_stoneSuppressUntil; // block re-arm through a respawn teleport
        private Vector3 m_stoneEntryPos;
        private float   m_stoneStalledSince;
        private Vector3 m_stoneLastProgressPos;
        private float   m_nextStoneLog;
        private Vector3 m_stoneCurCentre;      // centre of the disc the bot is currently on
        private bool    m_stoneHasCur;
        private bool    m_stoneHoppedFromCur;  // already queued the hop toward the next disc
        private float   m_stoneHopDebounce;
        private float   m_stoneGroundedSince;  // Time.time grounded regained (0 = airborne) — "settled" gate
        // Optional designer crossing: BotObstacleHint.stoneStart/stoneEnd. RaceBotBrain builds
        // an ordered jump path (start → disc centres in between → end) the bot hops along.
        private BotObstacleHint m_stonePathHint;
        private bool      m_stonePathSearched;
        private Vector3[] m_stonePath;         // the built jump path (null = auto disc-to-disc)
        private Vector3[] m_allDiscCentres;    // every RidePlatform disc centre (scene scan, once)
        private int       m_stonePathIdx;
        private bool      m_stonePathHopped;   // one hop per waypoint
        private bool      m_stonePathDoubleJumped; // used the mid-air 2nd jump to extend this hop
        private float     m_stonePathHopTime;
        private static readonly System.Collections.Generic.List<Vector3> s_stonePathBuild =
            new System.Collections.Generic.List<Vector3>(24);
        private const float k_stoneModeHold = 1.2f;
        private static readonly Collider[] s_stoneBuffer = new Collider[128];
        private static readonly System.Collections.Generic.List<Vector3> s_stoneCentres =
            new System.Collections.Generic.List<Vector3>(16);

        // Sticky "using a launch pad (trampoline / mushroom bounce)" state. A JumpingPlatform
        // or MushroomBounce underfoot, or just ahead on the line to the current checkpoint →
        // the bot must walk STRAIGHT onto it and let the launch carry it across the gap. Held
        // k_bounceModeHold past the last detection so it survives the whole airborne arc.
        // While active: ObstacleAvoidance aims dead at the checkpoint and skips every
        // wall/detour/gap check (the "gap" past the pad is exactly what the bounce clears),
        // weave is held straight, Navigate runs at full speed + run for horizontal carry,
        // and TryJump() hard no-ops (a queued jump on top of a 30-force launch just sends the
        // bot 10 m straight up and back down onto the same pad — the bounce loop the user hit
        // right after checkpoint 0 in DeathRunL2).
        private bool      m_bounceMode;
        private bool      m_bounceModePrev;
        private float     m_bounceModeUntil;
        private Vector3   m_bounceEntryPos;
        private bool      m_bounceLaunched;   // the pad has fired — the bot is now in the launch arc
        private bool      m_bounceAssisted;   // used the one mid-air assist jump for this launch
        private const float k_bounceModeHold = 2.2f;
        private static readonly Collider[] s_bounceBuffer = new Collider[16];

        // A brief pause after a timed hazard (hammer, spinner, etc.) actually clears, before
        // stepping forward — matches a real player watching the swing, confirming the gap, THEN
        // moving. Without this the bot resumes at full speed the exact instant its raycast stops
        // hitting the hazard, which reads as if it never actually watched/timed it at all.
        private bool  m_waitingForHazardClear;
        private float m_hazardResumeTime;

        // Safety valve: a pendulum/hammer whose swept volume always intersects the forward
        // ray at chest height never "clears", so the wait above becomes permanent and stuck-
        // recovery eventually nudges the bot off the platform. After this long standing at
        // one timed hazard, a real player stops waiting for the perfect gap and just commits —
        // jump and push through, accepting the knockback. Confirmed needed via Editor.log:
        // bots stalled at 'pendulum_ball' / 'spinner_blade' in DeathRunL1 until they died.
        private float m_timedHazardWaitStart;
        private const float k_maxTimedHazardWait = 4f;

        // ── Position reporting (20 Hz) ────────────────────────────────────────

        private float       m_nextPosReport;
        private const float k_posReportInterval = 0.05f;

        // ── Speed state ───────────────────────────────────────────────────────
        // Speed is smoothed with MoveTowards so the bot accelerates/decelerates
        // gradually — the single biggest factor in "feels human vs robot".

        private float m_currentSpeed;    // 0-1, what's actually fed to the input manager
        private float m_targetSpeed;     // where we want speed to be this frame
        private float m_speedAccelRate;  // units/sec acceleration (difficulty-tuned)
        private float m_speedDecelRate;  // units/sec deceleration

        // ── Path weaving ──────────────────────────────────────────────────────
        // Sinusoidal lateral deviation so the bot never goes in a perfectly straight
        // line. Amplitude and frequency are tuned per difficulty.

        private float m_weaveAmplitude;  // max yaw deviation in degrees
        private float m_weaveFrequency;  // oscillation speed in Hz
        private float m_weavePhase;      // per-instance random offset (bots don't sync)

        // ── Turn / brake parameters (difficulty-tuned) ────────────────────────

        private float m_lookAheadBlendDist; // start blending toward next-cp at this dist
        private float m_hardBrakeTurnAngle; // above this angle: walk speed
        private float m_softBrakeTurnAngle; // above this angle: partial slow

        // ── Random walk breaks (Easy difficulty) ─────────────────────────────

        private float m_walkBreakChance;    // probability per second of a brief walk
        private float m_walkBreakDuration;  // how long each walk break lasts
        private float m_walkBreakEndTime;   // Time.time when current break ends

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            if (!IsServer) { enabled = false; return; }

            m_input      = GetComponent<AIPlayerInputManager>();
            m_player     = GetComponent<Player>();
            m_respawner  = GetComponent<NetworkRespawner>();
            m_inventory  = GetComponent<PlayerPowerUpInventory>();
            m_stunEffect = GetComponent<StunEffect>();
            m_slipEffect = GetComponent<SlipEffect>();

            m_weavePhase     = Random.Range(0f, Mathf.PI * 2f); // each bot weaves out of sync
            m_preferredSide  = Random.value < 0.5f ? -1 : 1;    // which way this bot leans on a coin-flip detour
            CalibrateJumpHeightFromStats();
            ApplyDifficulty();
            ApplyPerBotVariance();

            if (RaceManager.Instance != null)
            {
                m_checkpoints = RaceManager.Instance.Checkpoints;
                RaceManager.Instance.AddBotEntry(BotId);
                RaceManager.Instance.RegisterPlayerTransform(BotId, transform);
            }
            else
            {
                Debug.LogWarning("[RaceBotBrain] RaceManager not found.");
            }

            m_lastStuckPos   = transform.position;
            m_nextStuckCheck = Time.time + stuckCheckInterval;
            m_spawnTime      = Time.time;
            m_nextProgressLogTime = Time.time + progressLogInterval;
            m_expectedGroundY = transform.position.y; // BotSpawner already ground-raycast this — trust it

            if (m_player != null)
                m_player.playerEvents.OnDie.AddListener(OnDiedTracking);

            Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') spawned at {transform.position} ({difficulty}).");
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            RaceManager.Instance?.UnregisterPlayerTransform(m_botId);
            if (m_player != null)
                m_player.playerEvents.OnDie.RemoveListener(OnDiedTracking);
        }

        // NetworkRespawner owns the actual respawn flow via its own listener — this is
        // diagnostic PLUS feeds m_deathPositions (see field comment) so the bot stops trusting
        // "onBridge" blindly the next time it passes through a spot that already killed it.
        private void OnDiedTracking()
        {
            m_deaths++;
            Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') died (death #{m_deaths}) at {transform.position}, " +
                      $"checkpoint {m_nextCpIdx - 1} → heading to {m_nextCpIdx}.");

            m_deathPositions.Add(transform.position);
            if (m_deathPositions.Count > k_maxTrackedDeaths)
                m_deathPositions.RemoveAt(0);

            // Drop bridge mode AND block it from re-arming for a beat — otherwise the
            // sticky window (or NearBridgeHint firing again while the corpse is still by
            // the bridge, before the respawn teleport) carries it through the respawn and
            // the bot wanders the spawn island in bridge mode with everything suppressed
            // ("travelled 30 m" on flat ground, confirmed in Editor.log).
            m_bridgeModeUntil     = 0f;
            m_bridgeMode          = false;
            m_bridgeModePrev      = false;
            m_bridgeAxis          = Vector3.zero;
            m_bridgeHint          = null;
            m_bridgeAligned       = false;
            // Cover the full respawn delay (NetworkRespawner waits 2 s) + a beat, so the
            // corpse lying in the water under the bridge can't re-arm a crossing that then
            // survives the teleport back to the spawn island.
            m_bridgeSuppressUntil = Time.time + 2.6f;

            m_stoneModeUntil     = 0f;
            m_stoneMode          = false;
            m_stoneModePrev      = false;
            m_stoneSuppressUntil = Time.time + 2.6f;
            m_stonePath          = null; // re-pick route/index on the next STONES ENTER

            m_bounceModeUntil = 0f;
            m_bounceMode      = false;
            m_bounceModePrev  = false;
            m_bounceLaunched  = false;
            m_bounceAssisted  = false;
        }

        // Every "is this actually safe to trust" heuristic below (onBridge, in particular) is a
        // guess from limited raycast information — it can be wrong, and when it's wrong near a
        // fall hazard the result is a death. Re-deciding fresh every single time a bot passes
        // through the same spot means a wrong guess repeats identically forever (confirmed via
        // Editor.log: the same bot died 10-20+ times in a row at nearly the exact same
        // coordinates in one race). Remembering where THIS bot has already died and refusing to
        // extend it that same trust again nearby breaks the loop without needing to perfectly
        // solve the underlying geometry classification — the whole point of stuck/obstacle
        // handling below is to be MORE careful, so falling back to it near a proven-fatal spot is
        // always the safe direction to err in, never actively harmful the way misplaced trust is.
        private readonly List<Vector3> m_deathPositions = new();
        private const int   k_maxTrackedDeaths  = 20;
        private const float k_deathMemoryRadius = 4f;

        private bool NearPastDeath(Vector3 pos)
        {
            for (int i = 0; i < m_deathPositions.Count; i++)
                if ((m_deathPositions[i] - pos).sqrMagnitude <= k_deathMemoryRadius * k_deathMemoryRadius)
                    return true;
            return false;
        }

        private void Update()
        {
            if (!IsServer || m_input == null || m_player == null) return;

            // Race results screen is up for EVERYONE (all players finished, or the
            // results timeout elapsed) — the race is over. Stop this bot completely:
            // no more steering, weaving, jumping or stuck-recovery. A bot still on the
            // track when the panel appeared used to keep visibly running and jumping
            // around behind it.
            if (RaceManager.Instance != null && RaceManager.Instance.AllPlayersFinished.Value)
            {
                StopBot();
                enabled = false;
                return;
            }

            // Position report
            if (Time.time >= m_nextPosReport)
            {
                m_nextPosReport = Time.time + k_posReportInterval;
                RaceManager.Instance?.UpdatePlayerPosition(m_botId, transform.position);
            }

            if (m_player.isGrounded) m_lastGroundedTime = Time.time;

            // Stunned/slipping: the effect already owns velocity, animator and AI-input
            // fields directly (see StunEffect/SlipEffect) — back off completely rather than
            // fighting it every frame with fresh steering input, and don't let CheckStuck
            // misread "frozen by a stun" as "stuck" and force a mid-stun respawn.
            bool incapacitated = (m_stunEffect != null && m_stunEffect.IsStunned)
                                  || (m_slipEffect != null && m_slipEffect.IsSlipping);
            if (incapacitated)
            {
                m_wasIncapacitated = true;
                return;
            }
            if (m_wasIncapacitated)
            {
                m_wasIncapacitated = false;
                m_backingUp        = false;
                m_stuckLevel       = 0;
                m_lastStuckPos     = transform.position;
                m_nextStuckCheck   = Time.time + stuckCheckInterval;
            }

            // Back-up recovery overrides all steering
            if (m_backingUp)
            {
                if (Time.time >= m_backUpEndTime)
                {
                    // Don't assume backing up actually freed the bot — m_lastStuckPos wasn't
                    // touched while backingUp held early-return above, so it still holds the
                    // position from BEFORE this back-up started. Leaving m_stuckLevel as-is lets
                    // the next CheckStuck() call measure the real outcome: genuinely escaped →
                    // moved clears the threshold and it resets to 0 on its own; still stuck →
                    // it escalates past level 3 into the force-respawn fallback instead of
                    // silently restarting the jump→nudge→back-up loop from scratch forever
                    // (confirmed via Editor.log: a bot embedded near an unstable terrain spot
                    // cycled stuck levels 1-3 for 60+ seconds with forcedRespawns staying at 0).
                    m_backingUp = false;
                }
                else
                {
                    m_currentSpeed = Mathf.MoveTowards(m_currentSpeed, 0.7f,
                        m_speedAccelRate * Time.deltaTime);
                    m_input.desiredMoveDirection = -transform.forward * m_currentSpeed;
                    m_input.runHeld = false;
                    return;
                }
            }

            // Wait for race start
            if (RaceManager.Instance != null && !RaceManager.Instance.RaceStarted.Value)
            {
                m_currentSpeed = 0f;
                m_input.desiredMoveDirection = Vector3.zero;
                m_input.runHeld = false;
                CheckEmbedWhileWaiting();
                return;
            }

            if (m_raceStartLogTime <= 0f)
            {
                m_raceStartLogTime = Time.time;
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') GO — starting race from {transform.position}, " +
                          $"{m_checkpoints?.Length ?? 0} checkpoint(s) ahead.");
            }

            // ── Bridge mode (sticky) ─────────────────────────────────────────
            // A BotObstacleHint(Bridge) underfoot OR within reach → cross-a-bridge
            // mode, held for a short window after so a brief airborne frame or a
            // forward-ray that misses the thin rails can't drop the bot out of it
            // mid-crossing. While active, ObstacleAvoidance holds a straight line
            // and NOTHING is allowed to make the bot jump (see TryJump / CheckStuck).
            var bridgeHint = ResolveBridgeHint();
            // Geometric gate: a line-equipped hint only arms bridge mode when the bot is
            // actually INSIDE the deck corridor. Bots walking PAST the bridge brush its
            // wide decorative end-frame colliders (x=±4 on the prefab root) from 2-4 m
            // off-centre — that used to arm a bogus crossing and march them into the water.
            // Also stops a corpse/respawn far from the deck from staying "on the bridge".
            bool armBridge;
            if (bridgeHint != null && bridgeHint.HasCrossingLine)
                armBridge = BridgeCorridorContains(bridgeHint, transform.position);
            else
                armBridge = bridgeHint != null; // markerless bridge: proximity is all we have
            if (armBridge && Time.time >= m_bridgeSuppressUntil)
            {
                m_bridgeHint      = bridgeHint;
                m_bridgeModeUntil = Time.time + k_bridgeModeHold;
            }
            m_bridgeMode = Time.time < m_bridgeModeUntil && Time.time >= m_bridgeSuppressUntil;

            if (m_bridgeMode && !m_bridgeModePrev)
            {
                Vector3 h = m_input.desiredMoveDirection.sqrMagnitude > 0.04f
                    ? m_input.desiredMoveDirection : transform.forward;
                m_bridgeAxis            = Vector3.ProjectOnPlane(h, Vector3.up).normalized;
                m_bridgeEntryPos        = transform.position;
                m_bridgeLastProgressPos = transform.position;
                m_bridgeAirborneSince   = 0f;
                m_bridgeStalledSince    = 0f;
                m_bridgeAligned         = false;

                // Latch the exit end ONCE. The bot enters from one end, so the marker
                // farther from it is the exit. Recomputing this per-frame makes the bot
                // flip direction at the midpoint and oscillate forever.
                m_bridgeExitValid = m_bridgeHint != null && m_bridgeHint.HasCrossingLine;
                if (m_bridgeExitValid)
                {
                    Vector3 pa = m_bridgeHint.pathA.position;
                    Vector3 pb = m_bridgeHint.pathB.position;
                    m_bridgeExitPos = (pb - transform.position).sqrMagnitude >= (pa - transform.position).sqrMagnitude ? pb : pa;
                }
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') BRIDGE ENTER at {transform.position:F1} " +
                          $"line={(m_bridgeExitValid ? "YES" : "no")} exit={(m_bridgeExitValid ? m_bridgeExitPos.ToString("F1") : "-")} " +
                          $"grounded={m_player.isGrounded} target=cp{m_nextCpIdx}");
            }
            if (!m_bridgeMode && m_bridgeModePrev)
            {
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') BRIDGE EXIT at {transform.position:F1} " +
                          $"grounded={m_player.isGrounded} — travelled {Vector3.Distance(m_bridgeEntryPos, transform.position):F1} m");
                m_bridgeHint       = null;
                m_bridgeExitValid  = false;
                m_bridgeLateralOff = 0f;
            }
            m_bridgeModePrev = m_bridgeMode;

            if (m_bridgeMode)
                m_input.jumpQueued = false;

            // ── Stepping-disc mode (sticky) ──────────────────────────────────
            // A BotObstacleHint(RidePlatform) underfoot or within reach → hop-the-discs
            // mode. Bridge mode wins if both somehow match. Unlike bridge mode this does
            // NOT suppress jumps — hopping between discs is the whole point.
            bool stoneNear = !m_bridgeMode && ResolveStoneHint();
            if (stoneNear && Time.time >= m_stoneSuppressUntil)
                m_stoneModeUntil = Time.time + k_stoneModeHold;
            m_stoneMode = !m_bridgeMode && Time.time < m_stoneModeUntil
                          && Time.time >= m_stoneSuppressUntil;

            if (m_stoneMode && !m_stoneModePrev)
            {
                m_stoneEntryPos        = transform.position;
                m_stoneLastProgressPos = transform.position;
                m_stoneStalledSince    = 0f;
                m_stoneHasCur          = false;
                m_stoneHoppedFromCur   = false;

                if (!m_stonePathSearched)
                {
                    // ONE scene scan: the crossing-marker hint, and EVERY disc centre (from
                    // the per-disc RidePlatform hints — reliable, unlike an OverlapSphere that
                    // saturates its buffer with the field's other obstacle colliders).
                    m_stonePathSearched = true;
                    var centres = new System.Collections.Generic.List<Vector3>(16);
                    foreach (var h in UnityEngine.Object.FindObjectsByType<BotObstacleHint>(FindObjectsSortMode.None))
                    {
                        if (h.behavior != BotObstacleHint.Behavior.RidePlatform) continue;
                        if (h.HasStoneCrossing) { m_stonePathHint = h; continue; }
                        centres.Add(h.transform.position);
                    }
                    m_allDiscCentres = centres.ToArray();
                }

                m_stonePath = null;
                if (m_stonePathHint != null && m_stonePathHint.stoneStart != null
                    && m_stonePathHint.stoneEnd != null && m_allDiscCentres != null)
                {
                    m_stonePath = BuildStoneJumpPath(
                        m_stonePathHint.stoneStart.position, m_stonePathHint.stoneEnd.position, m_allDiscCentres);
                    m_stonePathIdx          = NearestStonePathIdx(m_stonePath, transform.position);
                    m_stonePathHopped       = false;
                    m_stonePathDoubleJumped = false;
                }

                var sb = new System.Text.StringBuilder();
                if (m_stonePath != null) foreach (var p in m_stonePath) sb.Append(p.ToString("F1")).Append(' ');
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') STONES ENTER at {transform.position:F1} " +
                          $"grounded={m_player.isGrounded} target=cp{m_nextCpIdx} " +
                          $"path={(m_stonePath != null ? $"{m_stonePath.Length}pts@{m_stonePathIdx}" : "auto")}" +
                          $" discs={m_allDiscCentres?.Length ?? 0} — {sb}");
            }
            if (!m_stoneMode && m_stoneModePrev)
            {
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') STONES EXIT at {transform.position:F1} " +
                          $"grounded={m_player.isGrounded} — travelled {Vector3.Distance(m_stoneEntryPos, transform.position):F1} m");
                m_stonePath = null;
            }
            m_stoneModePrev = m_stoneMode;

            // ── Launch-pad mode (sticky) ─────────────────────────────────────
            // A trampoline / mushroom-bounce directly on the route → walk on and ride the
            // bounce; the sticky window keeps it active through the airborne arc so the bot
            // commits to the crossing instead of dropping back onto the pad and bouncing
            // forever. Loses to bridge / stone mode if any of them somehow match at once.
            bool bounceNear = !m_bridgeMode && !m_stoneMode && ResolveBouncePlatform();
            if (bounceNear)
                m_bounceModeUntil = Time.time + k_bounceModeHold;
            m_bounceMode = !m_bridgeMode && !m_stoneMode && Time.time < m_bounceModeUntil;

            if (m_bounceMode && !m_bounceModePrev)
            {
                m_bounceEntryPos = transform.position;
                m_bounceLaunched = false;
                m_bounceAssisted = false;
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') LAUNCH PAD at {transform.position:F1} " +
                          $"— walking on, riding the bounce to cp{m_nextCpIdx}.");
            }
            if (!m_bounceMode && m_bounceModePrev)
            {
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') LAUNCH PAD clear at {transform.position:F1} " +
                          $"— travelled {Vector3.Distance(m_bounceEntryPos, transform.position):F1} m");
                m_bounceLaunched = false;
                m_bounceAssisted = false;
            }
            m_bounceModePrev = m_bounceMode;

            if (m_bounceMode)
            {
                // Detect the launch: grounded → airborne with a strong upward kick is the pad
                // firing (JumpingPlatform sets verticalVelocity to up * 30). Reset on landing.
                if (m_player.isGrounded)
                {
                    m_bounceLaunched = false;
                    m_bounceAssisted = false;
                    m_input.jumpQueued = false;   // no hops on the ground / on the pad — that's the bounce loop
                }
                else if (!m_bounceLaunched && m_player.verticalVelocity.y > 10f)
                {
                    // A strong upward kick while airborne in bounce mode = the pad fired
                    // (JumpingPlatform/MushroomBounce set verticalVelocity directly). Ground
                    // jumps are suppressed here, so this won't false-trigger on a normal jump.
                    m_bounceLaunched = true;
                }
            }

            CheckCheckpointArrival();
            Navigate();
            CheckStuck();
            ConsiderUsingPowerUp();
            LogProgress();

            // Belt-and-suspenders: clear any jump queued by the steering pass above
            // before the Player's own Update can consume it this frame. In bounce mode this
            // only applies while grounded — the ONE mid-air assist jump (see the m_bounceMode
            // block in ObstacleAvoidance) must survive to the Player's own Update.
            if (m_bridgeMode || (m_bounceMode && m_player.isGrounded))
                m_input.jumpQueued = false;
        }

        // Runs only while waiting for the race to start, when the bot should be perfectly
        // stationary at its ground-raycast-verified spawn point — any significant Y drop here
        // can only be an unwanted embed (e.g. a collider seam letting it sink onto a lower
        // interior surface — Physics still reports grounded=True there, so nothing else
        // catches it), never legitimate gameplay. Safe to auto-correct without risking
        // interference with real race mechanics (deaths, pits, etc.) once racing actually starts —
        // this check only ever runs before that.
        private void CheckEmbedWhileWaiting()
        {
            // Verify the first recovery attempt actually held, one second later.
            if (m_embedRecoveryPending)
            {
                if (Time.time < m_embedRecoveryCheckTime) return;
                m_embedRecoveryPending = false;
                if (transform.position.y >= m_expectedGroundY - k_embedDropThreshold) return; // held — done

                // Teleporting back to this bot's OWN spawn marker didn't hold — it sank again
                // within a second. That means the marker itself sits over unstable ground (a thin
                // shell over a gap that a single raycast can't tell apart from solid ground), not
                // a one-off physics jitter, so retrying the exact same spot would just repeat the
                // loop. Nudge sideways from where this bot ALREADY is (still within ~1.5m of its
                // own marker — the first recovery attempt's own spread offset) and re-probe ground
                // there — NOT a relocation to some other slot's marker. Jumping to any fixed
                // shared fallback point (this used to target spawnPoints[0], then
                // spawnPoints[m_humanSlotsTaken]) reproduces the exact bug being fixed the moment
                // more than one bot needs it in the same race: every struggling bot piles up on
                // that one shared spot, reading as "spawned on top of another character" no matter
                // which specific slot was hardcoded as the target.
                // Use m_expectedGroundY (the bot's known-good spawn elevation), not its CURRENT
                // height, as the base for this — it's currently sunk, so the ground probe's
                // raycast origin (computed as this position + a fixed height above it) could
                // start underground and miss the real surface entirely if built from where it is
                // right now instead of where it's supposed to be.
                float angle = (BotId % 4) * 90f * Mathf.Deg2Rad;
                Vector3 basePos  = new Vector3(transform.position.x, m_expectedGroundY, transform.position.z);
                Vector3 nudgedXZ = basePos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f;
                var safe = BotSpawner.Instance?.ResolveGroundedPositionAt(nudgedXZ);
                if (safe.HasValue && m_player != null)
                {
                    Vector3 fallback = safe.Value;
                    Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') still embedded after the first " +
                                      $"recovery attempt — its own spawn marker looks structurally unsafe. " +
                                      $"Nudging sideways to a re-verified ground spot at {fallback}.");
                    m_player.SetRespawn(fallback, Quaternion.identity);
                    m_player.Respawn();
                    m_expectedGroundY = fallback.y;
                }
                else
                {
                    Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') is still embedded at " +
                                      $"{transform.position} and no fallback ground position is available " +
                                      "— this spawn point needs fixing in the Editor (likely a gap in the " +
                                      "ground mesh collider near its marker).");
                }
                return;
            }

            if (m_embedCorrected) return;
            if (transform.position.y >= m_expectedGroundY - k_embedDropThreshold) return;

            m_embedCorrected = true;
            Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') sank to {transform.position} while " +
                              $"waiting for race start (expected ground Y≈{m_expectedGroundY:F2}) — snapping back up.");
            m_respawner?.RespawnNow();

            // Don't just trust it — a bot can sink right back through the same unstable spot
            // within moments, which is exactly what happened the first time this was tried.
            m_embedRecoveryPending   = true;
            m_embedRecoveryCheckTime = Time.time + 1f;
        }

        // Throttled heartbeat so a full play session leaves a continuous trace of each bot's
        // position/speed/target — lets us confirm movement stayed smooth after the fact instead
        // of only finding out something went wrong when the bot never reaches the finish line.
        private void LogProgress()
        {
            if (Time.time < m_nextProgressLogTime) return;
            m_nextProgressLogTime = Time.time + progressLogInterval;

            string target = (m_checkpoints != null && m_nextCpIdx < m_checkpoints.Length && m_checkpoints[m_nextCpIdx] != null)
                ? $"cp{m_checkpoints[m_nextCpIdx].index} (dist {Vector3.Distance(transform.position, m_checkpoints[m_nextCpIdx].transform.position):F1}m)"
                : "none (past last checkpoint)";

            Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') PROGRESS t={Time.time - m_raceStartLogTime:F1}s " +
                      $"pos={transform.position} speed={m_currentSpeed:F2} grounded={m_player.isGrounded} " +
                      $"target={target} stuckLvl={m_stuckLevel} backingUp={m_backingUp} " +
                      $"deaths={m_deaths} forcedRespawns={m_forcedRespawns} " +
                      $"jumps={m_obstaclesJumped} detours={m_obstaclesDetoured}.");
        }

        // ── Checkpoint arrival ────────────────────────────────────────────────

        private void CheckCheckpointArrival()
        {
            if (m_checkpoints == null || m_nextCpIdx >= m_checkpoints.Length) return;

            var cp = m_checkpoints[m_nextCpIdx];
            if (cp == null) { m_nextCpIdx++; return; }

            if (cp.isFinishLine)
            {
                if (!HasReachedFinishLine(cp)) return;

                RaceManager.Instance?.PlayerFinished(m_botId);

                StopBot();
                enabled = false;

                float raceTime = m_raceStartLogTime > 0f ? Time.time - m_raceStartLogTime : -1f;
                Debug.Log($"[RaceBotBrain] Bot {m_botId} ('{name}') FINISHED in {raceTime:F1}s " +
                          $"({Time.time - m_spawnTime:F1}s since spawn) — " +
                          $"deaths={m_deaths} forcedRespawns={m_forcedRespawns} " +
                          $"stuckEscalations={m_stuckEscalations} jumps={m_obstaclesJumped} detours={m_obstaclesDetoured}.");
                return;
            }

            if (!HasReachedCheckpoint(cp)) return;

            RaceManager.Instance?.RegisterCheckpoint(m_botId, cp.index);
            m_respawner?.SetRespawnPoint(cp.transform.position);
            Debug.Log($"[RaceBotBrain] Bot {m_botId} ('{name}') → checkpoint {cp.index} " +
                      $"at t={(m_raceStartLogTime > 0f ? Time.time - m_raceStartLogTime : 0f):F1}s, pos={transform.position}.");
            m_nextCpIdx++;
        }

        // An ordinary checkpoint is a wide trigger GATE (on DeathRunL2, 12 m across and 15 m
        // tall) whose transform origin can sit several metres ABOVE the raceable surface — cp0
        // there is at y≈8.7 while bots pass through the gate down at y≈2.4. A fixed radius
        // around that origin is then physically unreachable, and the bot ends up "stuck"
        // standing INSIDE a checkpoint volume it never gets credit for (the trampoline
        // pile-up). Register the same way a human does: inside the trigger Collider's bounds
        // (expanded a little for the dead-reckoned ghost), with the origin radius only as a
        // fallback when the checkpoint has no Collider at all.
        private bool HasReachedCheckpoint(RaceCheckpoint cp)
        {
            if (m_cpColliderIdx != m_nextCpIdx)
            {
                m_cpCollider    = cp.GetComponent<Collider>();
                m_cpColliderIdx = m_nextCpIdx;
            }
            if (m_cpCollider != null)
            {
                // Inside the (loosely expanded) trigger box, OR close to the nearest point on
                // it — so a ghost that passes just UNDER a gate floating off the ground, or
                // arcs just over it out of a trampoline, still gets credit for reaching it
                // instead of standing next to a checkpoint it can never satisfy.
                Bounds b = m_cpCollider.bounds;
                b.Expand(2f);
                if (b.Contains(transform.position)) return true;
                if (Vector3.Distance(transform.position, m_cpCollider.ClosestPoint(transform.position)) <= arrivalRadius + 1.5f)
                    return true;
            }
            return Vector3.Distance(transform.position, cp.transform.position) <= arrivalRadius;
        }

        // The finish line needs the same precision a human gets from physically entering its
        // trigger collider — arrivalRadius (used above for ordinary waypoints) is intentionally
        // generous and would otherwise mark bots "Complete" while still short of the actual line.
        // Uses the checkpoint's own trigger Collider bounds when present (matching whatever the
        // level designer already configured for human detection — no separate tuning needed);
        // falls back to a tight radius only if the finish marker has no Collider at all.
        private bool HasReachedFinishLine(RaceCheckpoint cp)
        {
            if (m_finishCollider == null || m_finishCollider.gameObject != cp.gameObject)
                m_finishCollider = cp.GetComponent<Collider>();

            if (m_finishCollider != null)
                return m_finishCollider.bounds.Contains(transform.position);

            return Vector3.Distance(transform.position, cp.transform.position) <= finishArrivalRadius;
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private void Navigate()
        {
            if (m_checkpoints == null || m_nextCpIdx >= m_checkpoints.Length)
            {
                SetSpeed(0f, false); return;
            }

            var curCp = m_checkpoints[m_nextCpIdx];
            if (curCp == null) { SetSpeed(0f, false); return; }

            // ── Step 1: base direction toward current checkpoint ───────────────
            Vector3 toCur   = Vector3.ProjectOnPlane(curCp.transform.position - transform.position, Vector3.up);
            float   distCur = toCur.magnitude;
            Vector3 dirCur  = distCur > 0.1f ? toCur / distCur : transform.forward;

            // ── Step 2: look-ahead blend toward the NEXT checkpoint ───────────
            // As we close in on the current checkpoint, smoothly start aiming at
            // the next one — creates a racing line arc rather than a V-shaped path.
            Vector3 dir = dirCur;
            if (m_nextCpIdx + 1 < m_checkpoints.Length)
            {
                var nextCp = m_checkpoints[m_nextCpIdx + 1];
                if (nextCp != null)
                {
                    Vector3 toNext  = Vector3.ProjectOnPlane(nextCp.transform.position - transform.position, Vector3.up);
                    Vector3 dirNext = toNext.magnitude > 0.1f ? toNext.normalized : dirCur;
                    float   blend   = Mathf.Clamp01(1f - distCur / m_lookAheadBlendDist);
                    dir = Vector3.Slerp(dirCur, dirNext, blend).normalized;
                }
            }

            // ── Step 2.5: power-up awareness ──────────────────────────────────
            // A human naturally drifts toward a nearby box without abandoning their line —
            // blend a small pull toward it, stronger the closer it is, only while there's
            // room to hold another power-up. Never fully overrides checkpoint navigation.
            if (HasFreePowerUpSlot())
            {
                var box = FindNearbyPowerUpBox();
                if (box != null)
                {
                    Vector3 toBox   = Vector3.ProjectOnPlane(box.transform.position - transform.position, Vector3.up);
                    float   distBox = toBox.magnitude;
                    if (distBox > 0.1f)
                    {
                        float blend = Mathf.Clamp01(1f - distBox / powerUpDetectionRadius) * powerUpSeekStrength;
                        dir = Vector3.Slerp(dir, toBox / distBox, blend).normalized;
                    }
                }
            }

            // ── Step 2.75: route to a hinted bridge's mouth ───────────────────
            // Bots approach aiming at the next checkpoint, which sits off to one side of
            // the far end — so the checkpoint pull drags them diagonally into the SIDE of
            // the bridge structure (they brush the wide decorative end frames 3-4 m
            // off-centre, never reach the deck, and walk into the water). Before they're
            // on the deck, override the heading to aim at the bridge MOUTH so they line up
            // with the entrance first. Bridge mode's own alignment phase takes over once
            // they arrive.
            if (!m_bridgeMode && TryGetBridgeApproach(out Vector3 bridgeMouth))
            {
                Vector3 toMouth = Vector3.ProjectOnPlane(bridgeMouth - transform.position, Vector3.up);
                if (toMouth.sqrMagnitude > 0.25f)
                    dir = toMouth.normalized;
            }

            // ── Step 3: sinusoidal lateral weave ──────────────────────────────
            // Rotates the movement direction left/right in a sine wave so the bot
            // follows a gentle S-curve rather than a perfectly straight line.
            // Each bot has a unique m_weavePhase so they don't all sway together.
            //
            // Suppressed on a narrow crossing (a hazard — water, a pit — close on BOTH
            // sides, e.g. a bridge): a few degrees of wobble is harmless out in the open,
            // but is exactly what walks a bot off a narrow deck into the water beside it.
            // A real player instinctively walks a bridge in a straight line rather than
            // weaving across it, so hold dead-center instead of applying any S-curve here.
            bool onNarrowCrossing = HazardAhead(Quaternion.Euler(0f, -sideRayAngle, 0f) * dir, out _)
                                  && HazardAhead(Quaternion.Euler(0f,  sideRayAngle, 0f) * dir, out _);

            // Also hold a straight line while riding a moving/rotating platform or crossing a
            // hinted rope bridge — a few degrees of S-curve on a small disc or a 0.5 m plank
            // walks the bot straight off the edge.
            bool holdStraight = OnRideablePlatform() || m_bridgeMode || m_stoneMode || m_bounceMode;
            if (m_weaveAmplitude > 0.5f && !onNarrowCrossing && !holdStraight)
            {
                float weaveAngle = Mathf.Sin(Time.time * m_weaveFrequency * Mathf.PI * 2f + m_weavePhase)
                                   * m_weaveAmplitude;
                dir = Quaternion.Euler(0f, weaveAngle, 0f) * dir;
                dir.Normalize();
            }

            // ── Step 4: obstacle/gap avoidance ────────────────────────────────
            if (dir.sqrMagnitude > 0.01f)
                dir = ObstacleAvoidance(dir);

            // Hazard-boxed-in returns a zero vector deliberately (see ObstacleAvoidance) — brake
            // to a real stop here instead of letting m_currentSpeed keep ramping toward "sprint"
            // while stationary, which would otherwise cause an unnaturally instant full-speed
            // burst the moment a safe direction opens back up instead of a normal accel ramp.
            if (dir.sqrMagnitude < 0.0001f)
            {
                SetSpeed(0f, false);
                return;
            }

            // ── Step 5: compute target speed ──────────────────────────────────
            // Turn angle between current facing and desired direction determines
            // how much the bot needs to brake. Sharp turns → much slower.
            float turnAngle = Vector3.Angle(transform.forward, dir);

            bool  wantRun;
            float wantSpeed;

            if (turnAngle >= m_hardBrakeTurnAngle)
            {
                // Hard corner — walk slowly
                float t = Mathf.Clamp01((turnAngle - m_hardBrakeTurnAngle) / 60f);
                wantSpeed = Mathf.Lerp(0.65f, 0.40f, t);
                wantRun   = false;
            }
            else if (turnAngle >= m_softBrakeTurnAngle)
            {
                // Soft corner — partial slow
                float t = (turnAngle - m_softBrakeTurnAngle) / (m_hardBrakeTurnAngle - m_softBrakeTurnAngle);
                wantSpeed = Mathf.Lerp(1f, 0.65f, t);
                wantRun   = wantSpeed > 0.8f;
            }
            else
            {
                // Straight path — full sprint
                wantSpeed = 1f;
                wantRun   = true;
            }

            // Random walk breaks (Easy bots hesitate occasionally)
            if (m_walkBreakChance > 0f)
            {
                if (Time.time < m_walkBreakEndTime)
                {
                    wantSpeed = Mathf.Min(wantSpeed, 0.55f);
                    wantRun   = false;
                }
                else if (Random.value < m_walkBreakChance * Time.deltaTime)
                {
                    m_walkBreakEndTime = Time.time + Random.Range(m_walkBreakDuration * 0.7f, m_walkBreakDuration * 1.3f);
                }
            }

            // On a bridge, keep a steady moderate pace regardless of the turn-brake — the
            // deck-centring correction can swing the heading wide and the turn-brake would
            // otherwise stall the bot mid-crossing. Never sprint (overshoots the centre),
            // never crawl (a stalled bot on a rope bridge just gets shoved off by the sag).
            if (m_bridgeMode)
            {
                // Slow right down while correcting a lateral error so momentum can't
                // carry the bot past the centreline; steady moderate pace once on it.
                wantSpeed = Mathf.Abs(m_bridgeLateralOff) > 0.3f ? 0.4f : 0.65f;
                wantRun   = false;
            }
            else if (m_stoneMode)
            {
                // Full speed + run on the ground so the RUNNING jump actually clears the
                // ~6 m gap between disc centres; moderate in the air for landing control.
                bool g = m_player != null && m_player.isGrounded;
                wantSpeed = g ? 1.0f : 0.6f;
                wantRun   = g && m_currentSpeed > 0.7f;
            }
            else if (m_bounceMode)
            {
                // Moderate approach — enough momentum to carry onto the pad, not a flat-out
                // run that skims across without a grounded contact frame (which skips the
                // launch). Near-full + run once launched so air control carries the bot
                // forward onto the raised platform.
                wantSpeed = m_bounceLaunched ? 0.95f : 0.7f;
                wantRun   = m_bounceLaunched;
            }

            // ── Step 6: smooth speed toward target ────────────────────────────
            // MoveTowards gives linear ramp — feels like real momentum.
            float rate = (wantSpeed > m_currentSpeed) ? m_speedAccelRate : m_speedDecelRate;
            m_currentSpeed = Mathf.MoveTowards(m_currentSpeed, wantSpeed, rate * Time.deltaTime);

            m_input.desiredMoveDirection = dir * m_currentSpeed;
            m_input.runHeld              = wantRun && m_currentSpeed > 0.75f;
        }

        // Helper so backup path also goes through the same speed update
        private void SetSpeed(float target, bool run)
        {
            float rate = (target > m_currentSpeed) ? m_speedAccelRate : m_speedDecelRate;
            m_currentSpeed = Mathf.MoveTowards(m_currentSpeed, target, rate * Time.deltaTime);
            m_input.desiredMoveDirection = Vector3.zero;
            m_input.runHeld = run;
        }

        // ── Obstacle avoidance ────────────────────────────────────────────────
        //
        // Real players don't hop over every low obstacle in their path — they walk
        // around when there's room and only jump when they have to. So for a
        // jumpable-height obstacle we check side clearance FIRST and prefer
        // detouring around it; jumping is a fallback (both sides blocked) or an
        // occasional deliberate choice (m_obstacleHopBias) for a quick hop over
        // something small, rather than the automatic reaction it used to be.

        private Vector3 ObstacleAvoidance(Vector3 moveDir)
        {
            Vector3 origin = transform.position + Vector3.up * 0.6f;

            // ── Designer hint: Bridge ─────────────────────────────────────────────
            // Best case: the hint carries two end markers (pathA/pathB) on the deck
            // centreline — the bot pure-pursues exactly along that line, which is
            // completely reliable. Fallback (no markers): walk the entry heading with a
            // deck-scan lateral nudge. Jumps blocked in TryJump(); all other checks below
            // skipped. On a >2 s forward stall (wedged on a rope) → one recovery nudge.
            if (m_bridgeMode)
            {
                if (!m_wasOnBridge)
                {
                    m_wasOnBridge = true;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') crossing a hinted bridge " +
                              $"({(m_bridgeHint != null && m_bridgeHint.HasCrossingLine ? "line" : "scan")}).");
                }

                Vector3 pos = transform.position;
                Vector3 bridgeDir;
                string  method;
                float   lateralOff = 0f;

                if (m_bridgeExitValid && m_bridgeHint != null && m_bridgeHint.HasCrossingLine)
                {
                    // ── Pure pursuit along the A↔B centreline ────────────────────
                    Vector3 a = m_bridgeHint.pathA.position;
                    Vector3 b = m_bridgeHint.pathB.position;
                    // Exit end is LATCHED at bridge entry (see Update) — never recomputed
                    // here, or the bot flips direction at the midpoint and oscillates.
                    Vector3 exit  = m_bridgeExitPos;
                    Vector3 entry = (exit - a).sqrMagnitude <= (exit - b).sqrMagnitude ? b : a;

                    Vector3 lineDir   = (exit - entry); lineDir.y = 0f; lineDir.Normalize();
                    Vector3 rightAxis = Vector3.Cross(Vector3.up, lineDir); // unit, +right of travel
                    // closest point on the (flattened) line to the bot
                    Vector3 flatEntry = new Vector3(entry.x, pos.y, entry.z);
                    float   t = Vector3.Dot(pos - flatEntry, lineDir);
                    Vector3 onLine  = flatEntry + lineDir * t;
                    float   lineLen = Vector3.Distance(new Vector3(entry.x, 0, entry.z), new Vector3(exit.x, 0, exit.z));

                    lateralOff = Vector3.Dot(pos - onLine, rightAxis);
                    float progress01 = lineLen > 0.01f ? t / lineLen : 1f;

                    // Aligned once the bot has reached the mouth and is roughly centred;
                    // it then commits to the crossing and won't drop back to alignment on
                    // a transient sideways knock.
                    if (!m_bridgeAligned && t > 0.3f && Mathf.Abs(lateralOff) < 0.8f)
                        m_bridgeAligned = true;

                    if (!m_bridgeAligned)
                    {
                        // ── Alignment phase ────────────────────────────────────
                        // Armed while still off to the side / short of the deck: aim at a
                        // point ON the centreline (the mouth, or just ahead of the bot if
                        // it's already a little onto the deck) — never diagonally across
                        // the open middle, and never backwards.
                        Vector3 mouth   = flatEntry + lineDir * Mathf.Max(t + 0.3f, 0.8f);
                        Vector3 toMouth = mouth - pos; toMouth.y = 0f;
                        bridgeDir = toMouth.sqrMagnitude > 0.0001f ? toMouth.normalized : lineDir;
                        method    = "align";
                    }
                    else if (progress01 > 1.02f)
                    {
                        // At/past the exit marker → END the crossing. Line-follow here
                        // just crabs the bot side-to-side on the last plank (aim clamped
                        // to the line end) and it never steps off onto the far island.
                        m_bridgeModeUntil     = 0f;
                        m_bridgeSuppressUntil = Mathf.Max(m_bridgeSuppressUntil, Time.time + 1f);
                        m_bridgeLateralOff    = 0f;
                        if (Time.time >= m_nextBridgeLog)
                        {
                            m_nextBridgeLog = Time.time + 0.25f;
                            Debug.Log($"[RaceBotBrain] Bot {BotId} BRIDGE[line] EXIT-STEP pos={pos:F2} " +
                                      $"progress={progress01:F2} — walking off onto the far island");
                        }
                        return lineDir.sqrMagnitude > 0.01f ? lineDir : transform.forward;
                    }
                    else
                    {
                        // Continuous P-controller: march along the deck (forward bias) with
                        // a proportional pull back to the centreline. No bang-bang lookahead
                        // — that limit-cycled (dir flipping ±rightAxis, no forward progress).
                        // Forward bias RISES near the exit so the bot commits to stepping off.
                        float   fwdBias = progress01 > 0.8f ? 3.0f : 1.7f;
                        float   corr    = Mathf.Clamp(-lateralOff * 1.6f, -1.3f, 1.3f);
                        Vector3 desired = lineDir * fwdBias + rightAxis * corr;
                        bridgeDir = desired.sqrMagnitude > 0.0001f ? desired.normalized : lineDir;
                        method = "line";
                    }
                }
                else
                {
                    // ── Fallback: entry heading + short-range deck centring ──────
                    // Down-probe kept SHORT (1.2 m, and only counting hits near foot
                    // height) so it detects the actual planks, not a water/landscape
                    // collider several metres below (which read as "deck everywhere").
                    Vector3 axis = m_bridgeAxis.sqrMagnitude > 0.1f ? m_bridgeAxis
                        : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                    Vector3 rightAxis = Vector3.Cross(Vector3.up, axis).normalized;

                    float lo = 999f, hi = -999f;
                    for (float lat = -2f; lat <= 2f; lat += 0.2f)
                    {
                        Vector3 p = pos + rightAxis * lat + axis * 0.8f + Vector3.up * 0.5f;
                        if (Physics.Raycast(p, Vector3.down, out RaycastHit dh, 1.2f,
                                obstacleLayer, QueryTriggerInteraction.Ignore) && dh.point.y > pos.y - 0.8f)
                        { lo = Mathf.Min(lo, lat); hi = Mathf.Max(hi, lat); }
                    }
                    bridgeDir = axis;
                    if (hi >= lo)
                    {
                        float mid = (lo + hi) * 0.5f;
                        lateralOff = -mid;
                        if (Mathf.Abs(mid) > 0.1f)
                            bridgeDir = (axis + rightAxis * Mathf.Clamp(mid, -1f, 1f) * 1.2f).normalized;
                    }
                    method = "scan";
                }

                m_bridgeLateralOff = lateralOff; // Navigate() slows the bot while this is large

                // ── Stall recovery (wedged against a rope) ──────────────────────
                if (Vector3.Distance(pos, m_bridgeLastProgressPos) > 0.4f)
                { m_bridgeLastProgressPos = pos; m_bridgeStalledSince = 0f; }
                else if (m_bridgeStalledSince == 0f)
                { m_bridgeStalledSince = Time.time; }

                bool stalled = m_bridgeStalledSince != 0f && Time.time - m_bridgeStalledSince > 2f;
                if (stalled)
                {
                    // Alternate a small left/right yaw so it slides off whatever it's caught on.
                    float wob = Mathf.Sin(Time.time * 6f) * 25f;
                    bridgeDir = Quaternion.Euler(0f, wob, 0f) * bridgeDir;
                }

                // ── Logging (throttled) + airborne watch ────────────────────────
                if (Time.time >= m_nextBridgeLog)
                {
                    m_nextBridgeLog = Time.time + 0.25f;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} BRIDGE[{method}] pos={pos:F2} " +
                              $"grounded={m_player.isGrounded} spd={m_currentSpeed:F2} aligned={m_bridgeAligned} " +
                              $"lateralOff={lateralOff:F2} dir={bridgeDir:F2} stalled={stalled}");
                }
                if (!m_player.isGrounded)
                {
                    if (m_bridgeAirborneSince == 0f) m_bridgeAirborneSince = Time.time;
                    else if (Time.time - m_bridgeAirborneSince > 0.4f)
                        Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') AIRBORNE ON BRIDGE " +
                            $"{Time.time - m_bridgeAirborneSince:F1}s at {pos:F2} — falling off?");
                }
                else m_bridgeAirborneSince = 0f;

                m_lastLoggedObstacle    = null;
                m_lastLoggedTimedHazard = null;
                m_lastLoggedHazard      = null;
                m_waitingForHazardClear = false;
                return bridgeDir.sqrMagnitude > 0.01f ? bridgeDir.normalized : transform.forward;
            }

            // ── Designer hint: RidePlatform (stepping discs) ─────────────────────
            // A field of spaced rotating disc platforms bridging a gap (DeathRunL1
            // moving_platform cluster). The gaps are real (~several m between centres), so the
            // bot HOPS disc to disc: walk toward the next disc's centre, and once per disc
            // queue exactly ONE jump to clear the gap, aiming the arc at that centre. Land,
            // that becomes the current disc, repeat. Every wall/gap/hazard/detour check below
            // is skipped while this is active.
            if (m_stoneMode)
            {
                Vector3 pos  = transform.position;
                Vector3 toCp = StoneCheckpointDir(pos);
                bool grounded = m_player != null && m_player.isGrounded;
                if (grounded) { if (m_stoneGroundedSince == 0f) m_stoneGroundedSince = Time.time; }
                else m_stoneGroundedSince = 0f;
                bool settled = grounded && Time.time - m_stoneGroundedSince > 0.25f;

                int nc = CollectStoneCentres(pos, 20f);

                // ── Wedged? (fell against a disc underside at the edge / stuck in a seam) ──
                if (Vector3.Distance(pos, m_stoneLastProgressPos) > 0.4f)
                { m_stoneLastProgressPos = pos; m_stoneStalledSince = 0f; }
                else if (m_stoneStalledSince == 0f)
                { m_stoneStalledSince = Time.time; }
                if (m_stoneStalledSince != 0f && Time.time - m_stoneStalledSince > 3f)
                {
                    m_stoneModeUntil     = 0f;
                    m_stoneSuppressUntil = Time.time + 3f;
                    m_stoneStalledSince  = 0f;
                    if (Time.time >= m_nextStoneLog)
                    {
                        m_nextStoneLog = Time.time + 0.25f;
                        Debug.LogWarning($"[RaceBotBrain] Bot {BotId} STONES WEDGED at {pos:F2} — dropping for stuck-recovery.");
                    }
                    return toCp;
                }

                // ── Designer crossing (BotObstacleHint.stoneStart/stoneEnd) ──────────
                // Path was built on entry: start → disc centres in between → end. Walk to the
                // current waypoint; on reaching it, queue ONE hop toward the next and advance.
                if (m_stonePath != null && m_stonePath.Length >= 2)
                {
                    // This branch is the ONLY thing that jumps on the discs — wipe any hop
                    // queued elsewhere this frame; the hop below re-sets it if it's time.
                    m_input.jumpQueued = false;

                    if (m_stonePathIdx >= m_stonePath.Length)
                    {
                        m_stoneModeUntil     = 0f;
                        m_stoneSuppressUntil = Mathf.Max(m_stoneSuppressUntil, Time.time + 3f);
                        m_stonePath = null;
                        Debug.Log($"[RaceBotBrain] Bot {BotId} STONES PATH DONE at {pos:F1} → cp{m_nextCpIdx}");
                        return toCp;
                    }

                    Vector3 wp   = m_stonePath[m_stonePathIdx];
                    Vector3 toWp = wp - pos; toWp.y = 0f;
                    float   wpD  = toWp.magnitude;

                    // Reached this waypoint (grounded & close) → advance.
                    if (settled && wpD < 1.9f)
                    {
                        m_stonePathIdx++;
                        m_stonePathHopped       = false;
                        m_stonePathDoubleJumped = false;
                        if (m_stonePathIdx >= m_stonePath.Length)
                        {
                            m_stoneModeUntil     = 0f;
                            m_stoneSuppressUntil = Mathf.Max(m_stoneSuppressUntil, Time.time + 3f);
                            m_stonePath = null;
                            Debug.Log($"[RaceBotBrain] Bot {BotId} STONES PATH DONE at {pos:F1} → cp{m_nextCpIdx}");
                            return toCp;
                        }
                        wp   = m_stonePath[m_stonePathIdx];
                        toWp = wp - pos; toWp.y = 0f; wpD = toWp.magnitude;
                    }

                    Vector3 pdir = wpD > 0.05f ? toWp / wpD : toCp;

                    // Run across the current disc toward the next waypoint; hop when the
                    // ground actually runs out ahead (probe 1.4 m forward finds no disc).
                    // One hop per waypoint, debounced past the flight time.
                    bool pathGap = false;
                    if (settled && !m_stonePathHopped && Time.time >= m_stoneHopDebounce && wpD > 1.9f)
                    {
                        Vector3 probePt = pos + pdir * 1.4f + Vector3.up * 0.4f;
                        pathGap = !(Physics.Raycast(probePt, Vector3.down, out RaycastHit ph, 1.6f,
                                        obstacleLayer, QueryTriggerInteraction.Ignore)
                                    && ph.point.y > pos.y - 1.0f);
                        if (pathGap)
                        {
                            m_input.jumpQueued   = true;
                            m_stonePathHopped    = true;
                            m_stonePathHopTime   = Time.time;
                            m_stoneHopDebounce   = Time.time + 1.2f;
                            Debug.Log($"[RaceBotBrain] Bot {BotId} STONES PATH HOP idx {m_stonePathIdx} → {wp:F1} (d {wpD:F1})");
                        }
                    }

                    // Mid-air 2nd jump (double-jump for characters that have it) to EXTEND the
                    // hop when a ~6 m disc gap is too far for a single jump: past the arc apex
                    // (falling), still short of the waypoint, and nothing to land on below.
                    if (!grounded && m_stonePathHopped && !m_stonePathDoubleJumped
                        && Time.time - m_stonePathHopTime > 0.35f && wpD > 2.5f
                        && pos.y < wp.y + 1.2f
                        && !Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, 2.5f,
                                            obstacleLayer, QueryTriggerInteraction.Ignore))
                    {
                        m_input.jumpQueued      = true;
                        m_stonePathDoubleJumped = true;
                        Debug.Log($"[RaceBotBrain] Bot {BotId} STONES PATH HOP2 (extend) at {pos:F1} wpD={wpD:F1}");
                    }

                    if (Time.time >= m_nextStoneLog)
                    {
                        m_nextStoneLog = Time.time + 0.25f;
                        Debug.Log($"[RaceBotBrain] Bot {BotId} STONES PATH pos={pos:F2} grounded={grounded} settled={settled} " +
                                  $"idx={m_stonePathIdx}/{m_stonePath.Length} wpD={wpD:F2} gap={pathGap} hopped={m_stonePathHopped} dir={pdir:F2}");
                    }

                    m_lastLoggedObstacle    = null;
                    m_lastLoggedTimedHazard  = null;
                    m_lastLoggedHazard       = null;
                    m_waitingForHazardClear  = false;
                    return pdir.sqrMagnitude > 0.01f ? pdir.normalized : transform.forward;
                }

                // Current disc = the one nearest the bot.
                int curIdx = -1; float curD2 = float.MaxValue;
                for (int i = 0; i < nc; i++)
                {
                    Vector3 d = s_stoneCentres[i] - pos; d.y = 0f;
                    if (d.sqrMagnitude < curD2) { curD2 = d.sqrMagnitude; curIdx = i; }
                }
                if (curIdx < 0) return toCp; // no discs in range — shouldn't happen
                float   curD = Mathf.Sqrt(curD2);
                Vector3 cur  = s_stoneCentres[curIdx];

                // Landed and SETTLED on a NEW disc → reset the one-hop-per-disc latch. Must be
                // settled (not a 1-frame mid-air graze of a disc in this dense grid) or the
                // latch clears mid-flight and the bot double-jumps to Y≈6 and overshoots.
                if (settled && curD < 1.8f
                    && (!m_stoneHasCur || (cur - m_stoneCurCentre).sqrMagnitude > 2.0f))
                {
                    m_stoneCurCentre     = cur;
                    m_stoneHasCur        = true;
                    m_stoneHoppedFromCur = false;
                }

                // Next disc = nearest disc that is FORWARD of the disc we're hopping FROM
                // (m_stoneCurCentre) toward the checkpoint. Anchoring "forward" on the from-
                // disc, and excluding it, means a bot mid-hop keeps committing to the target
                // instead of air-steering back to the disc it just left.
                Vector3 fromC = m_stoneHasCur ? m_stoneCurCentre : cur;
                int nextIdx = -1; float nextD = float.MaxValue;
                for (int i = 0; i < nc; i++)
                {
                    if (i == curIdx) continue;
                    if ((s_stoneCentres[i] - fromC).sqrMagnitude < 2.25f) continue; // the from-disc
                    Vector3 s = s_stoneCentres[i] - fromC; s.y = 0f;
                    float sd = s.magnitude;
                    if (sd < 0.5f || Vector3.Dot(s / sd, toCp) < -0.2f) continue;   // clearly backward
                    // nearest disc to the from-disc that isn't backward = the next stepping stone
                    if (sd < nextD) { nextD = sd; nextIdx = i; }
                }

                if (nextIdx < 0)
                {
                    // On the last disc — the checkpoint is on the far island now.
                    if (grounded && curD < 3.0f)
                    {
                        // Gap between here and the island? Do ONE hop toward the checkpoint,
                        // then bail. (The user's "jump to the finish disc" step.)
                        Vector3 fp = pos + toCp * 1.3f + Vector3.up * 0.4f;
                        bool islandGap = !(Physics.Raycast(fp, Vector3.down, out RaycastHit fh2, 1.5f,
                                            obstacleLayer, QueryTriggerInteraction.Ignore)
                                           && fh2.point.y > pos.y - 1.0f);
                        if (settled && islandGap && !m_stoneHoppedFromCur && Time.time >= m_stoneHopDebounce)
                        {
                            m_input.jumpQueued   = true;
                            m_stoneHoppedFromCur = true;
                            m_stoneHopDebounce   = Time.time + 1.2f;
                            Debug.Log($"[RaceBotBrain] Bot {BotId} STONES FINAL HOP {pos:F1} → island (cp{m_nextCpIdx})");
                            return toCp;
                        }
                        m_stoneModeUntil     = 0f;
                        m_stoneSuppressUntil = Mathf.Max(m_stoneSuppressUntil, Time.time + 3f);
                        if (Time.time >= m_nextStoneLog)
                        {
                            m_nextStoneLog = Time.time + 0.25f;
                            Debug.Log($"[RaceBotBrain] Bot {BotId} STONES EXIT-STEP at {pos:F2} discs={nc} — across, to cp{m_nextCpIdx}");
                        }
                        m_lastLoggedObstacle = null;
                        return toCp;
                    }
                    // Airborne with nothing ahead: if we just did the final hop, keep flying
                    // toward the island; otherwise steer back onto the last disc.
                    if (m_stoneHoppedFromCur) return toCp;
                    Vector3 back = cur - pos; back.y = 0f;
                    return back.sqrMagnitude > 0.01f ? back.normalized : toCp;
                }

                Vector3 next    = s_stoneCentres[nextIdx];
                Vector3 toNext  = next - pos; toNext.y = 0f;
                float   gap     = toNext.magnitude;
                Vector3 stoneDir = gap > 0.05f ? toNext / gap : toCp;

                // These discs are large and overlap/near-touch in places, so the bot mostly
                // WALKS. It only HOPS when there's an actual gap right in front of it — a
                // down-probe ~1.3 m ahead at foot height finds no disc. One hop per disc,
                // debounced past the flight, and only once settled (not a mid-air graze).
                bool gapAhead = false;
                if (settled)
                {
                    Vector3 pp = pos + stoneDir * 1.3f + Vector3.up * 0.4f;
                    gapAhead = !(Physics.Raycast(pp, Vector3.down, out RaycastHit gh, 1.5f,
                                    obstacleLayer, QueryTriggerInteraction.Ignore)
                                 && gh.point.y > pos.y - 1.0f);
                }
                if (settled && gapAhead && !m_stoneHoppedFromCur && Time.time >= m_stoneHopDebounce)
                {
                    m_input.jumpQueued   = true;
                    m_stoneHoppedFromCur = true;
                    m_stoneHopDebounce   = Time.time + 1.2f;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} STONES HOP {pos:F1} → disc {next:F1} (gap {gap:F1} m)");
                }

                if (Time.time >= m_nextStoneLog)
                {
                    m_nextStoneLog = Time.time + 0.25f;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} STONES pos={pos:F2} grounded={grounded} settled={settled} " +
                              $"discs={nc} curD={curD:F2} gap={gap:F2} gapAhead={gapAhead} hopped={m_stoneHoppedFromCur} dir={stoneDir:F2}");
                }

                m_lastLoggedObstacle    = null;
                m_lastLoggedTimedHazard  = null;
                m_lastLoggedHazard       = null;
                m_waitingForHazardClear  = false;
                return stoneDir.sqrMagnitude > 0.01f ? stoneDir.normalized : transform.forward;
            }

            // ── Launch pad (trampoline / mushroom bounce) ────────────────────────
            // Aim straight at the checkpoint the whole time and skip every wall/detour/gap
            // check below (the pad's collider reads as a chest-high "wall" and the drop past
            // it as a "gap" — both reactions break the launch). Ground jumps are suppressed
            // in Update (the bounce loop); the ONE deliberate jump is a mid-air assist once
            // past the apex — the extra height + the air-control window is what lets a
            // non-glide character reach a raised checkpoint platform after the launch.
            if (m_bounceMode)
            {
                m_lastLoggedObstacle    = null;
                m_lastLoggedTimedHazard  = null;
                m_lastLoggedHazard       = null;
                m_waitingForHazardClear  = false;

                if (m_bounceLaunched && !m_bounceAssisted && !m_player.isGrounded
                    && m_player.verticalVelocity.y < 2f)
                {
                    m_input.jumpQueued = true;
                    m_bounceAssisted   = true;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') BOUNCE ASSIST at " +
                              $"{transform.position:F1} → cp{m_nextCpIdx}");
                }

                Vector3 aim = StoneCheckpointDir(transform.position);
                return aim.sqrMagnitude > 0.01f ? aim : transform.forward;
            }

            // ── Designer hint: WalkThrough ───────────────────────────────────────
            // Per-frame (not sticky) — walk straight onto/over whatever's directly ahead
            // that the designer marked (a log the heuristic misreads, a decorative prop).
            if (Physics.Raycast(origin, moveDir, out RaycastHit hintHit,
                    Mathf.Max(obstacleCheckDist, 3.5f), obstacleLayer, QueryTriggerInteraction.Ignore)
                && FindObstacleHint(hintHit.collider) == BotObstacleHint.Behavior.WalkThrough)
            {
                m_lastLoggedObstacle = null;
                return moveDir;
            }

            // ── Bridge centering — checked before anything else ─────────────────────
            // Solid geometry (railings/walls) found close on BOTH sides at once is the
            // signature of a narrow crossing — a bridge — rather than two unrelated
            // obstacles. A real player naturally walks the middle of a bridge instead of
            // hugging one edge; actively pull toward the midpoint between the rails here
            // rather than only reacting once one side is actually hit.
            bool railLeftFound  = Physics.Raycast(origin, Quaternion.Euler(0f, -90f, 0f) * moveDir,
                out RaycastHit railLeftHit, bridgeRailCheckDistance, obstacleLayer, QueryTriggerInteraction.Ignore);
            bool railRightFound = Physics.Raycast(origin, Quaternion.Euler(0f, 90f, 0f) * moveDir,
                out RaycastHit railRightHit, bridgeRailCheckDistance, obstacleLayer, QueryTriggerInteraction.Ignore);
            bool onBridge = railLeftFound && railRightFound;

            // Fallback for a narrow crossing with NO physical railings modeled. Confirmed via
            // Editor.log that this happens in practice: a water-flanked walkway with no rail
            // colliders never satisfies the check above, so a bot walking dead-center kept
            // reading "hazard on all sides" forever instead — hazardCheckRadius reaches the
            // flanking water even from the middle of a narrow deck, and with no rails to fall
            // back on there was nothing to tell that apart from a genuine dead end. Standing on
            // solid, non-hazard ground while hazard flanks both sides at 90° is the same
            // signature, just without a railing collider to measure a midpoint from.
            //
            // Deliberately uses checkBelow: false here — same-height hazard only, NOT the deeper
            // downward probe IsHazardAt otherwise does. Confirmed via Editor.log this matters:
            // enabling the deep probe here (to also catch an ELEVATED bridge with no rails)
            // caused bots to massively regress at an ordinary cliff edge / peninsula near the
            // start — any spot with open water/void merely somewhere below on both flanks got
            // misclassified as "a safe bridge to walk straight across," which suppresses every
            // other safety check (gap-jump, obstacle avoidance, hazard steering) below. A real
            // elevated bridge with actual rails is still caught by the railLeftFound/
            // railRightFound check above, unaffected by this restriction — this fallback only
            // exists for the narrower, rarer case of a railless walkway at roughly deck height,
            // and being conservative there is far safer than risking that misclassification
            // anywhere else on the map.
            if (!onBridge
                && !IsHazardAt(transform.position, out _, checkBelow: false)
                && HazardAhead(Quaternion.Euler(0f, -90f, 0f) * moveDir, out _, checkBelow: false)
                && HazardAhead(Quaternion.Euler(0f,  90f, 0f) * moveDir, out _, checkBelow: false))
            {
                onBridge = true;
            }

            // Death memory overrides the bridge classification entirely, however it was reached
            // (rails or the hazard fallback above): this bot has already fallen and died within
            // k_deathMemoryRadius of here at least once THIS race. Whatever made "onBridge" look
            // true was wrong last time — trusting it again would just reproduce the exact same
            // death, respawn, repeat loop confirmed in Editor.log (10-20+ identical deaths in a
            // row at the same spot). Falling through to full obstacle/gap/hazard handling below
            // is strictly more cautious, never worse, so this is always the safe direction to err.
            if (onBridge && NearPastDeath(transform.position))
                onBridge = false;

            if (onBridge)
            {
                if (railLeftFound && railRightFound)
                {
                    Vector3 midpoint = (railLeftHit.point + railRightHit.point) * 0.5f;
                    Vector3 toMid    = Vector3.ProjectOnPlane(midpoint - transform.position, Vector3.up);
                    if (toMid.sqrMagnitude > 0.01f)
                        moveDir = Vector3.Slerp(moveDir, toMid.normalized, 0.5f).normalized;
                }
                // else: no rails to measure a midpoint from (hazard-only fallback) — trust the
                // checkpoint-aimed heading, which is already pointed down the crossing.

                if (!m_wasOnBridge)
                {
                    m_wasOnBridge = true;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') on a narrow crossing — " +
                              "centering on the deck, jump/detour/hop suppressed until clear.");
                }
                m_lastLoggedObstacle    = null;
                m_lastLoggedTimedHazard = null;
                m_lastLoggedHazard      = null;
            }
            else
            {
                m_wasOnBridge = false;
            }

            // ── Hazard avoidance (kill zones) — checked FIRST, before physical obstacles ────
            // Steers off a lethal trigger volume before it's even reached, then lets the normal
            // obstacle/gap logic below run against the corrected direction — so a hazard directly
            // ahead is treated exactly like a wall to detour around, not something the bot only
            // reacts to after already standing in it.
            //
            // Skipped entirely while on a bridge (rails found, or the hazard-flank fallback
            // above): hazard flanking both sides there IS the bridge, not a dead end to detour
            // around or give up over — centering above already picked the direction.
            if (!onBridge && HazardAhead(moveDir, out Collider hazard))
            {
                if (hazard != m_lastLoggedHazard)
                {
                    m_lastLoggedHazard = hazard;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') hazard ahead: " +
                              $"'{hazard.name}' — steering around it instead of walking in.");
                }

                // Already committed to a side for this encounter — hold it instead of
                // re-picking every frame (see field comment above for why: an uneven hazard
                // boundary can flip safe/unsafe frame to frame even while barely moving, which
                // read as the bot hopping left-right-left instead of steering through).
                bool decided = false;
                if (Time.time < m_hazardAvoidCommitUntil && m_hazardAvoidSide != 0)
                {
                    Vector3 committedDir = Quaternion.Euler(0f, m_hazardAvoidAngle * m_hazardAvoidSide, 0f) * moveDir;
                    if (!HazardAhead(committedDir, out _)) { moveDir = committedDir; decided = true; }
                    // Committed side became unsafe — fall through and re-decide below.
                }

                if (!decided)
                {
                    // A single fixed ±sideRayAngle probe is fine for a normal-width hazard but too
                    // narrow to find a bridge entrance that sits at a sharper angle off the direct
                    // checkpoint line — confirmed live: a bot stopped dead at a bridge's foot
                    // instead of turning onto it because neither fixed-angle probe cleared the
                    // water flanking the ramp. Scanning progressively wider angles finds whatever
                    // the actual safe opening is — the bridge, or any other narrow crossing —
                    // instead of only ever checking one fixed pair of directions.
                    if (FindSafeHazardDirection(moveDir, out Vector3 safeDir, out int side, out float angle))
                    {
                        m_hazardAvoidSide        = side;
                        m_hazardAvoidAngle       = angle;
                        m_hazardAvoidCommitUntil = Time.time + avoidCommitDuration;
                        moveDir = safeDir;
                    }
                    // A moving/rotating platform ahead bridges this hazard — the DeathRunL1
                    // island-to-island gaps, where the water/void flanks every side of the
                    // landing spot. Head straight for the platform instead of giving up: the
                    // old "stop for stuck-recovery" here is exactly what left bots frozen at
                    // (or nudged off the edge next to) every disc bridge on that level.
                    else if (RideablePlatformAhead(moveDir, hazardCheckDistance + 4f, out _))
                    {
                        m_hazardAvoidSide = 0;
                        // keep moveDir unchanged — walk toward the platform
                    }
                    else
                    {
                        // Hazard on every side that's been checked — don't guess, and don't just fall
                        // through with the original (hazardous) direction either: that was the actual
                        // bug here, since it let the bot keep marching toward the hazard forever,
                        // re-triggering this exact branch every frame with no way out. Stopping dead
                        // (zero direction) makes CheckStuck's "moved < threshold" correctly see this as
                        // stuck and run its normal jump → nudge → back-up → respawn escalation instead.
                        m_hazardAvoidSide = 0;
                        if (Time.time >= m_nextHazardWarnTime)
                        {
                            m_nextHazardWarnTime = Time.time + 1f;
                            Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') hazard on all sides " +
                                              $"at {transform.position} — no safe detour, stopping for stuck-recovery.");
                        }
                        return Vector3.zero;
                    }
                }
            }
            else if (!onBridge)
            {
                m_lastLoggedHazard = null; // clear — next hazard hit is a genuinely new encounter
                m_hazardAvoidSide  = 0;
            }

            // The chosen direction (straight ahead, or a left/right detour) — every branch
            // below sets this instead of returning directly, so the gap probe at the bottom
            // always runs against whichever direction the bot is ACTUALLY about to move.
            // Previously each branch returned early and skipped the gap probe entirely, so a
            // detour around an obstacle never checked for a drop-off right where it was
            // steering to — the bot could walk straight off an edge beside the very obstacle
            // it was avoiding, with no gap detection to catch it.
            Vector3 result = moveDir;

            // While on a bridge, the wall/jump/detour logic below is exactly what was launching
            // bots into the water: entrance posts and railings register as physical "obstacles"
            // at chest height, and the occasional-hop bias would happily hop one — off the edge
            // of a one-lane deck with nowhere but water to land in. Once bridge centering (above)
            // has confirmed we're on a narrow crossing, trust that and skip obstacle handling
            // entirely — every deck on this course is meant to be continuous (confirmed with the
            // user), so there's never a legitimate gap to jump here; the gap probe further down
            // is skipped the same way, so a plank seam can't trigger an unwanted jump either.
            if (!onBridge && Physics.Raycast(origin, moveDir, out RaycastHit fwdHit,
                    obstacleCheckDist, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                if (IsRacer(fwdHit.collider))
                {
                    // Another racer directly ahead — NOT an obstacle. PLAYER TWO's
                    // EntityController already resolves player-vs-player push physically; the
                    // AI must not detour around or hop over a body capsule (two bots meeting
                    // nose to nose near a checkpoint deadlocked, each steering/jumping off the
                    // other forever — the DeathRunL2 pile-up just past the trampoline). Push
                    // straight through; nudge toward this bot's preferred side when they're
                    // right on top of each other so the symmetry breaks and they slide past.
                    m_lastLoggedObstacle    = null;
                    m_lastLoggedTimedHazard = null;
                    if (fwdHit.distance < 1.5f)
                        result = (Quaternion.Euler(0f, m_preferredSide * 25f, 0f) * moveDir).normalized;
                }
                else
                {
                // Edge-triggered: only log when a genuinely NEW obstacle is first detected,
                // not every frame it's still in view (would spam the log every physics tick).
                if (fwdHit.collider != m_lastLoggedObstacle)
                {
                    m_lastLoggedObstacle = fwdHit.collider;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') obstacle ahead: " +
                              $"'{fwdHit.collider.name}' at {fwdHit.point}, dist={fwdHit.distance:F1}m.");
                }

                if (IsWalkableSlope(fwdHit.normal))
                {
                    // A ramp/incline within the character's own slope limit isn't an obstacle at
                    // all — it's just ground. A human walks straight up it; jumping or detouring
                    // around a perfectly climbable slope is exactly the "robotic" behaviour this
                    // is meant to avoid. Let the normal ground-climb physics carry the bot up it
                    // and skip the wall-avoidance logic below entirely.
                    m_lastLoggedObstacle = null;
                }
                else if (IsWalkableLog(fwdHit.collider))
                {
                    // A RotatingLogObstacle — walk straight onto and across it like ordinary
                    // ground (see IsWalkableLog's doc comment for why this must NOT fall through
                    // to the timed-hazard branch below).
                    m_lastLoggedObstacle = null;
                }
                else if (IsRideablePlatform(fwdHit.collider))
                {
                    // A moving/rotating platform bridging a gap — step straight onto it and let
                    // it carry the bot across. Never wait for it (it never leaves) or detour
                    // around it (it IS the route). Clearing the timed-hazard state here matters:
                    // the same collider may have matched IsTimedHazard on a previous frame
                    // before the bot got close enough for the platform check to fire.
                    m_lastLoggedObstacle    = null;
                    m_lastLoggedTimedHazard = null;
                    m_waitingForHazardClear = false;
                }
                else if (IsTimedHazard(fwdHit.collider))
                {
                    // A rotating/swinging/moving hazard (hammer, pendulum, spinning log, moving
                    // platform) currently occupies the path RIGHT NOW — this raycast re-evaluates
                    // fresh every frame, so it naturally clears the instant the hazard swings or
                    // moves away. A real player doesn't try to detour around or jump through a
                    // swinging hammer — they watch it, wait a beat for the gap, then go. Holding
                    // here (rather than attempting a jump/detour) reproduces exactly that timing,
                    // with no need to predict the hazard's exact rotation/period analytically.
                    if (!m_waitingForHazardClear) m_timedHazardWaitStart = Time.time;
                    m_waitingForHazardClear = true;
                    if (fwdHit.collider != m_lastLoggedTimedHazard)
                    {
                        m_lastLoggedTimedHazard = fwdHit.collider;
                        Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') timing past moving hazard: " +
                                  $"'{fwdHit.collider.name}' — waiting for it to clear.");
                    }

                    // Safety valve: this hazard's swept volume never leaves the ray — stop
                    // waiting and commit (jump + push through), accepting the hit, rather than
                    // standing here until stuck-recovery walks the bot off the platform.
                    if (Time.time - m_timedHazardWaitStart > k_maxTimedHazardWait)
                    {
                        Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') gave up waiting on " +
                                          $"'{fwdHit.collider.name}' after {k_maxTimedHazardWait:F0}s — committing through it.");
                        m_waitingForHazardClear = false;
                        TryJump();
                        return moveDir;
                    }

                    return Vector3.zero;
                }
                else
                {
                    m_lastLoggedTimedHazard = null;

                    // The hazard just cleared this frame — hold a beat longer before actually
                    // moving, same reasoning as the wait itself: a real player confirms the gap
                    // is real before stepping into it, rather than launching the instant the
                    // swinging arm's collider stops overlapping the raycast.
                    if (m_waitingForHazardClear)
                    {
                        m_waitingForHazardClear = false;
                        m_hazardResumeTime = Time.time + Random.Range(m_input.reactionTimeMin, m_input.reactionTimeMax);
                    }
                    if (Time.time < m_hazardResumeTime)
                    {
                        return Vector3.zero;
                    }

                    Vector3 left  = Quaternion.Euler(0f, -sideRayAngle, 0f) * moveDir;
                    Vector3 right = Quaternion.Euler(0f,  sideRayAngle, 0f) * moveDir;
                    bool leftHit  = Physics.Raycast(origin, left,  obstacleCheckDist, obstacleLayer, QueryTriggerInteraction.Ignore);
                    bool rightHit = Physics.Raycast(origin, right, obstacleCheckDist, obstacleLayer, QueryTriggerInteraction.Ignore);
                    bool canLeft  = !leftHit;
                    bool canRight = !rightHit;

                    // Already committed to a choice for this encounter — hold it rather
                    // than re-rolling the hop-or-detour decision every single frame
                    // (which would make even a low hop bias trigger almost every time
                    // over the ~0.3-0.5s it takes to close the distance to an obstacle).
                    bool decided = false;
                    if (Time.time < m_avoidCommitUntil)
                    {
                        if (m_avoidSide == -1 && canLeft)  { result = left;    decided = true; }
                        else if (m_avoidSide == 1 && canRight) { result = right; decided = true; }
                        else if (m_avoidSide == 0)          { result = moveDir; decided = true; } // committed to hopping — already jumped
                        // Committed side became blocked — fall through and re-decide below.
                    }

                    if (!decided)
                    {
                        float hitHeight = fwdHit.point.y - transform.position.y;
                        bool  jumpable  = hitHeight < wallJumpHeight;

                        // A confirmed ObstacleKnockback hazard (Triangle, Dumbbell, SpikeRoller,
                        // etc.) costs damage/a shove on contact — unlike inert terrain, where
                        // brushing past costs nothing. Prefer a full, clean jump over it whenever
                        // one's available, rather than the occasional-hop bias tuned for harmless
                        // walls (which favours hugging a close detour past most of the time).
                        bool  isStaticHazard   = FindObstacleKnockback(fwdHit.collider) != null;
                        float effectiveHopBias = isStaticHazard ? 1f : m_obstacleHopBias;

                        if (canLeft || canRight)
                        {
                            // Occasionally just hop a low obstacle instead of detouring —
                            // feels like a deliberate shortcut rather than the only option.
                            // Decided once per encounter (see commit guard above), not per-frame.
                            if (jumpable && Random.value < effectiveHopBias)
                            {
                                TryJump();
                                m_obstaclesJumped++;
                                m_avoidSide        = 0;
                                m_avoidCommitUntil = Time.time + avoidCommitDuration;
                                result = moveDir;
                            }
                            else
                            {
                                // A genuine tie (both sides clear) breaks toward this bot's own
                                // preferred side rather than always defaulting left — otherwise
                                // every bot facing the same symmetric obstacle swerves identically.
                                bool goLeft = canLeft && (!canRight || m_preferredSide < 0);
                                m_avoidSide         = goLeft ? -1 : 1;
                                m_avoidCommitUntil  = Time.time + avoidCommitDuration;
                                m_obstaclesDetoured++;
                                result = goLeft ? left : right;
                            }
                        }
                        // Boxed in on both sides — jump if the obstacle is short enough to clear.
                        else if (jumpable)
                        {
                            TryJump();
                            m_obstaclesJumped++;
                        }
                        else
                        {
                            // Not jumpable and both sides blocked — this is a genuine dead end, not
                            // just a "not moved yet this tick" moment. Flag it clearly so a log scan
                            // can tell a wall-trap apart from ordinary stuck-recovery noise.
                            Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') boxed in by " +
                                              $"'{fwdHit.collider.name}' (height {hitHeight:F1}m, not jumpable) " +
                                              $"with no clear side — relying on stuck-recovery.");
                        }
                    }
                }
                } // end: not a racer
            }
            else
            {
                m_lastLoggedObstacle = null; // path clear — next hit is a genuinely new encounter
            }

            // Gap probe: if no ground ahead in the direction we're actually about to move while
            // grounded, jump to clear the gap. Runs against `result`, not the original `moveDir`
            // — see the comment above for why that distinction is the actual fix.
            //
            // Samples 3 parallel points (center + a small offset either side), not just one —
            // a plank bridge has small real gaps between boards, and a single straight-down ray
            // landing in one of those seams reads as "no ground ahead" even though the deck is
            // solid a few centimetres to either side. That false read was launching bots off the
            // side of a bridge with an unwanted jump instead of just walking across it normally.
            // Only treat it as a genuine gap — and actually jump — when EVERY sample agrees.
            //
            // Gated on `!onBridge`: tried removing this gate once to jump bots over what looked
            // like a real gap under a bridge, but the "gap" there is actually a hole in the deck
            // collider, not a jumpable design feature — every deck on this course is meant to be
            // continuous (confirmed with the user). A normal jump doesn't have the horizontal
            // reach to clear it anyway (bots jumped and still landed in the water below), so
            // trying to jump it just adds an awkward hop before the same fall. Leave bridges to
            // walk straight across; the actual fix for the hole is a collider/geometry repair on
            // the affected deck pieces, not AI jump logic.
            Vector3 gapCenter = transform.position + result * gapProbeDistance + Vector3.up * 0.3f;
            Vector3 gapSide   = Vector3.Cross(Vector3.up, result).normalized * 0.35f;

            bool groundCenter = Physics.Raycast(gapCenter,           Vector3.down, gapProbeDepth, obstacleLayer, QueryTriggerInteraction.Ignore);
            bool groundLeft   = Physics.Raycast(gapCenter - gapSide, Vector3.down, gapProbeDepth, obstacleLayer, QueryTriggerInteraction.Ignore);
            bool groundRight  = Physics.Raycast(gapCenter + gapSide, Vector3.down, gapProbeDepth, obstacleLayer, QueryTriggerInteraction.Ignore);

            // Uses the recent-grounded coyote window, not a flat m_player.isGrounded check:
            // confirmed via Editor.log that bots were falling into the level's kill-plane near
            // 'land_006' (repeated death loop, same spot, every bot) with no jump attempt logged
            // beforehand — that terrain is bumpy enough that isGrounded flickers false on
            // approach even while still effectively on solid ground, silently suppressing the
            // one safety jump that would've cleared the real gap just past it. A flat "ignore
            // isGrounded entirely" fix over-corrected: it let this re-fire on cooldown for the
            // whole time a bot was airborne, including a completely normal jump already in
            // progress, which looked like continuous bunny-hopping. The coyote window keeps the
            // bumpy-terrain save (still counts as "grounded" a few frames after the last real
            // contact) without re-triggering deep into an already-committed jump/fall.
            // Never jump the "gap" when riding a moving platform, or when one is right ahead
            // to step onto — the disc bridges span open void, so all three probes read "no
            // ground" and the bot would launch itself off the platform into the water.
            bool platformCoversGap = OnRideablePlatform()
                                     || RideablePlatformAhead(result, gapProbeDistance + 1.5f, out _);

            bool recentlyGrounded = Time.time - m_lastGroundedTime <= k_recentGroundedWindow;
            if (!onBridge && !platformCoversGap
                && !groundCenter && !groundLeft && !groundRight && recentlyGrounded)
                TryJump();

            return result;
        }

        private void TryJump()
        {
            // Never jump while crossing a hinted bridge — a hop off a 0.5 m plank has
            // nowhere to land. Same on the stepping discs: the disc crossing block owns
            // every hop directly (one per waypoint, precisely aimed) — any OTHER jump the
            // brain queues there just mistimes it and flings the bot off. And on a launch
            // pad (trampoline / mushroom): the pad supplies a 30-force vertical launch, so a
            // jump stacked on top of it just rockets the bot ~10 m straight up and back down
            // onto the same pad — the bounce loop. Single chokepoint for every non-crossing
            // jump (gap probe, obstacle hop, timed-hazard commit).
            if (m_bridgeMode || m_stoneMode || m_bounceMode) return;
            if (Time.time < m_nextJumpTime) return;
            m_input.jumpQueued = true;
            m_nextJumpTime     = Time.time + k_jumpCooldown;
        }

        /// <summary>
        /// Fully halts the bot — clears all AI input, kills residual physics momentum and
        /// drops it into Idle. Used both when the bot crosses the finish line and when the
        /// race results screen appears for everyone while this bot is still on the track.
        ///
        /// Zeroing desiredMoveDirection alone isn't enough to stop on the spot: it only
        /// gives AIPlayerInputManager a new TARGET to Lerp toward over turnSpeed, so the bot
        /// would keep coasting/sliding for up to a second. StopImmediately() zeroes the
        /// already-smoothed direction too, and clearing lateralVelocity kills any residual
        /// physics momentum in the same frame.
        /// </summary>
        private void StopBot()
        {
            m_currentSpeed = 0f;

            if (m_input != null)
            {
                m_input.StopImmediately();
                m_input.desiredMoveDirection = Vector3.zero;
                m_input.runHeld    = false;
                m_input.jumpQueued = false;
                m_input.enabled    = false;
            }

            if (m_player != null)
            {
                m_player.lateralVelocity = Vector3.zero;
                m_player.states.Change<IdlePlayerState>();
            }
        }

        // ── Stuck detection ───────────────────────────────────────────────────

        private void CheckStuck()
        {
            if (Time.time < m_nextStuckCheck) return;
            m_nextStuckCheck = Time.time + stuckCheckInterval;

            // Riding a moving/rotating platform: the bot is being carried, not stuck — and a
            // rotating disc can even bring it back to a similar XZ between checks. Never
            // escalate (jump / nudge / back-up / respawn) here; that's what threw bots off
            // the disc bridges in DeathRunL1. Same while crossing a hinted rope bridge —
            // a stuck-nudge there just walks the bot off the planks. EXCEPTION: a bridge
            // crossing that's been forward-stalled for >5 s (wedged on a rope and the
            // in-bridge wobble didn't free it) — let the normal escalation run so it can
            // eventually force-respawn rather than stand there forever.
            bool bridgeHardStuck = m_bridgeStalledSince != 0f && Time.time - m_bridgeStalledSince > 5f;
            bool stoneHardStuck  = m_stoneStalledSince  != 0f && Time.time - m_stoneStalledSince  > 3f;
            // Airborne on a launch pad's bounce is not "stuck" — the bot is mid-arc and its
            // XZ barely changes at the top. Grounded in bounce mode still escalates normally,
            // so a bot that somehow stays bounce-locked on the pad eventually force-respawns
            // back to the checkpoint instead of bouncing there forever.
            if (OnRideablePlatform() || (m_bridgeMode && !bridgeHardStuck) || (m_stoneMode && !stoneHardStuck)
                || (m_bounceMode && !m_player.isGrounded))
            {
                m_stuckLevel   = 0;
                m_lastStuckPos = transform.position;
                return;
            }

            // Horizontal-only: a bot embedded in an unstable terrain seam can bounce several
            // units vertically (physics repeatedly resolving it in/out of the collider) while
            // making zero actual progress along the track. Full 3D Vector3.Distance reads that
            // bounce alone as "moved", which resets m_stuckLevel to 0 every check and masks a
            // genuinely stuck bot indefinitely (confirmed via Editor.log: a bot floated near the
            // same XZ spot for 60+ seconds, y oscillating 0.3-4, never escalating past level 1).
            Vector3 delta = transform.position - m_lastStuckPos;
            float moved = new Vector2(delta.x, delta.z).magnitude;
            m_lastStuckPos = transform.position;

            if (moved >= stuckMoveThreshold) { m_stuckLevel = 0; return; }

            m_stuckLevel++;
            m_stuckEscalations++;
            Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') stuck level {m_stuckLevel} " +
                      $"(moved {moved:F2}m) at {transform.position}, checkpoint target {m_nextCpIdx}.");

            switch (m_stuckLevel)
            {
                case 1:
                    m_input.jumpQueued = true;
                    break;
                case 2:
                    m_input.jumpQueued = true;

                    // Prefer a hazard-free side where possible instead of a blind coin-flip —
                    // on a narrow bridge/ledge a random nudge can just as easily aim straight
                    // into the water as away from whatever's actually blocking progress.
                    Vector3 curDir = m_input.desiredMoveDirection.sqrMagnitude > 0.01f
                        ? m_input.desiredMoveDirection.normalized : transform.forward;
                    bool nudgeLeftOk  = !HazardAhead(Quaternion.Euler(0f, -sideRayAngle, 0f) * curDir, out _);
                    bool nudgeRightOk = !HazardAhead(Quaternion.Euler(0f,  sideRayAngle, 0f) * curDir, out _);

                    float nudge;
                    if (nudgeLeftOk && nudgeRightOk) nudge = Random.value > 0.5f ? sideRayAngle : -sideRayAngle;
                    else if (nudgeLeftOk)            nudge = -sideRayAngle;
                    else if (nudgeRightOk)           nudge = sideRayAngle;
                    else                              nudge = Random.value > 0.5f ? sideRayAngle : -sideRayAngle; // both risky — no safer option to prefer

                    m_input.desiredMoveDirection = Quaternion.Euler(0f, nudge, 0f) * m_input.desiredMoveDirection;
                    break;
                case 3:
                    m_backingUp     = true;
                    m_backUpEndTime = Time.time + backUpDuration;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') backing up to break free.");
                    break;
                default:
                    m_stuckLevel = 0;
                    m_forcedRespawns++;
                    Debug.LogWarning($"[RaceBotBrain] Bot {BotId} ('{name}') gave up freeing itself " +
                                      $"at {transform.position} (forced respawn #{m_forcedRespawns}) — " +
                                      "this is a genuine interruption, not routine navigation.");
                    m_respawner?.RespawnNow();
                    break;
            }
        }

        // ── Power-ups ────────────────────────────────────────────────────────
        // Pickup itself needs no bot-specific code — NetworkedPowerUpBox.OnTriggerEnter
        // already keys off the colliding NetworkObject's own PlayerPowerUpInventory, not
        // OwnerClientId, so it works for a bot exactly like a human the moment its collider
        // touches an active box. This section only adds the two things bots were actually
        // missing: steering awareness so they walk toward boxes in the first place, and a
        // decision for when a held power-up is actually worth using.

        private bool HasFreePowerUpSlot()
        {
            if (m_inventory == null) return false;
            for (int i = 0; i < 3; i++)
                if (m_inventory.GetSlot(i) == -1) return true;
            return false;
        }

        private NetworkedPowerUpBox FindNearbyPowerUpBox()
        {
            NetworkedPowerUpBox nearest = null;
            float nearestDist = powerUpDetectionRadius;

            foreach (var box in NetworkedPowerUpBox.All)
            {
                if (box == null || !box.IsActive) continue;
                float dist = Vector3.Distance(transform.position, box.transform.position);
                if (dist < nearestDist)
                {
                    nearest     = box;
                    nearestDist = dist;
                }
            }

            return nearest;
        }

        // Reuses the same reaction-time system already used for jumps/obstacles
        // (QueueDelayedAction) so using a power-up isn't an instant-robotic reflex the
        // moment it becomes "worth it" — there's a small human-like pause first.
        private void ConsiderUsingPowerUp()
        {
            if (Time.time < m_nextPowerUpDecisionTime) return;
            m_nextPowerUpDecisionTime = Time.time + powerUpDecisionInterval;

            if (m_inventory == null) return;

            for (int i = 0; i < 3; i++)
            {
                int slot = m_inventory.GetSlot(i);
                if (slot == -1) continue;
                if (!IsWorthUsingNow((PowerUpType)slot)) continue;

                int          slotIndex = i;
                int          slotType  = slot;
                var          type      = (PowerUpType)slot;
                Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') decided to use {type} from slot {slotIndex} " +
                          "— queuing with a human-like reaction delay.");

                m_input.QueueDelayedAction(() =>
                {
                    // The held slots can change during the reaction delay (a new pickup
                    // shifted things, or it was already used) — re-check before firing.
                    if (m_inventory == null || m_inventory.GetSlot(slotIndex) != slotType) return;
                    Debug.Log($"[RaceBotBrain] Bot {BotId} ('{name}') using {type} from slot {slotIndex}.");
                    m_inventory.UseSlotServerRpc(slotIndex);
                });
                return; // one decision per check — don't queue every held slot at once
            }
        }

        // Roughly matches human judgement: self-buffs are always worth using since there's
        // no downside, but offensive/trap power-ups only fire when there's actually a rival
        // close enough to matter — a human doesn't burn a Rocket into empty air just because
        // they're holding one.
        private bool IsWorthUsingNow(PowerUpType type)
        {
            var rm = RaceManager.Instance;
            if (rm == null) return true;

            switch (type)
            {
                case PowerUpType.SpeedBoost:
                case PowerUpType.Shield:
                case PowerUpType.Invisible:
                case PowerUpType.SuperCharge:
                    return true; // self-buffs — no target needed

                case PowerUpType.Rocket:
                {
                    // Matches DispatchRocket's own "caster in 1st place → no effect" check.
                    ulong first = rm.GetPlayerInFirst();
                    return first != ulong.MaxValue && first != BotId;
                }

                case PowerUpType.StunBolt:
                case PowerUpType.Swap:
                {
                    ulong ahead = rm.GetPlayerAhead(BotId);
                    return ahead != ulong.MaxValue && IsCloseEnoughToTarget(ahead);
                }

                case PowerUpType.Freeze:
                    // AoE — hits everyone behind at once regardless of distance, so any
                    // target at all makes it worth using (matches DispatchFreeze itself).
                    return rm.GetPlayersBehind(BotId).Count > 0;

                case PowerUpType.Banana:
                case PowerUpType.DecoyBox:
                {
                    ulong behind = rm.GetPlayerBehind(BotId);
                    return behind != ulong.MaxValue && IsCloseEnoughToTarget(behind);
                }

                default:
                    return false;
            }
        }

        private bool IsCloseEnoughToTarget(ulong raceId)
        {
            var t = RaceManager.Instance?.GetPlayerTransform(raceId);
            return t != null && Vector3.Distance(transform.position, t.position) <= powerUpTargetRange;
        }

        // ── Jump calibration ──────────────────────────────────────────────────

        // PlayerStats.maxJumpHeight is NOT a world-space height despite the name — Player.Jump()
        // uses it directly as an initial upward velocity (verticalVelocity = Vector3.up * height),
        // and PLAYER TWO characters fall under their own PlayerStats.gravity, not Physics.gravity.
        // The real peak height reached is the standard v²/2g projectile formula. Confirmed via the
        // character Stats assets that this varies a lot per character (e.g. Bolt's real max jump
        // clears roughly 2.4 units; Spike's roughly 5.7) — the flat wallJumpHeight inspector
        // default (1.5) was capping every bot's idea of "jumpable" at a low, character-blind
        // number, well under what most of them can actually clear.
        private void CalibrateJumpHeightFromStats()
        {
            var stats = m_player != null ? m_player.stats?.current : null;
            if (stats == null || stats.gravity <= 0f) return;

            float peakHeight = (stats.maxJumpHeight * stats.maxJumpHeight) / (2f * stats.gravity);

            // A safety margin below the true max: reaction-time delay and mid-air steering mean a
            // bot won't always hit the exact textbook peak, and clearing an obstacle with room to
            // spare reads as more natural than shaving it by centimetres.
            wallJumpHeight = peakHeight * 0.8f;
        }

        // ── Difficulty ────────────────────────────────────────────────────────

        /// <summary>
        /// Overrides the Inspector-configured difficulty. Must be called before this bot's
        /// NetworkObject is spawned (OnNetworkSpawn calls ApplyDifficulty() using whatever
        /// value is set at that point) — see BotSpawner.SpawnBots.
        /// </summary>
        public void SetDifficulty(BotDifficulty value) => difficulty = value;

        private void ApplyDifficulty()
        {
            // AIPlayerInputManager params
            // turnSpeed = how fast the CHARACTER'S facing direction Lerps to desired dir.
            // directionJitter = ±degrees of Perlin noise added to target direction.
            // reactionTime = delay before queued actions (jump, etc.) fire.

            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    m_input.reactionTimeMin  = 0.20f;
                    m_input.reactionTimeMax  = 0.45f;
                    m_input.turnSpeed        = 2.0f;   // slow character facing
                    m_input.directionJitter  = 6f;     // extra wobble on top of weave

                    m_speedAccelRate         = 0.45f;  // 0→1 in ~2.2s (sluggish start)
                    m_speedDecelRate         = 1.2f;   // 1→0 in ~0.8s
                    m_weaveAmplitude         = 18f;    // very visible S-curves
                    m_weaveFrequency         = 0.28f;  // slow, lazy oscillation (≈3.6s/cycle)
                    m_lookAheadBlendDist     = arrivalRadius * 1.5f;
                    m_softBrakeTurnAngle     = 20f;    // brakes even for gentle bends
                    m_hardBrakeTurnAngle     = 40f;
                    m_walkBreakChance        = 0.35f;  // 35%/s → short hesitation every ~3s
                    m_walkBreakDuration      = 0.45f;
                    m_obstacleHopBias        = 0.15f;  // cautious — mostly walks around obstacles

                    // Notices hazards/obstacles late — short lookahead means it sometimes only
                    // reacts once already close, occasionally clipping a hazard instead of
                    // clearing it with room to spare.
                    obstacleCheckDist        = 1.8f;
                    hazardCheckDistance      = 2.0f;
                    // Slow to recognize it's stuck and fumbles longer once it does — reads as
                    // clumsy rather than a snap correction.
                    stuckCheckInterval       = 1.0f;
                    backUpDuration           = 0.75f;
                    // Slow, cautious power-up use — only bothers when a target is already close.
                    powerUpDecisionInterval  = 2.2f;
                    powerUpTargetRange       = 14f;
                    break;

                case BotDifficulty.Medium:
                    m_input.reactionTimeMin  = 0.08f;
                    m_input.reactionTimeMax  = 0.18f;
                    m_input.turnSpeed        = 4.0f;
                    m_input.directionJitter  = 3f;

                    m_speedAccelRate         = 0.9f;   // 0→1 in ~1.1s
                    m_speedDecelRate         = 2.2f;   // 1→0 in ~0.45s
                    m_weaveAmplitude         = 10f;    // moderate S-curves
                    m_weaveFrequency         = 0.45f;  // ≈2.2s/cycle
                    m_lookAheadBlendDist     = arrivalRadius * 2.5f;
                    m_softBrakeTurnAngle     = 30f;
                    m_hardBrakeTurnAngle     = 60f;
                    m_walkBreakChance        = 0.07f;  // rare brief hesitations
                    m_walkBreakDuration      = 0.20f;
                    m_obstacleHopBias        = 0.30f;  // mix of hopping and walking around

                    obstacleCheckDist        = 2.5f;   // baseline awareness distance
                    hazardCheckDistance      = 3.0f;
                    stuckCheckInterval       = 0.6f;   // baseline recovery pace
                    backUpDuration           = 0.55f;
                    powerUpDecisionInterval  = 1.5f;   // baseline power-up reaction
                    powerUpTargetRange       = 20f;
                    break;

                case BotDifficulty.Hard:
                    m_input.reactionTimeMin  = 0.02f;
                    m_input.reactionTimeMax  = 0.06f;
                    m_input.turnSpeed        = 9f;
                    m_input.directionJitter  = 1.0f;

                    m_speedAccelRate         = 2.5f;   // 0→1 in 0.4s (snappy)
                    m_speedDecelRate         = 5f;     // 1→0 in 0.2s
                    m_weaveAmplitude         = 3f;     // barely noticeable drift
                    m_weaveFrequency         = 0.65f;  // ≈1.5s/cycle
                    m_lookAheadBlendDist     = arrivalRadius * 4f;
                    m_softBrakeTurnAngle     = 50f;    // barely brakes — expert lines
                    m_hardBrakeTurnAngle     = 80f;
                    m_walkBreakChance        = 0f;
                    m_walkBreakDuration      = 0f;
                    m_obstacleHopBias        = 0.5f;   // expert line — hops small obstacles for speed as often as it detours

                    // Spots hazards/obstacles early and clears them with room to spare, like a
                    // player who's already read the track ahead.
                    obstacleCheckDist        = 3.5f;
                    hazardCheckDistance      = 4.5f;
                    // Notices being stuck almost immediately and corrects fast instead of
                    // fumbling through a long back-up.
                    stuckCheckInterval       = 0.35f;
                    backUpDuration           = 0.35f;
                    // Fast, proactive power-up use — willing to fire on a target well before
                    // it's close.
                    powerUpDecisionInterval  = 0.8f;
                    powerUpTargetRange       = 30f;
                    break;
            }
        }

        // Called once at spawn, after ApplyDifficulty() sets the base per-tier values. Nudges
        // every navigation threshold by a random +/- perBotVariance fraction so bots of the
        // same difficulty don't all notice and react to the same obstacle at the exact same
        // distance and instant — without this, an entire pack hits the same wall and detours
        // in lockstep, which is what reads as robotic rather than a group of individual racers.
        private void ApplyPerBotVariance()
        {
            obstacleCheckDist   = Jitter(obstacleCheckDist);
            sideRayAngle        = Jitter(sideRayAngle);
            wallJumpHeight      = Jitter(wallJumpHeight);
            gapProbeDistance    = Jitter(gapProbeDistance);
            gapProbeDepth       = Jitter(gapProbeDepth);
            avoidCommitDuration = Jitter(avoidCommitDuration);
            hazardCheckDistance = Jitter(hazardCheckDistance);

            m_weaveAmplitude     = Jitter(m_weaveAmplitude);
            m_weaveFrequency     = Jitter(m_weaveFrequency);
            m_lookAheadBlendDist = Jitter(m_lookAheadBlendDist);
            m_hardBrakeTurnAngle = Jitter(m_hardBrakeTurnAngle);
            m_softBrakeTurnAngle = Jitter(m_softBrakeTurnAngle);
            m_obstacleHopBias    = Mathf.Clamp01(Jitter(m_obstacleHopBias));
            m_speedAccelRate     = Jitter(m_speedAccelRate);
            m_speedDecelRate     = Jitter(m_speedDecelRate);
        }

        private float Jitter(float baseValue) =>
            baseValue * Random.Range(1f - perBotVariance, 1f + perBotVariance);

        // ── Hazard detection (kill zones) ───────────────────────────────────────
        // Obstacle/gap raycasts elsewhere all use QueryTriggerInteraction.Ignore, so they never
        // see a KillZone — it's a trigger collider by design (PLAYERTWO.KillZone.Start() forces
        // isTrigger = true). That means a bot can walk down onto perfectly solid, walkable-looking
        // ground that's ALSO sitting inside a lethal trigger volume (water/lava/a pit at the foot
        // of a cliff) with nothing to stop it — confirmed by bots dying repeatedly at the exact
        // same grounded position near the same obstacle instead of a genuinely varying spot.

        private static readonly Collider[] s_hazardBuffer = new Collider[8];

        // checkBelow defaults to true for every ordinary use (steering away from a hazard
        // directly ahead, the fan-scan detour search, stuck-recovery nudge safety) — there,
        // finding a deeper hazard just makes the bot more cautious, which is always safe.
        // It's explicitly turned OFF for exactly one caller: the "no physical rails" bridge
        // fallback below. See that call site for why — treating every deep hazard-both-sides
        // spot as a safe bridge to walk straight across turned out to be actively dangerous.
        private bool IsHazardAt(Vector3 point, out Collider hitZone, bool checkBelow = true)
        {
            int count = Physics.OverlapSphereNonAlloc(point, hazardCheckRadius, s_hazardBuffer,
                obstacleLayer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (s_hazardBuffer[i] == null) continue;
                var kz = s_hazardBuffer[i].GetComponentInParent<KillZone>();
                if (kz != null) { hitZone = s_hazardBuffer[i]; return true; }
            }

            if (!checkBelow) { hitZone = null; return false; }

            // Same-height overlap sphere only ever sees a hazard level with the probe point —
            // confirmed via a live bridge (bridge_001, deck well above the water it crosses)
            // that this silently defeated bridge detection entirely: the water sat far enough
            // below deck height that no flank probe ever touched it, onBridge never went true,
            // and the AI treated the deck's railings/entrance posts as ordinary obstacles to hop
            // over instead of centering and walking straight across — a hop off a one-lane deck
            // has nowhere to land but the water it was trying to avoid. A downward raycast finds
            // that same hazard regardless of how far below the crossing it sits. Solid (non-
            // trigger) ground directly under the point is hit first and blocks this, so it can't
            // false-positive on ordinary flat ground with an unrelated hazard buried underneath.
            if (Physics.Raycast(point, Vector3.down, out RaycastHit belowHit,
                    hazardBelowProbeDepth, obstacleLayer, QueryTriggerInteraction.Collide))
            {
                var kzBelow = belowHit.collider.GetComponentInParent<KillZone>();
                if (kzBelow != null) { hitZone = belowHit.collider; return true; }
            }

            hitZone = null;
            return false;
        }

        // Samples the midpoint and endpoint along `dir` rather than a single far probe, so a
        // hazard volume that starts closer than hazardCheckDistance still gets caught.
        private bool HazardAhead(Vector3 dir, out Collider hitZone, bool checkBelow = true)
        {
            Vector3 half = transform.position + dir * (hazardCheckDistance * 0.5f);
            if (IsHazardAt(half, out hitZone, checkBelow)) return true;
            Vector3 full = transform.position + dir * hazardCheckDistance;
            return IsHazardAt(full, out hitZone, checkBelow);
        }

        // Widening scan angles tried in order — closest to the original heading first, so the
        // bot always takes the gentlest turn that's actually safe rather than overshooting past
        // a narrower opening (a bridge entrance) to a wider one further round.
        private static readonly float[] k_hazardScanAngles = { 20f, 35f, 50f, 65f, 80f };

        // Finds the narrowest turn off `baseDir` that clears the hazard directly ahead. A single
        // fixed-angle probe (the old behaviour) can miss a real opening — a bridge onto a narrow
        // deck — if that opening happens to sit at a sharper angle than the one fixed probe
        // checked, which reads as the bot stopping dead at the bridge's foot instead of turning
        // onto it. Tries this bot's preferred side first at each angle before the other side, so
        // a genuine tie still breaks the same way FindSafeHazardDirection's caller expects.
        private bool FindSafeHazardDirection(Vector3 baseDir, out Vector3 safeDir, out int side, out float angle)
        {
            foreach (float a in k_hazardScanAngles)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    int trySide = (pass == 0) ? m_preferredSide : -m_preferredSide;
                    Vector3 dir = Quaternion.Euler(0f, a * trySide, 0f) * baseDir;
                    if (!HazardAhead(dir, out _))
                    {
                        safeDir = dir;
                        side    = trySide;
                        angle   = a;
                        return true;
                    }
                }
            }

            safeDir = Vector3.zero;
            side    = 0;
            angle   = 0f;
            return false;
        }

        // ── Obstacle type classification ────────────────────────────────────────
        // Not every raycast hit ahead means "stop and go around" — a real player reacts
        // differently depending on what's actually there. These checks let ObstacleAvoidance
        // tell a climbable ramp, a walk-across log, a timed hazard, and a genuine wall apart
        // before deciding what to do about it.

        // Compares the hit surface's angle against the same slopeLimit the character's own
        // EntityController uses to decide walkable-ground-vs-wall, so the AI agrees with the
        // physics on what counts as "just a ramp" instead of guessing at its own threshold.
        private bool IsWalkableSlope(Vector3 surfaceNormal)
        {
            float limit = m_player != null && m_player.controller != null
                ? m_player.controller.slopeLimit
                : slopeLimitFallback;
            return Vector3.Angle(surfaceNormal, Vector3.up) <= limit;
        }

        // RotatingLogObstacle is a walk-across-while-balancing log — genuinely walkable ground,
        // not a hazard to wait out. It continuously rotates, so treating it as a timed hazard (as
        // this project used to, since every instance also carries a RotationScript the old
        // IsTimedHazard already matched) meant the forward raycast never cleared and the bot
        // stalled there forever. Its own lateral push, plus the existing gap-probe and
        // hazard-below checks, already handle staying on / falling off correctly — the AI just
        // needs to not treat the log itself as something to stop for.
        private static bool IsWalkableLog(Collider col)
        {
            return col.GetComponent<RotatingLogObstacle>() != null
                || col.GetComponentInParent<RotatingLogObstacle>() != null
                || col.GetComponentInChildren<RotatingLogObstacle>() != null;
        }

        // A DynamicPlatform / MovingPlatform is a surface the bot RIDES across a gap, not a
        // hazard to wait out or detour around. DeathRunL1 links its floating islands with
        // rotating disc platforms (DynamicPlatform + RotationScript, 13 of them) — the old
        // code matched their RotationScript in IsTimedHazard and had bots stand at every gap
        // waiting for the disc to "clear" (it never does — it IS the bridge), then stuck-
        // recovery would nudge them straight off the island. A real player just steps on and
        // lets it carry them; PLAYER TWO's entity-attachment system does exactly that for a
        // grounded bot with no bot-specific code, so all the AI has to do is walk onto it.
        // Checks Platform (the base class DynamicPlatform and MovingPlatform both extend).
        private static bool IsRideablePlatform(Collider col)
        {
            if (col == null) return false;
            return col.GetComponentInParent<Platform>()  != null
                || col.GetComponentInChildren<Platform>() != null;
        }

        // Short forward spherecast for a rideable platform surface roughly at foot height —
        // used both to steer toward a disc bridging a gap and to suppress the gap-jump when
        // one is there to land on.
        private bool RideablePlatformAhead(Vector3 dir, float dist, out Collider platform)
        {
            platform = null;
            Vector3 o = transform.position + Vector3.up * 0.3f;
            if (Physics.SphereCast(o, 0.5f, dir.normalized, out RaycastHit hit, dist,
                    obstacleLayer, QueryTriggerInteraction.Ignore)
                && IsRideablePlatform(hit.collider))
            {
                platform = hit.collider;
                return true;
            }
            return false;
        }

        private bool OnRideablePlatform() =>
            m_player != null && m_player.isGrounded
            && IsRideablePlatform(m_player.groundHit.collider);

        // Another racer (human player or bot) — its solid "Body" capsule (PlayerBodyCollider)
        // is on the default raycast layers, so the AI's forward obstacle ray hits it. It is
        // NOT terrain: PLAYER TWO's EntityController already resolves player-vs-player push
        // physically, so the AI must never detour around, hop, or wait out another racer.
        // Two bots converging on the same checkpoint used to jam nose to nose forever.
        private bool IsRacer(Collider col)
        {
            if (col == null) return false;
            var p = col.GetComponentInParent<Player>();
            return p != null && p != m_player;
        }

        // A JumpingPlatform (trampoline) or MushroomBounce — a pad that launches whatever
        // stands on it straight up. The bot must walk onto it and ride the bounce across a
        // gap, not detour around it or jump on it. Checks the collider, an ancestor, or a
        // descendant (the launch component and the MeshCollider proxy can be on different
        // GameObjects in the prefab).
        private static bool IsBouncePlatform(Collider col)
        {
            if (col == null) return false;
            return col.GetComponentInParent<JumpingPlatform>()  != null
                || col.GetComponentInChildren<JumpingPlatform>() != null
                || col.GetComponentInParent<MushroomBounce>()    != null
                || col.GetComponentInChildren<MushroomBounce>()  != null;
        }

        // Arms launch-pad mode: a bounce pad underfoot, or one on the line to the current
        // checkpoint within a short spherecast, or one in a close overlap. The sticky window
        // (k_bounceModeHold) then carries the mode through the whole launch arc.
        private bool ResolveBouncePlatform()
        {
            if (m_player != null && m_player.isGrounded && IsBouncePlatform(m_player.groundHit.collider))
                return true;

            if (m_checkpoints == null || m_nextCpIdx >= m_checkpoints.Length) return false;
            var cp = m_checkpoints[m_nextCpIdx];
            if (cp == null) return false;

            Vector3 toCp = Vector3.ProjectOnPlane(cp.transform.position - transform.position, Vector3.up);
            if (toCp.sqrMagnitude < 0.01f) return false;
            Vector3 o = transform.position + Vector3.up * 0.3f;
            if (Physics.SphereCast(o, 0.6f, toCp.normalized, out RaycastHit hit, 4.5f,
                    obstacleLayer, QueryTriggerInteraction.Ignore)
                && IsBouncePlatform(hit.collider))
                return true;

            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.3f, 1.6f,
                s_bounceBuffer, obstacleLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (IsBouncePlatform(s_bounceBuffer[i])) return true;

            return false;
        }

        // ── Designer obstacle hints ──────────────────────────────────────────

        // A BotObstacleHint on the collider, an ancestor, or a descendant — the
        // designer's explicit override for how to treat this obstacle.
        private static BotObstacleHint.Behavior FindObstacleHint(Collider col)
        {
            if (col == null) return BotObstacleHint.Behavior.Auto;
            var h = col.GetComponent<BotObstacleHint>()
                 ?? col.GetComponentInParent<BotObstacleHint>()
                 ?? col.GetComponentInChildren<BotObstacleHint>();
            return h != null ? h.behavior : BotObstacleHint.Behavior.Auto;
        }

        private BotObstacleHint.Behavior HintUnderfoot() =>
            (m_player != null && m_player.isGrounded)
                ? FindObstacleHint(m_player.groundHit.collider)
                : BotObstacleHint.Behavior.Auto;

        // The BotObstacleHint(Bridge) the bot is standing on OR within reach of —
        // proximity, not a single ray, so approaching the bridge foot and walking the
        // open corridor between its thin rails both count. Returns null if none.
        private BotObstacleHint ResolveBridgeHint()
        {
            if (m_player != null && m_player.isGrounded)
            {
                var u = m_player.groundHit.collider != null
                    ? m_player.groundHit.collider.GetComponentInParent<BotObstacleHint>() : null;
                if (u != null && u.behavior == BotObstacleHint.Behavior.Bridge) return u;
            }

            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, 2.5f,
                s_bridgeBuffer, obstacleLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (s_bridgeBuffer[i] == null) continue;
                var h = s_bridgeBuffer[i].GetComponentInParent<BotObstacleHint>();
                if (h != null && h.behavior == BotObstacleHint.Behavior.Bridge) return h;
            }
            return null;
        }

        // ── Stepping-disc (RidePlatform) helpers ─────────────────────────────

        // A BotObstacleHint(RidePlatform) underfoot OR within ~3.5 m — arms stone mode.
        private bool ResolveStoneHint()
        {
            if (m_player != null && m_player.isGrounded && m_player.groundHit.collider != null)
            {
                var u = m_player.groundHit.collider.GetComponentInParent<BotObstacleHint>();
                if (u != null && u.behavior == BotObstacleHint.Behavior.RidePlatform) return true;
            }
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, 3.5f,
                s_stoneBuffer, obstacleLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (s_stoneBuffer[i] == null) continue;
                var h = s_stoneBuffer[i].GetComponentInParent<BotObstacleHint>();
                if (h != null && h.behavior == BotObstacleHint.Behavior.RidePlatform) return true;
            }
            return false;
        }

        // Fills s_stoneCentres with the distinct world positions of every RidePlatform-hinted
        // disc within <radius>. Returns the count.
        private int CollectStoneCentres(Vector3 pos, float radius)
        {
            s_stoneCentres.Clear();
            int n = Physics.OverlapSphereNonAlloc(pos, radius, s_stoneBuffer,
                obstacleLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (s_stoneBuffer[i] == null) continue;
                var h = s_stoneBuffer[i].GetComponentInParent<BotObstacleHint>();
                if (h == null || h.behavior != BotObstacleHint.Behavior.RidePlatform) continue;
                Vector3 c = h.transform.position;
                bool dup = false;
                for (int j = 0; j < s_stoneCentres.Count; j++)
                    if ((s_stoneCentres[j] - c).sqrMagnitude < 0.25f) { dup = true; break; }
                if (!dup) s_stoneCentres.Add(c);
            }
            return s_stoneCentres.Count;
        }

        // Index of the path waypoint nearest <pos> — where a bot joining the route starts.
        private static int NearestStonePathIdx(Vector3[] path, Vector3 pos)
        {
            if (path == null) return 0;
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < path.Length; i++)
            {
                float d = Vector3.Distance(pos, path[i]);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        // Ordered jump path from <start> to <end> through <discs>. Greedy: from the current
        // point, step to the nearest disc that advances toward <end> and isn't past it, until
        // none remain, then <end>.
        private static Vector3[] BuildStoneJumpPath(Vector3 start, Vector3 end, Vector3[] discs)
        {
            s_stonePathBuild.Clear();
            s_stonePathBuild.Add(start);

            Vector3 axis = end - start; axis.y = 0f;
            float total = axis.magnitude;
            if (total < 0.01f || discs == null) { s_stonePathBuild.Add(end); return s_stonePathBuild.ToArray(); }
            axis /= total;

            int count = discs.Length;
            var used = new bool[count];
            Vector3 cur = start;
            for (int guard = 0; guard < count; guard++)
            {
                int bestI = -1; float bestStep = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    if (used[i]) continue;
                    Vector3 d = discs[i] - cur; d.y = 0f;
                    float along = Vector3.Dot(d, axis);
                    if (along < 0.5f) continue;                       // not forward of the current point
                    Vector3 fromStart = discs[i] - start; fromStart.y = 0f;
                    float prog = Vector3.Dot(fromStart, axis);
                    if (prog > total + 2f) continue;                  // past the end
                    float lateral = Vector3.Distance(fromStart, axis * prog);
                    if (lateral > 7f) continue;                       // way off the corridor
                    float step = d.magnitude + lateral * 0.5f;
                    if (step < bestStep) { bestStep = step; bestI = i; }
                }
                if (bestI < 0) break;
                used[bestI] = true;
                Vector3 p = discs[bestI]; p.y = start.y; // flatten to a consistent walk height
                s_stonePathBuild.Add(p);
                cur = discs[bestI];
            }

            s_stonePathBuild.Add(end);
            return s_stonePathBuild.ToArray();
        }

        private Vector3 StoneCheckpointDir(Vector3 pos)
        {
            if (m_checkpoints != null && m_nextCpIdx < m_checkpoints.Length
                && m_checkpoints[m_nextCpIdx] != null)
            {
                Vector3 d = m_checkpoints[m_nextCpIdx].transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude > 0.01f) return d.normalized;
            }
            return Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        }

        // Is <pos> actually inside a line-equipped bridge's deck corridor? Tight laterally
        // (deck width), generous along the line (approach runway + step-off). Used to gate
        // bridge mode so a bot merely brushing the wide decorative end-frame colliders
        // while walking PAST the bridge — or a corpse/respawn far away — doesn't arm a
        // bogus crossing.
        private static bool BridgeCorridorContains(BotObstacleHint hint, Vector3 pos)
        {
            if (hint == null || !hint.HasCrossingLine) return false;
            Vector3 a = hint.pathA.position;
            Vector3 b = hint.pathB.position;
            Vector3 ab = b - a; ab.y = 0f;
            float len = ab.magnitude;
            if (len < 0.01f) return true;
            Vector3 dir = ab / len;
            Vector3 fa = new Vector3(a.x, pos.y, a.z);
            float u = Vector3.Dot(pos - fa, dir);
            Vector3 onLine = fa + dir * Mathf.Clamp(u, 0f, len);
            float lat = new Vector2(pos.x - onLine.x, pos.z - onLine.z).magnitude;
            return u > -4f && u < len + 3f && lat < 2.0f;
        }

        // Is a hinted bridge close enough ahead that the bot should steer to its mouth
        // (entry-marker end) BEFORE the checkpoint pull drags it into the side of the
        // structure? Returns the mouth aim-point. Only engages while the bot is still on
        // the near-island side and the crossing actually leads toward the next checkpoint.
        private bool TryGetBridgeApproach(out Vector3 mouth)
        {
            if (Time.time >= m_nextBridgeApproachScan)
            {
                m_nextBridgeApproachScan = Time.time + 0.3f;
                m_hasBridgeApproach      = false;

                Vector3 cpPos = (m_checkpoints != null && m_nextCpIdx < m_checkpoints.Length
                                 && m_checkpoints[m_nextCpIdx] != null)
                    ? m_checkpoints[m_nextCpIdx].transform.position
                    : transform.position + transform.forward * 10f;

                int n = Physics.OverlapSphereNonAlloc(transform.position, 14f,
                    s_bridgeBuffer, obstacleLayer, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (s_bridgeBuffer[i] == null) continue;
                    var h = s_bridgeBuffer[i].GetComponentInParent<BotObstacleHint>();
                    if (h == null || h.behavior != BotObstacleHint.Behavior.Bridge || !h.HasCrossingLine)
                        continue;

                    Vector3 pos = transform.position;
                    Vector3 a = h.pathA.position, b = h.pathB.position;
                    Vector3 ab = b - a; ab.y = 0f;
                    float len = ab.magnitude;
                    if (len < 0.5f) continue;

                    // Which end is the mouth? Whichever the bot is nearer to.
                    Vector3 near = (a - pos).sqrMagnitude <= (b - pos).sqrMagnitude ? a : b;
                    Vector3 far  = near == a ? b : a;

                    Vector3 fa = new Vector3(near.x, pos.y, near.z);
                    float   u  = Vector3.Dot(pos - fa, (far - near).normalized);
                    if (u > 1.5f) continue; // already on the deck / crossed — corridor logic owns it

                    // Only if crossing this bridge heads toward the checkpoint.
                    if (Vector3.Dot((far - near).normalized, (cpPos - near).normalized) < 0.2f) continue;

                    float dist = Vector3.Distance(pos, near);
                    if (dist < best)
                    {
                        best = dist;
                        m_bridgeApproachMouth = near + (far - near).normalized * 0.6f;
                        m_hasBridgeApproach   = true;
                    }
                }
            }

            mouth = m_bridgeApproachMouth;
            return m_hasBridgeApproach;
        }

        // Finds the ObstacleKnockback on a hit collider's hierarchy — checking the collider's own
        // GameObject, then upward, then downward. Confirmed necessary via a scene survey: some
        // obstacle prefabs (e.g. hammer) put ObstacleKnockback on a child (the swinging head)
        // while a raycast can also hit a separate, sibling-less static pole/root collider with no
        // knockback component of its own but with the hazard as a descendant.
        private static ObstacleKnockback FindObstacleKnockback(Collider col)
        {
            var ok = col.GetComponent<ObstacleKnockback>();
            if (ok != null) return ok;
            ok = col.GetComponentInParent<ObstacleKnockback>();
            if (ok != null) return ok;
            return col.GetComponentInChildren<ObstacleKnockback>();
        }

        // Every known "this obstacle is actively moving right now" component type in this
        // project. A scene survey found FIVE near-identical RotationScript/OscillateRotation
        // classes across different namespaces (ithappy vs the RC-authored ithappy.rc variant) —
        // the project's own rotating hazards (rotating_beam.prefab, 172 instances across L1-L3;
        // rotating_log.prefab's knockback variant, 26 instances) use the RC namespace, which the
        // old single-namespace check never matched, so they were silently treated as static
        // walls with no timing/waiting behaviour at all. RandomStoneFall (used by Boulder) is a
        // fourth moving-hazard pattern that was never checked either. MovingPlatform is
        // deliberately NOT here — it's a rideable surface (see IsRideablePlatform), and a
        // rotating disc bridge (RotationScript + DynamicPlatform) would otherwise match on
        // its RotationScript and get waited-out instead of ridden.
        private static readonly System.Type[] k_motionComponentTypes =
        {
            typeof(RotationScript),               // ithappy.RotationScript
            typeof(ithappy.rc.RotationScript),
            typeof(OscillateRotation),             // ithappy.OscillateRotation
            typeof(ithappy.rc.OscillateRotation),
            typeof(RandomStoneFall),               // AdventureMultiplayer.RandomStoneFall (Boulder)
        };

        // True for a swinging/rotating/falling obstacle — the kind of thing a real player times
        // their crossing around rather than jumping through or detouring past. Checks the same
        // collider-then-up-then-down hierarchy as FindObstacleKnockback, since the motion
        // component and the knockback component don't always live on the same GameObject.
        private static bool IsTimedHazard(Collider col)
        {
            // A rideable platform that also spins (rotating disc bridge) is a RIDE, never a
            // wait-out hazard — check this before the motion-component scan below.
            if (IsRideablePlatform(col)) return false;

            foreach (var t in k_motionComponentTypes)
            {
                if (col.GetComponent(t) != null) return true;
                if (col.GetComponentInParent(t) != null) return true;
                if (col.GetComponentInChildren(t) != null) return true;
            }
            return false;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (m_checkpoints == null || m_nextCpIdx >= m_checkpoints.Length) return;
            var cp = m_checkpoints[m_nextCpIdx];
            if (cp == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, cp.transform.position);
            Gizmos.DrawWireSphere(cp.transform.position, arrivalRadius);

            Vector3 origin = transform.position + Vector3.up * 0.6f;
            Vector3 fwd    = transform.forward;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(origin, fwd * obstacleCheckDist);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, -sideRayAngle, 0f) * fwd * obstacleCheckDist);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f,  sideRayAngle, 0f) * fwd * obstacleCheckDist);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + fwd * gapProbeDistance + Vector3.up * 0.3f, Vector3.down * gapProbeDepth);
        }
#endif
    }
}
