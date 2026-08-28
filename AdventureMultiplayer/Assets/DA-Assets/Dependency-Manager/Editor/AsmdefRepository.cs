using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.DM
{

    public static class AsmdefRepository
    {
        internal const string AsmdefExtension = ".asmdef";

        public static string FindNearestAsmdef(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            string directory = Path.GetDirectoryName(assetPath);
            string projectRoot = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/');

            while (!string.IsNullOrEmpty(directory))
            {
                string normalizedDir = Path.GetFullPath(directory).Replace('\\', '/').TrimEnd('/');

                if (!normalizedDir.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(normalizedDir, projectRoot, StringComparison.OrdinalIgnoreCase))
                    break;

                if (string.Equals(normalizedDir, projectRoot, StringComparison.OrdinalIgnoreCase))
                    break;

                string[] asmdefFiles = Directory.GetFiles(directory, "*" + AsmdefExtension);
                if (asmdefFiles.Length > 0)
                {
                    string fullPath = Path.GetFullPath(asmdefFiles[0]).Replace('\\', '/');
                    return fullPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase)
                        ? fullPath.Substring(projectRoot.Length + 1)
                        : fullPath;
                }

                directory = Path.GetDirectoryName(directory);
            }

            return null;
        }

        public static string ReadName(string asmdefAssetPath)
        {
            string fullPath = Path.GetFullPath(asmdefAssetPath);
            string content = File.ReadAllText(fullPath);
            AsmdefData data = JsonUtility.FromJson<AsmdefData>(content);
            return data.name;
        }

        public static AsmdefData LoadData(string asmdefAssetPath)
        {
            string fullPath = Path.GetFullPath(asmdefAssetPath);
            string content = File.ReadAllText(fullPath);
            return JsonUtility.FromJson<AsmdefData>(content);
        }

        public static void WriteData(string asmdefAssetPath, AsmdefData data)
        {
            string fullPath = Path.GetFullPath(asmdefAssetPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(fullPath, json);
            AssetDatabase.ImportAsset(asmdefAssetPath);
        }
    }
}
