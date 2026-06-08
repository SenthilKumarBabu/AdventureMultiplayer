using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Centers the scene Environment GameObject's content at world origin.
    ///
    /// Steps:
    ///   1. Finds the GameObject named "Environment" in the active scene.
    ///   2. Computes the combined bounding box of all Renderers in its hierarchy.
    ///   3. Offsets every DIRECT child of Environment by -boundsCenter so the
    ///      collective visual content is centered on the origin.
    ///      Grandchildren and deeper nodes are not touched — they follow their
    ///      parent naturally, preserving the relative layout of the level.
    ///
    /// Usage: Adventure Multiplayer / Utils / Center Environment
    /// </summary>
    public static class EnvironmentCenterer
    {
        [MenuItem("Adventure Multiplayer/Utils/Center Environment")]
        public static void CenterEnvironment()
        {
            var env = GameObject.Find("Environment");
            if (env == null)
            {
                EditorUtility.DisplayDialog("Center Environment",
                    "No GameObject named 'Environment' found in the active scene.", "OK");
                return;
            }

            // ── 1. Compute world-space bounding box via MeshFilter.sharedMesh ─────
            // Renderer.bounds returns zero in editor mode when objects haven't
            // been rendered yet. sharedMesh.bounds is always valid if the mesh
            // asset is loaded, so we transform its corners to world space manually.
            //
            // Only gather meshes from direct children that are CONTAINERS (have
            // their own children). Leaf-node direct children (e.g. a flat water
            // plane with no children) span symmetrically around X=0 / Z=0 and
            // would pull the computed center to zero, masking the real offset.
            var envT = env.transform;
            var meshFilters = new List<MeshFilter>();
            foreach (Transform child in envT)
            {
                if (child.childCount == 0)
                {
                    Debug.Log($"[EnvironmentCenterer] Skipping '{child.name}' (leaf node — likely a flat plane).");
                    continue;
                }
                var mfs = child.GetComponentsInChildren<MeshFilter>(true);
                Debug.Log($"[EnvironmentCenterer] Including '{child.name}' — {mfs.Length} MeshFilters.");
                meshFilters.AddRange(mfs);
            }
            if (meshFilters.Count == 0)
            {
                EditorUtility.DisplayDialog("Center Environment",
                    "No MeshFilters found under Environment — cannot compute bounds.", "OK");
                return;
            }

            bool boundsInitialized = false;
            var bounds = new Bounds();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var matrix = mf.transform.localToWorldMatrix;
                var mb     = mf.sharedMesh.bounds;
                Vector3 c  = mb.center;
                Vector3 e  = mb.extents;
                // Transform all 8 corners of the local AABB into world space
                Vector3[] corners = {
                    matrix.MultiplyPoint3x4(c + new Vector3( e.x,  e.y,  e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3(-e.x,  e.y,  e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3( e.x, -e.y,  e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y,  e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3( e.x,  e.y, -e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3(-e.x,  e.y, -e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3( e.x, -e.y, -e.z)),
                    matrix.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z)),
                };
                foreach (var corner in corners)
                {
                    if (!boundsInitialized) { bounds = new Bounds(corner, Vector3.zero); boundsInitialized = true; }
                    else bounds.Encapsulate(corner);
                }
            }

            if (!boundsInitialized)
            {
                EditorUtility.DisplayDialog("Center Environment",
                    "No valid meshes found under Environment — cannot compute bounds.", "OK");
                return;
            }

            Vector3 contentCenter = bounds.center;
            Debug.Log($"[EnvironmentCenterer] Computed bounds center: {contentCenter} (size: {bounds.size}) from {meshFilters.Count} MeshFilters.");

            // ── 2. Snapshot direct children's world transforms ──────────────────
            Vector3 envOrigPos = envT.position;
            var snapshot    = new List<(Transform t, Vector3 worldPos, Quaternion worldRot)>();
            foreach (Transform child in envT)
                snapshot.Add((child, child.position, child.rotation));

            // ── 3. Register undo ────────────────────────────────────────────────
            Undo.RecordObject(envT, "Center Environment");
            foreach (var (t, _, _) in snapshot)
                Undo.RecordObject(t, "Center Environment");

            // ── 4. Zero Environment's position ──────────────────────────────────
            // Unity preserves children's world positions when the parent moves,
            // so the snapshot remains valid after this step.
            envT.position = Vector3.zero;

            // ── 5. Shift direct children so content center lands at origin ──────
            // With Environment now at (0,0,0), local == world for direct children,
            // so after this step their local positions will be near zero.
            // Grandchildren are not touched — their local transforms relative to
            // their own parent are unchanged, so the level layout is preserved.
            foreach (var (t, worldPos, worldRot) in snapshot)
                t.SetPositionAndRotation(worldPos - contentCenter, worldRot);

            Debug.Log($"[EnvironmentCenterer] Environment position zeroed (was {envOrigPos}).");
            Debug.Log($"[EnvironmentCenterer] Content bounds center was {contentCenter} — shifting all direct children by {-contentCenter}.");
            foreach (var (t, _, _) in snapshot)
                Debug.Log($"[EnvironmentCenterer]   '{t.name}' → new world pos {t.position}");
            Debug.Log($"[EnvironmentCenterer] Done. {snapshot.Count} direct child(ren) updated. Save the scene with Ctrl+S.");

            // ── 5. Mark scene dirty ─────────────────────────────────────────────
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem("Adventure Multiplayer/Utils/Center Environment", true)]
        private static bool Validate() => true;
    }
}
