using System.Collections.Generic;

namespace DA_Assets.DM
{

    public sealed class DependencyResolutionService
    {

        private static readonly DetectionStrategy[] Order =
        {
            DetectionStrategy.AssemblyDefinition,
            DetectionStrategy.PrecompiledAssembly,
            DetectionStrategy.UpmPackage,
            DetectionStrategy.MonoScript,
        };

        private readonly Dictionary<DetectionStrategy, IDependencyResolver> _resolvers;

        public DependencyResolutionService()
            : this(new IDependencyResolver[]
            {
                new MonoScriptResolver(),
                new AssemblyDefinitionResolver(),
                new PrecompiledAssemblyResolver(),
                new UpmPackageResolver(),
            })
        {
        }

        public DependencyResolutionService(IEnumerable<IDependencyResolver> resolvers)
        {
            _resolvers = new Dictionary<DetectionStrategy, IDependencyResolver>();
            foreach (IDependencyResolver r in resolvers)
            {
                if (r == null) continue;
                _resolvers[r.Source] = r;
            }
        }

        public ResolutionResult Resolve(DependencyItem item)
        {
            if (item == null) return ResolutionResult.NotFound;

            foreach (DetectionStrategy flag in Order)
            {
                if ((item.Strategy & flag) == 0) continue;
                if (!_resolvers.TryGetValue(flag, out IDependencyResolver resolver)) continue;

                ResolutionResult r = resolver.Resolve(item);
                if (r.Found) return r;
            }

            return ResolutionResult.NotFound;
        }
    }
}
