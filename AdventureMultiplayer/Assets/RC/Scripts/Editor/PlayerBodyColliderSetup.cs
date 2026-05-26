using AdventureMultiplayer;
using PLAYERTWO.PlatformerProject;
using UnityEditor;
using UnityEngine;

namespace AdventureMultiplayer.Editor
{
    /// <summary>
    /// Editor utility: sets up the solid body collider on a player prefab so that
    /// other players' EntityControllers can physically collide with it.
    ///
    /// Usage: select the player prefab root in the Hierarchy or Project window,
    /// then run Adventure Multiplayer > Setup Player Body Collider.
    /// </summary>
    public static class PlayerBodyColliderSetup
    {
        [MenuItem("Adventure Multiplayer/Setup Player Body Collider")]
        private static void Setup()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Setup Player Body Collider",
                    "No GameObject selected. Select the player prefab root first.", "OK");
                return;
            }

            var isPrefab = PrefabUtility.IsPartOfPrefabAsset(selected)
                        || PrefabUtility.IsPartOfPrefabInstance(selected);

            // Work inside a prefab editing session if open, otherwise edit directly.
            using var scope = new PrefabUtility.EditPrefabContentsScope(
                isPrefab && !PrefabUtility.IsPartOfPrefabInstance(selected)
                    ? AssetDatabase.GetAssetPath(selected)
                    : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected));

            var root = scope.prefabContentsRoot;

            SetupOnRoot(root);
        }

        [MenuItem("Adventure Multiplayer/Setup Player Body Collider", validate = true)]
        private static bool SetupValidate() => Selection.activeGameObject != null;

        // ── Core setup ────────────────────────────────────────────────────────

        private static void SetupOnRoot(GameObject root)
        {
            // Read EntityController dimensions to size the body collider correctly.
            var entityController = root.GetComponent<EntityController>();
            float radius = entityController != null ? entityController.radius : 0.5f;
            float height = entityController != null ? entityController.height : 2f;

            // Find or create the "Body" child.
            var bodyTransform = root.transform.Find("Body");
            GameObject body;
            if (bodyTransform == null)
            {
                body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale    = Vector3.one;
                Debug.Log("[PlayerBodyColliderSetup] Created 'Body' child.");
            }
            else
            {
                body = bodyTransform.gameObject;
                Debug.Log("[PlayerBodyColliderSetup] Found existing 'Body' child.");
            }

            // Add or configure the CapsuleCollider.
            var capsule = body.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = body.AddComponent<CapsuleCollider>();

            capsule.isTrigger = false;
            capsule.radius    = radius;
            capsule.height    = height;
            capsule.center    = new Vector3(0f, height * 0.5f, 0f);
            capsule.direction = 1; // Y-axis
            Debug.Log($"[PlayerBodyColliderSetup] CapsuleCollider configured: radius={radius}, height={height}.");

            // Add or find PlayerBodyCollider on the root.
            var pbc = root.GetComponent<PlayerBodyCollider>();
            if (pbc == null)
                pbc = root.AddComponent<PlayerBodyCollider>();

            // Assign the body collider via SerializedObject so Unity tracks it properly.
            var so      = new SerializedObject(pbc);
            var colProp = so.FindProperty("bodyCollider");
            colProp.objectReferenceValue = capsule;
            so.ApplyModifiedProperties();

            Debug.Log("[PlayerBodyColliderSetup] PlayerBodyCollider assigned.");
            EditorUtility.DisplayDialog("Setup Player Body Collider",
                $"Done! Body collider set up on '{root.name}'.", "OK");
        }
    }
}
