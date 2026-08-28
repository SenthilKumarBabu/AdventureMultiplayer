using System;
using System.Reflection;
using UnityEngine;

namespace DA_Assets.Extensions
{
    public static class ComponentExtensions
    {
        /// <summary>
        /// Copies all serialized fields (both <c>public</c> and <c>[SerializeField]</c> private)
        /// from <paramref name="source"/> to <paramref name="destination"/> via reflection.
        /// Intended for duplicating component state when a GameObject is reconstructed
        /// (e.g. when replacing Transform with RectTransform or vice versa).
        /// </summary>
        /// <param name="source">The component to read values from.</param>
        /// <param name="destination">The component to write values into. Must be the same type as source.</param>
        public static void CopySerializedFields(this Component source, Component destination)
        {
            FieldInfo[] fields = source.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                {
                    field.SetValue(destination, field.GetValue(source));
                }
            }
        }

        /// <summary>
        /// Null-safe wrapper around <see cref="Component.TryGetComponent{T}"/>.
        /// Accepts a <see cref="Component"/> as the receiver (rather than <see cref="GameObject"/>)
        /// and returns <c>false</c> immediately when it is <c>null</c>,
        /// preventing a MissingReferenceException for destroyed objects.
        /// </summary>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <param name="gameObject">The source Component whose GameObject is queried. May be null.</param>
        /// <param name="component">The found component, or <c>default</c> if not found or the source is null.</param>
        /// <returns><c>true</c> if the component was found; otherwise <c>false</c>.</returns>
        public static bool TryGetComponentSafe<T>(this Component gameObject, out T component)
        {
            component = default;

            if (gameObject == null)
                return false;

            return gameObject.TryGetComponent(out component);
        }

        /// <summary>
        /// Destroying script of Unity GameObject, but as an extension.
        /// <para>Works in Editor and Playmode.</para>
        /// </summary>
        public static bool Destroy(this UnityEngine.Component unityComponent)
        {
            try
            {
                if (unityComponent.IsRequiredByAnotherComponents())
                    return false;

#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(unityComponent);
#else
                UnityEngine.Object.Destroy(unityComponent);
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes all components from the GameObject that have a 'RequireComponent' attribute pointing to the given component.
        /// </summary>
        /// <param name="component">The target component which other components might depend on.</param>
        /// <returns>True if any dependent components were removed, false otherwise.</returns>
        public static bool RemoveComponentsDependingOn(this UnityEngine.Component component)
        {
            bool removedAny = false;
            Type type = component.GetType();
            Component[] componentsOnObject = component.gameObject.GetComponents<Component>();

            foreach (Component comp in componentsOnObject)
            {
                object[] requireAttributes = comp.GetType().GetCustomAttributes(typeof(RequireComponent), true);

                foreach (RequireComponent attribute in requireAttributes)
                {
                    if (attribute.m_Type0 == type ||
                        attribute.m_Type1 == type ||
                        attribute.m_Type2 == type)
                    {
                        comp.Destroy();
                        removedAny = true;
                        break;
                    }
                }
            }

            return removedAny;
        }

        /// <summary>
        /// Checks if the given component is required by any other components on the same GameObject via the RequireComponent attribute.
        /// </summary>
        /// <param name="component">The component to check for.</param>
        /// <returns>True if the component is required by another component on the same GameObject, otherwise false.</returns>
        public static bool IsRequiredByAnotherComponents(this UnityEngine.Component component)
        {
            Type type = component.GetType();
            Component[] componentsOnObject = component.gameObject.GetComponents<Component>();

            foreach (Component comp in componentsOnObject)
            {
                object[] requireAttributes = comp.GetType().GetCustomAttributes(typeof(RequireComponent), true);

                foreach (RequireComponent attribute in requireAttributes)
                {
                    if (attribute.m_Type0 == type ||
                        attribute.m_Type1 == type ||
                        attribute.m_Type2 == type)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}
