using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AdventureMultiplayer;

/// <summary>
/// Real-time debug window for AIBotBrain.
/// Open via:  Adventure Multiplayer → AI Bot Debugger
/// </summary>
public class AIBotDebugWindow : EditorWindow
{
    // ── State ─────────────────────────────────────────────────────────────────

    private AIBotBrain   m_brain;
    private Vector2      m_logScroll;
    private bool         m_autoScroll  = true;
    private int          m_prevLogCount;

    // ── Styles (created lazily) ───────────────────────────────────────────────

    private GUIStyle m_stateBadgeStyle;
    private GUIStyle m_logLineStyle;
    private GUIStyle m_headerStyle;
    private GUIStyle m_labelStyle;
    private bool     m_stylesReady;

    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Adventure Multiplayer/AI Bot Debugger")]
    public static void ShowWindow()
    {
        var w = GetWindow<AIBotDebugWindow>("AI Bot Debugger");
        w.minSize = new Vector2(340, 420);
        w.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        m_stylesReady = false;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (EditorApplication.isPlaying)
            Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GUI
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EnsureStyles();

        // ── Bot selector ─────────────────────────────────────────────────────
        DrawBotSelector();

        if (m_brain == null)
        {
            EditorGUILayout.HelpBox(
                EditorApplication.isPlaying
                    ? "No AIBotBrain found in the scene."
                    : "Enter Play Mode to use the debugger.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(4);

        // ── State badge ──────────────────────────────────────────────────────
        DrawStateBadge(m_brain.CurrentState);

        EditorGUILayout.Space(6);

        // ── Info grid ────────────────────────────────────────────────────────
        DrawInfoGrid();

        EditorGUILayout.Space(6);
        DrawSeparator();

        // ── Planned path ─────────────────────────────────────────────────────
        DrawPlannedPath();

        EditorGUILayout.Space(4);
        DrawSeparator();

        // ── Active abilities ─────────────────────────────────────────────────
        DrawActiveAbilities();

        EditorGUILayout.Space(4);
        DrawSeparator();

        // ── Scan scores ───────────────────────────────────────────────────────
        DrawScanScores();

        EditorGUILayout.Space(4);
        DrawSeparator();

        // ── Log ──────────────────────────────────────────────────────────────
        DrawLog();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bot selector
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawBotSelector()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Bot", EditorStyles.toolbarButton, GUILayout.Width(28));

        if (!EditorApplication.isPlaying)
        {
            GUILayout.Label("—  (not in Play Mode)", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return;
        }

        // Gather all brains in scene
        var brains = FindObjectsByType<AIBotBrain>(FindObjectsSortMode.None);

        if (brains.Length == 0)
        {
            GUILayout.Label("No AIBotBrain in scene", EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            m_brain = null;
            return;
        }

        // Auto-select if only one
        if (m_brain == null || System.Array.IndexOf(brains, m_brain) < 0)
            m_brain = brains[0];

        // Build name list
        var names = new string[brains.Length];
        for (int i = 0; i < brains.Length; i++)
            names[i] = brains[i].gameObject.name;

        int current = System.Array.IndexOf(brains, m_brain);
        int selected = EditorGUILayout.Popup(current, names, EditorStyles.toolbarPopup);
        if (selected != current) m_brain = brains[selected];

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(36)))
            EditorGUIUtility.PingObject(m_brain.gameObject);

        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State badge
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawStateBadge(AIState state)
    {
        Color bg  = GetStateColor(state);
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = bg;
        EditorGUILayout.BeginVertical(m_stateBadgeStyle);
        GUI.backgroundColor = prev;

        GUILayout.Label(state.ToString(), m_headerStyle);
        GUILayout.Label($"Time in state: {m_brain.TimeInState:F1} s", m_labelStyle);

        EditorGUILayout.EndVertical();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Info grid
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawInfoGrid()
    {
        float dist = m_brain.TargetDist;
        string distStr = dist >= 0f ? $"{dist:F2} m" : "—";

        DrawRow("Target",      m_brain.TargetName);
        DrawRow("Distance",    distStr);
        DrawRow("Launcher",    m_brain.ActiveLauncher);
        DrawRow("Blacklisted", $"{m_brain.BlacklistCount}");
    }

    private void DrawRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label(value, m_labelStyle);
        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Planned path panel
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawPlannedPath()
    {
        GUILayout.Label("Planned Path", EditorStyles.boldLabel);

        if (m_brain.CurrentState == AIState.Idle || m_brain.PathTargetPos == null)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("  — no active route —", m_labelStyle);
            GUI.color = prev;
            return;
        }

        Vector3 botPos    = m_brain.transform.position;
        Vector3 targetPos = m_brain.PathTargetPos.Value;

        if (m_brain.PathLauncherEntry.HasValue)
        {
            // Multi-hop: Bot → Launcher entry → (arc) → Launcher exit → Target
            Vector3 entry = m_brain.PathLauncherEntry.Value;
            Vector3 exit  = m_brain.PathLauncherExit.Value;

            DrawHop("Bot",                           null);
            DrawHop($"walk  {Vector3.Distance(botPos, entry):F1} m",     new Color(0.3f, 0.85f, 1f));
            DrawHop(m_brain.PathLauncherType + " entry",                 new Color(0.5f, 0.6f, 1f));

            if (m_brain.PathLauncherApex.HasValue)
            {
                Vector3 apex = m_brain.PathLauncherApex.Value;
                DrawHop($"arc  apex {apex.y:F1} m high",                new Color(0.5f, 0.5f, 1f));
            }
            else
            {
                DrawHop($"{m_brain.PathLauncherType}  {Vector3.Distance(entry, exit):F1} m", new Color(0.5f, 0.5f, 1f));
            }

            DrawHop($"{m_brain.PathLauncherType} exit",                  new Color(0.5f, 0.6f, 1f));
            DrawHop($"walk  {Vector3.Distance(exit, targetPos):F1} m",  new Color(0.3f, 1f, 0.5f));
            DrawHop(m_brain.TargetName,                                  Color.yellow);
        }
        else
        {
            // Direct walk
            DrawHop("Bot",                                               null);
            DrawHop($"walk  {Vector3.Distance(botPos, targetPos):F1} m", new Color(0.3f, 1f, 0.5f));
            DrawHop(m_brain.TargetName,                                  Color.yellow);
        }
    }

    private void DrawHop(string label, Color? lineColor)
    {
        if (lineColor.HasValue)
        {
            var prev = GUI.color;
            GUI.color = lineColor.Value;
            GUILayout.Label($"  │  {label}", m_labelStyle);
            GUI.color = prev;
        }
        else
        {
            GUILayout.Label($"  ◉  {label}", m_labelStyle);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Active abilities panel
    // ─────────────────────────────────────────────────────────────────────────

    private bool m_abilitiesFoldout = true;

    private void DrawActiveAbilities()
    {
        m_abilitiesFoldout = EditorGUILayout.Foldout(m_abilitiesFoldout, "Active Abilities", true, EditorStyles.foldoutHeader);
        if (!m_abilitiesFoldout) return;

        var inp = m_brain.InputManager;
        if (inp == null)
        {
            var prev2 = GUI.color; GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("  — AIPlayerInputManager not found —", m_labelStyle);
            GUI.color = prev2;
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawAbilityPill("Glide",   inp.glideHeld);
        DrawAbilityPill("Dive",    inp.diveHeld);
        DrawAbilityPill("Swim↑",   inp.swimUpwardHeld);
        DrawAbilityPill("Roll",    inp.rollHeld);
        DrawAbilityPill("Crouch",  inp.crouchHeld);
        DrawAbilityPill("Run",     inp.runHeld);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawAbilityPill("Spin",    inp.spinQueued);
        DrawAbilityPill("Stomp",   inp.stompQueued);
        DrawAbilityPill("Dash",    inp.dashQueued);
        DrawAbilityPill("AirDive", inp.airDiveQueued);
        DrawAbilityPill("Jump",    inp.jumpQueued);
        DrawAbilityPill("P&D",     inp.pickAndDropQueued);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
    }

    private void DrawAbilityPill(string label, bool active)
    {
        var prevBg = GUI.backgroundColor;
        var prevFg = GUI.color;
        GUI.backgroundColor = active ? new Color(0.18f, 0.72f, 0.22f) : new Color(0.25f, 0.25f, 0.25f);
        GUI.color           = active ? Color.white : new Color(0.55f, 0.55f, 0.55f);
        GUILayout.Label(label, EditorStyles.miniButton, GUILayout.Width(52), GUILayout.Height(18));
        GUI.backgroundColor = prevBg;
        GUI.color           = prevFg;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scan scores table
    // ─────────────────────────────────────────────────────────────────────────

    private Vector2 m_scanScroll;
    private bool    m_scanFoldout = true;

    private void DrawScanScores()
    {
        m_scanFoldout = EditorGUILayout.Foldout(m_scanFoldout, "Last Scan Scores", true, EditorStyles.foldoutHeader);
        if (!m_scanFoldout) return;

        var entries = m_brain.ScanEntries;
        if (entries == null || entries.Count == 0)
        {
            var prev = GUI.color; GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label("  — no scan data yet —", m_labelStyle);
            GUI.color = prev;
            return;
        }

        // Column header
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Name",      EditorStyles.miniLabel, GUILayout.Width(90));
        GUILayout.Label("Dist",      EditorStyles.miniLabel, GUILayout.Width(42));
        GUILayout.Label("hDiff",     EditorStyles.miniLabel, GUILayout.Width(42));
        GUILayout.Label("Nav",       EditorStyles.miniLabel, GUILayout.Width(32));
        GUILayout.Label("Walk",      EditorStyles.miniLabel, GUILayout.Width(42));
        GUILayout.Label("Launch",    EditorStyles.miniLabel, GUILayout.Width(42));
        GUILayout.Label("Via",       EditorStyles.miniLabel, GUILayout.Width(46));
        GUILayout.Label("",          EditorStyles.miniLabel, GUILayout.Width(16)); // winner star
        EditorGUILayout.EndHorizontal();

        DrawSeparator();

        // Scrollable rows — cap height so it doesn't consume the whole window
        float rowH   = EditorGUIUtility.singleLineHeight;
        float maxH   = rowH * Mathf.Min(entries.Count + 1, 8);
        m_scanScroll = EditorGUILayout.BeginScrollView(m_scanScroll,
            GUILayout.Height(maxH));

        foreach (var e in entries)
        {
            // Row background tint for winner
            if (e.isWinner)
            {
                var r = EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));
                EditorGUI.DrawRect(r, new Color(0.18f, 0.42f, 0.18f, 0.35f));
            }
            else
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(rowH));
            }

            // Name — gray if blacklisted
            Color nameCol = e.isBlacklisted ? new Color(0.5f, 0.5f, 0.5f)
                          : e.isWinner      ? Color.white
                          :                   new Color(0.78f, 0.78f, 0.78f);
            ColorLabel(Truncate(e.name, 13), nameCol, GUILayout.Width(90));

            // Dist
            ColorLabel($"{e.dist:F1}", new Color(0.7f, 0.7f, 0.7f), GUILayout.Width(42));

            // hDiff — red if heightOk=false, green if ok
            Color hCol = e.isBlacklisted ? new Color(0.5f,0.5f,0.5f)
                       : e.heightOk      ? new Color(0.4f, 1f, 0.4f)
                       :                   new Color(1f, 0.4f, 0.4f);
            ColorLabel($"{e.hDiff:+0.0;-0.0}", hCol, GUILayout.Width(42));

            // Nav status
            Color navCol = e.navStatus == "Full" ? new Color(0.3f, 1f, 0.3f)
                         : e.navStatus == "Part" ? new Color(1f, 0.85f, 0.2f)
                         :                         new Color(0.6f, 0.6f, 0.6f);
            ColorLabel(e.navStatus, navCol, GUILayout.Width(32));

            // Walk score
            string walkStr = e.walkScore > float.MinValue ? e.walkScore.ToString("F2") : "—";
            Color  walkCol = e.walkScore > float.MinValue ? new Color(0.3f, 1f, 0.5f)
                                                          : new Color(0.45f, 0.45f, 0.45f);
            ColorLabel(walkStr, walkCol, GUILayout.Width(42));

            // Best launch score
            string launchStr = e.launchScore > float.MinValue ? e.launchScore.ToString("F2") : "—";
            Color  launchCol = e.launchScore > float.MinValue ? new Color(0.4f, 0.7f, 1f)
                                                              : new Color(0.45f, 0.45f, 0.45f);
            ColorLabel(launchStr, launchCol, GUILayout.Width(42));

            // Via type
            ColorLabel(e.launchScore > float.MinValue ? e.launchType : "walk",
                new Color(0.65f, 0.65f, 0.65f), GUILayout.Width(46));

            // Winner star
            if (e.isWinner)
                ColorLabel("★", Color.yellow, GUILayout.Width(16));
            else
                GUILayout.Label("", GUILayout.Width(16));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ColorLabel(string text, Color color, params GUILayoutOption[] opts)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUILayout.Label(text, m_logLineStyle, opts);
        GUI.color = prev;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    // ─────────────────────────────────────────────────────────────────────────
    // Log panel
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawLog()
    {
        // Header row
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Event Log", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        m_autoScroll = GUILayout.Toggle(m_autoScroll, "Auto-scroll", GUILayout.Width(86));

        if (GUILayout.Button("Copy", GUILayout.Width(44)))
        {
            var sb = new System.Text.StringBuilder();
            var copyLog = m_brain.Log;
            sb.AppendLine($"=== AI Bot Log — {m_brain.gameObject.name} ===");
            sb.AppendLine($"State: {m_brain.CurrentState}  Target: {m_brain.TargetName}  Launcher: {m_brain.ActiveLauncher}");
            sb.AppendLine();
            foreach (var entry in copyLog)
            {
                string tt = entry.type switch
                {
                    AILogType.Ability      => "ABL",
                    AILogType.Movement     => "MOV",
                    AILogType.StateChange  => "STA",
                    AILogType.TargetChange => "TGT",
                    AILogType.Warning      => "WRN",
                    AILogType.Error        => "ERR",
                    _                      => "   ",
                };
                sb.AppendLine($"[{entry.time,6:F2}] [{tt}]  {entry.message}");
            }
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }

        if (GUILayout.Button("Clear", GUILayout.Width(44)))
        {
            // Can't directly clear the log (it's read-only) — just scroll to top
            m_logScroll = Vector2.zero;
        }
        EditorGUILayout.EndHorizontal();

        DrawSeparator();

        var log = m_brain.Log;
        int count = log.Count;

        // Auto-scroll when new entries arrive
        if (m_autoScroll && count > m_prevLogCount)
        {
            m_logScroll.y = float.MaxValue;
        }
        m_prevLogCount = count;

        m_logScroll = EditorGUILayout.BeginScrollView(m_logScroll);

        for (int i = 0; i < count; i++)
        {
            var entry = log[i];
            Color c = GetLogColor(entry.type);
            var prev = GUI.color;
            GUI.color = c;
            string typeTag = entry.type switch
            {
                AILogType.Ability      => "ABL",
                AILogType.Movement     => "MOV",
                AILogType.StateChange  => "STA",
                AILogType.TargetChange => "TGT",
                AILogType.Warning      => "WRN",
                AILogType.Error        => "ERR",
                _                      => "   ",
            };
            GUILayout.Label(
                $"[{entry.time,6:F2}] [{typeTag}]  {entry.message}",
                m_logLineStyle);
            GUI.color = prev;
        }

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
        EditorGUILayout.Space(2);
    }

    private void EnsureStyles()
    {
        if (m_stylesReady) return;
        m_stylesReady = true;

        m_stateBadgeStyle = new GUIStyle(GUI.skin.box)
        {
            padding  = new RectOffset(10, 10, 8, 8),
            margin   = new RectOffset(4, 4, 0, 0),
        };

        m_headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 16,
            alignment = TextAnchor.MiddleCenter,
        };

        m_labelStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = false,
        };

        m_logLineStyle = new GUIStyle(EditorStyles.label)
        {
            richText = false,
            wordWrap = true,
            fontSize = 11,
            font     = Font.CreateDynamicFontFromOSFont("Courier New", 11),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Color maps
    // ─────────────────────────────────────────────────────────────────────────

    private static Color GetStateColor(AIState state) => state switch
    {
        AIState.Idle              => new Color(0.38f, 0.38f, 0.38f),
        AIState.WalkToCollectible => new Color(0.18f, 0.72f, 0.22f),
        AIState.WalkToLauncher    => new Color(0.18f, 0.72f, 0.72f),
        AIState.UseSpring         => new Color(0.22f, 0.46f, 0.92f),
        AIState.ClimbPole         => new Color(0.22f, 0.46f, 0.92f),
        AIState.UseRail           => new Color(0.22f, 0.46f, 0.92f),
        AIState.UsePortal         => new Color(0.52f, 0.22f, 0.92f),
        AIState.WaitForPlatform   => new Color(0.76f, 0.36f, 0.92f),
        AIState.BoardPlatform     => new Color(0.76f, 0.36f, 0.92f),
        AIState.RidePlatform      => new Color(0.76f, 0.36f, 0.92f),
        AIState.UseForceField     => new Color(0.92f, 0.62f, 0.18f),
        AIState.UseSpeedBooster   => new Color(0.92f, 0.82f, 0.18f),
        _                         => Color.white,
    };

    private static Color GetLogColor(AILogType type) => type switch
    {
        AILogType.Warning      => new Color(1.00f, 0.88f, 0.18f),
        AILogType.Error        => new Color(1.00f, 0.38f, 0.38f),
        AILogType.StateChange  => new Color(0.38f, 0.90f, 1.00f),
        AILogType.TargetChange => new Color(0.38f, 1.00f, 0.48f),
        AILogType.Ability      => new Color(1.00f, 0.60f, 0.15f),   // orange
        AILogType.Movement     => new Color(0.78f, 0.55f, 1.00f),   // light purple
        _                      => new Color(0.76f, 0.76f, 0.76f),
    };
}
