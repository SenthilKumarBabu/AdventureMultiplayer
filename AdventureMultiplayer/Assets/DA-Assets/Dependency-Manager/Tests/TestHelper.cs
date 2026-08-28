using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DA_Assets.DM.Tests
{

    public static class TestHelper
    {
        public const string TempRoot = "Assets/_DMTestTemp";

        public static void EnsureTempDirectory()
        {
            string fullPath = Path.GetFullPath(TempRoot);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.ImportAsset(TempRoot);
            }
        }

        public static string EnsureSubDirectory(string subDir)
        {
            string assetPath = $"{TempRoot}/{subDir}";
            string fullPath = Path.GetFullPath(assetPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.ImportAsset(assetPath);
            }
            return assetPath;
        }

        public static DependencyItem CreatePrecompiledItem(
            string name,
            string assemblyQualifiedTypeName,
            string symbol,
            string[] expected = null,
            string[] unexpected = null)
        {
            EnsureTempDirectory();

            DependencyItem item = ScriptableObject.CreateInstance<DependencyItem>();
            item.Strategy = DetectionStrategy.PrecompiledAssembly;
            item.AssemblyQualifiedTypeName = assemblyQualifiedTypeName;
            item.ScriptingDefineSymbol = symbol;
            item.ExpectedPathSuffixes = expected;
            item.UnexpectedPathSuffixes = unexpected;

            string assetPath = $"{TempRoot}/{name}.asset";
            AssetDatabase.CreateAsset(item, assetPath);
            AssetDatabase.SaveAssets();
            return item;
        }

        public static DependencyItem CreateAsmdefItem(
            string name,
            AssemblyDefinitionAsset asmdef,
            string symbol)
        {
            EnsureTempDirectory();

            DependencyItem item = ScriptableObject.CreateInstance<DependencyItem>();
            item.Strategy = DetectionStrategy.AssemblyDefinition;
            item.Asmdef = asmdef;
            item.ScriptingDefineSymbol = symbol;

            string assetPath = $"{TempRoot}/{name}.asset";
            AssetDatabase.CreateAsset(item, assetPath);
            AssetDatabase.SaveAssets();
            return item;
        }

        public static string CreateTempAsmdef(string subDir, string assemblyName)
        {
            string dir = EnsureSubDirectory(subDir);
            string assetPath = $"{dir}/{assemblyName}.asmdef";
            string fullPath = Path.GetFullPath(assetPath);

            string json =
                "{\n" +
                $"    \"name\": \"{assemblyName}\",\n" +
                "    \"references\": [],\n" +
                "    \"includePlatforms\": [],\n" +
                "    \"excludePlatforms\": []\n" +
                "}\n";

            File.WriteAllText(fullPath, json);
            AssetDatabase.ImportAsset(assetPath);
            return assetPath;
        }

        public static void CleanupTempAssets()
        {
            string fullPath = Path.GetFullPath(TempRoot);
            if (Directory.Exists(fullPath))
                AssetDatabase.DeleteAsset(TempRoot);

            AssetDatabase.Refresh();
        }
    }
}
