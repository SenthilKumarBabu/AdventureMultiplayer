using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Authoring helpers for <see cref="BotRoute"/>: build a starter route from the scene's
    /// checkpoints, insert waypoints between existing ones, and snap them onto the ground.
    /// </summary>
    public static class BotRouteTools
    {
        // Creates the route with ONE waypoint at the race start (centre of the BotSpawner's spawn
        // points). Build the rest forward from there with "Insert Waypoint After This".
        [MenuItem("Tools/RC/Bot Route/Create From Start")]
        public static void CreateFromStart()
        {
            var existing = Object.FindFirstObjectByType<BotRoute>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Bot Route", "This scene already has a BotRoute (selected).", "OK");
                return;
            }

            if (!TryGetStartPosition(out Vector3 start))
            {
                EditorUtility.DisplayDialog("Bot Route", "No BotSpawner with spawn points found in the open scene.", "OK");
                return;
            }

            var root = new GameObject("BotRoute");
            Undo.RegisterCreatedObjectUndo(root, "Create Bot Route");
            Undo.AddComponent<BotRoute>(root);

            var wp = CreateWaypoint(root.transform, start);
            SnapToGround(wp.transform);

            // Then one waypoint per checkpoint, in race order.
            var rm = Object.FindFirstObjectByType<RaceManager>();
            if (rm != null && rm.Checkpoints != null)
                foreach (var cp in rm.Checkpoints)
                {
                    if (cp == null) continue;
                    var cpWp = CreateWaypoint(root.transform, cp.transform.position);
                    SnapToGround(cpWp.transform);
                }

            Rename(root.transform);
            Selection.activeGameObject = wp.gameObject;
            EditorGUIUtility.PingObject(wp.gameObject);
        }

        private static bool TryGetStartPosition(out Vector3 start)
        {
            start = Vector3.zero;
            var spawner = Object.FindFirstObjectByType<BotSpawner>();
            if (spawner == null) return false;

            var prop = new SerializedObject(spawner).FindProperty("spawnPoints");
            int count = 0;
            for (int i = 0; prop != null && i < prop.arraySize; i++)
            {
                var t = prop.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (t == null) continue;
                start += t.position;
                count++;
            }
            if (count == 0) return false;
            start /= count;
            return true;
        }

        // Which way a new waypoint after the last one should go: continue the last segment,
        // or for a one-point route, head for the first checkpoint.
        private static Vector3 ForwardFrom(BotWaypoint[] pts, int i)
        {
            Vector3 d = Vector3.zero;
            if (i > 0) d = pts[i].transform.position - pts[i - 1].transform.position;
            else
            {
                var rm = Object.FindFirstObjectByType<RaceManager>();
                if (rm != null && rm.Checkpoints != null && rm.Checkpoints.Length > 0 && rm.Checkpoints[0] != null)
                    d = rm.Checkpoints[0].transform.position - pts[i].transform.position;
            }
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
        }

        public static BotWaypoint CreateWaypoint(Transform parent, Vector3 pos)
        {
            var go = new GameObject("WP");
            Undo.RegisterCreatedObjectUndo(go, "Add Bot Waypoint");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return Undo.AddComponent<BotWaypoint>(go);
        }

        public static void SnapToGround(Transform t)
        {
            Vector3 origin = t.position + Vector3.up * 3f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Undo.RecordObject(t, "Snap Waypoint To Ground");
                t.position = hit.point + Vector3.up * 0.3f;
            }
        }

        public static void Rename(Transform root)
        {
            var pts = root.GetComponentsInChildren<BotWaypoint>();
            for (int i = 0; i < pts.Length; i++)
            {
                string n = $"WP_{i:00}_{pts[i].action}";
                if (pts[i].name != n)
                {
                    Undo.RecordObject(pts[i].gameObject, "Rename Waypoints");
                    pts[i].name = n;
                }
            }
        }

        public static void InsertAfter(BotWaypoint wp)
        {
            var route = wp.GetComponentInParent<BotRoute>();
            var pts   = route != null ? route.GetComponentsInChildren<BotWaypoint>() : new[] { wp };
            int i     = System.Array.IndexOf(pts, wp);

            Vector3 pos = i >= 0 && i + 1 < pts.Length
                ? Vector3.Lerp(wp.transform.position, pts[i + 1].transform.position, 0.5f)
                : wp.transform.position + ForwardFrom(pts, Mathf.Max(i, 0)) * 4f;

            var created = CreateWaypoint(wp.transform.parent, pos);
            if (i + 1 >= pts.Length) SnapToGround(created.transform);
            Undo.SetTransformParent(created.transform, wp.transform.parent, "Insert Waypoint");
            created.transform.SetSiblingIndex(wp.transform.GetSiblingIndex() + 1);
            created.laneWidth = wp.laneWidth;
            if (route != null) Rename(route.transform);
            Selection.activeGameObject = created.gameObject;
        }
    }

    [CustomEditor(typeof(BotRoute))]
    public class BotRouteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var route = (BotRoute)target;
            EditorGUILayout.HelpBox(
                "Waypoints = child objects with BotWaypoint, in hierarchy order.\n" +
                "Action = what the bot does when LEAVING that point:\n" +
                "  Walk (green) · Careful (yellow, bridges/discs) · Jump (red, take-off point)\n" +
                "  DoubleJump (pink) · Wait (blue, until the hazard zone is clear)",
                MessageType.Info);

            if (GUILayout.Button("Add Waypoint At End"))
            {
                var pts = route.GetComponentsInChildren<BotWaypoint>();
                if (pts.Length > 0) BotRouteTools.InsertAfter(pts[^1]);
                else
                {
                    var wp = BotRouteTools.CreateWaypoint(route.transform, route.transform.position);
                    BotRouteTools.Rename(route.transform);
                    Selection.activeGameObject = wp.gameObject;
                }
            }
            if (GUILayout.Button("Snap All To Ground"))
                foreach (var wp in route.GetComponentsInChildren<BotWaypoint>())
                    BotRouteTools.SnapToGround(wp.transform);
            if (GUILayout.Button("Rename Waypoints"))
                BotRouteTools.Rename(route.transform);
        }
    }

    [CustomEditor(typeof(BotWaypoint)), CanEditMultipleObjects]
    public class BotWaypointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            var wp = (BotWaypoint)target;
            var route = wp.GetComponentInParent<BotRoute>();
            if (changed && route != null) BotRouteTools.Rename(route.transform);

            EditorGUILayout.Space();
            if (GUILayout.Button("Insert Waypoint After This"))
                BotRouteTools.InsertAfter(wp);
            if (GUILayout.Button("Snap To Ground"))
                foreach (var t in targets)
                    BotRouteTools.SnapToGround(((BotWaypoint)t).transform);
            if (route != null && GUILayout.Button("Select Route"))
                Selection.activeGameObject = route.gameObject;
        }
    }
}
