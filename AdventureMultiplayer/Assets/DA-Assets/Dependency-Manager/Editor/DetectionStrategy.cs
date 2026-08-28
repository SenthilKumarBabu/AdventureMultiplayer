using System;

namespace DA_Assets.DM
{

    [Flags]
    public enum DetectionStrategy
    {
        None                = 0,
        MonoScript          = 1 << 0,
        AssemblyDefinition  = 1 << 1,
        PrecompiledAssembly = 1 << 2,
        UpmPackage          = 1 << 3,
    }
}
