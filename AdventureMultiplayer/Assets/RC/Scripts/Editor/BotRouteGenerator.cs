using System.Collections.Generic;
using PLAYERTWO.PlatformerProject;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Scans the open race level and builds bot routes automatically: start → every checkpoint
    /// → finish, as BotRoute_A / _B / _C (different lines where the track has room).
    ///
    /// How: the level is sampled on a grid of downward sphere-casts into walkable surfaces
    /// (several per cell, so bridges over ground work), then A* finds the cheapest path, with
    /// walk / drop / jump / launch-pad moves between surfaces. Edges and hazard zones cost
    /// extra, so the line stays centred and away from swinging things. The path is then
    /// simplified into BotWaypoints: Jump at take-offs, Careful on narrow ground, Wait before
    /// a hazard zone. The result is meant to be CHECKED in the Scene view and hand-fixed.
    /// </summary>
    public class BotRouteGenerator : EditorWindow
    {
        [SerializeField] private float cellSize      = 0.75f;
        [SerializeField] private float margin        = 25f;
        [SerializeField] private float maxJumpDist   = 6f;
        [SerializeField] private float maxStepUp     = 0.55f;
        [SerializeField] private float maxDrop       = 10f;
        [SerializeField] private int   routeCount    = 3;
        [SerializeField] private LayerMask layers    = Physics.DefaultRaycastLayers;

        [MenuItem("Tools/RC/Bot Route/Auto Generate Routes")]
        private static void Open() => GetWindow<BotRouteGenerator>("Bot Route Generator");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Scans the open scene and creates BotRoute_A/B/C from the bot start through every " +
                "checkpoint to the finish. Existing auto-generated routes are replaced.\n\n" +
                "Check the lines in the Scene view afterwards and fix any waypoint by hand.",
                MessageType.Info);

            cellSize    = EditorGUILayout.Slider("Grid cell (m)", cellSize, 0.4f, 1.5f);
            margin      = EditorGUILayout.FloatField("Scan margin (m)", margin);
            maxJumpDist = EditorGUILayout.FloatField("Max jump gap (m)", maxJumpDist);
            maxStepUp   = EditorGUILayout.FloatField("Max step up (m)", maxStepUp);
            maxDrop     = EditorGUILayout.FloatField("Max drop (m)", maxDrop);
            routeCount  = EditorGUILayout.IntSlider("Routes", routeCount, 1, 4);
            layers      = LayerMaskField("Ground layers", layers);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate", GUILayout.Height(32)))
                Generate();
        }

        private static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var names = UnityEditorInternal.InternalEditorUtility.layers;
            int packed = 0;
            for (int i = 0; i < names.Length; i++)
                if ((mask.value & (1 << LayerMask.NameToLayer(names[i]))) != 0) packed |= 1 << i;
            packed = EditorGUILayout.MaskField(label, packed, names);
            int value = 0;
            for (int i = 0; i < names.Length; i++)
                if ((packed & (1 << i)) != 0) value |= 1 << LayerMask.NameToLayer(names[i]);
            return value;
        }

        // ── Graph ──────────────────────────────────────────────────────────────

        private const int k_maxLayers = 6;   // surfaces kept per grid cell (swing poses add extra ones)

        private struct Node
        {
            public Vector3 p;
            public int     cx, cz;
            public bool    hazard;
            public bool    pad;
            public int     edgeDist;   // cells to the nearest drop-off (1..3, 4 = open ground)
            public int     swing;      // index into m_swings, -1 = static ground
            public int     pose;       // which swing pose this surface was scanned in
            public float   axisDist;   // swing: distance from the plank's pivot axis (how far it moves)
            public bool    platform;   // rideable moving platform (spinning disc)
            public float   rim;        // platform: distance in from that platform's own outer edge

            public int SwingKey => swing < 0 ? -1 : swing * 8 + pose;
        }

        // A see-saw plank rocked by OscillateRotation about a horizontal axis (the DeathRun
        // "pendulum" staircases). It is walkable ground, not a hazard — but only lined up with
        // the next surface at certain points of its swing, so it is scanned in every extreme
        // pose and the route WAITS at each hand-over until the plank there is in that pose.
        private class Swing
        {
            public Transform        t;
            public Quaternion       rest;
            public Vector3          pivot;
            public Vector3          axis;      // world-space, normalised
            public List<Quaternion> poses = new();   // [0] = rest
            public List<Collider>   cols  = new();
        }

        private readonly List<Swing> m_swings = new();
        private readonly Dictionary<Collider, int> m_swingOf = new();

        private enum Move : byte { Start, Walk, Drop, Jump, Pad }

        private readonly List<Node> m_nodes = new();
        private int[]   m_cells;          // (cx * nz + cz) * k_maxLayers + k → node index or -1
        private int     m_nx, m_nz;
        private Vector3 m_origin;         // world position of cell (0,0) centre, y unused
        private float   m_killY;
        private readonly Dictionary<Collider, bool> m_hazardCache = new();
        private readonly Collider[] m_overlap = new Collider[16];
        private readonly Dictionary<int, List<int>> m_hintJumps = new();
        private readonly Dictionary<Collider, Collider> m_proxyOf = new();
        private readonly List<GameObject> m_proxies = new();

        // ── Entry ──────────────────────────────────────────────────────────────

        private void Generate() => Generate(true, true);

        /// <summary>Runs the generator without dialogs or scene changes; returns the summary.</summary>
        public static string Diagnose()
        {
            var gen = CreateInstance<BotRouteGenerator>();
            try { return gen.Generate(false, false); }
            finally { DestroyImmediate(gen); }
        }

        /// <summary>Generates and writes BotRoute_A/B/C into the open scene without dialogs.</summary>
        public static string GenerateInScene()
        {
            var gen = CreateInstance<BotRouteGenerator>();
            try { return gen.Generate(false, true); }
            finally { DestroyImmediate(gen); }
        }

        private string Generate(bool interactive, bool write)
        {
            void Dialog(string m)
            {
                if (interactive) EditorUtility.DisplayDialog("Bot Route", m, "OK");
                else Debug.Log("[BotRouteGenerator] " + m);
            }

            var rm = FindFirstObjectByType<RaceManager>();
            if (rm == null || rm.Checkpoints == null || rm.Checkpoints.Length == 0)
            {
                Dialog("No RaceManager with checkpoints in the open scene.");
                return null;
            }
            if (!TryGetStart(out Vector3 start))
            {
                Dialog("No BotSpawner with spawn points in the open scene.");
                return null;
            }

            var goals = new List<RaceCheckpoint>();
            foreach (var cp in rm.Checkpoints) if (cp != null) goals.Add(cp);

            try
            {
                EditorUtility.DisplayProgressBar("Bot Route", "Scanning level…", 0f);
                BuildGrid(start, goals);
                if (m_nodes.Count == 0)
                {
                    Dialog("Scan found no walkable ground. Check 'Ground layers'.");
                    return null;
                }

                int startNode = NearestNode(start);
                var used      = new HashSet<int>();
                var routes    = new List<List<(int node, Move move)>>();
                var problems  = new List<string>();

                for (int r = 0; r < routeCount; r++)
                {
                    EditorUtility.DisplayProgressBar("Bot Route", $"Route {r + 1}/{routeCount}…", 0.3f + 0.6f * r / routeCount);
                    var full = new List<(int, Move)> { (startNode, Move.Start) };
                    int from = startNode;
                    for (int g = 0; g < goals.Count; g++)
                    {
                        var leg = FindPath(from, goals[g], used, r);
                        if (leg == null)
                        {
                            if (r == 0) problems.Add($"No path found to '{goals[g].name}' (index {goals[g].index}): " +
                                                     $"stuck at {m_lastFailPos:F1}, {m_lastFailDist:F1} m short. " +
                                                     "Route goes straight there — fix it by hand.");
                            int n = NearestNode(goals[g].transform.position);
                            full.Add((n, Move.Walk));
                            from = n;
                            continue;
                        }
                        for (int i = 1; i < leg.Count; i++) full.Add(leg[i]);
                        from = leg[^1].node;
                    }
                    routes.Add(full);
                    // Penalise the ground within ~1.5 m of this line so the next route takes a
                    // genuinely different line wherever the track is wide enough.
                    foreach (var (node, _) in full)
                    {
                        var nd = m_nodes[node];
                        for (int dx = -2; dx <= 2; dx++)
                            for (int dz = -2; dz <= 2; dz++)
                            {
                                int nb = FindSurface(nd.cx + dx, nd.cz + dz, nd.p.y, -1f, 1f);
                                if (nb >= 0) used.Add(nb);
                            }
                    }
                }

                EditorUtility.DisplayProgressBar("Bot Route", "Creating waypoints…", 0.95f);
                if (write) WriteRoutes(routes, goals);
                else LogRoutes(routes);

                string msg = $"Created {routes.Count} route(s) from {m_nodes.Count} scanned surface points.";
                if (problems.Count > 0) msg += "\n\n" + string.Join("\n", problems);
                Debug.Log("[BotRouteGenerator] " + msg);
                if (interactive) EditorUtility.DisplayDialog("Bot Route", msg, "OK");
                return msg;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                m_hazardCache.Clear();
                RestoreSwings();
                DestroyScanProxies();
            }
        }

        private static bool TryGetStart(out Vector3 start)
        {
            start = Vector3.zero;
            var spawner = FindFirstObjectByType<BotSpawner>();
            if (spawner == null) return false;
            var prop = new SerializedObject(spawner).FindProperty("spawnPoints");
            int count = 0;
            for (int i = 0; prop != null && i < prop.arraySize; i++)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue is Transform t)
                {
                    start += t.position;
                    count++;
                }
            }
            if (count == 0) return false;
            start /= count;
            return true;
        }

        // ── Scan ───────────────────────────────────────────────────────────────

        private void BuildGrid(Vector3 start, List<RaceCheckpoint> goals)
        {
            m_nodes.Clear();
            m_hazardCache.Clear();
            CreateScanProxies();

            var area = new Bounds(start, Vector3.zero);
            foreach (var cp in goals) area.Encapsulate(cp.transform.position);
            area.Expand(new Vector3(margin * 2f, 0f, margin * 2f));

            // Anything below the kill zone's top is death, never ground.
            m_killY = area.min.y - 50f;
            bool anyKill = false;
            foreach (var kz in FindObjectsByType<KillZone>(FindObjectsSortMode.None))
            {
                var c = kz.GetComponent<Collider>();
                if (c == null) continue;
                float top = c.bounds.max.y;
                m_killY = anyKill ? Mathf.Max(m_killY, top) : top;
                anyKill = true;
            }

            float topY = area.max.y + 40f;
            m_nx     = Mathf.CeilToInt(area.size.x / cellSize) + 1;
            m_nz     = Mathf.CeilToInt(area.size.z / cellSize) + 1;
            m_origin = new Vector3(area.min.x, 0f, area.min.z);
            m_cells  = new int[m_nx * m_nz * k_maxLayers];
            for (int i = 0; i < m_cells.Length; i++) m_cells[i] = -1;

            float castDist = topY - m_killY;
            CollectSwings();

            for (int cx = 0; cx < m_nx; cx++)
            {
                if (cx % 20 == 0)
                    EditorUtility.DisplayProgressBar("Bot Route", "Scanning level…", 0.3f * cx / m_nx);

                for (int cz = 0; cz < m_nz; cz++)
                    ScanCell(cx, cz, topY, castDist, -1, 0);
            }

            // Swinging planks again in each of their other extreme poses — only their own
            // surfaces, added on top of what the rest pose already found.
            for (int s = 0; s < m_swings.Count; s++)
            {
                var sw = m_swings[s];
                for (int pose = 1; pose < sw.poses.Count; pose++)
                {
                    sw.t.rotation = sw.poses[pose];
                    Physics.SyncTransforms();
                    Bounds b = sw.cols[0].bounds;
                    foreach (var c in sw.cols) b.Encapsulate(c.bounds);
                    int cx0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - m_origin.x) / cellSize));
                    int cx1 = Mathf.Min(m_nx - 1, Mathf.CeilToInt((b.max.x - m_origin.x) / cellSize));
                    int cz0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - m_origin.z) / cellSize));
                    int cz1 = Mathf.Min(m_nz - 1, Mathf.CeilToInt((b.max.z - m_origin.z) / cellSize));
                    for (int cx = cx0; cx <= cx1; cx++)
                        for (int cz = cz0; cz <= cz1; cz++)
                            ScanCell(cx, cz, topY, castDist, s, pose);
                }
                sw.t.rotation = sw.rest;
            }
            Physics.SyncTransforms();
            if (m_swings.Count > 0)
                Debug.Log($"[BotRouteGenerator] {m_swings.Count} swinging plank(s) scanned in every swing pose.");

            MarkEdgesAndHazards();

            m_hintJumps.Clear();
            foreach (var h in FindObjectsByType<BotObstacleHint>(FindObjectsSortMode.None))
            {
                if (!h.HasJumpLine) continue;
                int a = NearestNode(h.jumpFrom.position), b = NearestNode(h.jumpTo.position);
                if (a < 0 || b < 0 || a == b) continue;
                if (!m_hintJumps.TryGetValue(a, out var list)) m_hintJumps[a] = list = new List<int>();
                list.Add(b);
            }
        }

        // One grid cell: every walkable surface under it, top first. onlySwing >= 0 = a swing-pose
        // pass: keep only that plank's surfaces and append them to the cell's free slots.
        private readonly List<(float y, bool pad, int swing, bool platform, float rim)> m_ys = new(8);

        private void ScanCell(int cx, int cz, float topY, float castDist, int onlySwing, int pose)
        {
            Vector3 o = CellCentre(cx, cz, topY);
            var hits = Physics.SphereCastAll(o, 0.25f, Vector3.down, castDist, layers, QueryTriggerInteraction.Ignore);
            int baseIdx = CellIndex(cx, cz) * k_maxLayers;
            m_ys.Clear();
            foreach (var h in hits)
            {
                if (h.distance <= 0f || h.normal.y < 0.65f) continue;
                float y = h.point.y;
                if (y < m_killY + 0.3f) continue;
                var hc = Orig(h.collider);
                if (hc.GetComponentInParent<Player>() != null) continue;
                int swing = m_swingOf.TryGetValue(hc, out int si) ? si : -1;
                if (onlySwing >= 0 && swing != onlySwing) continue;
                bool pad = RaceBotBrain.IsBouncePlatform(hc);
                if (!pad && swing < 0 && IsHazard(hc)) continue;

                bool dup = false;
                for (int k = 0; k < m_ys.Count; k++)
                    if (Mathf.Abs(m_ys[k].y - y) < 0.4f) { dup = true; break; }
                // A swing pose that coincides with a surface already in the cell adds nothing.
                for (int k = 0; k < k_maxLayers && !dup && onlySwing >= 0; k++)
                {
                    int n = m_cells[baseIdx + k];
                    if (n >= 0 && Mathf.Abs(m_nodes[n].p.y - y) < 0.4f) dup = true;
                }
                bool platform = swing < 0 && RaceBotBrain.IsRideablePlatform(hc);
                float rim = 0f;
                if (platform)
                {
                    // Round spinning disc: how far inside its own edge is this point? (The proxy's
                    // bounds — the real collider's mesh only exists at runtime.)
                    Bounds pb = h.collider.bounds;
                    float r = Mathf.Min(pb.extents.x, pb.extents.z);
                    rim = r - new Vector2(o.x - pb.center.x, o.z - pb.center.z).magnitude;
                }
                if (!dup) m_ys.Add((y, pad, swing, platform, rim));
            }
            m_ys.Sort((a, b) => b.y.CompareTo(a.y)); // top surface first

            int slot = 0;
            while (slot < k_maxLayers && m_cells[baseIdx + slot] >= 0) slot++;
            foreach (var (y, pad, swing, platform, rim) in m_ys)
            {
                if (slot >= k_maxLayers) break;
                Vector3 p = new Vector3(o.x, y, o.z);
                if (!pad && Blocked(p)) continue;   // inside a wall / no headroom
                float axisDist = swing >= 0 ? Vector3.Cross(p - m_swings[swing].pivot, m_swings[swing].axis).magnitude : 0f;
                m_cells[baseIdx + slot++] = m_nodes.Count;
                m_nodes.Add(new Node { p = p, cx = cx, cz = cz, pad = pad, swing = swing, pose = swing >= 0 ? pose : 0,
                                       axisDist = axisDist, platform = platform, rim = rim });
            }
        }

        // See-saw planks: OscillateRotation (ithappy or the RC copy) rocking about a horizontal
        // axis. Hammers etc. that knock players back stay hazards.
        private void CollectSwings()
        {
            m_swings.Clear();
            m_swingOf.Clear();
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                Vector3 axis;
                var angles = new List<float>();
                if (mb is ithappy.OscillateRotation a)
                {
                    axis = a.rotationAxis;
                    angles.Add(a.rotationAngle);                 // one-sided: rest → rest + angle
                }
                else if (mb is ithappy.rc.OscillateRotation b)
                {
                    axis = b.rotationAxis;
                    if (b.throughCenter) { angles.Add(b.rotationAngle * 0.5f); angles.Add(-b.rotationAngle * 0.5f); }
                    else angles.Add(b.rotationAngle);
                }
                else continue;

                if (!mb.isActiveAndEnabled || axis.sqrMagnitude < 0.0001f) continue;
                Vector3 worldAxis = (mb.transform.rotation * axis).normalized;
                if (Mathf.Abs(worldAxis.y) > 0.3f) continue; // spinning about a vertical axis — not a see-saw

                var sw = new Swing { t = mb.transform, rest = mb.transform.rotation,
                                     pivot = mb.transform.position, axis = worldAxis };
                foreach (var c in mb.GetComponentsInChildren<Collider>())
                {
                    if (!c.enabled || c.isTrigger || RaceBotBrain.FindObstacleKnockback(c) != null) continue;
                    if (m_swingOf.ContainsKey(c)) continue;
                    sw.cols.Add(c);
                }
                if (sw.cols.Count == 0) continue;

                sw.poses.Add(sw.rest);
                foreach (float ang in angles)
                    sw.poses.Add(sw.rest * Quaternion.AngleAxis(ang, axis));
                foreach (var c in sw.cols) m_swingOf[c] = m_swings.Count;
                m_swings.Add(sw);
            }
        }

        private void RestoreSwings()
        {
            foreach (var sw in m_swings)
                if (sw.t != null) sw.t.rotation = sw.rest;
            Physics.SyncTransforms();
        }

        private bool IsSwing(Collider c) => c != null && m_swingOf.ContainsKey(Orig(c));

        private void MarkEdgesAndHazards()
        {
            // Hazard zones: the volume a moving obstacle can sweep, approximated as a sphere
            // around its pivot (a pendulum at rest only covers the bottom of its arc).
            var zones = new List<(Vector3 c, float r, float yMin, float yMax)>();
            var seen  = new HashSet<Transform>();
            foreach (var col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!col.enabled || col.isTrigger || IsSwing(col) || !IsHazard(col)) continue;
                Transform pivot = MotionPivot(col) ?? col.transform;
                Bounds b = col.bounds;
                if (IsSpunBy(col, "RotationScript") && b.size.y < 0.75f * Mathf.Max(b.size.x, b.size.z))
                {
                    // Flat thing spinning about a vertical axis (spinner drum, rotating log/beam):
                    // it sweeps a cylinder, not a sphere around the pivot. The sphere (pivot→far
                    // corner) put the Wait ~7 m short of the L1 spinner drum, so by the time the
                    // bot arrived the doorway it had waited for had rotated away.
                    Vector3 off = b.center - pivot.position; off.y = 0f;
                    float rh = off.magnitude + Mathf.Max(b.extents.x, b.extents.z) * 1.15f;
                    zones.Add((pivot.position, rh + 0.4f, b.min.y - 0.5f, b.max.y));
                }
                else if (MotionPivot(col) != null)
                {
                    float r = 0f;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                                                     (i & 2) == 0 ? b.min.y : b.max.y,
                                                     (i & 4) == 0 ? b.min.z : b.max.z);
                        r = Mathf.Max(r, Vector3.Distance(pivot.position, corner));
                    }
                    zones.Add((pivot.position, r + 0.4f, pivot.position.y - r - 0.5f, pivot.position.y + r));
                }
                else
                {
                    if (!seen.Add(col.transform)) continue;
                    zones.Add((b.center, Mathf.Max(b.extents.x, b.extents.z) + 0.5f, b.min.y - 0.5f, b.max.y));
                }
            }

            for (int i = 0; i < m_nodes.Count; i++)
            {
                var n = m_nodes[i];

                n.edgeDist = 4;
                for (int ring = 1; ring <= 3 && n.edgeDist == 4; ring++)
                    for (int dx = -ring; dx <= ring && n.edgeDist == 4; dx++)
                        for (int dz = -ring; dz <= ring; dz++)
                        {
                            if (Mathf.Abs(dx) != ring && Mathf.Abs(dz) != ring) continue;
                            if (FindSurface(n.cx + dx, n.cz + dz, n.p.y, -1f, 0.6f) < 0) { n.edgeDist = ring; break; }
                        }

                foreach (var z in zones)
                {
                    float dx = n.p.x - z.c.x, dz = n.p.z - z.c.z;
                    if (dx * dx + dz * dz <= z.r * z.r && n.p.y + 1.8f >= z.yMin && n.p.y <= z.yMax)
                    {
                        n.hazard = true;
                        break;
                    }
                }
                m_nodes[i] = n;
            }
        }

        // Some colliders only get their mesh at runtime (FanCylinderCollider builds it in Awake,
        // so the stepping discs have an EMPTY MeshCollider in the editor) and are invisible to
        // an edit-time scan. Stand in a temporary collider made from the visible mesh.
        private void CreateScanProxies()
        {
            DestroyScanProxies();
            foreach (var mc in FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
            {
                if (!mc.enabled || mc.isTrigger || mc.sharedMesh != null) continue;
                foreach (var mf in mc.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var go = new GameObject("__BotRouteScanProxy") { hideFlags = HideFlags.HideAndDontSave };
                    go.layer = mc.gameObject.layer;
                    go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
                    go.transform.localScale = mf.transform.lossyScale;
                    var proxy = go.AddComponent<MeshCollider>();
                    proxy.sharedMesh = mf.sharedMesh;
                    m_proxyOf[proxy] = mc;
                    m_proxies.Add(go);
                }
            }
            Physics.SyncTransforms();
            if (m_proxies.Count > 0)
                Debug.Log($"[BotRouteGenerator] {m_proxies.Count} runtime-built collider(s) stood in by their render mesh for the scan.");
        }

        private void DestroyScanProxies()
        {
            foreach (var go in m_proxies) if (go != null) DestroyImmediate(go);
            m_proxies.Clear();
            m_proxyOf.Clear();
        }

        private Collider Orig(Collider c) => c != null && m_proxyOf.TryGetValue(c, out var o) ? o : c;

        private bool IsHazard(Collider col)
        {
            col = Orig(col);
            if (m_hazardCache.TryGetValue(col, out bool v)) return v;
            v = !RaceBotBrain.IsRideablePlatform(col) && !RaceBotBrain.IsWalkableLog(col)
                && (RaceBotBrain.IsTimedHazard(col) || RaceBotBrain.FindObstacleKnockback(col) != null
                    || col.GetComponentInParent<Hazard>() != null);
            m_hazardCache[col] = v;
            return v;
        }

        private static bool IsSpunBy(Collider col, string scriptName)
        {
            foreach (var mb in col.GetComponentsInParent<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == scriptName) return true;
            return false;
        }

        private static Transform MotionPivot(Collider col)
        {
            foreach (var mb in col.GetComponentsInParent<MonoBehaviour>())
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if (n == "RotationScript" || n == "OscillateRotation") return mb.transform;
            }
            return null;
        }

        // A standing player capsule at p would overlap solid geometry (wall, pillar, low roof).
        private bool Blocked(Vector3 p)
        {
            int count = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.55f, p + Vector3.up * 1.6f, 0.28f,
                                                       m_overlap, layers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = Orig(m_overlap[i]);
                if (c.GetComponentInParent<Player>() != null) continue;
                if (IsHazard(c) || IsSwing(c)) continue; // moving things are hazard zones / swing poses, not walls
                return true;
            }
            return false;
        }

        // ── Grid helpers ───────────────────────────────────────────────────────

        private Vector3 CellCentre(int cx, int cz, float y) =>
            new Vector3(m_origin.x + cx * cellSize, y, m_origin.z + cz * cellSize);

        private int CellIndex(int cx, int cz) => cx * m_nz + cz;

        private bool InGrid(int cx, int cz) => cx >= 0 && cz >= 0 && cx < m_nx && cz < m_nz;

        // Surface in cell whose height is within [y+dyMin, y+dyMax], closest to y. -1 if none.
        private int FindSurface(int cx, int cz, float y, float dyMin, float dyMax)
        {
            if (!InGrid(cx, cz)) return -1;
            int best = -1;
            float bestAbs = float.MaxValue;
            int b = CellIndex(cx, cz) * k_maxLayers;
            for (int k = 0; k < k_maxLayers; k++)
            {
                int n = m_cells[b + k];
                if (n < 0) break;
                float dy = m_nodes[n].p.y - y;
                if (dy < dyMin || dy > dyMax) continue;
                if (Mathf.Abs(dy) < bestAbs) { bestAbs = Mathf.Abs(dy); best = n; }
            }
            return best;
        }

        private int NearestNode(Vector3 pos)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < m_nodes.Count; i++)
            {
                Vector3 d = m_nodes[i].p - pos;
                float s = d.x * d.x + d.z * d.z + 4f * d.y * d.y;
                if (s < bestD) { bestD = s; best = i; }
            }
            return best;
        }

        private bool ClearBetween(Vector3 a, Vector3 b, float h)
        {
            if (!Physics.Linecast(a + Vector3.up * h, b + Vector3.up * h, out RaycastHit hit, layers, QueryTriggerInteraction.Ignore))
                return true;
            var c = Orig(hit.collider);
            return c.GetComponentInParent<Player>() != null || IsHazard(c) || IsSwing(c);
        }

        // ── A* ─────────────────────────────────────────────────────────────────

        private static readonly int[] k_dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] k_dz = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private List<(int node, Move move)> FindPath(int start, RaceCheckpoint goal, HashSet<int> used, int routeIdx)
        {
            var col = goal.GetComponent<Collider>();
            Bounds gb = col != null ? col.bounds : new Bounds(goal.transform.position, Vector3.one * 3f);
            gb.Expand(new Vector3(1.5f, 4f, 1.5f));
            Vector3 gc = gb.center;

            int n = m_nodes.Count;
            var g    = new float[n];
            var from = new int[n];
            var how  = new Move[n];
            var closed = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = float.MaxValue; from[i] = -1; }

            var open = new MinHeap();
            g[start] = 0f;
            open.Push(start, Heuristic(start, gc));

            int maxJumpCells = Mathf.Max(2, Mathf.CeilToInt(maxJumpDist / cellSize));
            int reached = -1;
            int expanded = 0;

            while (open.Count > 0)
            {
                int cur = open.Pop();
                if (closed[cur]) continue;
                closed[cur] = true;
                if (++expanded > 400000) break;

                var cn = m_nodes[cur];
                if (cur != start && gb.Contains(cn.p + Vector3.up)) { reached = cur; break; }

                void Relax(int nb, float cost, Move mv)
                {
                    if (nb < 0 || closed[nb]) return;
                    var nn = m_nodes[nb];
                    if (!Standable(nn)) return;
                    float mult = 1f;
                    if (nn.hazard) mult += 3f;
                    // Stepping on a trampoline / mushroom launches the bot straight up; only do it
                    // when there's no way around (playtest: route crossed the L1 pad after cp2 and
                    // bots bounce-looped on it).
                    if (nn.pad) mult += 8f;
                    // Swinging planks: the further from the pivot axis, the more the ground moves.
                    if (nn.swing >= 0) mult += 0.5f + 0.25f * nn.axisDist;
                    if (nn.SwingKey != cn.SwingKey)
                    {
                        mult += 1f;                                         // every hand-over costs a wait
                        // ...and waiting while standing on a plank's swinging end throws the bot
                        // off (playtest: Bruno died 3x waiting at a plank tip). Hand over from
                        // static ground or near the pivot instead.
                        if (cn.swing >= 0) mult += 4f + 3f * cn.axisDist;
                    }
                    if (nn.edgeDist < 4) mult += (4 - nn.edgeDist) * 0.8f;
                    if (routeIdx > 0 && used.Contains(nb)) mult += 1.5f;
                    float ng = g[cur] + cost * mult;
                    if (ng >= g[nb]) return;
                    g[nb] = ng; from[nb] = cur; how[nb] = mv;
                    open.Push(nb, ng + Heuristic(nb, gc));
                }

                for (int d = 0; d < 8; d++)
                {
                    int nx = cn.cx + k_dx[d], nz = cn.cz + k_dz[d];
                    float step = cellSize * (d < 4 ? 1f : 1.4142f);

                    int walk = FindSurface(nx, nz, cn.p.y, -1f, maxStepUp);
                    // A disc rim is ground the bot must not stand on — jump over it instead.
                    if (walk >= 0 && Standable(m_nodes[walk]))
                    {
                        if (ClearBetween(cn.p, m_nodes[walk].p, 0.7f) && ClearBetween(cn.p, m_nodes[walk].p, 1.4f))
                            Relax(walk, step, Move.Walk);
                        continue; // ground continues — no need to jump or drop in this direction
                    }

                    int drop = FindSurface(nx, nz, cn.p.y, -maxDrop, -1f);
                    if (drop >= 0 && ClearBetween(cn.p, m_nodes[drop].p + Vector3.up * (cn.p.y - m_nodes[drop].p.y), 0.7f))
                        Relax(drop, step + 1f, Move.Drop);

                    // Gap in this direction → try a running jump across it. Prefer landing at
                    // about the same height (the far side of the gap) over the first lower
                    // surface seen (usually just the floor of the pit).
                    int lower = -1; float lowerDist = 0f;
                    bool jumped = false;
                    for (int k = 2; k <= maxJumpCells; k++)
                    {
                        int jx = cn.cx + k_dx[d] * k, jz = cn.cz + k_dz[d] * k;
                        if (!InGrid(jx, jz)) break;
                        float dist = k * step;
                        if (dist > maxJumpDist) break;
                        int land = FindSurface(jx, jz, cn.p.y, -1.5f, 1.0f);
                        if (land >= 0 && !Standable(m_nodes[land])) continue; // disc rim — land further in
                        if (land < 0)
                        {
                            if (lower < 0)
                            {
                                lower = FindSurface(jx, jz, cn.p.y, -maxDrop, -1.5f);
                                lowerDist = dist;
                            }
                            continue;
                        }
                        if (ClearBetween(cn.p, m_nodes[land].p, 1.2f) && ClearBetween(cn.p, m_nodes[land].p, 2.0f))
                            Relax(land, dist * 1.3f + 4f + NarrowLandingCost(m_nodes[land]), Move.Jump);
                        jumped = true;
                        break;
                    }
                    if (!jumped && lower >= 0 && Standable(m_nodes[lower]) && ClearBetween(cn.p, m_nodes[lower].p, 1.2f))
                        Relax(lower, lowerDist * 1.3f + 4f + NarrowLandingCost(m_nodes[lower]), Move.Jump);
                }

                // Designer jump line (BotObstacleHint jumpFrom → jumpTo) near this node.
                if (m_hintJumps.TryGetValue(cur, out var hinted))
                    foreach (int land in hinted)
                        Relax(land, Vector3.Distance(cn.p, m_nodes[land].p) * 1.3f + 4f, Move.Jump);

                // Launch pad: the bounce carries the bot up and forward — link to anything
                // landable in range.
                if (cn.pad)
                {
                    int range = Mathf.CeilToInt(12f / cellSize);
                    for (int dx = -range; dx <= range; dx++)
                        for (int dz = -range; dz <= range; dz++)
                        {
                            float hd = new Vector2(dx, dz).magnitude * cellSize;
                            if (hd < 2f || hd > 12f) continue;
                            int land = FindSurface(cn.cx + dx, cn.cz + dz, cn.p.y, 0.5f, 10f);
                            if (land < 0 || m_nodes[land].pad) continue;
                            Relax(land, hd + 2f, Move.Pad);
                        }
                }
            }

            if (reached < 0)
            {
                int bestN = start; float bestH = float.MaxValue;
                for (int i = 0; i < n; i++)
                    if (closed[i]) { float h = Heuristic(i, gc); if (h < bestH) { bestH = h; bestN = i; } }
                m_lastFailPos  = m_nodes[bestN].p;
                m_lastFailDist = bestH;
                LogFailSurroundings(bestN, goal);
                return null;
            }

            var path = new List<(int, Move)>();
            for (int c = reached; c >= 0; c = from[c])
            {
                path.Add((c, c == start ? Move.Start : how[c]));
                if (c == start) break;
            }
            path.Reverse();
            return path;
        }

        // Why is the search stuck here? Print what surrounds the furthest point it reached:
        // the colliders nearby (and how the scan classified them) and the surfaces in the
        // cells ahead toward the goal, so the blocker can be identified from the log alone.
        private void LogFailSurroundings(int node, RaceCheckpoint goal)
        {
            var n  = m_nodes[node];
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[BotRouteGenerator] Stuck at {n.p:F1} heading for '{goal.name}' at {goal.transform.position:F1}. " +
                          $"hazard={n.hazard} edgeDist={n.edgeDist}");

            sb.AppendLine("  Colliders within 6 m:");
            var seen = new HashSet<Collider>();
            foreach (var c in Physics.OverlapSphere(n.p, 6f, layers, QueryTriggerInteraction.Ignore))
            {
                var o = Orig(c);
                if (!seen.Add(o)) continue;
                sb.AppendLine($"    {Path(o.transform)} [{o.GetType().Name}] bounds={o.bounds.min:F1}..{o.bounds.max:F1} " +
                              $"hazard={IsHazard(o)} pad={RaceBotBrain.IsBouncePlatform(o)} platform={RaceBotBrain.IsRideablePlatform(o)}" +
                              (o is MeshCollider mc && mc.sharedMesh == null ? " EMPTY-MESH" : ""));
            }

            Vector3 dir = goal.transform.position - n.p; dir.y = 0f; dir.Normalize();
            sb.AppendLine("  Surfaces ahead toward the goal (distance: heights):");
            for (float d = cellSize; d <= 12f; d += cellSize)
            {
                Vector3 q = n.p + dir * d;
                int cx = Mathf.RoundToInt((q.x - m_origin.x) / cellSize);
                int cz = Mathf.RoundToInt((q.z - m_origin.z) / cellSize);
                if (!InGrid(cx, cz)) break;
                var ys = new List<string>();
                int b = CellIndex(cx, cz) * k_maxLayers;
                for (int k = 0; k < k_maxLayers && m_cells[b + k] >= 0; k++)
                {
                    var m = m_nodes[m_cells[b + k]];
                    ys.Add($"{m.p.y:F1}{(m.hazard ? "H" : "")}");
                }
                sb.AppendLine($"    {d:F1} m: {(ys.Count > 0 ? string.Join(", ", ys) : "none")}");
            }
            Debug.Log(sb.ToString());
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            for (var c = t.parent; c != null; c = c.parent) p = c.name + "/" + p;
            return p;
        }

        private Vector3 m_lastFailPos;
        private float   m_lastFailDist;

        private float Heuristic(int node, Vector3 goal)
        {
            Vector3 d = m_nodes[node].p - goal;
            return new Vector2(d.x, d.z).magnitude;
        }

        private class MinHeap
        {
            private readonly List<(int item, float pri)> m_h = new();
            public int Count => m_h.Count;

            public void Push(int item, float pri)
            {
                m_h.Add((item, pri));
                int i = m_h.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (m_h[p].pri <= m_h[i].pri) break;
                    (m_h[p], m_h[i]) = (m_h[i], m_h[p]);
                    i = p;
                }
            }

            public int Pop()
            {
                int top = m_h[0].item;
                int last = m_h.Count - 1;
                m_h[0] = m_h[last];
                m_h.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, s = i;
                    if (l < m_h.Count && m_h[l].pri < m_h[s].pri) s = l;
                    if (r < m_h.Count && m_h[r].pri < m_h[s].pri) s = r;
                    if (s == i) break;
                    (m_h[s], m_h[i]) = (m_h[i], m_h[s]);
                    i = s;
                }
                return top;
            }
        }

        // ── Path → waypoints ───────────────────────────────────────────────────

        private struct Key
        {
            public Vector3 p;
            public BotWaypoint.Action action;
            public float lane;
            public Vector3 watch;
            public float watchRadius;
            public bool footing;        // Wait for the swinging plank ahead to line up
        }

        private void WriteRoutes(List<List<(int node, Move move)>> routes, List<RaceCheckpoint> goals)
        {
            var parentOld = GameObject.Find("BotRoutes (auto)");
            if (parentOld != null) Undo.DestroyObjectImmediate(parentOld);

            var parent = new GameObject("BotRoutes (auto)");
            Undo.RegisterCreatedObjectUndo(parent, "Generate Bot Routes");

            for (int r = 0; r < routes.Count; r++)
            {
                var keys = ToKeys(routes[r]);
                var go = new GameObject($"BotRoute_{(char)('A' + r)}");
                Undo.RegisterCreatedObjectUndo(go, "Generate Bot Routes");
                go.transform.SetParent(parent.transform, false);
                Undo.AddComponent<BotRoute>(go);

                foreach (var k in keys)
                {
                    var wp = BotRouteTools.CreateWaypoint(go.transform, k.p + Vector3.up * 0.3f);
                    wp.action    = k.action;
                    wp.laneWidth = k.lane;
                    wp.radius    = k.action == BotWaypoint.Action.Walk ? 1.2f : 0.9f;
                    if (k.footing)
                    {
                        wp.waitFor = BotWaypoint.WaitFor.Footing;
                        wp.maxWait = 6f;
                    }
                    else if (k.action == BotWaypoint.Action.Wait)
                    {
                        var w = new GameObject("Watch");
                        Undo.RegisterCreatedObjectUndo(w, "Generate Bot Routes");
                        w.transform.SetParent(wp.transform, false);
                        w.transform.position = k.watch + Vector3.up;
                        wp.watchPoint  = w.transform;
                        wp.watchRadius = k.watchRadius;
                        wp.maxWait     = 4f;
                    }
                }
                BotRouteTools.Rename(go.transform);
            }

            EditorSceneManager.MarkSceneDirty(parent.scene);
            Selection.activeGameObject = parent;
        }

        private void LogRoutes(List<List<(int node, Move move)>> routes)
        {
            var sb = new System.Text.StringBuilder("[BotRouteGenerator] dry run waypoints\n");
            for (int r = 0; r < routes.Count; r++)
            {
                sb.AppendLine($"route {(char)('A' + r)}");
                var keys = ToKeys(routes[r]);
                for (int i = 0; i < keys.Count; i++)
                    sb.AppendLine($"  {i} {keys[i].action}{(keys[i].footing ? "(footing)" : "")} {keys[i].p + Vector3.up * 0.3f:F2} lane={keys[i].lane:F2}");
            }
            Debug.Log(sb.ToString());
        }

        private List<Key> ToKeys(List<(int node, Move move)> path)
        {
            var keys = new List<Key>();
            if (path.Count == 0) return keys;

            // Per-node flags along the path.
            int count = path.Count;
            var careful = new bool[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 a = m_nodes[path[Mathf.Max(0, i - 1)].node].p;
                Vector3 b = m_nodes[path[Mathf.Min(count - 1, i + 1)].node].p;
                careful[i] = IsNarrow(m_nodes[path[i].node].p, b - a);
            }

            // A point where the path must be marked: anywhere the kind of movement changes.
            bool MustKeep(int i)
            {
                if (i == 0 || i == count - 1) return true;
                var mv = path[i].move; var mvNext = path[i + 1].move;
                if (mv == Move.Jump || mv == Move.Pad || mv == Move.Drop) return true;          // landing
                if (mvNext == Move.Jump || mvNext == Move.Pad || mvNext == Move.Drop) return true; // take-off
                if (m_nodes[path[i].node].pad) return true;
                if (careful[i] != careful[i - 1] || careful[i] != careful[i + 1]) return true;
                int sk = m_nodes[path[i].node].SwingKey;
                if (sk != m_nodes[path[i + 1].node].SwingKey || sk != m_nodes[path[i - 1].node].SwingKey) return true;
                bool hz = m_nodes[path[i].node].hazard;
                if (hz != m_nodes[path[i + 1].node].hazard || hz != m_nodes[path[i - 1].node].hazard) return true;
                return false;
            }

            int anchor = 0;
            AddKey(keys, path, careful, 0);
            for (int i = 1; i < count; i++)
            {
                bool keep = MustKeep(i);
                // String-pull: keep going while a straight line from the anchor still stays on
                // walkable ground at a sensible height.
                if (!keep && i + 1 < count && StraightWalkable(m_nodes[path[anchor].node].p, m_nodes[path[i + 1].node].p))
                    continue;
                AddKey(keys, path, careful, i);
                anchor = i;
            }
            return keys;
        }

        private void AddKey(List<Key> keys, List<(int node, Move move)> path, bool[] careful, int i)
        {
            var node = m_nodes[path[i].node];
            var mvNext = i + 1 < path.Count ? path[i + 1].move : Move.Walk;
            var key = new Key { p = node.p, action = BotWaypoint.Action.Walk, lane = LaneAt(node) };

            // Hand-over to/from a swinging plank: wait here until it has swung into line, then
            // step (or jump) across.
            if (i + 1 < path.Count && NeedsFooting(node, m_nodes[path[i + 1].node]))
            {
                keys.Add(new Key { p = node.p, action = BotWaypoint.Action.Wait, footing = true });
                if (mvNext == Move.Jump)
                    keys.Add(new Key { p = node.p, action = BotWaypoint.Action.Jump });
                return;
            }

            if (mvNext == Move.Jump)
            {
                key.action = BotWaypoint.Action.Jump;
                key.lane   = 0f;
            }
            else if (i + 1 < path.Count && !node.hazard && m_nodes[path[i + 1].node].hazard)
            {
                // About to enter a hazard zone: wait here until the zone ahead is clear.
                int j = i + 1;
                Vector3 sum = Vector3.zero; int n = 0;
                Vector3 first = m_nodes[path[j].node].p, last = first;
                while (j < path.Count && m_nodes[path[j].node].hazard)
                {
                    last = m_nodes[path[j].node].p;
                    sum += last; n++; j++;
                }
                key.action      = BotWaypoint.Action.Wait;
                key.watch       = sum / Mathf.Max(1, n);
                key.watchRadius = Mathf.Clamp(Vector3.Distance(first, last) * 0.5f + 0.5f, 1.5f, 3.5f);
                key.lane        = 0f;
            }
            // Never creep through a hazard zone — careful = 60 % speed, and the spinner / dumbbell
            // swatted bots that crossed slowly. Run it.
            else if (i + 1 < path.Count && careful[i] && careful[i + 1] && !node.hazard)
            {
                key.action = BotWaypoint.Action.Careful;
                key.lane   = 0f;
            }

            if (keys.Count > 0 && Vector3.Distance(keys[^1].p, key.p) < 0.3f && key.action == BotWaypoint.Action.Walk)
                return;
            keys.Add(key);
        }

        // Spinning discs: their outer ring is no place to stand or land — the edge carries the
        // bot fastest, and the edit-time scan "bridges" the water gaps between discs there
        // (playtest: 11 of 15 deaths were bots walking across disc rims). Stay a cell in and
        // jump across instead.
        private static bool Standable(Node n) => !(n.platform && (n.edgeDist <= 1 || n.rim < 0.9f));

        // Landing a jump on narrow ground (rope bridge deck, ledge strip) — bots carry their run
        // speed and sail off the far side (playtest: Lily 10 deaths jumping sideways onto the L1
        // bridge, Blaze 7 onto the strip by the pad). Only when there is no other way.
        private static float NarrowLandingCost(Node n) => n.pad ? 0f : n.edgeDist <= 1 ? 25f : n.edgeDist == 2 ? 6f : 0f;

        private static bool NeedsFooting(Node from, Node to) =>
            to.swing >= 0 ? to.SwingKey != from.SwingKey : from.swing >= 0;

        private float LaneAt(Node n) => n.platform || n.swing >= 0 ? 0f : n.edgeDist >= 4 ? 2.5f : Mathf.Max(0f, (n.edgeDist - 1) * cellSize);

        // No ground on both sides within ~1.3 m → narrow crossing (bridge, plank, beam).
        private bool IsNarrow(Vector3 p, Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return false;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized);
            bool left = false, right = false;
            foreach (float s in k_sideProbe)
            {
                left  |= !HasGroundAt(p - side * s, p.y);
                right |= !HasGroundAt(p + side * s, p.y);
            }
            return left && right;
        }

        private static readonly float[] k_sideProbe = { 0.6f, 1.2f };

        private bool HasGroundAt(Vector3 world, float y)
        {
            int cx = Mathf.RoundToInt((world.x - m_origin.x) / cellSize);
            int cz = Mathf.RoundToInt((world.z - m_origin.z) / cellSize);
            return FindSurface(cx, cz, y, -1f, 0.6f) >= 0;
        }

        private bool StraightWalkable(Vector3 a, Vector3 b)
        {
            float len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / (cellSize * 0.5f)));
            for (int s = 1; s < steps; s++)
            {
                Vector3 q = Vector3.Lerp(a, b, s / (float)steps);
                int cx = Mathf.RoundToInt((q.x - m_origin.x) / cellSize);
                int cz = Mathf.RoundToInt((q.z - m_origin.z) / cellSize);
                int n = FindSurface(cx, cz, q.y, -0.8f, 0.8f);
                if (n < 0) return false;
                var nn = m_nodes[n];
                if (nn.hazard || nn.pad || nn.swing >= 0 || nn.edgeDist <= 1) return false;
            }
            return ClearBetween(a, b, 0.7f);
        }
    }
}
