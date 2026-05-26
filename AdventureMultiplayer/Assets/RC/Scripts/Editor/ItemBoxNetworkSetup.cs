using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using AdventureMultiplayer;

/// <summary>
/// One-shot editor tool: adds NetworkObject + NetworkedItemBox to every ItemBox
/// in the active scene, and removes NetworkPickable + Rigidbody if present.
/// Run via Tools > Setup Item Boxes for Multiplayer.
/// </summary>
public static class ItemBoxNetworkSetup
{
    [MenuItem("Tools/Setup Item Boxes for Multiplayer")]
    public static void SetupItemBoxes()
    {
        var itemBoxes = Object.FindObjectsByType<ItemBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        foreach (var box in itemBoxes)
        {
            Undo.RecordObject(box.gameObject, "Setup Item Box for Multiplayer");

            // Remove NetworkPickable if present (wrong component for item boxes).
            var networkPickable = box.GetComponent<NetworkPickable>();
            if (networkPickable != null)
            {
                Undo.DestroyObjectImmediate(networkPickable);
                Debug.Log($"[ItemBoxSetup] Removed NetworkPickable from '{box.name}'.");
            }

            // Remove Rigidbody if present (not needed, was added by NetworkPickable).
            var rb = box.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Undo.DestroyObjectImmediate(rb);
                Debug.Log($"[ItemBoxSetup] Removed Rigidbody from '{box.name}'.");
            }

            // Add NetworkObject if missing.
            if (box.GetComponent<NetworkObject>() == null)
            {
                Undo.AddComponent<NetworkObject>(box.gameObject);
                Debug.Log($"[ItemBoxSetup] Added NetworkObject to '{box.name}'.");
            }

            // Add NetworkedItemBox if missing.
            if (box.GetComponent<NetworkedItemBox>() == null)
            {
                Undo.AddComponent<NetworkedItemBox>(box.gameObject);
                Debug.Log($"[ItemBoxSetup] Added NetworkedItemBox to '{box.name}'.");
            }

            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[ItemBoxSetup] Done. Processed {count} Item Box(es).");
    }
}
