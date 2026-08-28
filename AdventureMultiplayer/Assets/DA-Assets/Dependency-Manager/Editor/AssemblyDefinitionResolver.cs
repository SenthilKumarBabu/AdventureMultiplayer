using System.IO;
using UnityEditor;

namespace DA_Assets.DM
{
    public sealed class AssemblyDefinitionResolver : IDependencyResolver
    {
        public DetectionStrategy Source => DetectionStrategy.AssemblyDefinition;

        public ResolutionResult Resolve(DependencyItem item)
        {
            if (item == null || item.Asmdef == null)
                return ResolutionResult.NotFound;

            string assetPath = AssetDatabase.GetAssetPath(item.Asmdef);
            if (string.IsNullOrEmpty(assetPath))
                return ResolutionResult.NotFound;

            if (!File.Exists(Path.GetFullPath(assetPath)))
                return ResolutionResult.NotFound;

            return ResolutionResult.Match(assetPath, DetectionStrategy.AssemblyDefinition);
        }
    }
}
