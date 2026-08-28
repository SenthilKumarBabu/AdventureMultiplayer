using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.DM
{

    public static class AsmdefGenerator
    {
        private const string GuidPrefix = "GUID:";
        private const string PackagesRoot = "Packages/";
        private static readonly Regex AsmdefGuidRegex = new Regex("^[0-9a-fA-F]{32}$", RegexOptions.Compiled);

        public static string EnsureGenerated(
            string directory,
            string asmdefName,
            string[] referenceGuids = null,
            string[] includePlatforms = null,
            string desiredMetaGuid = null)
        {
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(asmdefName))
                return null;

            string normalizedDir = directory.Replace('\\', '/').TrimEnd('/');
            if (normalizedDir.StartsWith(PackagesRoot, StringComparison.OrdinalIgnoreCase))
            {
                DependencyManagerLog.Warning(
                    $"Dependency Manager: skipping asmdef generation '{asmdefName}' inside read-only Packages directory '{normalizedDir}'. " +
                    "Vendor the dependency under Assets/ or rely on its own asmdef.");
                return null;
            }

            string assetPath = $"{normalizedDir}/{asmdefName}{AsmdefRepository.AsmdefExtension}";
            string fullPath = Path.GetFullPath(assetPath);

            string[] desiredRefs = BuildReferences(referenceGuids);
            string[] desiredPlatforms = includePlatforms ?? Array.Empty<string>();
            string normalizedDesiredGuid = NormalizeMetaGuid(desiredMetaGuid);
            if (string.IsNullOrEmpty(normalizedDesiredGuid))
            {
                DependencyManagerLog.Error(
                    $"[DM][AsmdefGen] EnsureGenerated: stable GUID is required for '{asmdefName}'. " +
                    "Generation skipped.");
                return null;
            }

            if (File.Exists(fullPath))
            {
                AsmdefData existing = AsmdefRepository.LoadData(assetPath);

                if (string.Equals(existing.name, asmdefName, StringComparison.Ordinal))
                {
                    bool refsChanged = !Same(existing.references ?? Array.Empty<string>(), desiredRefs);
                    bool platformsChanged = !Same(existing.includePlatforms ?? Array.Empty<string>(), desiredPlatforms);

                    if (refsChanged || platformsChanged)
                    {
                        DependencyManagerLog.Log($"[DM][AsmdefGen] EnsureGenerated: UPDATING '{assetPath}' (refsΔ={refsChanged}, platformsΔ={platformsChanged}).");
                        existing.references = desiredRefs;
                        existing.includePlatforms = desiredPlatforms;
                        AsmdefRepository.WriteData(assetPath, existing);
                    }

                    EnsureMetaGuid(assetPath, normalizedDesiredGuid);
                    return assetPath;
                }
            }

            DependencyManagerLog.Log($"[DM][AsmdefGen] EnsureGenerated: CREATING '{assetPath}'.");

            var data = new AsmdefData
            {
                name = asmdefName,
                autoReferenced = true,
                references = desiredRefs,
                includePlatforms = desiredPlatforms,
                excludePlatforms = Array.Empty<string>(),
                precompiledReferences = Array.Empty<string>(),
                defineConstraints = Array.Empty<string>(),
                versionDefines = Array.Empty<VersionDefineData>(),
            };

            string fullAsmdefPath = Path.GetFullPath(assetPath);
            string asmdefDirectory = Path.GetDirectoryName(fullAsmdefPath);
            if (!string.IsNullOrEmpty(asmdefDirectory) && !Directory.Exists(asmdefDirectory))
                Directory.CreateDirectory(asmdefDirectory);

            File.WriteAllText(fullAsmdefPath, JsonUtility.ToJson(data, true));

            TryWriteAsmdefMeta(assetPath, normalizedDesiredGuid);

            AssetDatabase.ImportAsset(assetPath);

            EnsureMetaGuid(assetPath, normalizedDesiredGuid);
            return assetPath;
        }

        public static string NormalizeMetaGuid(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string trimmed = raw.Trim().Replace("-", string.Empty);
            if (!AsmdefGuidRegex.IsMatch(trimmed))
                throw new ArgumentException(
                    $"Invalid asmdef meta GUID '{raw}'. Expected 32 hexadecimal characters.",
                    nameof(raw));

            return trimmed.ToLowerInvariant();
        }

        private static void EnsureMetaGuid(string assetPath, string desiredGuid)
        {
            if (string.IsNullOrEmpty(desiredGuid)) return;

            string currentGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.Equals(currentGuid, desiredGuid, StringComparison.OrdinalIgnoreCase))
                return;

            string ownerOfDesired = AssetDatabase.GUIDToAssetPath(desiredGuid);
            if (!string.IsNullOrEmpty(ownerOfDesired) &&
                !string.Equals(ownerOfDesired, assetPath, StringComparison.OrdinalIgnoreCase))
            {
                DependencyManagerLog.Warning(
                    $"[DM][AsmdefGen] EnsureMetaGuid: desired GUID '{desiredGuid}' is already used by '{ownerOfDesired}'. " +
                    $"Leaving '{assetPath}' with its current GUID '{currentGuid}'.");
                return;
            }

            if (TryWriteAsmdefMeta(assetPath, desiredGuid))
            {
                DependencyManagerLog.Log($"[DM][AsmdefGen] EnsureMetaGuid: rewrote meta of '{assetPath}' GUID {currentGuid} → {desiredGuid}.");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static bool TryWriteAsmdefMeta(string assetPath, string guid)
        {
            try
            {
                string metaFullPath = Path.GetFullPath(assetPath + ".meta");
                string metaContent =
                    "fileFormatVersion: 2\n" +
                    $"guid: {guid}\n" +
                    "AssemblyDefinitionImporter:\n" +
                    "  externalObjects: {}\n" +
                    "  userData: \n" +
                    "  assetBundleName: \n" +
                    "  assetBundleVariant: \n";
                File.WriteAllText(metaFullPath, metaContent);
                return true;
            }
            catch (Exception ex)
            {
                DependencyManagerLog.Warning($"[DM][AsmdefGen] TryWriteAsmdefMeta failed for '{assetPath}': {ex.Message}");
                return false;
            }
        }

        public static bool DeleteByName(string asmdefName)
        {
            if (string.IsNullOrWhiteSpace(asmdefName)) return false;

            string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                AsmdefData data;
                try { data = AsmdefRepository.LoadData(path); }
                catch { continue; }

                if (string.Equals(data.name, asmdefName, StringComparison.Ordinal))
                {
                    return AssetDatabase.DeleteAsset(path);
                }
            }

            return false;
        }

        private static string[] BuildReferences(string[] referenceGuids)
        {
            if (referenceGuids == null || referenceGuids.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>(referenceGuids.Length);
            foreach (string guid in referenceGuids)
            {
                if (string.IsNullOrWhiteSpace(guid)) continue;
                result.Add(GuidPrefix + guid);
            }
            return result.ToArray();
        }

        private static bool Same(string[] a, string[] b)
        {
            if (a.Length != b.Length) return false;
            var setA = new HashSet<string>(a, StringComparer.Ordinal);
            foreach (string item in b)
                if (!setA.Contains(item)) return false;
            return true;
        }
    }
}
