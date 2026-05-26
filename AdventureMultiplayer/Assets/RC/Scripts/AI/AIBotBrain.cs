using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// FSM-based AI brain.
    ///
    /// States
    ///   Idle              — scans for best collectible, picks target once, then commits.
    ///   WalkToCollectible — NavMesh-steered walk directly to the target.
    ///   WalkToLauncher    — walks to a launcher's entry point before using it.
    ///   UseSpring         — rides the spring arc to the landing zone.
    ///   ClimbPole         — grabs pole, climbs, jumps off at the right height.
    ///   UseRail           — enters rail grind, steers to exit, jumps off.
    ///   UsePortal         — walks into portal A; detects teleport to B.
    ///   WaitForPlatform   — waits at boarding waypoint until platform arrives.
    ///   BoardPlatform     — steps onto the moving platform.
    ///   RidePlatform      — rides to the exit waypoint.
    ///
    /// Key design decisions
    ///   • Target is chosen ONLY in Idle — never mid-execution.
    ///   • The only interrupt is "target was collected" (physics trigger fired on it).
    ///   • No multi-segment route lists — the FSM handles sequencing naturally.
    /// </summary>
    public enum BotDifficulty { Easy, Medium, Hard }

    [RequireComponent(typeof(AIPlayerInputManager))]
    [AddComponentMenu("Adventure Multiplayer/AI Bot Brain")]
    public class AIBotBrain : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Difficulty")]
        [SerializeField] private BotDifficulty difficulty = BotDifficulty.Medium;

        [Header("Scanning")]
        [SerializeField] private float scanInterval = 1.5f;

        [Header("Navigation")]
        [SerializeField] private float arrivalRadius            = 1.2f;
        [SerializeField] private float jumpCooldown             = 0.6f;
        [SerializeField] private float maxDirectJumpHeight      = 4f;
        [SerializeField] private float directWalkFallbackMaxHeight = 2f;
        [SerializeField] private float jumpProximityRange       = 4f;
        [SerializeField] private float launcherExitReach        = 20f;
        [SerializeField] private float obstacleCheckDistance    = 1.5f;
        [SerializeField] private float wallJumpClearHeight      = 2.5f;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Stall Recovery")]
        [SerializeField] private float stallTimeout     = 4f;
        [SerializeField] private float blacklistDuration = 20f;

        [Header("Platform Boarding")]
        [SerializeField] private float platformBoardingRange    = 3.5f;
        [SerializeField] private float platformBoardedThreshold = 1.5f;

        [Header("Collectible Values")]
        [SerializeField] private float starValue     = 100f;
        [SerializeField] private float heartValue    = 80f;
        [SerializeField] private float lifeValue     = 70f;
        [SerializeField] private float redCoinValue  = 30f;
        [SerializeField] private float blueCoinValue = 25f;
        [SerializeField] private float itemBoxValue  = 20f;
        [SerializeField] private float coinValue     = 10f;

        [Header("Goal Mode")]
        [SerializeField] private AIGoalMode goalMode = AIGoalMode.Balanced;

        [Header("Avoidance")]
        [SerializeField] private float hazardAvoidRadius   = 4f;
        [SerializeField] private float hazardAvoidStrength = 2.5f;

        [Header("Abilities")]
        [SerializeField] private float spinRange        = 2.5f;
        [SerializeField] private float spinCooldown     = 1.2f;
        [SerializeField] private float stompDetectRange = 6f;
        [SerializeField] private float dashMinDistance  = 10f;
        [SerializeField] private float dashCooldown     = 3f;
        [SerializeField] private float airDiveMinYDiff  = 4f;
        [SerializeField] private float rollMinDistance  = 12f;
        [SerializeField] private float pickupRange      = 2.5f;
        [SerializeField] private float crouchCeilHeight = 1.3f;
        [SerializeField] private float ledgeReleaseTimeout = 2.5f;

        // ── Public read-only (consumed by the editor debug window) ────────────

        public AIState CurrentState  { get; private set; } = AIState.Idle;
        public string  TargetName    => m_target  != null ? m_target.name : "—";
        public float   TargetDist    => m_target  != null
                                          ? Vector3.Distance(transform.position, m_target.transform.position)
                                          : -1f;
        public string  ActiveLauncher => m_launcher != null
                                          ? $"{m_launcher.type} ({m_launcher.source?.name ?? "?"})"
                                          : "—";
        public float   TimeInState    => Time.time - m_stateStartTime;

        // Path waypoints for the editor window and Scene-view gizmos
        public Vector3? PathTargetPos       => m_target?.transform.position;
        public Vector3? PathLauncherEntry   => m_launcher?.entryPoint;
        public Vector3? PathLauncherExit    => m_launcher?.exitPoint;
        public Vector3? PathLauncherApex    => (m_launcher?.type == LauncherType.Spring
                                                && m_launcher?.apexPoint != Vector3.zero)
                                               ? m_launcher?.apexPoint : (Vector3?)null;
        public string   PathLauncherType    => m_launcher?.type.ToString() ?? "—";

        public int BlacklistCount
        {
            get
            {
                int n = 0;
                foreach (var kvp in m_blacklist)
                    if (Time.time - kvp.Value < blacklistDuration) n++;
                return n;
            }
        }

        public  AIPlayerInputManager InputManager => m_input;

        private const int MaxLogEntries = 80;
        private readonly List<AILogEntry> m_log = new List<AILogEntry>(MaxLogEntries);
        public  IReadOnlyList<AILogEntry> Log => m_log;

        private readonly List<CollectibleScanEntry> m_scanEntries = new List<CollectibleScanEntry>();
        public  IReadOnlyList<CollectibleScanEntry> ScanEntries   => m_scanEntries;

        // ── Private references ────────────────────────────────────────────────

        private AIPlayerInputManager m_input;
        private Player               m_self;

        // ── Scene cache ───────────────────────────────────────────────────────

        private Collectible[]      m_collectibles    = new Collectible[0];
        private List<LauncherInfo> m_launchers       = new List<LauncherInfo>();
        private Player[]           m_rivals          = new Player[0];
        private GravityField[]     m_gravityFields   = new GravityField[0];
        private Enemy[]            m_enemies         = new Enemy[0];
        private Breakable[]        m_breakables      = new Breakable[0];
        private Pickable[]         m_pickables       = new Pickable[0];
        private Hazard[]           m_hazards         = new Hazard[0];
        private KillZone[]         m_killZones       = new KillZone[0];
        private Panel[]            m_panels          = new Panel[0];
        private ItemBox[]          m_itemBoxes       = new ItemBox[0];
        private FallingPlatform[]  m_fallingPlatforms = new FallingPlatform[0];
        private ConveyorBelt[]     m_conveyors       = new ConveyorBelt[0];

        // ── Target mode — what kind of object the bot is currently chasing ────

        private enum TargetMode { Collectible, ItemBox, Breakable }
        private TargetMode m_targetMode  = TargetMode.Collectible;
        private ItemBox    m_targetItemBox;
        private Breakable  m_targetBreakable;

        // ── FSM state ─────────────────────────────────────────────────────────

        private Collectible  m_target;
        private LauncherInfo m_launcher;
        private float        m_stateStartTime;
        private float        m_lastProgressTime;
        private Vector3      m_lastStallCheckPos;
        private float        m_nextJumpTime;
        private float        m_nextIdleScanTime;
        private bool         m_springLaunched;

        // ── Ability cooldowns ─────────────────────────────────────────────────

        private float m_nextSpinTime;
        private float m_nextDashTime;
        private float m_ledgeHangStartTime = -1f;

        // ── Difficulty / human-sim runtime values ─────────────────────────────

        private float m_thinkPauseMax;   // extra random wait added to idle scan interval
        private float m_skillVariance;   // ± jitter added to spin/dash/jump cooldowns

        // ── Log throttle ──────────────────────────────────────────────────────

        private bool    m_glideLogFired;
        private bool    m_rollLogActive;
        private float   m_lastAbilityLogTime = -999f;
        private float   m_lastNavDownLogTime = -999f;
        private Vector3 m_lastNonZeroMoveDir = Vector3.forward;
        private Vector3 m_walkOffEdgeDir     = Vector3.zero;   // latched while walking off a ledge
        private const float k_abilityLogInterval = 1f;

        // ── Blacklist ─────────────────────────────────────────────────────────

        private readonly Dictionary<Collectible,  float> m_blacklist          = new();
        private readonly Dictionary<ItemBox,     float> m_itemBoxBlacklist   = new();
        private readonly Dictionary<Breakable,   float> m_breakableBlacklist = new();
        private readonly Dictionary<LauncherInfo,float> m_launcherBlacklist  = new();

        // ── NavMesh ───────────────────────────────────────────────────────────

        private NavMeshPath       m_navPath;
        private NavMeshPath       m_queryPath;
        private NavMeshPathStatus m_lastNavStatus = NavMeshPathStatus.PathInvalid;

        // ─────────────────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            m_input = GetComponent<AIPlayerInputManager>();
            m_self  = GetComponent<Player>();

            if (m_input == null) Debug.LogError("[AIBotBrain] AIPlayerInputManager missing.");
            if (m_self  == null) Debug.LogError("[AIBotBrain] Player missing.");

            ApplyDifficulty();
        }

        private void ApplyDifficulty()
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    m_input.reactionTimeMin  = 0.30f;
                    m_input.reactionTimeMax  = 0.50f;
                    m_input.turnSpeed        = 4f;
                    m_input.directionJitter  = 12f;
                    m_thinkPauseMax          = 0.8f;
                    m_skillVariance          = 0.30f;
                    break;

                case BotDifficulty.Medium:
                    m_input.reactionTimeMin  = 0.12f;
                    m_input.reactionTimeMax  = 0.25f;
                    m_input.turnSpeed        = 8f;
                    m_input.directionJitter  = 6f;
                    m_thinkPauseMax          = 0.35f;
                    m_skillVariance          = 0.12f;
                    break;

                case BotDifficulty.Hard:
                    m_input.reactionTimeMin  = 0.04f;
                    m_input.reactionTimeMax  = 0.10f;
                    m_input.turnSpeed        = 18f;
                    m_input.directionJitter  = 1.5f;
                    m_thinkPauseMax          = 0.05f;
                    m_skillVariance          = 0.03f;
                    break;
            }

            Debug.Log($"[AIBot] Difficulty={difficulty} reaction=[{m_input.reactionTimeMin:F2}-{m_input.reactionTimeMax:F2}]s turnSpeed={m_input.turnSpeed} jitter={m_input.directionJitter}° thinkPause={m_thinkPauseMax:F2}s");
        }

        private void Start()
        {
            m_navPath   = new NavMeshPath();
            m_queryPath = new NavMeshPath();

            if (!gameObject.CompareTag("Player"))
                Debug.LogError($"[AIBotBrain] Tag is '{gameObject.tag}' — must be 'Player' or collectibles won't be picked up.");

            ScanEnvironment();
            BrainLog(AILogType.StateChange, "Brain started — entering Idle.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Environment scanning — once at Start
        // ─────────────────────────────────────────────────────────────────────

        private void ScanEnvironment()
        {
            m_collectibles     = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            m_gravityFields    = FindObjectsByType<GravityField>(FindObjectsSortMode.None);
            m_enemies          = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            m_breakables       = FindObjectsByType<Breakable>(FindObjectsSortMode.None);
            m_pickables        = FindObjectsByType<Pickable>(FindObjectsSortMode.None);
            m_hazards          = FindObjectsByType<Hazard>(FindObjectsSortMode.None);
            m_killZones        = FindObjectsByType<KillZone>(FindObjectsSortMode.None);
            m_panels           = FindObjectsByType<Panel>(FindObjectsSortMode.None);
            m_itemBoxes        = FindObjectsByType<ItemBox>(FindObjectsSortMode.None);
            m_fallingPlatforms = FindObjectsByType<FallingPlatform>(FindObjectsSortMode.None);
            m_conveyors        = FindObjectsByType<ConveyorBelt>(FindObjectsSortMode.None);
            BuildLauncherList();
            RefreshRivals();
            Debug.Log($"[AIBotBrain] Scanned: {m_collectibles.Length} collectibles, {m_launchers.Count} launchers, {m_enemies.Length} enemies, {m_breakables.Length} breakables, {m_hazards.Length} hazards, {m_killZones.Length} killzones, {m_panels.Length} panels, {m_itemBoxes.Length} itemboxes, {m_fallingPlatforms.Length} fallingPlatforms, {m_conveyors.Length} conveyors.");
        }

        private void BuildLauncherList()
        {
            m_launchers.Clear();
            RegisterSprings();
            RegisterPoles();
            RegisterRails();
            RegisterPortals();
            RegisterMovingPlatforms();
            RegisterForceFields();
            RegisterSpeedBoosters();
        }

        private void RefreshRivals()
        {
            var all  = FindObjectsByType<Player>(FindObjectsSortMode.None);
            var list = new List<Player>(all.Length);
            foreach (var p in all)
                if (p != m_self) list.Add(p);
            m_rivals = list.ToArray();
        }

        private void RegisterSprings()
        {
            foreach (var s in FindObjectsByType<Spring>(FindObjectsSortMode.None))
            {
                if (!s.gameObject.activeInHierarchy) continue;
                float apexHeight = (s.force * s.force) / (2f * Mathf.Abs(Physics.gravity.y));
                Vector3 apex = s.transform.position + Vector3.up * apexHeight;

                Vector3 exitPoint;
                if (Physics.Raycast(apex + Vector3.up * 0.5f, Vector3.down,
                        out RaycastHit hit, apexHeight + 1f, ~0, QueryTriggerInteraction.Ignore))
                    exitPoint = SampleNavMesh(hit.point);
                else
                    exitPoint = SampleNavMesh(apex);

                // entryPoint = top surface of the spring disc so the bot can hop onto it.
                // Use the first NON-trigger collider so we get the physical disc height,
                // not a large trigger volume that would give a false (too-high) entry Y.
                Collider springCol = null;
                foreach (var col in s.GetComponents<Collider>())
                {
                    if (!col.isTrigger) { springCol = col; break; }
                }
                // Fall back to any collider if there is no non-trigger collider.
                if (springCol == null) springCol = s.GetComponent<Collider>();

                Vector3 springTop = springCol != null
                    ? new Vector3(s.transform.position.x, springCol.bounds.max.y, s.transform.position.z)
                    : s.transform.position;

                m_launchers.Add(new LauncherInfo
                {
                    type       = LauncherType.Spring,
                    entryPoint = springTop,
                    exitPoint  = exitPoint,
                    apexPoint  = apex,
                    source     = s,
                });
                Debug.Log($"[AIBotBrain] Spring: top={springTop.y:F2}m apex={apex.y:F1}m exit={exitPoint.y:F1}m");
            }
        }

        private void RegisterPoles()
        {
            foreach (var p in FindObjectsByType<Pole>(FindObjectsSortMode.None))
            {
                if (!p.gameObject.activeInHierarchy) continue;
                m_launchers.Add(new LauncherInfo
                {
                    type       = LauncherType.Pole,
                    entryPoint = SampleNavMesh(p.collider.bounds.center),
                    exitPoint  = p.collider.bounds.max,
                    source     = p,
                });
            }
        }

        private void RegisterRails()
        {
            foreach (var sc in FindObjectsByType<SplineContainer>(FindObjectsSortMode.None))
            {
                if (!sc.gameObject.activeInHierarchy || sc.Spline == null) continue;

                bool isRail = sc.CompareTag("Interactive/Rail");
                if (!isRail)
                {
                    var col = sc.GetComponentInChildren<Collider>();
                    if (col == null || !col.CompareTag("Interactive/Rail")) continue;
                }

                var s  = sc.Spline;
                var p0 = (Vector3)s.EvaluatePosition(0f);
                var p1 = (Vector3)s.EvaluatePosition(1f);
                Vector3 startW = sc.transform.TransformPoint(p0);
                Vector3 endW   = sc.transform.TransformPoint(p1);

                m_launchers.Add(new LauncherInfo
                {
                    type = LauncherType.Rail, entryPoint = SampleNavMesh(startW),
                    exitPoint = SampleNavMesh(endW), source = sc,
                });
                m_launchers.Add(new LauncherInfo
                {
                    type = LauncherType.Rail, entryPoint = SampleNavMesh(endW),
                    exitPoint = SampleNavMesh(startW), source = sc,
                });
            }
        }

        private void RegisterPortals()
        {
            foreach (var p in FindObjectsByType<Portal>(FindObjectsSortMode.None))
            {
                if (!p.gameObject.activeInHierarchy || p.exit == null) continue;
                Vector3 exitPos = p.exit.transform.position + p.exit.transform.forward * p.exit.exitOffset;
                m_launchers.Add(new LauncherInfo
                {
                    type       = LauncherType.Portal,
                    entryPoint = SampleNavMesh(p.transform.position),
                    exitPoint  = exitPos,
                    source     = p,
                });
            }
        }

        private void RegisterMovingPlatforms()
        {
            foreach (var mp in FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
            {
                if (!mp.gameObject.activeInHierarchy) continue;
                var wps = mp.waypoints?.waypoints;
                if (wps == null || wps.Count < 2) continue;

                for (int i = 0; i < wps.Count - 1; i++)
                {
                    Vector3 entry = SampleNavMeshNearGround(wps[i].position);
                    Vector3 exit  = SampleNavMeshNearGround(wps[i + 1].position);

                    Debug.Log($"[AIBotBrain] MovingPlatform '{mp.name}' waypoint[{i}] → entry={entry} exit={exit}  (raw wp[{i}]={wps[i].position} wp[{i+1}]={wps[i+1].position})");

                    m_launchers.Add(new LauncherInfo
                    {
                        type             = LauncherType.MovingPlatform,
                        entryPoint       = entry,
                        exitPoint        = exit,
                        boardingWaypoint = wps[i],
                        exitWaypoint     = wps[i + 1],
                        source           = mp,
                    });
                }
            }
        }

        /// <summary>
        /// Like SampleNavMesh but also tries the floor directly below the position
        /// (useful for platform waypoints that sit over water or off-mesh terrain).
        /// Tries progressively larger radii before falling back to the raw position.
        /// </summary>
        private Vector3 SampleNavMeshNearGround(Vector3 pos)
        {
            // 1. Try straight at the position with a generous radius.
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                return hit.position;

            // 2. Raycast down to find the floor, then sample from there.
            if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out RaycastHit rh, 20f,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                if (NavMesh.SamplePosition(rh.point, out hit, 10f, NavMesh.AllAreas))
                    return hit.position;
            }

            // 3. No NavMesh found — return raw position and log a warning.
            Debug.LogWarning($"[AIBotBrain] MovingPlatform waypoint at {pos} has no NavMesh within 10 m. Boarding may fail.");
            return pos;
        }

        private void RegisterForceFields()
        {
            foreach (var ff in FindObjectsByType<ForceField>(FindObjectsSortMode.None))
            {
                if (!ff.gameObject.activeInHierarchy) continue;
                var col = ff.GetComponent<Collider>();
                if (col == null) continue;

                // Entry: NavMesh point at the base of the force field trigger
                Vector3 entryBase = new Vector3(col.bounds.center.x,
                                                col.bounds.min.y,
                                                col.bounds.center.z);

                // Exit: estimated apex based on 1.5 s of constant upward push
                float liftTime  = 1.5f;
                float apexHeight = 0.5f * ff.force * liftTime * liftTime;
                Vector3 exit    = col.bounds.center + Vector3.up * apexHeight;

                m_launchers.Add(new LauncherInfo
                {
                    type       = LauncherType.ForceField,
                    entryPoint = SampleNavMesh(entryBase),
                    exitPoint  = SampleNavMesh(exit),
                    source     = ff,
                });
            }
        }

        private void RegisterSpeedBoosters()
        {
            foreach (var sb in FindObjectsByType<SpeedBooster>(FindObjectsSortMode.None))
            {
                if (!sb.gameObject.activeInHierarchy) continue;

                // Determine boost direction (mirrors SpeedBooster.GetBoostDirection)
                Vector3 boostDir = sb.boostDirection == SpeedBooster.BoostDirection.Forward
                    ? sb.transform.forward
                    : sb.transform.up;

                Vector3 exit = sb.transform.position + boostDir * (sb.boostForce * 0.35f);

                m_launchers.Add(new LauncherInfo
                {
                    type       = LauncherType.SpeedBooster,
                    entryPoint = SampleNavMesh(sb.transform.position),
                    exitPoint  = SampleNavMesh(exit),
                    source     = sb,
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update — FSM dispatcher
        // ─────────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (m_self == null || m_input == null) return;

            // Always check if our target was collected by someone else mid-execution
            CheckTargetCollected();

            // Ability decisions run every frame, independent of FSM state
            HandleAbilities();

            switch (CurrentState)
            {
                case AIState.Idle:              TickIdle();             break;
                case AIState.WalkToCollectible: TickWalkToCollectible();break;
                case AIState.WalkToLauncher:    TickWalkToLauncher();   break;
                case AIState.UseSpring:         TickUseSpring();        break;
                case AIState.ClimbPole:         TickClimbPole();        break;
                case AIState.UseRail:           TickUseRail();          break;
                case AIState.UsePortal:         TickUsePortal();        break;
                case AIState.WaitForPlatform:   TickWaitForPlatform();  break;
                case AIState.BoardPlatform:     TickBoardPlatform();    break;
                case AIState.RidePlatform:      TickRidePlatform();     break;
                case AIState.UseForceField:     TickUseForceField();    break;
                case AIState.UseSpeedBooster:   TickUseSpeedBooster();  break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Idle — scan and pick a target
        // ─────────────────────────────────────────────────────────────────────

        private void TickIdle()
        {
            m_input.desiredMoveDirection = Vector3.zero;

            if (Time.time < m_nextIdleScanTime) return;
            m_nextIdleScanTime = Time.time + scanInterval + Random.Range(0f, m_thinkPauseMax);

            // Refresh collectible list
            m_collectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            if (goalMode == AIGoalMode.Rival) RefreshRivals();

            Collectible  bestTarget   = null;
            LauncherInfo bestLauncher = null;
            float        bestScore    = float.MinValue;

            m_scanEntries.Clear();

            foreach (var c in m_collectibles)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                if (c.triggerCollider != null && !c.triggerCollider.enabled) continue;

                float dist  = Vector3.Distance(transform.position, c.transform.position);
                float hDiff = c.transform.position.y - transform.position.y;

                var entry = new CollectibleScanEntry
                {
                    name         = c.name,
                    dist         = dist,
                    hDiff        = hDiff,
                    walkScore    = float.MinValue,
                    launchScore  = float.MinValue,
                    launchType   = "—",
                    isBlacklisted = IsBlacklisted(c),
                };

                if (IsBlacklisted(c))
                {
                    entry.navStatus = "—";
                    m_scanEntries.Add(entry);
                    continue;
                }

                float value = GetCollectibleValue(c);

                // ── Direct walk? ──────────────────────────────────────────────
                var status  = QueryPathStatus(transform.position, c.transform.position);
                bool navOk  = status == NavMeshPathStatus.PathComplete ||
                              status == NavMeshPathStatus.PathPartial;

                entry.navStatus = status switch
                {
                    NavMeshPathStatus.PathComplete => "Full",
                    NavMeshPathStatus.PathPartial  => "Part",
                    _                              => "None",
                };

                // Height check:
                //   PathComplete  → full trust, allow up to maxDirectJumpHeight.
                //   PathPartial   → NavMesh led to a wall/edge but couldn't complete.
                //                   If hDiff > 0 (target is above) this almost always
                //                   means a wall is blocking — treat like PathInvalid.
                //   PathInvalid   → cap at directWalkFallbackMaxHeight.
                //   Downhill (hDiff < 0) → always reachable regardless of path.
                bool pathComplete = status == NavMeshPathStatus.PathComplete;
                bool heightOk = pathComplete
                    ? hDiff <= maxDirectJumpHeight
                    : hDiff <= directWalkFallbackMaxHeight;
                entry.heightOk = heightOk;

                if (heightOk)
                {
                    // PathComplete  → trust the NavMesh, no penalty.
                    // PathPartial   → partial path, mild penalty.
                    // PathInvalid + target below bot (hDiff < 0): likely a NavMesh
                    //   baking gap on a drop-down — the bot can always reach it by
                    //   walking/falling, so use the same mild penalty as PathPartial.
                    // PathInvalid + target above bot: genuinely uncertain, heavy penalty.
                    float costMul = status switch
                    {
                        NavMeshPathStatus.PathComplete             => 1.0f,
                        NavMeshPathStatus.PathPartial              => 1.5f,
                        _ when hDiff < 0f                         => 1.5f,
                        _                                         => 2.5f,
                    };
                    float adjValue = AdjustedValue(c, value);
                    if (goalMode == AIGoalMode.Greedy) costMul *= 0.6f;
                    float walkScore = adjValue / (dist * costMul + 0.1f);
                    if (goalMode == AIGoalMode.Rival) walkScore *= RivalBoost(c.transform.position);
                    entry.walkScore    = walkScore;

                    if (walkScore > bestScore)
                    {
                        bestScore    = walkScore;
                        bestTarget   = c;
                        bestLauncher = null;
                    }

                    if (navOk) { m_scanEntries.Add(entry); continue; }
                }

                // ── Via launcher? ────────────────────────────────────────────
                foreach (var launcher in m_launchers)
                {
                    if (IsLauncherBlacklisted(launcher)) continue;
                    float score = ScoreLauncherRoute(c, AdjustedValue(c, value), launcher);
                    if (score == float.MinValue) continue;
                    if (score > entry.launchScore)
                    {
                        entry.launchScore = score;
                        entry.launchType  = launcher.type.ToString();
                    }
                    if (score > bestScore)
                    {
                        bestScore    = score;
                        bestTarget   = c;
                        bestLauncher = launcher;
                    }
                }

                m_scanEntries.Add(entry);
            }

            // Mark winner
            for (int i = 0; i < m_scanEntries.Count; i++)
            {
                if (bestTarget != null && m_scanEntries[i].name == bestTarget.name)
                {
                    var e = m_scanEntries[i];
                    e.isWinner = true;
                    m_scanEntries[i] = e;
                }
            }

            // ── Also score ItemBoxes ─────────────────────────────────────────
            ItemBox    bestItemBox    = null;
            Breakable  bestBreakable  = null;

            foreach (var box in m_itemBoxes)
            {
                if (box == null || !box.gameObject.activeInHierarchy) continue;
                if (box.collectibles == null || box.collectibles.Length == 0) continue;
                if (IsItemBoxBlacklisted(box)) continue;

                float value = GetItemBoxValue(box);
                if (value <= 0f) continue;

                float dist  = Vector3.Distance(transform.position, box.transform.position);
                float score = value / (dist + 0.1f);

                if (score > bestScore)
                {
                    bestScore    = score;
                    bestItemBox  = box;
                    bestBreakable = null;
                    bestTarget   = null;
                    bestLauncher = null;
                }
            }

            // ── Also score Breakables that contain collectibles ───────────────
            foreach (var b in m_breakables)
            {
                if (b == null || !b.gameObject.activeInHierarchy || b.broken) continue;
                if (b.collectibles == null || b.collectibles.Length == 0) continue;
                if (IsBreakableBlacklisted(b)) continue;

                float value = GetBreakableValue(b);
                if (value <= 0f) continue;

                float dist  = Vector3.Distance(transform.position, b.transform.position);
                float score = value / (dist + 0.1f);

                if (score > bestScore)
                {
                    bestScore     = score;
                    bestBreakable = b;
                    bestItemBox   = null;
                    bestTarget    = null;
                    bestLauncher  = null;
                }
            }

            // ── Commit to winner ─────────────────────────────────────────────
            if (bestItemBox != null)
            {
                m_targetItemBox   = bestItemBox;
                m_targetBreakable = null;
                m_target          = null;
                m_launcher        = null;
                m_targetMode      = TargetMode.ItemBox;
                BrainLog(AILogType.TargetChange, $"WINNER ItemBox '{bestItemBox.name}'  score={bestScore:F2}");
                SetState(AIState.WalkToCollectible);
            }
            else if (bestBreakable != null)
            {
                m_targetBreakable = bestBreakable;
                m_targetItemBox   = null;
                m_target          = null;
                m_launcher        = null;
                m_targetMode      = TargetMode.Breakable;
                BrainLog(AILogType.TargetChange, $"WINNER Breakable '{bestBreakable.name}'  score={bestScore:F2}");
                SetState(AIState.WalkToCollectible);
            }
            else if (bestTarget != null)
            {
                m_target          = bestTarget;
                m_launcher        = bestLauncher;
                m_targetItemBox   = null;
                m_targetBreakable = null;
                m_targetMode      = TargetMode.Collectible;
                float winnerHDiff = bestTarget.transform.position.y - transform.position.y;
                float winnerDist  = Vector3.Distance(transform.position, bestTarget.transform.position);
                BrainLog(AILogType.TargetChange,
                    $"WINNER '{bestTarget.name}'  score={bestScore:F2}  via {(bestLauncher != null ? bestLauncher.type.ToString() : "walk")}  dist={winnerDist:F1}  hDiff={winnerHDiff:F2}");
                SetState(bestLauncher != null ? AIState.WalkToLauncher : AIState.WalkToCollectible);
            }
            else
            {
                BrainLog(AILogType.Info, "No reachable targets.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Walk directly to the collectible
        // ─────────────────────────────────────────────────────────────────────

        private void TickWalkToCollectible()
        {
            if (CheckTimeout(20f)) return;

            Vector3 targetPos;
            switch (m_targetMode)
            {
                case TargetMode.ItemBox when m_targetItemBox != null:
                {
                    var boxPos        = m_targetItemBox.transform.position;
                    float boxHeightDiff = boxPos.y - transform.position.y;

                    // If the bot is above the item box, it ended up on the wrong floor.
                    // Route it to a point below the box so NavMesh takes it back down.
                    float targetNavY = boxHeightDiff < -0.5f
                        ? boxPos.y - 1.5f        // bot above box — aim below it
                        : transform.position.y;  // bot below box — aim at current floor

                    targetPos = new Vector3(boxPos.x, targetNavY, boxPos.z);

                    float xzDist = Vector3.Distance(
                        new Vector3(transform.position.x, 0f, transform.position.z),
                        new Vector3(boxPos.x,             0f, boxPos.z));

                    Debug.Log($"[AIBot] [ItemBox] '{m_targetItemBox.name}' xzDist={xzDist:F2} hDiff={boxHeightDiff:F2} botY={transform.position.y:F2} boxY={boxPos.y:F2} navTargetY={targetNavY:F2}");

                    if (xzDist < arrivalRadius && m_self.isGrounded && Time.time >= m_nextJumpTime)
                    {
                        if (boxHeightDiff < -0.5f)
                        {
                            // Bot is above the box — don't jump, let navigation route it down.
                            Debug.Log($"[AIBot] [ItemBox] '{m_targetItemBox.name}' bot is ABOVE box (hDiff={boxHeightDiff:F2}) — routing to lower level.");
                        }
                        else if (boxHeightDiff > maxDirectJumpHeight)
                        {
                            Debug.Log($"[AIBot] [ItemBox] '{m_targetItemBox.name}' too high (hDiff={boxHeightDiff:F2} > maxJump={maxDirectJumpHeight:F1}) — waiting.");
                        }
                        else
                        {
                            BrainLog(AILogType.Ability, $"Jumping to bump ItemBox '{m_targetItemBox.name}' from below (hDiff={boxHeightDiff:F2}).");
                            Debug.Log($"[AIBot] [ItemBox] Bumping '{m_targetItemBox.name}' from below (hDiff={boxHeightDiff:F2}).");
                            QueueJump();
                        }
                    }
                    break;
                }
                case TargetMode.Breakable when m_targetBreakable != null:
                {
                    // Always navigate toward the crate's position directly.
                    // When the bot is above the crate, HandleSpin will jump it off the ledge.
                    targetPos = m_targetBreakable.transform.position;
                    break;
                }
                default:
                    if (m_target == null) { SetState(AIState.Idle); return; }
                    targetPos = m_target.transform.position;
                    break;
            }

            NavigateToward(targetPos);
            CheckStall();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Walk to a launcher entry point
        // ─────────────────────────────────────────────────────────────────────

        private void TickWalkToLauncher()
        {
            if (CheckTimeout(20f)) return;

            // For springs, navigate toward the floor below the spring (XZ position, bot's Y)
            // so NavMesh can path there. UseSpring handles the final hop up onto the disc.
            var navTarget = m_launcher.type == LauncherType.Spring
                ? new Vector3(m_launcher.entryPoint.x, transform.position.y, m_launcher.entryPoint.z)
                : m_launcher.entryPoint;

            if (m_launcher.type == LauncherType.MovingPlatform)
            {
                var pathStatus = QueryPathStatus(transform.position, navTarget);
                float distToEntry = Vector3.Distance(transform.position, navTarget);
                Debug.Log($"[AIBot] WalkToLauncher (MovingPlatform) entry={navTarget} dist={distToEntry:F1} navStatus={pathStatus}");
            }

            NavigateToward(navTarget);
            if (CheckStall()) return;

            // Use XZ-only distance so the height of the spring disc doesn't inflate the check
            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(m_launcher.entryPoint.x, 0, m_launcher.entryPoint.z));

            float arrivalDist = m_launcher.type == LauncherType.Spring ? arrivalRadius * 2.5f : arrivalRadius * 2f;
            if (dist < arrivalDist)
            {
                BrainLog(AILogType.Info, $"Reached {m_launcher.type} entry.");
                SetState(LauncherStateFor(m_launcher.type));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Spring
        // ─────────────────────────────────────────────────────────────────────

        private void TickUseSpring()
        {
            if (CheckTimeout(12f)) return;

            if (!m_springLaunched)
            {
                var toSpring  = Vector3.ProjectOnPlane(
                    m_launcher.entryPoint - transform.position, Vector3.up);
                float horizDist  = toSpring.magnitude;

                // Compare the spring disc top against the bot's FEET (stepPosition),
                // not its centre. The bot centre is ~0.9 m above ground, so the spring
                // disc top (~0.5 m world space) would give a negative heightDiff if we
                // used transform.position.y — and no jump would ever fire.
                float heightDiff = m_launcher.entryPoint.y - m_self.stepPosition.y;

                // Walk directly toward the spring center (raw XZ — no NavMesh).
                // Slow down when very close so the bot doesn't overshoot the disc.
                float speed = horizDist > 1.5f ? 1f : 0.5f;
                m_input.desiredMoveDirection = horizDist > 0.1f
                    ? toSpring.normalized * speed : Vector3.zero;

                // Queue a jump when grounded, within range, and the spring disc is
                // above the bot's feet by at least 0.05 m.
                // Direct QueueJump (not TryJump) to bypass the generic 0.3 m threshold.
                if (m_self.isGrounded
                    && Time.time >= m_nextJumpTime
                    && horizDist < jumpProximityRange
                    && heightDiff > 0.05f
                    && heightDiff <= maxDirectJumpHeight)
                {
                    QueueJump();
                }

                // Detect spring activation: bot is airborne and shooting upward.
                // Use 5 m/s threshold (force ≥ 5) so weaker springs are also caught.
                if (!m_self.isGrounded && m_self.verticalVelocity.y > 5f)
                {
                    m_springLaunched = true;
                    BrainLog(AILogType.Info, "Spring launched — airborne.");
                }
            }
            else
            {
                // Steer toward target while in the air
                if (m_target != null)
                {
                    var toTarget = Vector3.ProjectOnPlane(
                        m_target.transform.position - transform.position, Vector3.up);
                    m_input.desiredMoveDirection = toTarget.magnitude > 0.1f
                        ? toTarget.normalized : Vector3.zero;
                }

                if (m_self.isGrounded)
                {
                    BrainLog(AILogType.Info, "Landed after spring.");
                    SetState(AIState.WalkToCollectible);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Pole
        // ─────────────────────────────────────────────────────────────────────

        private void TickClimbPole()
        {
            if (CheckTimeout(20f)) return;

            var pole = m_launcher?.source as Pole;
            if (pole == null) { SetState(AIState.WalkToCollectible); return; }

            if (!IsPoleClimbing())
            {
                var toPole = Vector3.ProjectOnPlane(
                    pole.collider.bounds.center - transform.position, Vector3.up);
                m_input.desiredMoveDirection = toPole.magnitude > 0.1f
                    ? toPole.normalized : Vector3.zero;

                if (toPole.magnitude < 2f && m_self.isGrounded)
                    QueueJump();
            }
            else
            {
                m_input.desiredMoveDirection = Vector3.forward;

                float botY    = transform.position.y;
                float targetY = m_target != null
                    ? m_target.transform.position.y
                    : m_launcher.exitPoint.y;

                if (botY + maxDirectJumpHeight >= targetY - 0.5f)
                {
                    var toTarget = m_target != null
                        ? Vector3.ProjectOnPlane(
                            m_target.transform.position - transform.position, Vector3.up)
                        : (Vector3?)null;
                    m_input.desiredMoveDirection = (toTarget.HasValue && toTarget.Value.sqrMagnitude > 0.01f)
                        ? toTarget.Value.normalized : Vector3.forward;
                    QueueJump();
                    BrainLog(AILogType.Info, "Jumping off pole.");
                    SetState(AIState.WalkToCollectible);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Rail
        // ─────────────────────────────────────────────────────────────────────

        private void TickUseRail()
        {
            if (CheckTimeout(20f)) return;

            if (IsRailGrinding())
            {
                var toExit = Vector3.ProjectOnPlane(
                    m_launcher.exitPoint - transform.position, Vector3.up);
                m_input.desiredMoveDirection = toExit.magnitude > 0.1f
                    ? toExit.normalized : Vector3.zero;

                if (Vector3.Distance(transform.position, m_launcher.exitPoint) < 2.5f)
                {
                    QueueJump();
                    BrainLog(AILogType.Info, "Jumping off rail.");
                    SetState(AIState.WalkToCollectible);
                }
            }
            else
            {
                var toEntry = Vector3.ProjectOnPlane(
                    m_launcher.entryPoint - transform.position, Vector3.up);
                m_input.desiredMoveDirection = toEntry.magnitude > 0.1f
                    ? toEntry.normalized : Vector3.zero;

                float hDiff = m_launcher.entryPoint.y - transform.position.y;
                if (hDiff > 0.5f && m_self.isGrounded && toEntry.magnitude < 2f)
                    QueueJump();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Portal
        // ─────────────────────────────────────────────────────────────────────

        private void TickUsePortal()
        {
            if (CheckTimeout(10f)) return;

            var toPortal = Vector3.ProjectOnPlane(
                m_launcher.entryPoint - transform.position, Vector3.up);
            m_input.desiredMoveDirection = toPortal.magnitude > 0.1f
                ? toPortal.normalized : Vector3.zero;

            if (Vector3.Distance(transform.position, m_launcher.exitPoint) < 3f)
            {
                BrainLog(AILogType.Info, "Portal traversed.");
                SetState(AIState.WalkToCollectible);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Moving platform — Wait
        // ─────────────────────────────────────────────────────────────────────

        private void TickWaitForPlatform()
        {
            if (CheckTimeout(35f)) return;

            var platform = m_launcher?.source as MovingPlatform;
            if (platform == null) { SetState(AIState.WalkToCollectible); return; }

            // Stay at the NavMesh boarding spot; don't chase the moving platform.
            NavigateToward(m_launcher.entryPoint);

            float xzDist = Vector3.Distance(
                new Vector3(platform.transform.position.x, 0f, platform.transform.position.z),
                new Vector3(transform.position.x,          0f, transform.position.z));
            float hDiff = platform.transform.position.y - transform.position.y;

            // Board only when platform is close in XZ AND within jump reach (not too far above, not below).
            bool xzClose  = xzDist <= platformBoardingRange;
            bool jumpable = hDiff  <= maxDirectJumpHeight && hDiff >= -1f;

            Debug.Log($"[AIBot] WaitForPlatform — botPos={transform.position} entryPt={m_launcher.entryPoint} platformPos={platform.transform.position} xzDist={xzDist:F1} hDiff={hDiff:F1} xzClose={xzClose} jumpable={jumpable}");

            if (xzClose && jumpable)
            {
                BrainLog(AILogType.Info, $"Platform in range (xz={xzDist:F1} hDiff={hDiff:F1}) — boarding.");
                SetState(AIState.BoardPlatform);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Moving platform — Board
        // ─────────────────────────────────────────────────────────────────────

        private void TickBoardPlatform()
        {
            if (CheckTimeout(10f)) return;

            var platform = m_launcher?.source as MovingPlatform;
            if (platform == null) { SetState(AIState.WalkToCollectible); return; }

            float xzDist = Vector3.Distance(
                new Vector3(platform.transform.position.x, 0f, platform.transform.position.z),
                new Vector3(transform.position.x,          0f, transform.position.z));
            float hDiff = platform.transform.position.y - transform.position.y;

            // If platform drifted out of jump reach, back off and wait for it to return.
            if (hDiff > maxDirectJumpHeight || xzDist > platformBoardingRange * 2f)
            {
                BrainLog(AILogType.Info, "Platform moved out of reach — waiting again.");
                SetState(AIState.WaitForPlatform);
                return;
            }

            // Steer toward platform (XZ) and jump onto it when it's within reach.
            var toPlatform = Vector3.ProjectOnPlane(
                platform.transform.position - transform.position, Vector3.up);
            m_input.desiredMoveDirection = toPlatform.magnitude > 0.1f
                ? toPlatform.normalized : Vector3.zero;

            if (hDiff > 0.3f && hDiff <= maxDirectJumpHeight && m_self.isGrounded)
                QueueJump();

            // Boarded: bot is grounded and the platform surface is directly below.
            if (m_self.isGrounded && xzDist < platformBoardingRange
                && Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down,
                    out RaycastHit hit, 1.5f)
                && (hit.transform == platform.transform || hit.transform.IsChildOf(platform.transform)))
            {
                BrainLog(AILogType.Info, "Boarded platform.");
                SetState(AIState.RidePlatform);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Moving platform — Ride
        // ─────────────────────────────────────────────────────────────────────

        private void TickRidePlatform()
        {
            if (CheckTimeout(35f)) return;

            var toExit = Vector3.ProjectOnPlane(
                m_launcher.exitWaypoint.position - transform.position, Vector3.up);
            m_input.desiredMoveDirection = toExit.magnitude > 0.1f
                ? toExit.normalized : Vector3.zero;

            if (Vector3.Distance(transform.position, m_launcher.exitWaypoint.position) < arrivalRadius * 3f)
            {
                BrainLog(AILogType.Info, "Platform ride complete.");
                SetState(AIState.WalkToCollectible);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Navigation helper
        // ─────────────────────────────────────────────────────────────────────

        private void NavigateToward(Vector3 destination)
        {
            // ── Underwater: skip NavMesh; use 3-D direct steering ──────────────
            if (m_self.onWater)
            {
                var toTarget3D = destination - transform.position;
                float yDiff    = destination.y - transform.position.y;

                // Lateral steering (XZ plane)
                var flat   = Vector3.ProjectOnPlane(toTarget3D, Vector3.up);
                m_input.desiredMoveDirection = flat.magnitude > 0.1f ? flat.normalized : Vector3.zero;

                // Vertical swim inputs — disabled while holding an object
                m_input.diveHeld        = !m_self.holding && yDiff < -0.3f;
                m_input.swimUpwardHeld  = !m_self.holding && yDiff >  0.3f;

                Debug.Log($"[AIBot] Swim — yDiff={yDiff:F2} dive={m_input.diveHeld} up={m_input.swimUpwardHeld} dist={toTarget3D.magnitude:F1}");
                return;
            }

            // Not in water — ensure swim inputs are cleared
            m_input.diveHeld       = false;
            m_input.swimUpwardHeld = false;

            // Ledge hang — always try to climb
            if (IsLedgeHanging())
            {
                m_input.desiredMoveDirection = Vector3.forward;
                m_input.glideHeld = false;
                return;
            }

            var flatXZ = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);

            Vector3 moveDir;
            if (!m_self.isGrounded)
            {
                // Check whether a path exists from the current airborne position
                NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, m_navPath);
                bool pathInvalid = m_navPath.status == NavMeshPathStatus.PathInvalid
                                || m_navPath.corners.Length < 2;

                if (pathInvalid)
                {
                    float yDiff = transform.position.y - destination.y;

                    float horizDist = flatXZ.magnitude;
                    if (yDiff >= 0f)
                    {
                        // Only glide when the bot is high enough above the target that
                        // gliding is useful — more than a jump height above means we have
                        // real altitude to trade for horizontal distance.
                        // Release glide when directly above a breakable — let the bot fall and stomp it.
                        bool aboveBreakable = m_targetMode == TargetMode.Breakable
                            && m_targetBreakable != null
                            && horizDist < spinRange;
                        bool shouldGlide = !aboveBreakable && yDiff > maxDirectJumpHeight;
                        m_input.glideHeld = shouldGlide;
                        moveDir = horizDist > 0.1f ? flatXZ.normalized : Vector3.zero;
                        if (shouldGlide && !m_glideLogFired)
                        {
                            m_glideLogFired = true;
                            string tname = GetCurrentTargetName();
                            BrainLog(AILogType.Ability, $"Gliding toward '{tname}' (yDiff={yDiff:F2} horizDist={horizDist:F1})");
                            Debug.Log($"[AIBot] Gliding toward '{tname}' (yDiff={yDiff:F2} horizDist={horizDist:F1})");
                        }
                        else if (!shouldGlide)
                        {
                            m_glideLogFired = false; // reset so it logs if we go far again
                        }
                    }
                    else
                    {
                        // Still below target — close glider, steer back to nearest upward gravity field
                        m_input.glideHeld = false;
                        var fieldPos = NearestUprightGravityFieldPosition();
                        if (fieldPos.HasValue)
                        {
                            var toField = Vector3.ProjectOnPlane(fieldPos.Value - transform.position, Vector3.up);
                            moveDir = toField.magnitude > 0.1f ? toField.normalized : Vector3.zero;
                            BrainLog(AILogType.Movement, $"Airborne below target (yDiff={yDiff:F2}) — steering back to gravity field");
                            Debug.Log($"[AIBot] Airborne below target (yDiff={yDiff:F2}) — steering back to gravity field");
                        }
                        else
                        {
                            moveDir = flatXZ.magnitude > 0.1f ? flatXZ.normalized : Vector3.zero;
                        }
                    }
                }
                else
                {
                    m_input.glideHeld = false;
                    moveDir = flatXZ.magnitude > 0.1f ? flatXZ.normalized : Vector3.zero;
                }
            }
            else
            {
                // Grounded — close glider
                m_input.glideHeld = false;

                NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, m_navPath);
                m_lastNavStatus = m_navPath.status;

                float groundedHDiff = destination.y - transform.position.y;

                // Clear the walk-off latch once the bot is no longer grounded (fell off).
                if (!m_self.isGrounded)
                    m_walkOffEdgeDir = Vector3.zero;

                // When the bot is nearly directly above a below-target (xzDist < 1.5 m),
                // NavMesh corners route AROUND the platform instead of toward the edge.
                // Skip them and scan for the nearest physical drop-off instead.
                // Once an edge direction is found, latch it until the bot becomes airborne.
                bool directlyAbove = groundedHDiff < -1f && flatXZ.magnitude < 1.5f;
                bool hasLatchedDir  = m_walkOffEdgeDir.sqrMagnitude > 0.01f;

                if (groundedHDiff < -1f && (directlyAbove || hasLatchedDir))
                {
                    if (!hasLatchedDir)
                    {
                        m_walkOffEdgeDir = FindEdgeDirection(Vector3.zero);
                        m_lastNavDownLogTime = Time.time;
                        Debug.Log($"[AIBot] [NavDown] LATCH edge dir={m_walkOffEdgeDir:F2} (hDiff={groundedHDiff:F2} xzDist={flatXZ.magnitude:F2})");
                    }
                    else if (Time.time > m_lastNavDownLogTime + 1f)
                    {
                        m_lastNavDownLogTime = Time.time;
                        Debug.Log($"[AIBot] [NavDown] HOLDING latch dir={m_walkOffEdgeDir:F2} (hDiff={groundedHDiff:F2} xzDist={flatXZ.magnitude:F2})");
                    }
                    moveDir = m_walkOffEdgeDir;
                }
                else if (!directlyAbove && m_navPath.status != NavMeshPathStatus.PathInvalid
                    && m_navPath.corners.Length >= 2)
                {
                    var toCorner = Vector3.ProjectOnPlane(
                        m_navPath.corners[1] - transform.position, Vector3.up);
                    moveDir = toCorner.magnitude > 0.1f ? toCorner.normalized : flatXZ.normalized;

                    if (groundedHDiff < -1f && Time.time > m_lastNavDownLogTime + 1f)
                    {
                        m_lastNavDownLogTime = Time.time;
                        Debug.Log($"[AIBot] [NavDown] target BELOW (hDiff={groundedHDiff:F2}) navStatus={m_navPath.status} corners={m_navPath.corners.Length} corner1={m_navPath.corners[1]:F1} moveDir={moveDir:F2}");
                    }
                }
                else
                {
                    if (groundedHDiff < -1f)
                    {
                        moveDir = flatXZ.magnitude > 0.01f ? flatXZ.normalized : FindEdgeDirection(m_lastNonZeroMoveDir);
                        if (Time.time > m_lastNavDownLogTime + 1f)
                        {
                            m_lastNavDownLogTime = Time.time;
                            Debug.Log($"[AIBot] [NavDown] target BELOW (hDiff={groundedHDiff:F2}) navStatus={m_navPath.status} — flatXZ moveDir={moveDir:F2}");
                        }
                    }
                    else
                    {
                        moveDir = flatXZ.magnitude > 0.1f ? flatXZ.normalized : Vector3.zero;
                    }
                }
            }

            // Blend in hazard avoidance
            var avoid = ComputeHazardAvoidance();
            if (avoid.sqrMagnitude > 0.01f)
                moveDir = (moveDir + avoid * hazardAvoidStrength).normalized;

            // Compensate for conveyor belt if standing on one
            moveDir = ApplyConveyorCompensation(moveDir);

            if (moveDir.sqrMagnitude > 0.01f)
                m_lastNonZeroMoveDir = moveDir;

            m_input.desiredMoveDirection = moveDir;
            TryJump(moveDir, destination);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ForceField & SpeedBooster states
        // ─────────────────────────────────────────────────────────────────────

        private void TickUseForceField()
        {
            if (CheckTimeout(10f)) return;

            // Hold still horizontally — the field pushes the bot upward automatically
            m_input.desiredMoveDirection = Vector3.zero;

            // Determine required height from current target
            Vector3 targetPos = GetCurrentTargetPosition();
            float heightNeeded = targetPos.y;

            if (transform.position.y >= heightNeeded - 0.5f)
            {
                BrainLog(AILogType.Ability, $"ForceField lift complete at Y={transform.position.y:F1}, heading to target.");
                SetState(AIState.WalkToCollectible);
            }
        }

        private void TickUseSpeedBooster()
        {
            // The boost trigger already fired when the bot walked through the collider.
            // Just transition immediately — NavigateToward handles the airborne phase.
            BrainLog(AILogType.Ability, "SpeedBooster used — resuming WalkToCollectible.");
            SetState(AIState.WalkToCollectible);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Abilities — run every frame, independent of FSM state
        // ─────────────────────────────────────────────────────────────────────

        private void HandleAbilities()
        {
            HandleSpin();
            HandleStomp();
            HandleDash();
            HandleAirDive();
            HandleCrouch();
            HandleRoll();
            HandlePickAndDrop();
            HandleReleaseLedge();
            HandleFallingPlatform();
        }

        /// <summary>Spin attack when an enemy or breakable is within melee range.</summary>
        private void HandleSpin()
        {
            if (m_self.holding) return;
            if (Time.time < m_nextSpinTime) return;

            foreach (var e in m_enemies)
            {
                if (e == null || !e.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(transform.position, e.transform.position) <= spinRange)
                {
                    m_nextSpinTime = Time.time + Vary(spinCooldown);
                    m_input.QueueDelayedAction(() => m_input.spinQueued = true);
                    BrainLog(AILogType.Ability, $"Spin attack on enemy '{e.name}'");
                    Debug.Log($"[AIBot] Spin attack on enemy '{e.name}'");
                    return;
                }
            }

            foreach (var b in m_breakables)
            {
                if (b == null || !b.gameObject.activeInHierarchy || b.broken) continue;
                float dist3D   = Vector3.Distance(transform.position, b.transform.position);
                float hDiff    = b.transform.position.y - transform.position.y;
                float distXZ   = Vector3.Distance(
                    new Vector3(transform.position.x, 0, transform.position.z),
                    new Vector3(b.transform.position.x, 0, b.transform.position.z));
                if (dist3D <= spinRange)
                {
                    // Bot is on a ledge above the crate — spin hitbox won't reach; jump off the ledge instead
                    if (hDiff < -0.3f && m_self.isGrounded && distXZ < spinRange)
                    {
                        var towardCrate = Vector3.ProjectOnPlane(
                            b.transform.position - transform.position, Vector3.up).normalized;
                        m_input.desiredMoveDirection = towardCrate;
                        QueueJump();
                        Debug.Log($"[AIBot] [SPIN] bot above crate hDiff={hDiff:F2} — jumping toward crate to fall to its level");
                        return;
                    }

                    bool isCurrentTarget = m_targetMode == TargetMode.Breakable && m_targetBreakable == b;
                    string tag = isCurrentTarget ? "Spinning TARGET crate" : "Spinning BONUS crate";
                    BrainLog(AILogType.Ability, $"{tag} '{b.name}'");
                    m_nextSpinTime = Time.time + Vary(spinCooldown);
                    m_input.QueueDelayedAction(() => m_input.spinQueued = true);
                    return;
                }
            }
        }

        /// <summary>Stomp when airborne and an enemy, breakable, or stomp-panel is directly below.</summary>
        private void HandleStomp()
        {
            if (m_self.holding) return;
            if (m_self.isGrounded) return;
            if (m_self.verticalVelocity.y > 0f) return; // still rising

            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, stompDetectRange,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                bool isEnemy    = hit.collider.GetComponent<Enemy>() != null;
                bool isBreakable = hit.collider.GetComponent<Breakable>() != null;
                bool isPanel    = hit.collider.TryGetComponent<Panel>(out var panel) && panel.requireStomp && !panel.activated;

                if (isEnemy || isBreakable || isPanel)
                {
                    m_input.QueueDelayedAction(() => m_input.stompQueued = true);
                    string reason;
                    if (isEnemy)   reason = $"enemy '{hit.collider.name}'";
                    else if (isPanel) reason = $"panel '{hit.collider.name}'";
                    else
                    {
                        var br = hit.collider.GetComponent<Breakable>();
                        bool isCurrentTarget = m_targetMode == TargetMode.Breakable && m_targetBreakable == br;
                        reason = isCurrentTarget
                            ? $"[HIT] TARGET crate '{hit.collider.name}'"
                            : $"[HIT] BONUS crate '{hit.collider.name}'";
                    }
                    BrainLog(AILogType.Ability, $"Stomp on {reason}");
                    Debug.Log($"[AIBot] Stomp on {reason}");
                }
            }
        }

        /// <summary>Dash when grounded and the target is far away.</summary>
        private void HandleDash()
        {
            if (Time.time < m_nextDashTime) return;
            if (!m_self.isGrounded) return;
            if (m_target == null) return;

            float dist = Vector3.Distance(transform.position, m_target.transform.position);
            if (dist < dashMinDistance) return;

            m_nextDashTime = Time.time + Vary(dashCooldown);
            m_input.QueueDelayedAction(() => m_input.dashQueued = true);
            string dtarget = GetCurrentTargetName();
            BrainLog(AILogType.Ability, $"Dash toward '{dtarget}' (dist={dist:F1})");
            Debug.Log($"[AIBot] Dash toward '{dtarget}' (dist={dist:F1})");
        }

        /// <summary>Air dive when well above target and not gliding.</summary>
        private void HandleAirDive()
        {
            if (m_self.holding) return;
            if (m_self.isGrounded) return;
            if (m_input.glideHeld) return;
            if (m_target == null) return;

            float yDiff = transform.position.y - m_target.transform.position.y;
            if (yDiff >= airDiveMinYDiff)
            {
                m_input.airDiveQueued = true;
                if (Time.time > m_lastAbilityLogTime + k_abilityLogInterval)
                {
                    m_lastAbilityLogTime = Time.time;
                    string atarget = GetCurrentTargetName();
                    BrainLog(AILogType.Ability, $"Air dive toward '{atarget}' (yDiff={yDiff:F1})");
                    Debug.Log($"[AIBot] Air dive toward '{atarget}' (yDiff={yDiff:F1})");
                }
            }
        }

        /// <summary>Hold crouch when a low ceiling is detected overhead.</summary>
        private void HandleCrouch()
        {
            if (m_self.holding) { m_input.crouchHeld = false; return; }
            m_input.crouchHeld = Physics.Raycast(transform.position, transform.up,
                crouchCeilHeight, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Hold roll when on flat ground and far from target for a speed boost.</summary>
        private void HandleRoll()
        {
            if (m_self.holding) { m_input.rollHeld = false; m_rollLogActive = false; return; }
            if (m_target == null) { m_input.rollHeld = false; m_rollLogActive = false; return; }
            float dist = Vector3.Distance(transform.position, m_target.transform.position);
            bool shouldRoll = m_self.isGrounded && dist >= rollMinDistance;

            if (shouldRoll && !m_rollLogActive)
            {
                m_rollLogActive = true;
                string rtarget = GetCurrentTargetName();
                BrainLog(AILogType.Ability, $"Rolling toward '{rtarget}' (dist={dist:F1})");
                Debug.Log($"[AIBot] Rolling toward '{rtarget}' (dist={dist:F1})");
            }
            else if (!shouldRoll)
            {
                m_rollLogActive = false;
            }

            m_input.rollHeld = shouldRoll;
        }

        /// <summary>Pick up a nearby pickable; throw it when an enemy is close.</summary>
        private void HandlePickAndDrop()
        {
            if (m_self.holding)
            {
                // Already carrying — throw toward nearest enemy
                foreach (var e in m_enemies)
                {
                    if (e == null || !e.gameObject.activeInHierarchy) continue;
                    if (Vector3.Distance(transform.position, e.transform.position) <= spinRange * 2f)
                    {
                        m_input.pickAndDropQueued = true;
                        return;
                    }
                }
                return;
            }

            foreach (var p in m_pickables)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(transform.position, p.transform.position) <= pickupRange)
                {
                    m_input.pickAndDropQueued = true;
                    return;
                }
            }
        }

        /// <summary>Jump off a falling platform as soon as it starts shaking.</summary>
        private void HandleFallingPlatform()
        {
            if (!m_self.isGrounded) return;

            // Raycast down to find what the bot is standing on
            if (!Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 1.5f,
                    ~0, QueryTriggerInteraction.Ignore)) return;

            if (!hit.collider.TryGetComponent<FallingPlatform>(out var fp)) return;

            if (fp.activated)
            {
                // Platform is counting down — jump off immediately
                BrainLog(AILogType.Ability, "Standing on falling platform — jumping off.");
                m_input.jumpQueued = true;
            }
        }

        /// <summary>Release ledge if the bot has been hanging for too long.</summary>
        private void HandleReleaseLedge()
        {
            bool hanging = m_self.states.IsCurrentOfType(typeof(LedgeHangingPlayerState));

            if (!hanging)
            {
                m_ledgeHangStartTime = -1f;
                return;
            }

            if (m_ledgeHangStartTime < 0f)
                m_ledgeHangStartTime = Time.time;

            if (Time.time - m_ledgeHangStartTime >= ledgeReleaseTimeout)
            {
                m_input.releaseLedgeQueued = true;
                m_ledgeHangStartTime = -1f;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // FSM helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SetState(AIState newState)
        {
            if (newState != CurrentState)
                BrainLog(AILogType.StateChange, $"{CurrentState} → {newState}");

            CurrentState        = newState;
            m_stateStartTime    = Time.time;
            m_lastProgressTime  = Time.time;
            m_lastStallCheckPos = transform.position;
            m_springLaunched    = false;
            m_glideLogFired     = false;
            m_rollLogActive     = false;
            m_walkOffEdgeDir    = Vector3.zero;

            if (newState == AIState.Idle)
            {
                m_input.desiredMoveDirection = Vector3.zero;
                m_nextIdleScanTime = 0f; // scan on the very next frame
            }
        }

        private void CheckTargetCollected()
        {
            if (CurrentState == AIState.Idle) return;

            // ── Bonus broken-crate scan ─────────────────────────────────────────
            // If a BONUS crate was shattered by a spin (not the current target),
            // its coins are live but nothing is chasing them. Pivot to the nearest
            // one — unless we're already mid-chase on a bouncy coin (don't interrupt).
            bool alreadyChasingBouncyCoin = m_targetMode == TargetMode.Collectible
                && m_target != null
                && m_target.GetComponent<CollectiblePhysics>() != null;

            if (!alreadyChasingBouncyCoin)
            {
                Collectible bonusNearest = null;
                float       bonusDist   = float.MaxValue;
                foreach (var br in m_breakables)
                {
                    if (br == null || br.collectibles == null) continue;
                    if (!br.broken) continue;
                    // Skip the current Breakable target — its pivot is handled below.
                    if (br == m_targetBreakable) continue;
                    foreach (var c in br.collectibles)
                    {
                        if (c == null) continue;
                        if (c.gameObject.activeSelf
                            && c.triggerCollider != null
                            && c.triggerCollider.enabled)
                        {
                            float d = Vector3.Distance(transform.position, c.transform.position);
                            if (d < bonusDist) { bonusDist = d; bonusNearest = c; }
                        }
                    }
                }

                if (bonusNearest != null)
                {
                    BrainLog(AILogType.TargetChange,
                        $"[HIT] Bonus crate dropped '{bonusNearest.name}' ({bonusDist:F1}m) — pivoting to collect it.");
                    Debug.Log($"[AIBot] [HIT] Bonus crate dropped '{bonusNearest.name}' ({bonusDist:F1}m) — pivoting.");
                    m_target          = bonusNearest;
                    m_targetItemBox   = null;
                    m_targetBreakable = null;
                    m_targetMode      = TargetMode.Collectible;
                    m_stateStartTime  = Time.time;
                    m_lastProgressTime = Time.time;
                    return;
                }
            }

            bool done = false;
            string doneName = "?";

            switch (m_targetMode)
            {
                case TargetMode.ItemBox:
                    doneName = m_targetItemBox != null ? m_targetItemBox.name : "ItemBox";
                    if (m_targetItemBox == null || !m_targetItemBox.gameObject.activeInHierarchy)
                    {
                        done = true;
                    }
                    else
                    {
                        // Check first whether the box just dispensed a live collectible.
                        // CheckTargetCollected runs before TickWalkToCollectible every frame,
                        // so the pivot must live here — otherwise allDispensed fires first
                        // and sends the bot to Idle before the pivot can happen.
                        bool pivoted = false;
                        foreach (var c in m_targetItemBox.collectibles)
                        {
                            if (c == null) continue;
                            if (c.gameObject.activeSelf
                                && c.collectOnContact
                                && c.triggerCollider != null
                                && c.triggerCollider.enabled)
                            {
                                BrainLog(AILogType.TargetChange,
                                    $"ItemBox '{m_targetItemBox.name}' dispensed '{c.name}' — pivoting to collect it.");
                                Debug.Log($"[AIBot] ItemBox dispensed '{c.name}' — pivoting.");
                                m_target           = c;
                                m_targetItemBox    = null;
                                m_targetBreakable  = null;
                                m_targetMode       = TargetMode.Collectible;
                                // Reset timeout so the bot gets a full budget to reach the bouncing coin.
                                m_stateStartTime   = Time.time;
                                m_lastProgressTime = Time.time;
                                pivoted            = true;
                                break;
                            }
                        }

                        if (!pivoted)
                        {
                            // Box is truly empty when every slot has been dispensed
                            // (SetActive(true) on each) and none are live any more.
                            bool allDispensed = true;
                            foreach (var c in m_targetItemBox.collectibles)
                            {
                                if (c != null && !c.gameObject.activeSelf) { allDispensed = false; break; }
                            }
                            done = allDispensed;
                        }
                    }
                    break;
                case TargetMode.Breakable:
                    doneName = m_targetBreakable != null ? m_targetBreakable.name : "Breakable";
                    if (m_targetBreakable == null || !m_targetBreakable.gameObject.activeInHierarchy)
                    {
                        done = true;
                    }
                    else if (m_targetBreakable.broken)
                    {
                        BrainLog(AILogType.Info, $"[HIT] Crate '{m_targetBreakable.name}' broke — scanning for coins.");
                        Debug.Log($"[AIBot] [HIT] Crate '{m_targetBreakable.name}' broke — scanning for coins.");
                        // The targeted crate broke. Also check ALL broken crates (the bot's
                        // spin range may have shattered multiple crates simultaneously).
                        // Pick the nearest live coin from any broken crate.
                        Collectible nearest     = null;
                        float       nearestDist = float.MaxValue;
                        foreach (var br in m_breakables)
                        {
                            if (br == null || br.collectibles == null) continue;
                            if (!br.broken) continue;
                            foreach (var c in br.collectibles)
                            {
                                if (c == null) continue;
                                if (c.gameObject.activeSelf
                                    && c.triggerCollider != null
                                    && c.triggerCollider.enabled)
                                {
                                    float d = Vector3.Distance(transform.position, c.transform.position);
                                    if (d < nearestDist) { nearestDist = d; nearest = c; }
                                }
                            }
                        }

                        if (nearest != null)
                        {
                            BrainLog(AILogType.TargetChange,
                                $"Crate(s) broke — pivoting to nearest coin '{nearest.name}' ({nearestDist:F1}m).");
                            Debug.Log($"[AIBot] Crate(s) broke — pivoting to nearest coin '{nearest.name}' ({nearestDist:F1}m).");
                            m_target          = nearest;
                            m_targetItemBox   = null;
                            m_targetBreakable = null;
                            m_targetMode      = TargetMode.Collectible;
                            // Reset timeout so the bot gets a full budget to reach the bouncing coin.
                            m_stateStartTime  = Time.time;
                            m_lastProgressTime = Time.time;
                        }
                        else
                        {
                            BrainLog(AILogType.Warning, $"Crate(s) broke but no live coins found — going Idle.");
                            Debug.LogWarning($"[AIBot] Crate(s) broke but no live coins found.");
                            done = true;
                        }
                    }
                    break;
                default:
                    if (m_target == null) return;
                    doneName = m_target.name;
                    if (!m_target.gameObject.activeInHierarchy)
                    {
                        done = true;
                        BrainLog(AILogType.Info, $"[COLLECT] '{doneName}' deactivated (collected or removed).");
                        Debug.Log($"[AIBot] [COLLECT] '{doneName}' collected/removed.");
                    }
                    else if (m_target.triggerCollider != null && !m_target.triggerCollider.enabled)
                    {
                        done = true;
                        // Distinguish: if display is also hidden, it expired; if display still on, it was collected.
                        bool displayActive = m_target.display != null && m_target.display.activeSelf;
                        if (displayActive)
                        {
                            BrainLog(AILogType.Warning, $"[COLLECT] '{doneName}' trigger disabled but display active — likely just collected.");
                            Debug.Log($"[AIBot] [COLLECT] '{doneName}' collected (trigger off, display on).");
                        }
                        else
                        {
                            BrainLog(AILogType.Warning, $"[COLLECT] '{doneName}' expired before we reached it.");
                            Debug.LogWarning($"[AIBot] [COLLECT] '{doneName}' EXPIRED — bot did not collect it in time.");
                        }
                    }
                    break;
            }

            if (done)
            {
                BrainLog(AILogType.Info, $"'{doneName}' done — returning to Idle.");
                m_target          = null;
                m_targetItemBox   = null;
                m_targetBreakable = null;
                m_launcher        = null;
                SetState(AIState.Idle);
            }
        }

        /// <returns>true if timed out (state already set to Idle)</returns>
        private bool CheckTimeout(float timeout)
        {
            if (Time.time - m_stateStartTime <= timeout) return false;
            string tname = GetCurrentTargetName();

            bool targetIsBouncy = m_target != null && m_target.GetComponent<CollectiblePhysics>() != null;
            if (targetIsBouncy)
            {
                BrainLog(AILogType.Warning, $"{CurrentState} timed out on bouncy coin '{tname}' — rescan without blacklisting.");
                Debug.LogWarning($"[AIBot] {CurrentState} timed out on bouncy coin '{tname}' — rescan without blacklisting.");
            }
            else
            {
                BrainLog(AILogType.Warning, $"{CurrentState} timed out ({timeout}s) on '{tname}' — going Idle.");
                Debug.LogWarning($"[AIBot] {CurrentState} timed out on '{tname}'");
                if (m_target != null) m_blacklist[m_target] = Time.time;
            }

            if (m_targetItemBox   != null) m_itemBoxBlacklist[m_targetItemBox]       = Time.time;
            if (m_targetBreakable != null) m_breakableBlacklist[m_targetBreakable]   = Time.time;
            if (m_launcher        != null) m_launcherBlacklist[m_launcher]           = Time.time;
            m_target          = null;
            m_targetItemBox   = null;
            m_targetBreakable = null;
            m_launcher        = null;
            SetState(AIState.Idle);
            return true;
        }

        /// <returns>true if stalled (state already set to Idle)</returns>
        private bool CheckStall()
        {
            float moved = Vector3.Distance(transform.position, m_lastStallCheckPos);
            if (moved > 0.3f)
            {
                m_lastStallCheckPos = transform.position;
                m_lastProgressTime  = Time.time;
                return false;
            }

            // If we're right at the target, don't treat stillness as a stall
            if (m_target != null &&
                Vector3.Distance(transform.position, m_target.transform.position) < arrivalRadius)
            {
                m_lastProgressTime = Time.time;
                return false;
            }
            if (m_targetItemBox != null &&
                Vector3.Distance(transform.position, m_targetItemBox.transform.position) < arrivalRadius)
            {
                m_lastProgressTime = Time.time;
                return false;
            }
            if (m_targetBreakable != null &&
                Vector3.Distance(transform.position, m_targetBreakable.transform.position) < arrivalRadius)
            {
                m_lastProgressTime = Time.time;
                return false;
            }

            if (Time.time - m_lastProgressTime <= stallTimeout) return false;

            string tname = GetCurrentTargetName();

            // Bouncy physics collectibles (CollectiblePhysics) have a short ~5s lifetime.
            // Blacklisting them for 20s means the bot can never retry them before they expire.
            // Instead, just clear the target and go to Idle so the next scan can re-evaluate.
            bool targetIsBouncy = m_target != null && m_target.GetComponent<CollectiblePhysics>() != null;

            if (targetIsBouncy)
            {
                BrainLog(AILogType.Warning, $"Stalled on bouncy coin '{tname}' — rescan without blacklisting.");
                Debug.LogWarning($"[AIBot] Stalled on bouncy coin '{tname}' — rescan without blacklisting.");
            }
            else
            {
                BrainLog(AILogType.Warning, $"Stalled on '{tname}' — blacklisting.");
                Debug.LogWarning($"[AIBot] Stalled on '{tname}'");
                if (m_target        != null) m_blacklist[m_target]                     = Time.time;
            }

            if (m_targetItemBox   != null) m_itemBoxBlacklist[m_targetItemBox]       = Time.time;
            if (m_targetBreakable != null) m_breakableBlacklist[m_targetBreakable]   = Time.time;
            if (m_launcher        != null) m_launcherBlacklist[m_launcher]           = Time.time;
            m_target          = null;
            m_targetItemBox   = null;
            m_targetBreakable = null;
            m_launcher        = null;
            SetState(AIState.Idle);
            return true;
        }

        private AIState LauncherStateFor(LauncherType type) => type switch
        {
            LauncherType.Spring         => AIState.UseSpring,
            LauncherType.Pole           => AIState.ClimbPole,
            LauncherType.Rail           => AIState.UseRail,
            LauncherType.Portal         => AIState.UsePortal,
            LauncherType.MovingPlatform => AIState.WaitForPlatform,
            LauncherType.ForceField     => AIState.UseForceField,
            LauncherType.SpeedBooster   => AIState.UseSpeedBooster,
            _                           => AIState.WalkToCollectible,
        };

        // ─────────────────────────────────────────────────────────────────────
        // Jump logic
        // ─────────────────────────────────────────────────────────────────────

        private void TryJump(Vector3 moveDir, Vector3 target)
        {
            if (Time.time < m_nextJumpTime)   return;
            if (IsPoleClimbing())             return;
            if (IsLedgeHanging()) { QueueJump(); return; }
            if (IsWallDragging()) { QueueJump(); return; }
            if (!m_self.isGrounded)           return;

            float heightDiff = target.y - transform.position.y;
            float horizDist  = Vector3.ProjectOnPlane(target - transform.position, Vector3.up).magnitude;


            // Proximity jump — fires even when standing still (bot may be directly below target).
            if (heightDiff > 0.3f && heightDiff <= maxDirectJumpHeight
                && horizDist < jumpProximityRange)
            {
                var toTarget    = (target - transform.position).normalized;
                var rayOrigin   = transform.position + Vector3.up * 0.5f;
                float rayDist   = Mathf.Max(horizDist, 0.5f);
                bool wallAbove  = Physics.Raycast(rayOrigin, toTarget, rayDist,
                                      obstacleLayer, QueryTriggerInteraction.Ignore);

                if (wallAbove)
                {
                    BrainLog(AILogType.Movement, $"Proximity jump suppressed — wall blocking path to target (hDiff={heightDiff:F2})");
                    Debug.Log($"[AIBot] [TryJump] Proximity jump SUPPRESSED — wall blocking (hDiff={heightDiff:F2} horizDist={horizDist:F2})");
                    return;
                }

                BrainLog(AILogType.Ability, $"Proximity jump — hDiff={heightDiff:F2} horizDist={horizDist:F2}");
                Debug.Log($"[AIBot] [TryJump] PROXIMITY JUMP fired (hDiff={heightDiff:F2} horizDist={horizDist:F2})");
                QueueJump(); return;
            }

            if (moveDir.sqrMagnitude < 0.01f)
            {
                Debug.Log($"[AIBot] [TryJump] skipped — moveDir is zero");
                return;
            }

            // Two-ray wall jump — runs regardless of NavMesh status.
            // Low ray at feet detects a physical wall; high ray checks clearance above it.
            // If low is blocked AND high is clear, the bot can jump over the obstacle.
            bool lowBlocked = Physics.Raycast(
                transform.position + Vector3.up * 0.15f, moveDir,
                obstacleCheckDistance, obstacleLayer, QueryTriggerInteraction.Ignore);
            bool highBlocked = Physics.Raycast(
                transform.position + Vector3.up * wallJumpClearHeight, moveDir,
                obstacleCheckDistance, obstacleLayer, QueryTriggerInteraction.Ignore);

            if (lowBlocked && !highBlocked && heightDiff > -0.5f)
            {
                BrainLog(AILogType.Ability, $"Wall jump — low ray blocked, high ray clear (hDiff={heightDiff:F2})");
                Debug.Log($"[AIBot] [TryJump] WALL JUMP — low blocked, high clear (hDiff={heightDiff:F2})");
                QueueJump();
                return;
            }

            bool blocked = m_lastNavStatus == NavMeshPathStatus.PathInvalid
                        || (m_lastNavStatus == NavMeshPathStatus.PathPartial && heightDiff > 0.5f);
            if (!blocked)
            {
                Debug.Log($"[AIBot] [TryJump] skipped — path not blocked (navStatus={m_lastNavStatus})");
                return;
            }

            // Case A — wall directly ahead: jump over it.
            // Suppress when target is well below — bot should walk off the edge and drop.
            bool caseAWall = Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDir,
                obstacleCheckDistance, obstacleLayer, QueryTriggerInteraction.Ignore);
            if (caseAWall)
            {
                if (heightDiff > -0.5f)
                {
                    BrainLog(AILogType.Ability, $"Case A jump — wall ahead (hDiff={heightDiff:F2})");
                    Debug.Log($"[AIBot] [TryJump] CASE A JUMP — wall ahead (hDiff={heightDiff:F2})");
                    QueueJump();
                    return;
                }
                else
                {
                    Debug.Log($"[AIBot] [TryJump] Case A SUPPRESSED — wall ahead but target is below (hDiff={heightDiff:F2})");
                }
            }

            // Case B — gap ahead: ground disappears within one step.
            // Do NOT jump if the target is below us — just walk off the edge and fall.
            var probeOrigin  = transform.position + moveDir * 0.9f + Vector3.up * 0.1f;
            bool noGroundAhead = !Physics.Raycast(probeOrigin, Vector3.down, 1.2f,
                ~0, QueryTriggerInteraction.Ignore);
            if (noGroundAhead)
            {
                if (heightDiff >= 0f)
                {
                    Debug.Log($"[AIBot] [TryJump] CASE B JUMP — gap ahead (hDiff={heightDiff:F2})");
                    QueueJump();
                }
                else
                {
                    Debug.Log($"[AIBot] [TryJump] Case B SUPPRESSED — gap ahead but target is below, walking off (hDiff={heightDiff:F2})");
                }
            }
            else
            {
                Debug.Log($"[AIBot] [TryJump] No case fired — no wall, no gap (hDiff={heightDiff:F2} caseAWall={caseAWall} noGroundAhead={noGroundAhead})");
            }
        }

        /// <summary>
        /// Probes 8 directions around the bot and returns the edge direction closest
        /// to <paramref name="preferredDir"/> — used when the bot is directly above
        /// its target and needs to walk off the platform to fall down.
        /// </summary>
        /// <summary>
        /// Probes 8 directions and returns the one that leads to a valid drop-off:
        /// no near ground (≤1.5 m) confirming the edge, but floor exists within 8 m
        /// confirming a landing spot. Wall-adjacent directions are rejected because a
        /// downward probe inside wall geometry never finds a landing floor.
        /// </summary>
        private Vector3 FindEdgeDirection(Vector3 preferredDir)
        {
            const float nearDist = 1.5f;   // below this → still on platform surface
            const float farDist  = 8f;     // landing floor must exist within this range

            var dirs = new Vector3[8];
            for (int i = 0; i < 8; i++)
                dirs[i] = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;

            bool hasPreferred = preferredDir.sqrMagnitude > 0.01f;
            if (hasPreferred)
                System.Array.Sort(dirs, (a, b) =>
                    Vector3.Dot(b, preferredDir).CompareTo(Vector3.Dot(a, preferredDir)));

            // First pass: edge with valid landing, no wall blocking path.
            // nearHit=false → at the drop-off; farHit=true → floor below to land on.
            // wallAhead=true → direction is into a wall → skip.
            var sb = new System.Text.StringBuilder();
            sb.Append("[AIBot] [NavDown] FindEdgeDirection scan:");

            for (int step = 1; step <= 3; step++)
            {
                float probeDist = step * 0.8f;
                foreach (var dir in dirs)
                {
                    // Reject directions where a wall blocks the path at body height.
                    bool wallAhead = Physics.Raycast(
                        transform.position + Vector3.up * 0.5f, dir, probeDist + 0.3f,
                        obstacleLayer, QueryTriggerInteraction.Ignore);

                    var probe    = transform.position + dir * probeDist + Vector3.up * 0.1f;
                    bool nearHit = Physics.Raycast(probe, Vector3.down, nearDist, ~0, QueryTriggerInteraction.Ignore);
                    bool farHit  = Physics.Raycast(probe, Vector3.down, farDist,  ~0, QueryTriggerInteraction.Ignore);
                    float ang    = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);

                    sb.Append($"\n  angle={ang:F0} dist={probeDist:F1} wallAhead={wallAhead} nearHit={nearHit} farHit={farHit}");

                    if (!wallAhead && !nearHit && farHit)
                    {
                        sb.Append("  ← CHOSEN");
                        Debug.Log(sb.ToString());
                        return dir;
                    }
                }
            }

            // Fallback: no wall ahead, any edge (even without confirmed landing)
            foreach (var dir in dirs)
            {
                bool wallAhead = Physics.Raycast(
                    transform.position + Vector3.up * 0.5f, dir, 1.4f,
                    obstacleLayer, QueryTriggerInteraction.Ignore);
                if (wallAhead) continue;

                var probe = transform.position + dir * 0.8f + Vector3.up * 0.1f;
                bool nearHit = Physics.Raycast(probe, Vector3.down, nearDist, ~0, QueryTriggerInteraction.Ignore);
                if (!nearHit)
                {
                    float ang = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
                    sb.Append($"\n  FALLBACK angle={ang:F0}");
                    Debug.Log(sb.ToString());
                    return dir;
                }
            }

            sb.Append("\n  no edge found — using forward");
            Debug.Log(sb.ToString());
            return transform.forward;
        }

        private void QueueJump()
        {
            // Cooldown set immediately so TryJump doesn't double-queue.
            m_nextJumpTime = Time.time + Vary(jumpCooldown);
            m_input.QueueDelayedAction(() => m_input.jumpQueued = true);
        }

        /// <summary>Adds ±m_skillVariance random jitter to a cooldown value.</summary>
        private float Vary(float cooldown) =>
            cooldown + Random.Range(-m_skillVariance, m_skillVariance);

        // ─────────────────────────────────────────────────────────────────────
        // Scoring
        // ─────────────────────────────────────────────────────────────────────

        private float ScoreLauncherRoute(Collectible c, float value, LauncherInfo launcher)
        {
            // ── Reachability check from the launcher exit ────────────────────────
            var exitStatus = QueryPathStatus(launcher.exitPoint, c.transform.position);
            if (exitStatus == NavMeshPathStatus.PathInvalid)
            {
                bool isSpring = launcher.type == LauncherType.Spring && launcher.apexPoint != Vector3.zero;

                if (isSpring)
                {
                    // Springs go straight up; horizontal travel is only from aerial
                    // steering. Cap the effective reach at a realistic arc radius
                    // (half the apex height, not launcherExitReach which is too generous).
                    float arcRadius = launcher.apexPoint.y * 0.5f;
                    float hDist = Vector3.Distance(
                        new Vector3(launcher.entryPoint.x, 0, launcher.entryPoint.z),
                        new Vector3(c.transform.position.x, 0, c.transform.position.z));
                    float hDiff = c.transform.position.y - launcher.apexPoint.y;

                    // Target must be below the apex and within the aerial arc radius.
                    if (hDiff > 0f || hDist > arcRadius)
                        return float.MinValue;
                }
                else
                {
                    float refY    = launcher.exitPoint.y;
                    float hDiff   = c.transform.position.y - refY;
                    float hDist   = Vector3.Distance(
                        new Vector3(launcher.exitPoint.x, 0, launcher.exitPoint.z),
                        new Vector3(c.transform.position.x, 0, c.transform.position.z));

                    if (hDiff > maxDirectJumpHeight || hDist > launcherExitReach)
                        return float.MinValue;
                }
            }

            // ── Entry cost ───────────────────────────────────────────────────────
            var entryStatus = QueryPathStatus(transform.position, launcher.entryPoint);
            float entryPenalty = entryStatus switch
            {
                NavMeshPathStatus.PathComplete => 0f,
                NavMeshPathStatus.PathPartial  => 5f,
                _                              => 30f,
            };

            float costEntry  = Vector3.Distance(transform.position, launcher.entryPoint) + entryPenalty;
            float costExit   = Vector3.Distance(launcher.exitPoint, c.transform.position) * 1.2f;
            float costLaunch = GetLauncherCost(launcher);
            float totalCost  = costEntry + costLaunch + costExit + 0.1f;
            // Tactical mode: heavy surcharge makes launchers unattractive vs. direct walks.
            if (goalMode == AIGoalMode.Tactical) totalCost *= 2.5f;

            // ── Reliability discount ─────────────────────────────────────────────
            // Launcher routes are more complex to execute than a plain walk.
            // Apply a per-type discount so a walk wins when scores are close.
            float reliability = launcher.type switch
            {
                LauncherType.Spring         => 0.80f, // spring must be landed on precisely
                LauncherType.Pole           => 0.90f,
                LauncherType.Rail           => 0.90f,
                LauncherType.Portal         => 0.95f, // portals are nearly automatic
                LauncherType.MovingPlatform => 0.75f, // timing-sensitive
                LauncherType.ForceField     => 0.85f, // reliable but slow
                LauncherType.SpeedBooster   => 0.90f, // fast and reliable
                _                           => 0.85f,
            };

            float launchScore = (value / totalCost) * reliability;
            if (goalMode == AIGoalMode.Rival) launchScore *= RivalBoost(c.transform.position);
            return launchScore;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Player state helpers
        // ─────────────────────────────────────────────────────────────────────

        private bool IsWallDragging() => m_self.states?.current is WallDragPlayerState;
        private bool IsPoleClimbing() => m_self.states?.current is PoleClimbingPlayerState;
        private bool IsLedgeHanging() => m_self.states?.current is LedgeHangingPlayerState;
        private bool IsRailGrinding() => m_self.states?.current is RailGrindPlayerState;

        // ─────────────────────────────────────────────────────────────────────
        // Value / cost helpers
        // ─────────────────────────────────────────────────────────────────────

        private float GetCollectibleValue(Collectible c)
        {
            string n = c.name.ToLower();
            if (n.Contains("star"))     return starValue;
            if (n.Contains("heart"))    return heartValue;
            if (n.Contains("life"))     return lifeValue;
            if (n.Contains("red"))      return redCoinValue;
            if (n.Contains("blue"))     return blueCoinValue;
            if (n.Contains("item box")) return itemBoxValue;
            return coinValue;
        }

        private float GetLauncherCost(LauncherInfo l) => l.type switch
        {
            LauncherType.Spring         => 5f,
            LauncherType.Pole           => 8f,
            LauncherType.Rail           => 4f,
            LauncherType.Portal         => 2f,
            LauncherType.MovingPlatform => 15f,
            LauncherType.ForceField     => 6f,   // takes time to rise
            LauncherType.SpeedBooster   => 1f,   // instant, nearly free
            _                           => 10f,
        };

        /// <summary>
        /// Applies goal-mode scaling to the base collectible value before scoring.
        /// Survivor boosts hearts/lives when health is low.
        /// Greedy doubles all values.
        /// Sprinter returns 1 for everything (pure proximity).
        /// All other modes return the base value unchanged.
        /// </summary>
        private float AdjustedValue(Collectible c, float baseValue)
        {
            switch (goalMode)
            {
                case AIGoalMode.Survivor:
                {
                    if (m_self.health == null) return baseValue;
                    bool isHeart = c.name.ToLower().Contains("heart");
                    bool isLife  = c.name.ToLower().Contains("life");
                    int  hp    = m_self.health.current;
                    int  maxHp = m_self.health.max;
                    if (hp <= 1)
                        return isHeart ? baseValue * 4f : isLife ? baseValue * 3f : baseValue * 0.7f;
                    if (hp <= maxHp / 2)
                        return isHeart ? baseValue * 2f : isLife ? baseValue * 1.5f : baseValue;
                    return baseValue;
                }
                case AIGoalMode.Greedy:   return baseValue * 2.0f;
                case AIGoalMode.Sprinter: return 1f;
                default:                  return baseValue;
            }
        }

        /// <summary>
        /// Returns a score multiplier [1, 3] for Rival mode.
        /// Ratio > 1 when a rival is closer to the item than the bot —
        /// the bot boosts priority to deny high-opportunity items.
        /// </summary>
        private float RivalBoost(Vector3 collectiblePos)
        {
            if (m_rivals.Length == 0) return 1f;
            float botDist     = Vector3.Distance(transform.position, collectiblePos);
            float nearestRival = float.MaxValue;
            foreach (var rival in m_rivals)
            {
                if (rival == null) continue;
                float d = Vector3.Distance(rival.transform.position, collectiblePos);
                if (d < nearestRival) nearestRival = d;
            }
            if (nearestRival == float.MaxValue) return 1f;
            return Mathf.Clamp(botDist / (nearestRival + 0.1f), 1f, 3f);
        }

        private bool IsBlacklisted(Collectible c) =>
            m_blacklist.TryGetValue(c, out float t) && Time.time - t < blacklistDuration;

        private bool IsItemBoxBlacklisted(ItemBox box) =>
            m_itemBoxBlacklist.TryGetValue(box, out float t) && Time.time - t < blacklistDuration;

        private bool IsBreakableBlacklisted(Breakable b) =>
            m_breakableBlacklist.TryGetValue(b, out float t) && Time.time - t < blacklistDuration;

        private bool IsLauncherBlacklisted(LauncherInfo l) =>
            m_launcherBlacklist.TryGetValue(l, out float t) && Time.time - t < blacklistDuration;

        /// <summary>Returns the display name of the current target across all target modes.</summary>
        private string GetCurrentTargetName()
        {
            if (m_targetMode == TargetMode.ItemBox   && m_targetItemBox   != null) return m_targetItemBox.name;
            if (m_targetMode == TargetMode.Breakable && m_targetBreakable != null) return m_targetBreakable.name;
            return m_target != null ? m_target.name : "?";
        }

        /// <summary>Returns the current navigation target world position across all target modes.</summary>
        private Vector3 GetCurrentTargetPosition()
        {
            if (m_targetMode == TargetMode.ItemBox   && m_targetItemBox   != null) return m_targetItemBox.transform.position;
            if (m_targetMode == TargetMode.Breakable && m_targetBreakable != null) return m_targetBreakable.transform.position;
            if (m_target != null) return m_target.transform.position;
            return transform.position;
        }

        /// <summary>Scores the collectibles inside an ItemBox.</summary>
        private float GetItemBoxValue(ItemBox box)
        {
            float total = 0f;
            foreach (var c in box.collectibles)
            {
                if (c == null) continue;
                // Skip already-collected or expired slots:
                //   activeSelf=false  → not yet dispensed, still inside box (count it)
                //   activeSelf=true + triggerCollider enabled  → dispensed and live (count it)
                //   activeSelf=true + triggerCollider disabled → collected/expired (skip)
                if (c.gameObject.activeSelf
                    && (c.triggerCollider == null || !c.triggerCollider.enabled))
                    continue;
                total += GetCollectibleValue(c);
            }
            return total;
        }

        /// <summary>Scores the collectibles released by breaking a Breakable.</summary>
        private float GetBreakableValue(Breakable b)
        {
            float total = 0f;
            foreach (var c in b.collectibles)
            {
                if (c == null) continue;
                // Skip already-dispensed collectibles that have been collected or expired.
                if (c.gameObject.activeSelf
                    && (c.triggerCollider == null || !c.triggerCollider.enabled))
                    continue;
                total += GetCollectibleValue(c);
            }
            return total;
        }

        /// <summary>
        /// Returns a repulsion vector away from nearby hazards and kill zones.
        /// Returns Vector3.zero when no hazards are in range.
        /// </summary>
        private Vector3 ComputeHazardAvoidance()
        {
            var repulsion = Vector3.zero;

            foreach (var h in m_hazards)
            {
                if (h == null || !h.gameObject.activeInHierarchy) continue;
                var toBot = transform.position - h.transform.position;
                float dist = toBot.magnitude;
                if (dist < hazardAvoidRadius && dist > 0.01f)
                    repulsion += toBot.normalized * ((hazardAvoidRadius - dist) / hazardAvoidRadius);
            }

            foreach (var kz in m_killZones)
            {
                if (kz == null || !kz.gameObject.activeInHierarchy) continue;
                var toBot = transform.position - kz.transform.position;
                float dist = toBot.magnitude;
                float kzRadius = hazardAvoidRadius * 1.5f;
                if (dist < kzRadius && dist > 0.01f)
                    repulsion += toBot.normalized * ((kzRadius - dist) / kzRadius) * 2f;
            }

            return Vector3.ProjectOnPlane(repulsion, Vector3.up);
        }

        /// <summary>
        /// If the bot is standing on a conveyor belt, offsets the desired direction
        /// to counteract the belt's push so the bot steers where it intends.
        /// </summary>
        private Vector3 ApplyConveyorCompensation(Vector3 moveDir)
        {
            if (!m_self.isGrounded) return moveDir;

            if (!Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, 1.5f,
                    ~0, QueryTriggerInteraction.Ignore)) return moveDir;

            if (!hit.collider.TryGetComponent<ConveyorBelt>(out var belt)) return moveDir;

            // Subtract normalised belt velocity from desired direction to compensate drift
            var beltFlat = Vector3.ProjectOnPlane(belt.velocity, Vector3.up);
            if (beltFlat.sqrMagnitude < 0.01f) return moveDir;

            return (moveDir - beltFlat.normalized * 0.6f).normalized;
        }

        /// <summary>
        /// Returns the center position of the nearest gravity field that pulls upward
        /// (inverted = true, meaning gravity points up), or null if none exist.
        /// </summary>
        private Vector3? NearestUprightGravityFieldPosition()
        {
            Vector3? best = null;
            float    bestDist = float.MaxValue;

            foreach (var field in m_gravityFields)
            {
                if (field == null || !field.gameObject.activeInHierarchy) continue;
                if (!field.inverted) continue;  // only upward-pull fields

                float d = Vector3.Distance(transform.position, field.center);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = field.center;
                }
            }

            return best;
        }

        // ─────────────────────────────────────────────────────────────────────
        // NavMesh helpers
        // ─────────────────────────────────────────────────────────────────────

        private NavMeshPathStatus QueryPathStatus(Vector3 from, Vector3 to)
        {
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, m_queryPath);
            return m_queryPath.status;
        }

        private Vector3 SampleNavMesh(Vector3 pos)
        {
            return NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas)
                ? hit.position : pos;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal log
        // ─────────────────────────────────────────────────────────────────────

        private void BrainLog(AILogType type, string message)
        {
            if (m_log.Count >= MaxLogEntries)
                m_log.RemoveAt(0);

            m_log.Add(new AILogEntry { time = Time.time, type = type, message = message });

            switch (type)
            {
                case AILogType.Warning:     Debug.LogWarning($"[AIBot] {message}"); break;
                case AILogType.Error:       Debug.LogError($"[AIBot] {message}");   break;
                case AILogType.StateChange:
                case AILogType.TargetChange:
                    Debug.Log($"[AIBot] {message}"); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scene-view path gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Vector3 botPos    = transform.position;
            Vector3 targetPos = GetCurrentTargetPosition();
            bool    hasTarget = m_target != null || m_targetItemBox != null || m_targetBreakable != null;

            if (!hasTarget) return;

            if (m_launcher != null)
            {
                // ── Bot → launcher entry (NavMesh path) ──────────────────────
                Gizmos.color = new Color(0.2f, 0.85f, 1f);
                DrawNavMeshPath(botPos, m_launcher.entryPoint, new Color(0.2f, 0.85f, 1f));
                Gizmos.DrawSphere(m_launcher.entryPoint, 0.25f);

                // ── Launcher arc ─────────────────────────────────────────────
                Gizmos.color = new Color(0.4f, 0.5f, 1f);
                if (m_launcher.type == LauncherType.Spring && m_launcher.apexPoint != Vector3.zero)
                {
                    DrawArc(m_launcher.entryPoint, m_launcher.apexPoint, m_launcher.exitPoint, 16);
                    Gizmos.DrawSphere(m_launcher.apexPoint, 0.15f);
                }
                else
                {
                    Gizmos.DrawLine(m_launcher.entryPoint, m_launcher.exitPoint);
                }
                Gizmos.DrawSphere(m_launcher.exitPoint, 0.25f);

                // ── Launcher exit → target ───────────────────────────────────
                DrawNavMeshPath(m_launcher.exitPoint, targetPos, new Color(0.2f, 1f, 0.45f));
            }
            else
            {
                // ── Direct walk — draw actual NavMesh corners ────────────────
                DrawNavMeshPath(botPos, targetPos, new Color(0.2f, 1f, 0.45f));
            }

            // ── Target marker ────────────────────────────────────────────────
            Gizmos.color = m_targetMode == TargetMode.ItemBox   ? Color.cyan
                         : m_targetMode == TargetMode.Breakable ? new Color(1f, 0.5f, 0f)
                         : Color.yellow;
            Gizmos.DrawWireSphere(targetPos, 0.45f);

            // ── Hazard avoidance radius (red rings around nearby hazards) ────
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            foreach (var h in m_hazards)
            {
                if (h == null || !h.gameObject.activeInHierarchy) continue;
                Gizmos.DrawWireSphere(h.transform.position, hazardAvoidRadius);
            }
            foreach (var kz in m_killZones)
            {
                if (kz == null || !kz.gameObject.activeInHierarchy) continue;
                Gizmos.DrawWireSphere(kz.transform.position, hazardAvoidRadius * 1.5f);
            }

            // ── Wall-jump raycasts (low = feet, high = apex) ─────────────────
            if (m_input != null)
            {
                Vector3 fwd = m_input.desiredMoveDirection.sqrMagnitude > 0.01f
                    ? m_input.desiredMoveDirection.normalized
                    : transform.forward;

                Vector3 lowOrigin  = botPos + Vector3.up * 0.15f;
                Vector3 highOrigin = botPos + Vector3.up * wallJumpClearHeight;

                bool lowHit  = Physics.Raycast(lowOrigin,  fwd, obstacleCheckDistance,
                    obstacleLayer, QueryTriggerInteraction.Ignore);
                bool highHit = Physics.Raycast(highOrigin, fwd, obstacleCheckDistance,
                    obstacleLayer, QueryTriggerInteraction.Ignore);

                // Low ray — red = wall detected, green = clear
                Gizmos.color = lowHit ? Color.red : Color.green;
                Gizmos.DrawRay(lowOrigin, fwd * obstacleCheckDistance);
                Gizmos.DrawSphere(lowOrigin, 0.06f);

                // High ray — red = blocked (can't jump over), green = clearance to jump
                Gizmos.color = highHit ? Color.red : new Color(0f, 1f, 0.4f);
                Gizmos.DrawRay(highOrigin, fwd * obstacleCheckDistance);
                Gizmos.DrawSphere(highOrigin, 0.06f);

                // Vertical connector so the two rays read as a pair
                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                Gizmos.DrawLine(lowOrigin, highOrigin);
            }

            // ── Spin range (orange wire sphere) ──────────────────────────────
            Gizmos.color = new Color(1f, 0.55f, 0f, 0.35f);
            Gizmos.DrawWireSphere(botPos, spinRange);

            // ── Stomp detect ray (downward, only when airborne) ───────────────
            if (m_self != null && !m_self.isGrounded)
            {
                bool stompHit = Physics.Raycast(botPos, Vector3.down, out RaycastHit stompHitInfo,
                    stompDetectRange, ~0, QueryTriggerInteraction.Ignore);
                Gizmos.color = stompHit ? new Color(1f, 0.3f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.4f);
                Gizmos.DrawRay(botPos, Vector3.down * stompDetectRange);
                if (stompHit)
                    Gizmos.DrawSphere(stompHitInfo.point, 0.12f);
            }
        }

        /// <summary>Draws the NavMesh path as a series of lines through each corner.</summary>
        private void DrawNavMeshPath(Vector3 from, Vector3 to, Color color)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);

            Gizmos.color = color;

            if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length >= 2)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                    Gizmos.DrawSphere(path.corners[i + 1], 0.1f);
                }
            }
            else
            {
                // No valid path — draw dashed-style line in red to signal invalid
                Gizmos.color = Color.red;
                Gizmos.DrawLine(from, to);
            }
        }

        /// <summary>Draws a quadratic bezier arc (entry → apex → exit) in segments.</summary>
        private void DrawArc(Vector3 start, Vector3 apex, Vector3 end, int segments)
        {
            Vector3 prev = start;
            for (int i = 1; i <= segments; i++)
            {
                float t  = i / (float)segments;
                float t1 = 1f - t;
                Vector3 p = t1 * t1 * start + 2f * t1 * t * apex + t * t * end;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
