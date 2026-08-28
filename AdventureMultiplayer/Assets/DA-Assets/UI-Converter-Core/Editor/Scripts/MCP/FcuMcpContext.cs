using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    internal static class FcuMcpContext
    {
        private const string SelectedFcuKeyPrefix = "FCU.MCP.SelectedFcu.";

        public static IReadOnlyList<FcuMcpInstanceInfo> GetInstances(string instanceTypeName = null)
        {
            return UnityEngine.Object
                .FindObjectsOfType<ConverterBase>(true)
                .Where(x => x != null && x.gameObject.scene.IsValid())
                .Where(x => MatchesInstanceType(x, instanceTypeName))
                .Select(x => CreateInfo(x, instanceTypeName))
                .OrderBy(x => x.scene)
                .ThenBy(x => x.path)
                .ToList();
        }

        public static ConverterBase GetSelectedOrThrow(string instanceTypeName, string instanceLabel)
        {
            string id = GetSelectedId(instanceTypeName);
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException($"No {instanceLabel} instance selected. Select an instance before running this tool.");
            }

            ConverterBase selected = UnityEngine.Object
                .FindObjectsOfType<ConverterBase>(true)
                .FirstOrDefault(x => x != null && x.Guid == id && MatchesInstanceType(x, instanceTypeName));

            if (selected == null)
            {
                throw new InvalidOperationException($"Selected {instanceLabel} instance '{id}' was not found in loaded scenes. Select an instance again.");
            }

            selected.InitServices();
            return selected;
        }

        public static bool TrySelect(string id, string instanceTypeName, out FcuMcpInstanceInfo selected)
        {
            selected = default;

            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            FcuMcpInstanceInfo match = GetInstances(instanceTypeName).FirstOrDefault(x => x.id == id);
            if (match == null)
            {
                return false;
            }

            EditorPrefs.SetString(GetSelectedKey(instanceTypeName), id);
            selected = match;
            selected.is_active = true;
            return true;
        }

        public static string GetSelectedId(string instanceTypeName = null)
        {
            return EditorPrefs.GetString(GetSelectedKey(instanceTypeName), string.Empty);
        }

        private static FcuMcpInstanceInfo CreateInfo(ConverterBase fcu, string instanceTypeName)
        {
            string id = fcu.Guid;
            return new FcuMcpInstanceInfo
            {
                id = id,
                name = fcu.name,
                scene = string.IsNullOrEmpty(fcu.gameObject.scene.path) ? fcu.gameObject.scene.name : fcu.gameObject.scene.path,
                path = GetPath(fcu.transform),
                is_active = id == GetSelectedId(instanceTypeName)
            };
        }

        private static bool MatchesInstanceType(ConverterBase fcu, string instanceTypeName)
        {
            return string.IsNullOrWhiteSpace(instanceTypeName) || fcu.GetType().FullName == instanceTypeName;
        }

        private static string GetPath(Transform transform)
        {
            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static string GetSelectedKey(string instanceTypeName)
        {
            string projectId = Hash128.Compute(Application.dataPath).ToString();
            string typeKey = string.IsNullOrWhiteSpace(instanceTypeName) ? "all" : instanceTypeName;
            return $"{SelectedFcuKeyPrefix}{projectId}.{typeKey}";
        }
    }

    [Serializable]
    internal class FcuMcpInstanceInfo
    {
        public string id;
        public string name;
        public string scene;
        public string path;
        public bool is_active;
    }
}