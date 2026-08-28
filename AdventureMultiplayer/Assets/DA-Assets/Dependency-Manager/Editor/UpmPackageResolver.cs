using System;
using UnityEditor.PackageManager;

namespace DA_Assets.DM
{
    public sealed class UpmPackageResolver : IDependencyResolver
    {
        public DetectionStrategy Source => DetectionStrategy.UpmPackage;

        public ResolutionResult Resolve(DependencyItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.UpmPackageId))
                return ResolutionResult.NotFound;

            string targetId = item.UpmPackageId.Trim();

            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();
            if (packages == null) return ResolutionResult.NotFound;

            foreach (PackageInfo pkg in packages)
            {
                if (pkg == null) continue;
                if (string.Equals(pkg.name, targetId, StringComparison.Ordinal))
                {
                    string display = !string.IsNullOrEmpty(pkg.resolvedPath) ? pkg.resolvedPath : pkg.name;
                    return ResolutionResult.Match(display, DetectionStrategy.UpmPackage);
                }
            }

            return ResolutionResult.NotFound;
        }
    }
}
