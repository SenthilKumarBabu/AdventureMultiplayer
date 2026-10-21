using System.Collections.Generic;
using System.IO;
using System.Text;
using PLAYERTWO.PlatformerProject;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Dumps the open race level's walkable surfaces, checkpoints and moving obstacles to a text
    /// file so bot routes can be checked/authored outside the Editor. Runs from the menu, or once
    /// automatically after a recompile when <c>Temp/BotRouteProbe.request</c> exists.
    /// Output: <c>Temp/BotRouteProbe.txt</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class BotRouteLevelProbe
    {
        private const string k_request = "Temp/BotRouteProbe.request";
        private const string k_output  = "Temp/BotRouteProbe.txt";

        private const string k_generateRequest = "Temp/BotRouteGenerate.request";

        static BotRouteLevelProbe()
        {
            // Requests wait (re-checked every couple of seconds) until a race level — a scene with
            // a RaceManager — is open in Edit mode, so they never run against / save the Lobby.
            if (File.Exists(k_generateRequest) || File.Exists(k_request))
                EditorApplication.update += PollRequests;
        }

        private static double s_nextPoll;

        private static void PollRequests()
        {
            if (EditorApplication.timeSinceStartup < s_nextPoll) return;
            s_nextPoll = EditorApplication.timeSinceStartup + 2.0;

            bool gen = File.Exists(k_generateRequest), probe = File.Exists(k_request);
            if (!gen && !probe) { EditorApplication.update -= PollRequests; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            // The request file may name the level (one line, e.g. Assets/RC/Scenes/DeathRunL1.unity).
            // If that level isn't open, open it additively for the job and close it afterwards,
            // so the user's open scene (Lobby) is never touched.
            string levelPath = ReadLevelPath(gen ? k_generateRequest : k_request);
            var previousActive = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEngine.SceneManagement.Scene level = default;
            bool openedHere = false;
            if (!string.IsNullOrEmpty(levelPath))
            {
                level = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(levelPath);
                if (!level.IsValid() || !level.isLoaded)
                {
                    level = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(levelPath,
                        UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    openedHere = true;
                }
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(level);
            }
            if (Object.FindFirstObjectByType<RaceManager>() == null) return; // not a race level

            try
            {
                if (gen)
                {
                    File.Delete(k_generateRequest);
                    string msg = BotRouteGenerator.GenerateInScene();
                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    bool saved = msg != null && UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    File.WriteAllText("Temp/BotRouteGenerate.txt", $"{msg}\nsaved={saved} scene={scene.path}");
                }
                if (probe)
                {
                    File.Delete(k_request);
                    Run();
                }
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded && previousActive != level)
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActive);
                if (openedHere && level.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(level, true);
            }
        }

        private static string ReadLevelPath(string requestFile)
        {
            try
            {
                string s = File.Exists(requestFile) ? File.ReadAllText(requestFile).Trim() : "";
                return s.EndsWith(".unity") && File.Exists(s) ? s : null;
            }
            catch (IOException) { return null; }
        }

        [MenuItem("Tools/RC/Bot Route/Dump Level Probe")]
        public static void Run()
        {
            var sb  = new StringBuilder();
            var ids = new Dictionary<Collider, int>();
            var cols = new List<Collider>();

            int Id(Collider c)
            {
                if (!ids.TryGetValue(c, out int id)) { id = cols.Count; ids[c] = id; cols.Add(c); }
                return id;
            }

            sb.AppendLine($"scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}");

            var rm = Object.FindFirstObjectByType<RaceManager>();
            sb.AppendLine("== checkpoints");
            if (rm != null && rm.Checkpoints != null)
                foreach (var cp in rm.Checkpoints)
                {
                    if (cp == null) continue;
                    var c = cp.GetComponent<Collider>();
                    sb.AppendLine($"cp {cp.name} index={cp.index} pos={V(cp.transform.position)} " +
                                  (c != null ? $"bounds={V(c.bounds.min)}..{V(c.bounds.max)}" : "no-collider"));
                }

            sb.AppendLine("== killzones");
            foreach (var kz in Object.FindObjectsByType<KillZone>(FindObjectsSortMode.None))
            {
                var c = kz.GetComponent<Collider>();
                if (c != null) sb.AppendLine($"kill {Path(kz.transform)} bounds={V(c.bounds.min)}..{V(c.bounds.max)}");
            }

            sb.AppendLine("== hints");
            foreach (var h in Object.FindObjectsByType<BotObstacleHint>(FindObjectsSortMode.None))
                sb.AppendLine($"hint {Path(h.transform)} behavior={h.behavior}" +
                              (h.HasJumpLine ? $" jump {V(h.jumpFrom.position)}->{V(h.jumpTo.position)}" : "") +
                              (h.HasCrossingLine ? $" line {V(h.pathA.position)}->{V(h.pathB.position)}" : "") +
                              (h.HasStoneCrossing ? $" stones {V(h.stoneStart.position)}->{V(h.stoneEnd.position)}" : ""));

            sb.AppendLine("== moving / special colliders");
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!c.enabled || c.isTrigger) continue;
                string tag = Classify(c);
                if (tag.Length == 0) continue;
                sb.AppendLine($"#{Id(c)} {tag} {Path(c.transform)} [{c.GetType().Name}] bounds={V(c.bounds.min)}..{V(c.bounds.max)}" +
                              (c is MeshCollider mc && mc.sharedMesh == null ? " EMPTY-MESH" : ""));
            }

            // Generator dry run: why does A* fail where it fails?
            sb.AppendLine("== generator");
            var logs = new StringBuilder();
            void Capture(string msg, string st, LogType t) { if (msg.Contains("BotRouteGenerator")) logs.AppendLine(msg); }
            Application.logMessageReceived += Capture;
            try { sb.AppendLine(BotRouteGenerator.Diagnose() ?? "(null)"); }
            catch (System.Exception e) { sb.AppendLine("EXCEPTION " + e); }
            finally { Application.logMessageReceived -= Capture; }
            sb.AppendLine(logs.ToString());

            // Surface grid: every upward-facing surface per cell, top first. Format:
            // z x: y#collider[,y#collider...]
            sb.AppendLine("== surfaces (cell 0.75)");
            float x0 = -18f, x1 = 18f, z0 = -216f, z1 = 100f, step = 0.75f;
            if (rm != null && rm.Checkpoints != null)
                foreach (var cp in rm.Checkpoints)
                    if (cp != null) z1 = Mathf.Max(z1, cp.transform.position.z + 8f);
            for (float z = z0; z <= z1; z += step)
            {
                var row = new StringBuilder();
                for (float x = x0; x <= x1; x += step)
                {
                    var hits = Physics.SphereCastAll(new Vector3(x, 80f, z), 0.2f, Vector3.down, 120f,
                                                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
                    var cell = new StringBuilder();
                    float lastY = float.MaxValue;
                    foreach (var h in hits)
                    {
                        if (h.distance <= 0f || h.normal.y < 0.65f) continue;
                        if (h.collider.GetComponentInParent<Player>() != null) continue;
                        if (Mathf.Abs(lastY - h.point.y) < 0.3f) continue;
                        lastY = h.point.y;
                        if (cell.Length > 0) cell.Append(',');
                        cell.Append(h.point.y.ToString("F2")).Append('#').Append(Id(h.collider));
                    }
                    if (cell.Length > 0) row.Append($" {x:F2}:{cell}");
                }
                if (row.Length > 0) sb.AppendLine($"z{z:F2}{row}");
            }

            sb.AppendLine("== colliders");
            for (int i = 0; i < cols.Count; i++)
            {
                var c = cols[i];
                sb.AppendLine($"#{i} {Path(c.transform)} [{c.GetType().Name}] {Classify(c)} bounds={V(c.bounds.min)}..{V(c.bounds.max)}");
            }

            File.WriteAllText(k_output, sb.ToString());
            Debug.Log($"[BotRouteLevelProbe] wrote {k_output} ({sb.Length / 1024} KB, {cols.Count} colliders)");
        }

        private static string Classify(Collider c)
        {
            var tags = new List<string>();
            if (RaceBotBrain.IsBouncePlatform(c))     tags.Add("PAD");
            if (RaceBotBrain.IsRideablePlatform(c))   tags.Add("PLATFORM");
            if (RaceBotBrain.IsWalkableLog(c))        tags.Add("LOG");
            if (RaceBotBrain.IsTimedHazard(c))        tags.Add("TIMED");
            if (RaceBotBrain.FindObstacleKnockback(c) != null) tags.Add("KNOCK");
            if (c.GetComponentInParent<Hazard>() != null)      tags.Add("HAZARD");
            foreach (var mb in c.GetComponentsInParent<MonoBehaviour>())
                if (mb != null && (mb.GetType().Name.Contains("Rotat") || mb.GetType().Name.Contains("Oscillat")))
                { tags.Add(mb.GetType().Name); break; }
            return string.Join("|", tags);
        }

        private static string V(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";

        private static string Path(Transform t)
        {
            string p = t.name;
            for (var c = t.parent; c != null; c = c.parent) p = c.name + "/" + p;
            return p;
        }
    }
}
