#if UNITY_EDITOR
using PLAYERTWO.PlatformerProject;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Replaces PlayerInputManager with SlipAwarePlayerInputManager on every player
    /// prefab found in Assets/RC/Prefabs so banana-peel slip correctly restricts input.
    ///
    /// Run once via: Tools > Adventure Multiplayer > Setup Slip Input Manager
    /// </summary>
    public static class SlipInputSetup
    {
        private const string PlayerPrefabDir = "Assets/RC/Prefabs";

        [MenuItem("Tools/Adventure Multiplayer/Setup Slip Input Manager")]
        static void Run()
        {
            var guids   = AssetDatabase.FindAssets("t:Prefab", new[] { PlayerPrefabDir });
            int updated = 0;
            int skipped = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);

                var existing = root.GetComponentInChildren<PlayerInputManager>(true);

                if (existing == null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                // Already the correct type — nothing to do.
                if (existing is SlipAwarePlayerInputManager)
                {
                    Debug.Log($"[SlipSetup] {System.IO.Path.GetFileName(path)} already has SlipAwarePlayerInputManager — skipped.");
                    skipped++;
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                // Preserve the InputActionAsset reference before destroying the old component.
                var actionsAsset = existing.actions;
                var go           = existing.gameObject;

                UnityEngine.Object.DestroyImmediate(existing, true);

                var replacement = go.AddComponent<SlipAwarePlayerInputManager>();
                replacement.actions = actionsAsset;

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);

                Debug.Log($"[SlipSetup] Replaced PlayerInputManager → SlipAwarePlayerInputManager in {System.IO.Path.GetFileName(path)}");
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Slip Input Setup",
                $"Updated: {updated} prefab(s)\nAlready correct: {skipped} prefab(s)\n\n" +
                "Check the Console for per-prefab details.", "OK");
        }
    }
}
#endif
