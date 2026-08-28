using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DA_Assets.UCC.Snapshot
{
    public static class SnapshotComparer
    {
        public static ComparisonReport Compare(Transform root, string zipPath)
        {
            if (root == null)
            {
                Debug.LogError("SnapshotComparer: root Transform is null.");
                return default;
            }

            var baselineData = SnapshotSaver.ReadBaselineFromZip(zipPath, out string figmaResponseJson);

            if (baselineData.Count == 0)
            {
                Debug.LogError("SnapshotComparer: baseline ZIP is empty or could not be read.");
                return default;
            }


            Dictionary<string, string> nodeMap = null;

            if (!string.IsNullOrEmpty(figmaResponseJson))
            {
                nodeMap = FigmaResponseParser.ParseNodesFromLog(figmaResponseJson);
            }

            var sceneData = SnapshotSaver.CollectSceneData(root, nodeMap);
            string rootName = SnapshotSaver.SanitizeName(root.name);

            var report = new ComparisonReport
            {
                RootEntries = new List<GameObjectEntry>()
            };

            var rootEntry = BuildHierarchy(rootName, baselineData, sceneData, nodeMap);
            report.RootEntries.Add(rootEntry);
            report.TotalDeviations = rootEntry.DeviationCount;
            report.TotalComponents = CountTotalComponents(rootEntry);

            return report;
        }

        private static GameObjectEntry BuildHierarchy(
            string rootPath,
            Dictionary<string, string> baselineData,
            Dictionary<string, string> sceneData,
            Dictionary<string, string> nodeMap = null)
        {

            var allGoPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string key in baselineData.Keys)
            {
                string goPath = GetParentPath(key);
                AddAllAncestors(goPath, allGoPaths);
            }

            foreach (string key in sceneData.Keys)
            {
                string goPath = GetParentPath(key);
                AddAllAncestors(goPath, allGoPaths);
            }


            var relevantPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in allGoPaths)
            {
                if (path.Equals(rootPath, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    relevantPaths.Add(path);
                }
            }


            Dictionary<string, string> pathToFigmaId = null;

            if (nodeMap != null)
            {
                pathToFigmaId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);



                foreach (string key in baselineData.Keys)
                {
                    if (key.EndsWith("/Node-1", StringComparison.OrdinalIgnoreCase))
                    {
                        string goPath = GetParentPath(key);

                        string nodeJson = baselineData[key];

                        foreach (var kvp in nodeMap)
                        {
                            if (string.Equals(kvp.Value, nodeJson, StringComparison.Ordinal))
                            {
                                pathToFigmaId[goPath] = kvp.Key;
                                break;
                            }
                        }
                    }
                }
            }


            return BuildNode(rootPath, relevantPaths, baselineData, sceneData, nodeMap, pathToFigmaId);
        }

        private static GameObjectEntry BuildNode(
            string path,
            HashSet<string> allGoPaths,
            Dictionary<string, string> baselineData,
            Dictionary<string, string> sceneData,
            Dictionary<string, string> nodeMap = null,
            Dictionary<string, string> pathToFigmaId = null)
        {
            string name = GetLastSegment(path);

            var entry = new GameObjectEntry
            {
                Name = name,
                RelativePath = path,
                Components = new List<ComponentEntry>(),
                Children = new List<GameObjectEntry>(),
                Status = EntryStatus.Match,
                DeviationCount = 0
            };


            if (nodeMap != null && pathToFigmaId != null && pathToFigmaId.TryGetValue(path, out string figmaId))
            {
                if (nodeMap.TryGetValue(figmaId, out string figmaJson))
                {
                    entry.FigmaJson = figmaJson;
                }
            }


            var baselineComponents = GetDirectComponents(path, baselineData);
            var sceneComponents = GetDirectComponents(path, sceneData);

            var allComponentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string k in baselineComponents.Keys) allComponentKeys.Add(k);
            foreach (string k in sceneComponents.Keys) allComponentKeys.Add(k);

            foreach (string compName in allComponentKeys)
            {
                bool inBaseline = baselineComponents.TryGetValue(compName, out string baselineJson);
                bool inScene = sceneComponents.TryGetValue(compName, out string sceneJson);

                ComponentEntry comp;

                if (inBaseline && inScene)
                {
                    bool isMatch = string.Equals(
                        NormalizeJson(baselineJson),
                        NormalizeJson(sceneJson),
                        StringComparison.Ordinal);

                    comp = new ComponentEntry
                    {
                        FileName = compName,
                        BaselineJson = baselineJson,
                        SceneJson = sceneJson,
                        Status = isMatch ? EntryStatus.Match : EntryStatus.Diff,
                        DiffLineCount = isMatch ? 0 : CountDiffLines(baselineJson, sceneJson)
                    };

                    if (!isMatch)
                        entry.DeviationCount++;
                }
                else if (inBaseline)
                {
                    comp = new ComponentEntry
                    {
                        FileName = compName,
                        BaselineJson = baselineJson,
                        SceneJson = "",
                        Status = EntryStatus.Missing,
                        DiffLineCount = 0
                    };
                    entry.DeviationCount++;
                }
                else
                {
                    comp = new ComponentEntry
                    {
                        FileName = compName,
                        BaselineJson = "",
                        SceneJson = sceneJson,
                        Status = EntryStatus.Extra,
                        DiffLineCount = 0
                    };
                    entry.DeviationCount++;
                }

                entry.Components.Add(comp);
            }


            var directChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string prefix = path + "/";

            foreach (string goPath in allGoPaths)
            {
                if (!goPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string remainder = goPath.Substring(prefix.Length);

                if (!remainder.Contains("/"))
                {
                    directChildren.Add(goPath);
                }
            }


            var sortedChildren = directChildren.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (string childPath in sortedChildren)
            {
                var childEntry = BuildNode(childPath, allGoPaths, baselineData, sceneData, nodeMap, pathToFigmaId);
                entry.Children.Add(childEntry);
                entry.DeviationCount += childEntry.DeviationCount;
            }


            bool hasBaselineContent = HasAnyKeysUnder(path, baselineData);
            bool hasSceneContent = HasAnyKeysUnder(path, sceneData);

            if (hasBaselineContent && !hasSceneContent)
                entry.Status = EntryStatus.Missing;
            else if (!hasBaselineContent && hasSceneContent)
                entry.Status = EntryStatus.Extra;
            else if (entry.DeviationCount > 0)
                entry.Status = EntryStatus.Diff;
            else
                entry.Status = EntryStatus.Match;

            return entry;
        }

        private static string GetParentPath(string key)
        {
            int idx = key.LastIndexOf('/');
            return idx > 0 ? key.Substring(0, idx) : key;
        }

        private static string GetLastSegment(string path)
        {
            int idx = path.LastIndexOf('/');
            return idx >= 0 ? path.Substring(idx + 1) : path;
        }

        private static void AddAllAncestors(string path, HashSet<string> set)
        {
            while (!string.IsNullOrEmpty(path))
            {
                if (!set.Add(path))
                    break;

                int idx = path.LastIndexOf('/');
                if (idx <= 0) break;
                path = path.Substring(0, idx);
            }
        }

        private static Dictionary<string, string> GetDirectComponents(
            string goPath,
            Dictionary<string, string> data)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string prefix = goPath + "/";

            foreach (var kvp in data)
            {
                if (!kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string remainder = kvp.Key.Substring(prefix.Length);

                if (!remainder.Contains("/"))
                {
                    result[remainder] = kvp.Value;
                }
            }

            return result;
        }

        private static bool HasAnyKeysUnder(string path, Dictionary<string, string> data)
        {
            string prefix = path + "/";
            foreach (string key in data.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string NormalizeJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return "";

            return json.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();
        }

        public static int CountDiffLines(string baseline, string scene)
        {
            string[] linesA = NormalizeJson(baseline).Split('\n');
            string[] linesB = NormalizeJson(scene).Split('\n');

            int maxLen = Math.Max(linesA.Length, linesB.Length);
            int diffCount = 0;

            for (int i = 0; i < maxLen; i++)
            {
                string a = i < linesA.Length ? linesA[i] : "";
                string b = i < linesB.Length ? linesB[i] : "";

                if (!string.Equals(a, b, StringComparison.Ordinal))
                    diffCount++;
            }

            return diffCount;
        }

        private static int CountTotalComponents(GameObjectEntry entry)
        {
            int count = entry.Components.Count;

            foreach (var child in entry.Children)
            {
                count += CountTotalComponents(child);
            }

            return count;
        }
    }
}