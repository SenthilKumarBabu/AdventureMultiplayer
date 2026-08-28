using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.DM
{
    public static class DefineSyncService
    {
        private const string LogPrefix = "[DM][DefineSync]";

        public static void Apply(IReadOnlyList<DependencyItem> items)
        {
            if (items == null) return;

            HashSet<string> managed = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> shouldBeOn = new HashSet<string>(StringComparer.Ordinal);

            foreach (DependencyItem item in items)
            {
                if (item == null) continue;

                string symbol = item.ScriptingDefineSymbol?.Trim();
                if (string.IsNullOrEmpty(symbol)) continue;

                managed.Add(symbol);
                if (item.IsEnabled) shouldBeOn.Add(symbol);
            }

            HashSet<string> current = new HashSet<string>(GetDefines(), StringComparer.Ordinal);
            HashSet<string> desired = new HashSet<string>(current, StringComparer.Ordinal);

            List<string> toAdd = new List<string>();
            List<string> toRemove = new List<string>();

            foreach (string symbol in managed)
            {
                bool present = current.Contains(symbol);
                bool wantOn = shouldBeOn.Contains(symbol);

                if (wantOn && !present)
                {
                    desired.Add(symbol);
                    toAdd.Add(symbol);
                }
                else if (!wantOn && present)
                {
                    desired.Remove(symbol);
                    toRemove.Add(symbol);
                }
            }

            if (current.SetEquals(desired))
            {
                DependencyManagerLog.Log($"{LogPrefix} Apply: managed={managed.Count}, defines unchanged ({current.Count}).");
                return;
            }

            if (toAdd.Count > 0) DependencyManagerLog.Log($"{LogPrefix} Apply: ADD [{string.Join(",", toAdd)}].");
            if (toRemove.Count > 0) DependencyManagerLog.Log($"{LogPrefix} Apply: REMOVE [{string.Join(",", toRemove)}].");

            List<string> sorted = desired.OrderBy(d => d, StringComparer.Ordinal).ToList();
            SetDefines(sorted);
        }

        public static List<string> GetDefines()
        {
            string raw = ReadRawDefines();
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            return raw.Split(';')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        public static void SetDefines(IEnumerable<string> defines)
        {
            List<string> cleaned = defines
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            string joined = string.Join(";", cleaned);
            string current = ReadRawDefines();
            if (string.Equals(current, joined, StringComparison.Ordinal))
                return;

            WriteRawDefines(joined);
        }

        public static void ClearLoopGuard(string symbol)
        {
        }

        private static string ReadRawDefines()
        {
#if UNITY_2023_1_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            UnityEditor.Build.NamedBuildTarget named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            return PlayerSettings.GetScriptingDefineSymbols(named);
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
        }

        private static void WriteRawDefines(string joined)
        {
#if UNITY_2023_1_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            UnityEditor.Build.NamedBuildTarget named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            PlayerSettings.SetScriptingDefineSymbols(named, joined);
#else
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, joined);
#endif
        }
    }
}
