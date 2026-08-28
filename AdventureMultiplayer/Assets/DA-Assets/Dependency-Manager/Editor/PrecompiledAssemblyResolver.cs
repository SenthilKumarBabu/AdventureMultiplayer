using System;

namespace DA_Assets.DM
{
    public sealed class PrecompiledAssemblyResolver : IDependencyResolver
    {
        public DetectionStrategy Source => DetectionStrategy.PrecompiledAssembly;

        public ResolutionResult Resolve(DependencyItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.AssemblyQualifiedTypeName))
                return ResolutionResult.NotFound;

            Type type = Type.GetType(item.AssemblyQualifiedTypeName, throwOnError: false, ignoreCase: true);
            if (type == null)
                return ResolutionResult.NotFound;

            string location = SafeAssemblyLocation(type);
            if (!ValidateAssemblyPath(item, location))
                return ResolutionResult.NotFound;

            return ResolutionResult.Match(location, DetectionStrategy.PrecompiledAssembly);
        }

        internal static bool ValidateAssemblyPath(DependencyItem item, string assemblyLocation)
        {
            string normalized = (assemblyLocation ?? string.Empty).Replace('\\', '/');

            if (item.ExpectedPathSuffixes != null && item.ExpectedPathSuffixes.Length > 0)
            {
                bool any = false;
                foreach (string suffix in item.ExpectedPathSuffixes)
                {
                    if (string.IsNullOrWhiteSpace(suffix)) continue;
                    if (normalized.EndsWith(suffix.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any) return false;
            }

            if (item.UnexpectedPathSuffixes != null && item.UnexpectedPathSuffixes.Length > 0)
            {
                foreach (string suffix in item.UnexpectedPathSuffixes)
                {
                    if (string.IsNullOrWhiteSpace(suffix)) continue;
                    if (normalized.EndsWith(suffix.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }

            return true;
        }

        private static string SafeAssemblyLocation(Type type)
        {
            try { return type.Assembly.Location ?? string.Empty; }
            catch { return string.Empty; }
        }
    }
}
