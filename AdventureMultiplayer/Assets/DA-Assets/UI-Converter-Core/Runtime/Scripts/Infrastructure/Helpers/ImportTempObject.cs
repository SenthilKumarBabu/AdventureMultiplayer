#if UNITY_EDITOR
using UnityEngine;

namespace DA_Assets.UCC
{
    public class ImportTempObject : MonoBehaviour
    {
        public static void DestroyAll()
        {
#if UNITY_2021_3_OR_NEWER
            ImportTempObject[] tempObjects = FindObjectsByType<ImportTempObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            ImportTempObject[] tempObjects = FindObjectsOfType<ImportTempObject>(true);
#endif

            foreach (ImportTempObject tempObject in tempObjects)
            {
                if (tempObject != null && tempObject.gameObject != null)
                {
                    Debug.Log($"[ImportTempObject] Destroying leftover temp object: {tempObject.gameObject.name}");
                    DestroyImmediate(tempObject.gameObject);
                }
            }
        }
    }
}
#endif