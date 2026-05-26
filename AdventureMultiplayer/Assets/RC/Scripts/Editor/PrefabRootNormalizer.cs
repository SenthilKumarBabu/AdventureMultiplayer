using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Centers a prefab's content at world origin.
    ///
    /// Steps:
    ///   1. Computes the bounding box of all Renderers in the hierarchy.
    ///   2. Zeroes the root position/rotation.
    ///   3. Offsets every direct child by -boundsCenter so the visual
    ///      content sits centered on the origin.
    ///      Grandchildren and deeper nodes auto-correct because their
    ///      local offsets relative to their own parent are unchanged.
    ///
    /// Usage: select the root GameObject (in scene or Prefab Mode)
    ///        → Adventure Multiplayer / Utils / Center Prefab at Origin
    /// </summary>
    public static class PrefabRootNormalizer
    {
        [MenuItem("Adventure Multiplayer/Utils/Center Prefab at Origin %t")]
        public static void CenterAtOrigin()
        {
            var root = Selection.activeGameObject;
            if (root == null)
            {
                EditorUtility.DisplayDialog("Center Prefab at Origin",
                    "Select the root GameObject first.", "OK");
                return;
            }

            // ── 1. Compute world-space bounding box from all renderers ──────────
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("Center Prefab at Origin",
                    "No Renderers found in hierarchy — cannot compute bounds.", "OK");
                return;
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 contentCenter = bounds.center;

            // ── 2. Snapshot direct children's world transforms ──────────────────
            var rootT    = root.transform;
            var snapshot = new List<(Transform t, Vector3 worldPos, Quaternion worldRot)>();
            foreach (Transform child in rootT)
                snapshot.Add((child, child.position, child.rotation));

            // ── 3. Register undo ────────────────────────────────────────────────
            Undo.RecordObject(rootT, "Center Prefab at Origin");
            foreach (var (t, _, _) in snapshot)
                Undo.RecordObject(t, "Center Prefab at Origin");

            // ── 4. Zero the root ────────────────────────────────────────────────
            rootT.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // ── 5. Shift direct children so content center lands at origin ──────
            //  After zeroing root, local pos == world pos for direct children.
            //  Subtract contentCenter to pull the visual center to (0,0,0).
            foreach (var (t, worldPos, worldRot) in snapshot)
                t.SetPositionAndRotation(worldPos - contentCenter, worldRot);

            Debug.Log($"[PrefabRootNormalizer] '{root.name}' centered. " +
                      $"Content was at {contentCenter}, shifted by {-contentCenter}. " +
                      $"{snapshot.Count} direct child(ren) updated.");

            // ── 6. Save / apply ─────────────────────────────────────────────────
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorUtility.SetDirty(root);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                Debug.Log("[PrefabRootNormalizer] Prefab stage dirty — Ctrl+S to save.");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                bool apply = EditorUtility.DisplayDialog("Center Prefab at Origin",
                    $"'{root.name}' is a prefab instance.\nApply changes to the asset?",
                    "Apply to Prefab", "Keep as Override");

                if (apply)
                {
                    PrefabUtility.ApplyObjectOverride(rootT, assetPath, InteractionMode.UserAction);
                    foreach (var (t, _, _) in snapshot)
                        PrefabUtility.ApplyObjectOverride(t, assetPath, InteractionMode.UserAction);

                    Debug.Log("[PrefabRootNormalizer] Changes applied to prefab asset.");
                }
            }

            EditorUtility.SetDirty(root);
        }

        [MenuItem("Adventure Multiplayer/Utils/Center Prefab at Origin", true)]
        private static bool Validate() => Selection.activeGameObject != null;
    }
}
