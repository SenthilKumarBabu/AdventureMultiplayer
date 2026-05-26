using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer.Editor
{
    public static class BreakableNetworkSetup
    {
        // GUID of the "Log With Coin" prefab asset
        private const string LogWithCoinPrefabGuid = "5480cf772a54ec44182b4123be358975";

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkBreakable to Scene Crates")]
        public static void SetupBreakables()
        {
            var breakables = Object.FindObjectsByType<Breakable>(FindObjectsSortMode.None);

            if (breakables.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No Breakable objects found in scene.");
                return;
            }

            int updatedCount = 0;

            foreach (var breakable in breakables)
            {
                var go = breakable.gameObject;
                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (!go.TryGetComponent<NetworkBreakable>(out _))
                {
                    Undo.AddComponent<NetworkBreakable>(go);
                    changed = true;
                }

                foreach (var collectible in breakable.collectibles)
                {
                    if (collectible == null) continue;
                    changed |= StripNetworkComponentsFromCollectible(collectible.gameObject);
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "Breakable");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkPanel to Scene Logs")]
        public static void SetupPanels()
        {
            // Find all scene GameObjects that are instances of the "Log With Coin" prefab.
            // We search for PrefabInstances by checking the prefab asset GUID via
            // PrefabUtility.GetCorrespondingObjectFromSource.
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(LogWithCoinPrefabGuid));

            if (prefabAsset == null)
            {
                Debug.LogWarning($"[BreakableNetworkSetup] Could not find Log With Coin prefab " +
                                 $"(GUID={LogWithCoinPrefabGuid}). Check the GUID.");
                return;
            }

            var allGos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int updatedCount = 0;

            foreach (var go in allGos)
            {
                // Match only root-level instances of the Log With Coin prefab.
                var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (source != prefabAsset) continue;

                bool changed = false;

                // Add NetworkObject to the root.
                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                // Add NetworkPanel to the root.
                if (!go.TryGetComponent<NetworkPanel>(out _))
                {
                    Undo.AddComponent<NetworkPanel>(go);
                    changed = true;
                }

                // Strip NetworkObject + NetworkedCollectible from all Collectible children.
                foreach (var collectible in go.GetComponentsInChildren<Collectible>(true))
                {
                    changed |= StripNetworkComponentsFromCollectible(collectible.gameObject);
                }

                // Auto-assign Collectibles field on NetworkPanel.
                if (go.TryGetComponent<NetworkPanel>(out var networkPanel))
                {
                    var collectibles = go.GetComponentsInChildren<Collectible>(true);
                    var so = new SerializedObject(networkPanel);
                    var prop = so.FindProperty("collectibles");
                    prop.arraySize = collectibles.Length;
                    for (int i = 0; i < collectibles.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = collectibles[i];
                    so.ApplyModifiedProperties();
                    changed = true;
                    Debug.Log($"[BreakableNetworkSetup] Assigned {collectibles.Length} collectible(s) to NetworkPanel on '{go.name}'.", go);
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up Log '{go.name}'.", go);
                }
            }

            if (updatedCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log($"[BreakableNetworkSetup] Done — {updatedCount} Log(s) updated. " +
                          "IMPORTANT: Assign the Bouncy Coin Collectible to the 'Collectibles' " +
                          "field on each NetworkPanel in the Inspector, then save the scene.");
            }
            else
            {
                Debug.Log("[BreakableNetworkSetup] All Log With Coin objects already configured.");
            }
        }

        private static bool StripNetworkComponentsFromCollectible(GameObject collectibleGo)
        {
            bool changed = false;

            if (collectibleGo.TryGetComponent<NetworkedCollectible>(out var nc))
            {
                Undo.DestroyObjectImmediate(nc);
                EditorUtility.SetDirty(collectibleGo);
                changed = true;
                Debug.Log($"[BreakableNetworkSetup] Removed NetworkedCollectible from '{collectibleGo.name}'.", collectibleGo);
            }

            if (collectibleGo.TryGetComponent<NetworkObject>(out var netObj))
            {
                Undo.DestroyObjectImmediate(netObj);
                EditorUtility.SetDirty(collectibleGo);
                changed = true;
                Debug.Log($"[BreakableNetworkSetup] Removed NetworkObject from '{collectibleGo.name}'.", collectibleGo);
            }

            return changed;
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkPanelSync to Button Panels")]
        public static void SetupButtonPanels()
        {
            // Finds all Panel objects that are NOT part of a Log With Coin prefab.
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(LogWithCoinPrefabGuid));

            var panels = Object.FindObjectsByType<Panel>(FindObjectsSortMode.None);
            var npsType = System.Type.GetType("AdventureMultiplayer.NetworkPanelSync, Assembly-CSharp");
            var npType  = System.Type.GetType("AdventureMultiplayer.NetworkPanel, Assembly-CSharp");
            int updatedCount = 0;

            foreach (var panel in panels)
            {
                var go = panel.gameObject;

                // Skip Log With Coin panels — those use NetworkPanel.
                var root = PrefabUtility.GetCorrespondingObjectFromSource(go.transform.root.gameObject);
                if (prefabAsset != null && root == prefabAsset) continue;

                // Skip if already has NetworkPanelSync.
                if (npsType != null && go.GetComponent(npsType) != null) continue;

                // Remove incorrect NetworkPanel if present.
                if (npType != null && go.GetComponent(npType) is Component np)
                {
                    Undo.DestroyObjectImmediate(np);
                    Debug.Log($"[BreakableNetworkSetup] Removed NetworkPanel from '{go.name}'.", go);
                }

                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (npsType != null && go.GetComponent(npsType) == null)
                {
                    Undo.AddComponent(go, npsType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up Panel '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "Panel");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkToggle to Scene Toggles")]
        public static void SetupToggles()
        {
            var toggles = Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None);

            if (toggles.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No Toggle objects found in scene.");
                return;
            }

            var ntType = System.Type.GetType("AdventureMultiplayer.NetworkToggle, Assembly-CSharp");
            int updatedCount = 0;

            foreach (var t in toggles)
            {
                var go = t.gameObject;
                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (ntType != null && go.GetComponent(ntType) == null)
                {
                    Undo.AddComponent(go, ntType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up Toggle '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "Toggle");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkCheckpoint to Scene Checkpoints")]
        public static void SetupCheckpoints()
        {
            var checkpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);

            if (checkpoints.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No Checkpoint objects found in scene.");
                return;
            }

            var ncType = System.Type.GetType("AdventureMultiplayer.NetworkCheckpoint, Assembly-CSharp");
            int updatedCount = 0;

            foreach (var cp in checkpoints)
            {
                var go = cp.gameObject;
                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (ncType != null && go.GetComponent(ncType) == null)
                {
                    Undo.AddComponent(go, ncType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up Checkpoint '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "Checkpoint");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkFallingPlatform to Scene Falling Platforms")]
        public static void SetupFallingPlatforms()
        {
            var platforms = Object.FindObjectsByType<FallingPlatform>(FindObjectsSortMode.None);

            if (platforms.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No FallingPlatform objects found in scene.");
                return;
            }

            var nfpType = System.Type.GetType("AdventureMultiplayer.NetworkFallingPlatform, Assembly-CSharp");
            int updatedCount = 0;

            foreach (var fp in platforms)
            {
                var go = fp.gameObject;
                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (nfpType != null && go.GetComponent(nfpType) == null)
                {
                    Undo.AddComponent(go, nfpType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up FallingPlatform '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "FallingPlatform");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkedPickable to Scene Pickables")]
        public static void SetupPickables()
        {
            var pickables = Object.FindObjectsByType<Pickable>(FindObjectsSortMode.None);

            if (pickables.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No Pickable objects found in scene.");
                return;
            }

            var networkPickableType   = System.Type.GetType("AdventureMultiplayer.NetworkPickable, Assembly-CSharp");
            var networkedPickableType = System.Type.GetType("AdventureMultiplayer.NetworkedPickable, Assembly-CSharp");

            int updatedCount = 0;

            foreach (var pickable in pickables)
            {
                var go = pickable.gameObject;

                // Skip crates that already have NetworkBreakable — those are handled separately.
                if (go.TryGetComponent<NetworkBreakable>(out _)) continue;

                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (networkPickableType != null && go.GetComponent(networkPickableType) == null)
                {
                    Undo.AddComponent(go, networkPickableType);
                    changed = true;
                }

                if (networkedPickableType != null && go.GetComponent(networkedPickableType) == null)
                {
                    Undo.AddComponent(go, networkedPickableType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up Pickable '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "Pickable");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkGridPlatform to Scene Grid Platforms")]
        public static void SetupGridPlatforms()
        {
            var platforms = Object.FindObjectsByType<GridPlatform>(FindObjectsSortMode.None);

            if (platforms.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No GridPlatform objects found in scene.");
                return;
            }

            int updatedCount = 0;

            foreach (var gp in platforms)
            {
                var go = gp.gameObject;
                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                var ngpType = System.Type.GetType("AdventureMultiplayer.NetworkGridPlatform, Assembly-CSharp");
                if (ngpType != null && go.GetComponent(ngpType) == null)
                {
                    Undo.AddComponent(go, ngpType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up GridPlatform '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "GridPlatform");
        }

        [MenuItem("Adventure Multiplayer/Setup/Add NetworkEnemy to Scene Enemies")]
        public static void SetupEnemies()
        {
            var enemies = Object.FindObjectsByType<PLAYERTWO.PlatformerProject.Enemy>(FindObjectsSortMode.None);

            if (enemies.Length == 0)
            {
                Debug.LogWarning("[BreakableNetworkSetup] No Enemy objects found in scene.");
                return;
            }

            var neType = System.Type.GetType("AdventureMultiplayer.NetworkEnemy, Assembly-CSharp");
            int updatedCount = 0;

            foreach (var enemy in enemies)
            {
                var go = enemy.gameObject;
                bool changed = false;

                if (!go.TryGetComponent<NetworkObject>(out _))
                {
                    Undo.AddComponent<NetworkObject>(go);
                    changed = true;
                }

                if (neType != null && go.GetComponent(neType) == null)
                {
                    Undo.AddComponent(go, neType);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    updatedCount++;
                    Debug.Log($"[BreakableNetworkSetup] Set up Enemy '{go.name}'.", go);
                }
            }

            FinishSetup(updatedCount, "Enemy");
        }

        private static void FinishSetup(int updatedCount, string type)
        {
            if (updatedCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log($"[BreakableNetworkSetup] Done — {updatedCount} {type} object(s) updated. Save the scene.");
            }
            else
            {
                Debug.Log($"[BreakableNetworkSetup] All {type} objects already configured.");
            }
        }
    }
}
