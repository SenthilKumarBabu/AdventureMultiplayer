using System.IO;
using UnityEditor;

namespace DA_Assets.DM
{
    public sealed class MonoScriptResolver : IDependencyResolver
    {
        public DetectionStrategy Source => DetectionStrategy.MonoScript;

        public ResolutionResult Resolve(DependencyItem item)
        {
            if (item == null || item.Script == null)
                return ResolutionResult.NotFound;

            string assetPath = AssetDatabase.GetAssetPath(item.Script);
            if (string.IsNullOrEmpty(assetPath))
                return ResolutionResult.NotFound;

            if (!File.Exists(Path.GetFullPath(assetPath)))
                return ResolutionResult.NotFound;

            return ResolutionResult.Match(assetPath, DetectionStrategy.MonoScript);
        }
    }
}
