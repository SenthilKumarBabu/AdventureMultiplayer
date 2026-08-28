using System;

namespace DA_Assets.DM
{

    [Serializable]
    public struct AsmdefData
    {
        public string name;
        public string rootNamespace;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public VersionDefineData[] versionDefines;
        public bool noEngineReferences;
    }

    [Serializable]
    public struct VersionDefineData
    {
        public string name;
        public string expression;
        public string define;
    }
}
