using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using AdventureMultiplayer;

/// <summary>
/// Editor tool: adds NetworkObject + NetworkedCollectible to STANDALONE collectibles,
/// and REMOVES those components from collectibles that are referenced by an ItemBox,
/// Breakable (crate), or NetworkPanel (log) — those are handled by their own network
/// components and must NOT be NetworkObjects.
/// Run via Tools > Setup Collectibles for Multiplayer.
/// </summary>
public static class CollectibleNetworkSetup
{
    [MenuItem("Tools/Setup Collectibles for Multiplayer")]
    public static void SetupCollectibles()
    {
        var allCollectibles = Object.FindObjectsByType<Collectible>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var allItemBoxes    = Object.FindObjectsByType<ItemBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var allBreakables   = Object.FindObjectsByType<Breakable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var allPanels       = Object.FindObjectsByType<NetworkPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int added   = 0;
        int removed = 0;
        int skipped = 0;

        foreach (var col in allCollectibles)
        {
            bool managed = IsReferencedByItemBox(col, allItemBoxes)
                        || IsReferencedByBreakable(col, allBreakables)
                        || IsReferencedByNetworkPanel(col, allPanels);

            if (managed)
            {
                // Remove NetworkedCollectible + NetworkObject — these are managed by their
                // parent component (NetworkedItemBox / NetworkBreakable / NetworkPanel).
                var nc = col.GetComponent<NetworkedCollectible>();
                if (nc != null)
                {
                    Undo.DestroyObjectImmediate(nc);
                    Debug.Log($"[CollectibleSetup] Removed NetworkedCollectible from managed collectible '{col.name}'.");
                    removed++;
                }

                var no = col.GetComponent<NetworkObject>();
                if (no != null)
                {
                    Undo.DestroyObjectImmediate(no);
                    Debug.Log($"[CollectibleSetup] Removed NetworkObject from managed collectible '{col.name}'.");
                    removed++;
                }

                skipped++;
                continue;
            }

            // Standalone collectible — ensure NetworkObject + NetworkedCollectible are present.
            Undo.RecordObject(col.gameObject, "Setup Collectible for Multiplayer");

            if (col.GetComponent<NetworkObject>() == null)
            {
                Undo.AddComponent<NetworkObject>(col.gameObject);
                Debug.Log($"[CollectibleSetup] Added NetworkObject to '{col.name}'.");
            }

            if (col.GetComponent<NetworkedCollectible>() == null)
            {
                Undo.AddComponent<NetworkedCollectible>(col.gameObject);
                Debug.Log($"[CollectibleSetup] Added NetworkedCollectible to '{col.name}'.");
            }

            added++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[CollectibleSetup] Done. Setup {added} standalone collectible(s). Cleaned {removed} component(s) from {skipped} managed collectible(s).");
    }

    private static bool IsReferencedByItemBox(Collectible col, ItemBox[] itemBoxes)
    {
        foreach (var box in itemBoxes)
        {
            if (box.collectibles == null) continue;
            foreach (var c in box.collectibles)
                if (c == col) return true;
        }
        return false;
    }

    private static bool IsReferencedByBreakable(Collectible col, Breakable[] breakables)
    {
        foreach (var b in breakables)
        {
            if (b.collectibles == null) continue;
            foreach (var c in b.collectibles)
                if (c == col) return true;
        }
        return false;
    }

    private static bool IsReferencedByNetworkPanel(Collectible col, NetworkPanel[] panels)
    {
        foreach (var p in panels)
        {
            var so = new SerializedObject(p);
            var prop = so.FindProperty("collectibles");
            if (prop == null) continue;
            for (int i = 0; i < prop.arraySize; i++)
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == col) return true;
        }
        return false;
    }
}
