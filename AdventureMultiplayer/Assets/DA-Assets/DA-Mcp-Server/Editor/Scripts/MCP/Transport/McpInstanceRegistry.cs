using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    internal readonly struct McpInstanceDescriptor
    {
        public McpInstanceDescriptor(string id, string displayName, string typeName, string path)
        {
            Id = id;
            DisplayName = displayName;
            TypeName = typeName;
            Path = path;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string TypeName { get; }
        public string Path { get; }
    }

    internal static class McpInstanceRegistry
    {
        public static IReadOnlyList<McpInstanceDescriptor> List()
        {
            return Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                .OfType<IMcpInstance>()
                .Select(instance => new McpInstanceDescriptor(
                    instance.McpInstanceId,
                    instance.McpDisplayName,
                    instance.GetType().Name,
                    GetPath(instance)))
                .OrderBy(x => x.TypeName)
                .ThenBy(x => x.DisplayName)
                .ToList();
        }

        private static string GetPath(IMcpInstance instance)
        {
            if (instance is Component component)
            {
                string assetPath = AssetDatabase.GetAssetPath(component);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    return assetPath;
                }
                return GetTransformPath(component.transform);
            }
            if (instance is Object unityObject)
            {
                return AssetDatabase.GetAssetPath(unityObject);
            }
            return string.Empty;
        }

        private static string GetTransformPath(Transform transform)
        {
            var names = new Stack<string>();
            while (transform != null)
            {
                names.Push(transform.name);
                transform = transform.parent;
            }
            return string.Join("/", names);
        }
    }
}
