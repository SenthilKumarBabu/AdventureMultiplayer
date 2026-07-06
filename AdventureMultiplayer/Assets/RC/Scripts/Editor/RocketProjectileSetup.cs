#if UNITY_EDITOR
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Creates the RocketProjectile prefab, registers it in DefaultNetworkPrefabs,
    /// and wires it into every player prefab that has a PlayerPowerUpInventory.
    ///
    /// Run via: Tools > Adventure Multiplayer > Setup Rocket Projectile Prefab
    /// </summary>
    public static class RocketProjectileSetup
    {
        private const string PrefabPath      = "Assets/RC/Prefabs/RocketProjectile.prefab";
        private const string NetPrefabsPath  = "Assets/DefaultNetworkPrefabs.asset";
        private const string PlayerPrefabDir = "Assets/RC/Prefabs";

        [MenuItem("Tools/Adventure Multiplayer/Setup Rocket Projectile Prefab")]
        static void Run()
        {
            // ── 1. Create the prefab ──────────────────────────────────────────
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)!);

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null &&
                !EditorUtility.DisplayDialog("Prefab Exists",
                    "RocketProjectile.prefab already exists. Recreate it?", "Recreate", "Cancel"))
            {
                WireAndRegister(existing);
                return;
            }

            // Build the GameObject in memory
            var root = new GameObject("RocketProjectile");

            // Rigidbody — kinematic so we drive it via MovePosition/MoveRotation
            var rb          = root.AddComponent<Rigidbody>();
            rb.isKinematic  = true;
            rb.useGravity   = false;

            // SphereCollider — trigger for hit detection
            var sc      = root.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius    = 0.3f;

            // NGO components (NetworkObject must come before NetworkTransform)
            root.AddComponent<NetworkObject>();
            var nt = root.AddComponent<NetworkTransform>();
            // NGO 2.x: default authority is Server — no extra property needed.

            // Game script
            root.AddComponent<RocketProjectile>();

            // Visual child — capsule stretched along the local Z axis (forward)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>()); // collider lives on root
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale    = new Vector3(0.15f, 0.5f, 0.15f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // align with forward

            // Assign a simple red material so the rocket is visible in the scene
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name  = "RocketMat",
                    color = new Color(1f, 0.3f, 0.05f)
                };
                renderer.sharedMaterial = mat;
            }

            // Save to disk and clean up the in-memory object
            var prefabGO = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            if (prefabGO == null)
            {
                Debug.LogError("[RocketSetup] Failed to save prefab.");
                return;
            }

            Debug.Log($"[RocketSetup] Prefab saved: {PrefabPath}");

            WireAndRegister(prefabGO);
        }

        // ── Step 2 & 3: register + wire ───────────────────────────────────────

        static void WireAndRegister(GameObject prefabGO)
        {
            RegisterInNetworkPrefabs(prefabGO);
            WireToPlayerPrefabs(prefabGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Rocket Setup Complete",
                $"Prefab:   {PrefabPath}\n" +
                $"Registered in: {NetPrefabsPath}\n" +
                "Player prefabs wired. Check the console for details.", "OK");
        }

        // ── Register in DefaultNetworkPrefabs ─────────────────────────────────

        static void RegisterInNetworkPrefabs(GameObject prefabGO)
        {
            var netPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetPrefabsPath);
            if (netPrefabs == null)
            {
                Debug.LogWarning($"[RocketSetup] Could not load NetworkPrefabsList at '{NetPrefabsPath}'. " +
                                 "Register the prefab in NetworkManager manually.");
                return;
            }

            // Check for duplicates
            foreach (var entry in netPrefabs.PrefabList)
                if (entry.Prefab == prefabGO)
                {
                    Debug.Log("[RocketSetup] Prefab already registered in DefaultNetworkPrefabs.");
                    return;
                }

            Undo.RecordObject(netPrefabs, "Register Rocket Prefab");
            netPrefabs.Add(new NetworkPrefab { Prefab = prefabGO });
            EditorUtility.SetDirty(netPrefabs);
            Debug.Log("[RocketSetup] Registered in DefaultNetworkPrefabs.");
        }

        // ── Wire rocketProjectilePrefab in every player prefab ────────────────

        static void WireToPlayerPrefabs(GameObject prefabGO)
        {
            var netObjOnRocket = prefabGO.GetComponent<NetworkObject>();
            int wired = 0;

            // Search player prefabs by name convention (all *Player.prefab in RC/Prefabs)
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PlayerPrefabDir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);

                var inv = root.GetComponentInChildren<PlayerPowerUpInventory>(true);
                if (inv == null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                var so   = new SerializedObject(inv);
                var prop = so.FindProperty("rocketProjectilePrefab");

                if (prop == null)
                {
                    Debug.LogWarning($"[RocketSetup] 'rocketProjectilePrefab' field not found in {path}.");
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                if (prop.objectReferenceValue != null)
                {
                    Debug.Log($"[RocketSetup] {Path.GetFileName(path)} already has a rocket prefab assigned — skipped.");
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                prop.objectReferenceValue = netObjOnRocket;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);

                Debug.Log($"[RocketSetup] Wired rocket prefab → {Path.GetFileName(path)}");
                wired++;
            }

            if (wired == 0)
                Debug.LogWarning("[RocketSetup] No player prefabs updated. " +
                                 "Assign rocketProjectilePrefab in the Inspector manually if needed.");
            else
                Debug.Log($"[RocketSetup] Wired {wired} player prefab(s).");
        }
    }
}
#endif
