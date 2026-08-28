using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.UCC.Snapshot
{
    public static class SnapshotSaver
    {

        private static readonly HashSet<Type> IgnoredTypes = new HashSet<Type>
        {
            typeof(SyncHelper),
            typeof(CanvasRenderer)
        };

        public static string BaselinesFolder
        {
            get
            {
                string path = Path.Combine(
                    Application.dataPath,
                    "DA-Assets",
                    "Figma-Converter-for-Unity",
                    "Runtime",
                    "Resources",
                    "Baselines");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        public static void ExportBaselineZip(Transform root, string figmaLogPath = null)
        {
            if (root == null)
            {
                Debug.LogError("SnapshotSaver: root Transform is null.");
                return;
            }

            string fileName = SanitizeName(root.name) + "_baseline.zip";
            string savePath = Path.Combine(BaselinesFolder, fileName);

            string tempDir = Path.Combine(Path.GetTempPath(), "fcu_baseline_" + Guid.NewGuid().ToString("N"));


            Dictionary<string, string> nodeMap = null;

            if (!string.IsNullOrEmpty(figmaLogPath) && File.Exists(figmaLogPath))
            {
                string logContent = File.ReadAllText(figmaLogPath);
                nodeMap = FigmaResponseParser.ParseNodesFromLog(logContent);
                Debug.Log($"SnapshotSaver: Parsed {nodeMap.Count} Nodes from log.");
            }

            try
            {
                Directory.CreateDirectory(tempDir);
                string rootFolder = Path.Combine(tempDir, SanitizeName(root.name));
                Directory.CreateDirectory(rootFolder);

                SerializeGameObject(root.gameObject, rootFolder, nodeMap);


                if (!string.IsNullOrEmpty(figmaLogPath) && File.Exists(figmaLogPath))
                {
                    string destPath = Path.Combine(tempDir, FigmaResponseParser.FigmaResponseEntryName);
                    File.Copy(figmaLogPath, destPath, true);
                }

                if (File.Exists(savePath))
                    File.Delete(savePath);

                ZipFile.CreateFromDirectory(tempDir, savePath);

                AssetDatabase.Refresh();
                Debug.Log($"SnapshotSaver: Baseline exported to '{savePath}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"SnapshotSaver: Export failed. {ex}");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        public static string[] GetAvailableBaselines()
        {
            string folder = BaselinesFolder;
            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            string[] files = Directory.GetFiles(folder, "*.zip");
            string[] names = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return names;
        }

        public static string GetBaselinePath(string displayName)
        {
            return Path.Combine(BaselinesFolder, displayName + ".zip");
        }

        public static Dictionary<string, string> ReadBaselineFromZip(string zipPath)
        {
            return ReadBaselineFromZip(zipPath, out _);
        }

        public static Dictionary<string, string> ReadBaselineFromZip(string zipPath, out string figmaResponseJson)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            figmaResponseJson = null;

            if (!File.Exists(zipPath))
            {
                Debug.LogError($"SnapshotSaver: ZIP file not found: '{zipPath}'.");
                return result;
            }

            using (var stream = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {

                    if (string.IsNullOrEmpty(entry.Name))
                        continue;


                    if (string.Equals(entry.Name, FigmaResponseParser.FigmaResponseEntryName, StringComparison.OrdinalIgnoreCase))
                    {
                        using (var entryStream = entry.Open())
                        using (var reader = new StreamReader(entryStream))
                        {
                            figmaResponseJson = reader.ReadToEnd();
                        }
                        continue;
                    }


                    if (!entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        continue;


                    string relativePath = entry.FullName.Replace('\\', '/');
                    if (relativePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        relativePath = relativePath.Substring(0, relativePath.Length - 4);

                    using (var entryStream = entry.Open())
                    using (var reader = new StreamReader(entryStream))
                    {
                        result[relativePath] = reader.ReadToEnd();
                    }
                }
            }

            return result;
        }

        private static void SerializeGameObject(GameObject go, string folderPath, Dictionary<string, string> nodeMap = null)
        {
            SerializeComponents(go, folderPath);


            if (nodeMap != null)
            {
                var syncHelper = go.GetComponent<SyncHelper>();

                if (syncHelper != null && syncHelper.Data != null && !string.IsNullOrEmpty(syncHelper.Data.Id))
                {
                    if (nodeMap.TryGetValue(syncHelper.Data.Id, out string nodeJson))
                    {
                        string nodePath = Path.Combine(folderPath, "Node-1.txt");
                        File.WriteAllText(nodePath, nodeJson);
                    }
                }
            }

            string[] indexedNames = BuildIndexedChildNames(go.transform);

            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                string childFolder = Path.Combine(folderPath, indexedNames[i]);
                Directory.CreateDirectory(childFolder);
                SerializeGameObject(child.gameObject, childFolder, nodeMap);
            }
        }

        private static void SerializeComponents(GameObject go, string folderPath)
        {
            Component[] components = go.GetComponents<Component>();
            var typeCounters = new Dictionary<string, int>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                Type type = component.GetType();

                if (IgnoredTypes.Contains(type))
                    continue;

                string typeName = type.Name;

                if (!typeCounters.ContainsKey(typeName))
                    typeCounters[typeName] = 0;

                typeCounters[typeName]++;
                int index = typeCounters[typeName];

                string fileName = $"{typeName}-{index}.txt";
                string filePath = Path.Combine(folderPath, fileName);

                string json = EditorJsonUtility.ToJson(component, true);
                File.WriteAllText(filePath, json);
            }
        }

        public static Dictionary<string, string> CollectSceneData(Transform root)
        {
            return CollectSceneData(root, null);
        }

        public static Dictionary<string, string> CollectSceneData(Transform root, Dictionary<string, string> nodeMap)
        {
            var result = new Dictionary<string, string>();
            CollectRecursive(root.gameObject, "", result, null, nodeMap);
            return result;
        }

        private static void CollectRecursive(
            GameObject go,
            string parentPath,
            Dictionary<string, string> result,
            string indexedName,
            Dictionary<string, string> nodeMap = null)
        {
            string goName = indexedName ?? SanitizeName(go.name);
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? goName
                : parentPath + "/" + goName;

            Component[] components = go.GetComponents<Component>();
            var typeCounters = new Dictionary<string, int>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                Type type = component.GetType();

                if (IgnoredTypes.Contains(type))
                    continue;

                string typeName = type.Name;

                if (!typeCounters.ContainsKey(typeName))
                    typeCounters[typeName] = 0;

                typeCounters[typeName]++;
                int index = typeCounters[typeName];

                string key = $"{currentPath}/{typeName}-{index}";
                string json = EditorJsonUtility.ToJson(component, true);
                result[key] = json;
            }


            if (nodeMap != null)
            {
                var syncHelper = go.GetComponent<SyncHelper>();

                if (syncHelper != null && syncHelper.Data != null && !string.IsNullOrEmpty(syncHelper.Data.Id))
                {
                    if (nodeMap.TryGetValue(syncHelper.Data.Id, out string nodeJson))
                    {
                        result[$"{currentPath}/Node-1"] = nodeJson;
                    }
                }
            }

            string[] childIndexedNames = BuildIndexedChildNames(go.transform);

            for (int i = 0; i < go.transform.childCount; i++)
            {
                CollectRecursive(
                    go.transform.GetChild(i).gameObject,
                    currentPath,
                    result,
                    childIndexedNames[i],
                    nodeMap);
            }
        }

        public static string[] BuildIndexedChildNames(Transform parent)
        {
            int count = parent.childCount;
            string[] result = new string[count];
            var nameCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < count; i++)
            {
                string baseName = SanitizeName(parent.GetChild(i).name);

                if (!nameCounters.ContainsKey(baseName))
                    nameCounters[baseName] = 0;

                nameCounters[baseName]++;
                int occurrence = nameCounters[baseName];

                result[i] = occurrence == 1 ? baseName : $"{baseName} ({occurrence})";
            }

            return result;
        }

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unnamed";

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }

            return name.Trim();
        }
    }
}