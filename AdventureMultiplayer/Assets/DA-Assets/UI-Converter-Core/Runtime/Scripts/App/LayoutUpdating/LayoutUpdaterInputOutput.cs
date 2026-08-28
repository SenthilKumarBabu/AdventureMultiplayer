#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;

namespace DA_Assets.UCC
{
    public struct LayoutUpdaterInput
    {
        public SelectableObject<DiffInfo> ToImport { get; set; }
        public SelectableObject<SyncData> ToRemove { get; set; }
    }

    public struct LayoutUpdaterOutput
    {
        public IEnumerable<string> ToImport { get; set; }
        public IEnumerable<SyncData> ToRemove { get; set; }
    }
}
#endif