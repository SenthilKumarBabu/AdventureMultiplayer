using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace DA_Assets.DM
{

    [InitializeOnLoad]
    public static class DependencyManager
    {
        public const string PathNotFound = "Not found";
        private const string LogPrefix = "[DM]";

        private static readonly DependencyResolutionService Resolver = new DependencyResolutionService();
        private static bool _runScheduled;

        static DependencyManager()
        {
            DependencyManagerLog.Log($"{LogPrefix} static ctor — domain loaded; scheduling initial check.");
            Events.registeredPackages += _ =>
            {
                DependencyManagerLog.Log($"{LogPrefix} registeredPackages event — scheduling check.");
                ScheduleRun();
            };
            ScheduleRun();
        }

        public static IReadOnlyList<DependencyItem> GetDependencyItems()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(DependencyItem));
            List<DependencyItem> items = new List<DependencyItem>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                DependencyItem item = AssetDatabase.LoadAssetAtPath<DependencyItem>(path);
                if (item != null) items.Add(item);
            }
            return items;
        }

        public static void CheckAllDependencies()
        {
            IReadOnlyList<DependencyItem> items = GetDependencyItems();
            DependencyManagerLog.Log($"{LogPrefix} CheckAllDependencies start — items={items.Count}");

            bool anyChanged = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (ProcessSingleItem(items[i])) anyChanged = true;
            }

            string definesBefore = string.Join(";", DefineSyncService.GetDefines());
            DefineSyncService.Apply(items);
            string definesAfter = string.Join(";", DefineSyncService.GetDefines());
            bool definesChanged = !string.Equals(definesBefore, definesAfter, System.StringComparison.Ordinal);

            DependencyManagerLog.Log($"{LogPrefix} CheckAllDependencies end — anyItemChanged={anyChanged}, definesChanged={definesChanged}");
            if (definesChanged)
                DependencyManagerLog.Log($"{LogPrefix}   defines before: {definesBefore}\n{LogPrefix}   defines after : {definesAfter}");

            if (anyChanged)
            {
                DependencyManagerLog.Log($"{LogPrefix} Calling AssetDatabase.SaveAssets() because items changed.");
                AssetDatabase.SaveAssets();
            }
        }

        public static void CheckSingleItem(DependencyItem item)
        {
            if (item == null) return;
            ProcessSingleItem(item);
            DefineSyncService.Apply(GetDependencyItems());
            AssetDatabase.SaveAssets();
        }

        public static void ForceEnableDependency(DependencyItem item)
        {
            if (item == null) return;
            item.DisabledManually = false;
            ProcessSingleItem(item);
            DefineSyncService.Apply(GetDependencyItems());
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
        }

        public static void ForceDisableDependency(DependencyItem item)
        {
            if (item == null) return;
            item.DisabledManually = true;
            item.IsEnabled = false;
            SyncReferencingAsmdef(item, null, false);
            DefineSyncService.Apply(GetDependencyItems());
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
        }

        public static bool ProcessSingleItem(DependencyItem item)
        {
            if (item == null) return false;

            try
            {
                ResolutionResult result = Resolver.Resolve(item);
                bool desiredEnabled = result.Found && !item.DisabledManually;
                string dependencyAsmdefPath = null;

                if (result.Found)
                {
                    if (result.MatchedSource == DetectionStrategy.MonoScript)
                        dependencyAsmdefPath = EnsureGeneratedAsmdefForScript(item, result.DisplayPath);
                    else if (result.MatchedSource == DetectionStrategy.AssemblyDefinition)
                        dependencyAsmdefPath = result.DisplayPath;

                    SyncReferencingAsmdef(item, dependencyAsmdefPath, desiredEnabled);
                }
                else
                {
                    SyncReferencingAsmdef(item, null, false);

                    if ((item.Strategy & DetectionStrategy.MonoScript) != 0)
                        RemoveGeneratedAsmdefForScript(item);
                }

                string newPath = result.DisplayPath ?? PathNotFound;

                bool enabledChanged = item.IsEnabled != desiredEnabled;
                bool pathChanged = item.LastFoundPath != newPath;
                bool sourceChanged = item.LastMatchedSource != result.MatchedSource;
                bool changed = enabledChanged || pathChanged || sourceChanged;

                if (changed)
                {
                    DependencyManagerLog.Log($"{LogPrefix}   item '{item.name}' changed — " +
                              $"enabled:{item.IsEnabled}→{desiredEnabled} (Δ={enabledChanged}), " +
                              $"path:'{item.LastFoundPath}'→'{newPath}' (Δ={pathChanged}), " +
                              $"source:{item.LastMatchedSource}→{result.MatchedSource} (Δ={sourceChanged})");
                }

                item.IsEnabled = desiredEnabled;
                item.LastFoundPath = newPath;
                item.LastMatchedSource = result.MatchedSource;

                if (changed) EditorUtility.SetDirty(item);
                return changed;
            }
            catch (Exception ex)
            {
                DependencyManagerLog.Error($"{LogPrefix} ProcessSingleItem('{item.name}'): unhandled exception — {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private static string EnsureGeneratedAsmdefForScript(DependencyItem item, string scriptAssetPath)
        {
            if (string.IsNullOrEmpty(scriptAssetPath)) return null;

            string scriptDir = Path.GetDirectoryName(scriptAssetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(scriptDir)) return null;

            string nearest = AsmdefRepository.FindNearestAsmdef(scriptAssetPath);
            if (!string.IsNullOrEmpty(nearest))
            {
                bool isOwnWrapper = false;
                if (!string.IsNullOrEmpty(item.GeneratedAsmdefName))
                {
                    try
                    {
                        string nearestName = AsmdefRepository.ReadName(nearest);
                        isOwnWrapper = string.Equals(nearestName, item.GeneratedAsmdefName, System.StringComparison.Ordinal);
                    }
                    catch {   }
                }

                if (!isOwnWrapper)
                {
                    DependencyManagerLog.Log($"{LogPrefix}     EnsureGeneratedAsmdefForScript('{item.name}'): script '{scriptAssetPath}' already under third-party '{nearest}' — skip.");
                    return nearest;
                }

                DependencyManagerLog.Log($"{LogPrefix}     EnsureGeneratedAsmdefForScript('{item.name}'): nearest '{nearest}' is own wrapper — will sync.");
            }

            if (string.IsNullOrEmpty(item.GeneratedAsmdefName)) return null;

            DependencyManagerLog.Log($"{LogPrefix}     EnsureGeneratedAsmdefForScript('{item.name}'): generating '{item.GeneratedAsmdefName}' in '{scriptDir}'.");

            if (string.IsNullOrWhiteSpace(item.GeneratedAsmdefGuid))
            {
                DependencyManagerLog.Error(
                    $"{LogPrefix}     EnsureGeneratedAsmdefForScript('{item.name}'): " +
                    $"GeneratedAsmdefGuid is empty. Set a stable 32-hex GUID on the DependencyItem .asset " +
                    $"before this asmdef can be generated. Skipping.");
                return null;
            }

            string[] runtimeRefGuids = ToGuidArray(item.GeneratedAsmdefReferences);
            string[] runtimePlatforms = new string[0];
            string runtimeAssetPath = AsmdefGenerator.EnsureGenerated(
                scriptDir,
                item.GeneratedAsmdefName,
                runtimeRefGuids,
                runtimePlatforms,
                item.GeneratedAsmdefGuid);

            if (string.IsNullOrEmpty(item.GeneratedEditorAsmdefName)) return runtimeAssetPath;

            string editorDir = scriptDir + "/Editor";
            if (!Directory.Exists(Path.GetFullPath(editorDir))) return runtimeAssetPath;

            if (string.IsNullOrWhiteSpace(item.GeneratedEditorAsmdefGuid))
            {
                DependencyManagerLog.Error(
                    $"{LogPrefix}     EnsureGeneratedAsmdefForScript('{item.name}'): " +
                    $"GeneratedEditorAsmdefGuid is empty. Set a stable 32-hex GUID on the DependencyItem .asset " +
                    $"before the editor asmdef can be generated. Skipping editor asmdef.");
                return runtimeAssetPath;
            }

            List<string> editorRefs = new List<string>();
            if (!string.IsNullOrEmpty(runtimeAssetPath))
            {
                string runtimeGuid = AssetDatabase.AssetPathToGUID(runtimeAssetPath);
                if (!string.IsNullOrEmpty(runtimeGuid)) editorRefs.Add(runtimeGuid);
            }
            editorRefs.AddRange(ToGuidArray(item.GeneratedEditorAsmdefReferences));

            AsmdefGenerator.EnsureGenerated(
                editorDir,
                item.GeneratedEditorAsmdefName,
                editorRefs.ToArray(),
                new[] { "Editor" },
                item.GeneratedEditorAsmdefGuid);

            return runtimeAssetPath;
        }

        private static void RemoveGeneratedAsmdefForScript(DependencyItem item)
        {
            if (!string.IsNullOrEmpty(item.GeneratedEditorAsmdefName))
            {
                bool del = AsmdefGenerator.DeleteByName(item.GeneratedEditorAsmdefName);
                if (del) DependencyManagerLog.Log($"{LogPrefix}     RemoveGeneratedAsmdef('{item.name}'): deleted editor asmdef '{item.GeneratedEditorAsmdefName}'.");
            }

            if (!string.IsNullOrEmpty(item.GeneratedAsmdefName))
            {
                bool del = AsmdefGenerator.DeleteByName(item.GeneratedAsmdefName);
                if (del) DependencyManagerLog.Log($"{LogPrefix}     RemoveGeneratedAsmdef('{item.name}'): deleted runtime asmdef '{item.GeneratedAsmdefName}'.");
            }
        }

        private static string[] ToGuidArray(List<AssemblyDefinitionAsset> assets)
        {
            if (assets == null || assets.Count == 0) return new string[0];

            List<string> guids = new List<string>(assets.Count);
            foreach (AssemblyDefinitionAsset asset in assets)
            {
                if (asset == null) continue;
                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path)) continue;
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid)) guids.Add(guid);
            }
            return guids.ToArray();
        }

        private static string ResolveDependencyAsmdefGuid(DependencyItem item, string dependencyAsmdefPath)
        {
            if (!string.IsNullOrEmpty(dependencyAsmdefPath))
                return AssetDatabase.AssetPathToGUID(dependencyAsmdefPath);

            if (item == null) return null;

            if ((item.Strategy & DetectionStrategy.AssemblyDefinition) != 0 && item.Asmdef != null)
            {
                string asmdefPath = AssetDatabase.GetAssetPath(item.Asmdef);
                if (!string.IsNullOrEmpty(asmdefPath))
                    return AssetDatabase.AssetPathToGUID(asmdefPath);
            }

            if ((item.Strategy & DetectionStrategy.MonoScript) != 0 && item.Script != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(item.Script);
                string nearest = AsmdefRepository.FindNearestAsmdef(scriptPath);
                if (!string.IsNullOrEmpty(nearest))
                    return AssetDatabase.AssetPathToGUID(nearest);
            }

            if ((item.Strategy & DetectionStrategy.MonoScript) != 0 && !string.IsNullOrWhiteSpace(item.GeneratedAsmdefGuid))
                return AsmdefGenerator.NormalizeMetaGuid(item.GeneratedAsmdefGuid);

            return null;
        }

        private static void SyncReferencingAsmdef(DependencyItem item, string dependencyAsmdefPath, bool shouldReference)
        {
            if (item == null || item.ReferencingAsmdef == null)
                return;

            string dependencyAsmdefGuid = ResolveDependencyAsmdefGuid(item, dependencyAsmdefPath);
            if (string.IsNullOrEmpty(dependencyAsmdefGuid)) return;

            string targetPath = AssetDatabase.GetAssetPath(item.ReferencingAsmdef);
            if (string.IsNullOrEmpty(targetPath)) return;

            string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);
            if (string.Equals(targetGuid, dependencyAsmdefGuid, System.StringComparison.OrdinalIgnoreCase))
                return;

            string reference = "GUID:" + dependencyAsmdefGuid;
            AsmdefData data = AsmdefRepository.LoadData(targetPath);
            List<string> references = new List<string>(data.references ?? new string[0]);

            bool changed = false;
            if (shouldReference)
            {
                if (!references.Contains(reference))
                {
                    references.Add(reference);
                    changed = true;
                    DependencyManagerLog.Log($"{LogPrefix}     Added asmdef reference '{reference}' to '{targetPath}'.");
                }
            }
            else
            {
                int removed = references.RemoveAll(x => string.Equals(x, reference, System.StringComparison.Ordinal));
                if (removed > 0)
                {
                    changed = true;
                    DependencyManagerLog.Log($"{LogPrefix}     Removed asmdef reference '{reference}' from '{targetPath}'.");
                }
            }

            if (!changed) return;

            data.references = references.ToArray();
            AsmdefRepository.WriteData(targetPath, data);
        }

        private static void ScheduleRun()
        {
            if (_runScheduled) return;
            _runScheduled = true;

            EditorApplication.delayCall += () =>
            {
                _runScheduled = false;
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    DependencyManagerLog.Log($"{LogPrefix} delayCall fired but editor busy (compiling={EditorApplication.isCompiling}, updating={EditorApplication.isUpdating}) — rescheduling.");
                    ScheduleRun();
                    return;
                }
                DependencyManagerLog.Log($"{LogPrefix} delayCall fired — running CheckAllDependencies.");
                CheckAllDependencies();
            };
        }

        private sealed class AssetWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                string trigger = null;
                trigger ??= FirstRelevant(importedAssets, "imported");
                trigger ??= FirstRelevant(deletedAssets, "deleted");
                trigger ??= FirstRelevant(movedAssets, "moved");
                trigger ??= FirstRelevant(movedFromAssetPaths, "movedFrom");

                if (trigger != null)
                {
                    DependencyManagerLog.Log($"{LogPrefix} AssetPostprocessor triggered by {trigger} — scheduling check.");
                    ScheduleRun();
                }
            }

            private static string FirstRelevant(string[] paths, string label)
            {
                if (paths == null) return null;
                for (int i = 0; i < paths.Length; i++)
                {
                    string p = paths[i];
                    if (string.IsNullOrEmpty(p)) continue;
                    if (p.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith(".asmdef", System.StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase) ||
                        (p.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase) &&
                         p.IndexOf("/Dependencies/", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return $"{label}:{p}";
                    }
                }
                return null;
            }
        }
    }
}
