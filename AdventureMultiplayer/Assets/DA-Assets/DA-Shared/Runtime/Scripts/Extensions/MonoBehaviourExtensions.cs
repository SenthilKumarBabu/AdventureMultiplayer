using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DA_Assets.Shared.Extensions;
using UnityEngine.UI;

#pragma warning disable CS0162

namespace DA_Assets.Extensions
{
    public static class MonoBehExtensions
    {
        /// <summary>
        /// Destroys all immediate child GameObjects of <paramref name="parent"/> in reverse order
        /// (last child first) to avoid index shifting issues during iteration.
        /// Logs the number of destroyed children via <see cref="SharedLocKey.log_canvas_children_destroyed"/>.
        /// </summary>
        /// <param name="parent">The parent GameObject whose children will be destroyed. No-op if <c>null</c>.</param>
        public static void DestroyChilds(this GameObject parent)
        {
            if (parent == null)
                return;

            int childCount = parent.transform.childCount;

            for (int i = childCount - 1; i >= 0; i--)
            {
                GameObject go = parent.transform.GetChild(i).gameObject;
                go.Destroy();
            }

            Debug.Log(SharedLocKey.log_canvas_children_destroyed.Localize(childCount));
        }

        /// <summary>
        /// Null-safe wrapper around <see cref="GameObject.TryGetComponent{T}"/>.
        /// Returns <c>false</c> immediately when the target <paramref name="gameObject"/> is <c>null</c>,
        /// preventing a MissingReferenceException in situations where the object may have been destroyed.
        /// </summary>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <param name="gameObject">The target GameObject. May be null.</param>
        /// <param name="component">The found component, or <c>default</c> if not found or GameObject is null.</param>
        /// <returns><c>true</c> if the component exists on the GameObject; otherwise <c>false</c>.</returns>
        public static bool TryGetComponentSafe<T>(this GameObject gameObject, out T component)
        {
            component = default;

            if (gameObject == null)
                return false;

            return gameObject.TryGetComponent(out component);
        }

        /// <summary>
        /// Returns all components of type <typeparamref name="T"/> found in child GameObjects,
        /// excluding the component on the root <paramref name="gameObject"/> itself.
        /// Inactive children are included in the search.
        /// </summary>
        /// <typeparam name="T">The component type to search for.</typeparam>
        /// <param name="gameObject">The root GameObject whose children are traversed.</param>
        /// <returns>Array of <typeparamref name="T"/> components found in children.</returns>
        public static T[] GetChilds<T>(this GameObject gameObject)
        {
            T[] childs = gameObject.GetComponentsInChildren<T>(true).Skip(1).ToArray();
            return childs;
        }

        /// <summary>
        /// Removes the RectTransform component from a GameObject by creating a new GameObject
        /// with the same components (excluding RectTransform), children, and parent.
        /// </summary>
        /// <param name="gameObject">The GameObject from which to remove the RectTransform.</param>
        public static GameObject RemoveRectTransform(this GameObject gameObject)
        {
            GameObject newGameObject = CreateEmptyGameObject();
            newGameObject.name = gameObject.name;

            int siblingIndex = gameObject.transform.GetSiblingIndex();

            newGameObject.transform.SetParent(gameObject.transform.parent);
            newGameObject.transform.localPosition = gameObject.transform.localPosition;
            newGameObject.transform.localRotation = gameObject.transform.localRotation;
            newGameObject.transform.localScale = gameObject.transform.localScale;

            newGameObject.transform.SetSiblingIndex(siblingIndex);

            for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = gameObject.transform.GetChild(i);

                int childSiblingIndex = child.GetSiblingIndex();
                child.SetParent(newGameObject.transform);
                child.SetSiblingIndex(childSiblingIndex);
            }

            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (!(component is Transform))
                {
                    Component newComponent = newGameObject.AddComponent(component.GetType());
                    component.CopySerializedFields(newComponent);
                }
            }

            gameObject.Destroy();
            return newGameObject;
        }

        /// <summary>
        /// Collects components of type <typeparamref name="T"/> from the entire hierarchy in
        /// depth-first, reverse-child-order traversal — i.e. deepest children first, last sibling first.
        /// Useful when components must be processed from leaves toward the root without relying on sibling index.
        /// </summary>
        /// <typeparam name="T">The component type to collect.</typeparam>
        /// <param name="parent">The root of the hierarchy to traverse.</param>
        /// <returns>List of <typeparamref name="T"/> components in reverse depth-first order.</returns>
        public static List<T> GetComponentsInReverseOrder<T>(this GameObject parent) where T : Component
        {
            List<T> results = new List<T>();
            AddComponentsInReverseOrder(parent.transform);
            return results;

            void AddComponentsInReverseOrder(Transform current)
            {
                for (int i = current.childCount - 1; i >= 0; i--)
                {
                    AddComponentsInReverseOrder(current.GetChild(i));
                }

                T component = current.GetComponent<T>();
                if (component != null/* && !results.Contains(component)*/)
                {
                    results.Add(component);
                }
            }
        }

        /// <summary>
        /// Saves the GameObject as a prefab asset at the specified local path and tries to get the component of type T from the prefab.
        /// </summary>
        /// <typeparam name="T">The type of the MonoBehaviour to retrieve from the prefab.</typeparam>
        /// <param name="gameObject">The GameObject to be saved as a prefab.</param>
        /// <param name="localPath">The local path within the project where the prefab should be saved.</param>
        /// <param name="savedPrefab">The component of type T retrieved from the prefab, or null if the operation failed.</param>
        /// <param name="ex">Any exceptions that occurred during the process.</param>
        /// <returns>True if the prefab was saved and the component of type T was successfully retrieved, otherwise false.</returns>
        public static bool SaveAsPrefabAsset<T>(this GameObject gameObject, string localPath, out T savedPrefab, out Exception ex) where T : MonoBehaviour
        {
            if (gameObject == null)
            {
                ex = new NullReferenceException("GameObject is null.");
                savedPrefab = null;
                return false;
            }

#if UNITY_EDITOR
            GameObject prefabGo = null;

            try
            {
                prefabGo = UnityEditor.PrefabUtility.SaveAsPrefabAsset(gameObject, localPath, out bool success);
            }
            catch (Exception ex1)
            {
                ex = ex1;
            }

            if (prefabGo == null)
            {
                ex = new NullReferenceException("Prefab is null.");
                savedPrefab = null;
                return false;
            }

            if (prefabGo.TryGetComponent<T>(out T prefabComponent))
            {
                ex = null;
                savedPrefab = prefabComponent;
                return true;
            }
            else
            {
                ex = new Exception($"Can't get Type '{typeof(T).Name}' from GameObject '{prefabGo.name}'.");
                savedPrefab = null;
                return false;
            }
#endif

            ex = new Exception("Unsupported in not-Editor mode.");
            savedPrefab = null;
            return false;
        }

        /// <summary>
        /// Checks if the provided UnityEngine.Object is part of any prefab.
        /// </summary>
        /// <param name="gameObject">The UnityEngine.Object to check.</param>
        /// <returns>True if the object is part of a prefab, otherwise false.</returns>
        public static bool IsPartOfAnyPrefab(this UnityEngine.Object gameObject)
        {
            if (gameObject == null)
                return false;
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject);
#endif
            return false;
        }

        /// <summary>
        /// Checks if any instance of the provided MonoBehaviour type exists on the scene.
        /// </summary>
        /// <typeparam name="T">Type of MonoBehaviour to check for.</typeparam>
        /// <returns>True if at least one instance of T exists on the scene, otherwise false.</returns>
        public static bool IsExistsOnScene<T>() where T : MonoBehaviour
        {
            int count = MonoBehaviour.FindObjectsOfType<T>().Length;
            return count != 0;
        }

        /// <summary>
        /// Destroys a Unity <see cref="UnityEngine.Object"/> as an extension method.
        /// Uses <c>DestroyImmediate</c> in Editor mode and <c>Destroy</c> in Play mode.
        /// </summary>
        /// <param name="object">The object to destroy. Safely handles <c>null</c>.</param>
        /// <returns><c>true</c> if destruction succeeded; <c>false</c> if the object was already <c>null</c> or an exception occurred.</returns>
        public static bool Destroy(this UnityEngine.Object @object)
        {
            if (@object == null)
                return false;

            try
            {
                if (@object != null)
                {
                    //Debug.LogError($"Destroy | {unityObject.name}");
                }

#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(@object);
#else
                UnityEngine.Object.Destroy(@object);
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a component of type <typeparamref name="T"/> to the GameObject if one does not already exist.
        /// When <paramref name="supportMultiInstance"/> is <c>false</c> (default), returns the existing instance
        /// rather than adding a duplicate. When <c>true</c>, always adds a new instance regardless of existing ones.
        /// </summary>
        /// <typeparam name="T">The component type to add.</typeparam>
        /// <param name="gameObject">The target GameObject.</param>
        /// <param name="component">The existing or newly added component.</param>
        /// <param name="supportMultiInstance">When <c>true</c>, always adds a new component even if one already exists.</param>
        /// <returns><c>true</c> if the component already existed; <c>false</c> if a new one was added.</returns>
        public static bool TryAddComponent<T>(this GameObject gameObject, out T component, bool supportMultiInstance = false) where T : UnityEngine.Component
        {
            component = null;

            if (gameObject == null)
            {
                Debug.LogWarning(SharedLocKey.log_gameobject_is_null.Localize());
                return false;
            }

            if (gameObject.TryGetComponent(out component) && !supportMultiInstance)
            {
                return true;
            }
            else
            {
                component = gameObject.AddComponent<T>();
                return false;
            }
        }

        /// <summary>
        /// Retrieves a component of type <typeparamref name="T"/> from the GameObject using exception-based detection.
        /// Unlike the built-in <c>TryGetComponent</c>, this variant accesses <c>.name</c> to confirm the component
        /// is not a destroyed instance, which catches cases where the reference is non-null but the object is dead.
        /// </summary>
        /// <typeparam name="T">The component type to retrieve.</typeparam>
        /// <param name="gameObject">The target GameObject.</param>
        /// <param name="component">The found component, or <c>default</c> if not found or destroyed.</param>
        /// <returns><c>true</c> if a live component was found; otherwise <c>false</c>.</returns>
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T : UnityEngine.Component
        {
            try
            {
                component = gameObject.GetComponent<T>();
                string _ = component.name;
                return true;
            }
            catch
            {
                component = default;
                return false;
            }
        }

        /// <summary>
        /// Adds a Unity UI <see cref="Graphic"/> component of type <typeparamref name="T"/> to the GameObject,
        /// but only if neither the exact type <typeparamref name="T"/> nor any other <see cref="Graphic"/> subtype
        /// is already present. This prevents layout-breaking duplicates since only one Graphic per object is valid.
        /// </summary>
        /// <typeparam name="T">The Graphic subtype to add (e.g. Image, RawImage).</typeparam>
        /// <param name="gameObject">The target GameObject.</param>
        /// <param name="graphic">The existing or newly added Graphic component.</param>
        /// <returns><c>true</c> if a new component was added; <c>false</c> if one already existed.</returns>
        public static bool TryAddGraphic<T>(this GameObject gameObject, out T graphic) where T : Graphic
        {
            if (gameObject.TryGetComponent(out graphic))
            {
                return false;
            }
            else if (gameObject.TryGetComponent(out Graphic _graphic))
            {
                return false;
            }
            else
            {
                graphic = gameObject.AddComponent<T>();
                return true;
            }
        }

        /// <summary>
        /// Finds and destroys the first component of type <typeparamref name="T"/> on the GameObject, if present.
        /// </summary>
        /// <typeparam name="T">The component type to destroy.</typeparam>
        /// <param name="gameObject">The target GameObject.</param>
        /// <returns><c>true</c> if the component was found and destroyed; <c>false</c> if it did not exist.</returns>
        public static bool TryDestroyComponent<T>(this GameObject gameObject) where T : UnityEngine.Component
        {
            if (gameObject.TryGetComponent(out T component))
            {
                component.Destroy();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Marks target object as dirty, but as an extension.
        /// </summary>
        /// <param name="object">The object to mark as dirty.</param>
        public static void SetDirtyExt(this UnityEngine.Object @object)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(@object);
#endif
        }

        /// <summary>
        /// Selects the given GameObject in the Unity Editor Hierarchy window,
        /// equivalent to clicking on it manually. No-op outside of the Editor.
        /// </summary>
        /// <param name="activeGameObject">The GameObject to select.</param>
        public static void MakeGameObjectSelectedInHierarchy(this GameObject activeGameObject)
        {
#if UNITY_EDITOR
            UnityEditor.Selection.activeGameObject = activeGameObject;
#endif
        }

        /// <summary>
        /// Creates an empty GameObject via <c>Instantiate</c>, optionally assigning a name and parent transform.
        /// The temporary source object is destroyed immediately after instantiation,
        /// so only the final instance survives.
        /// </summary>
        /// <param name="name">Optional display name for the new GameObject.</param>
        /// <param name="parent">Optional parent transform. When <c>null</c>, the object is created at the scene root.</param>
        /// <returns>The newly created empty GameObject.</returns>
        public static GameObject CreateEmptyGameObject(string name = null, Transform parent = null)
        {
            GameObject tempGO = new GameObject();
            GameObject emptyGO;

            if (parent == null)
            {
                emptyGO = UnityEngine.Object.Instantiate(tempGO);
            }
            else
            {
                emptyGO = UnityEngine.Object.Instantiate(tempGO, parent);
            }

            if (name != null)
            {
                tempGO.name = name;
            }

            tempGO.Destroy();
            return emptyGO;
        }
    }
}
