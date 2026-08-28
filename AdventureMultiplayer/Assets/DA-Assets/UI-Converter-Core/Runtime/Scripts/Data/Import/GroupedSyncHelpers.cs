#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System.Collections.Generic;

namespace DA_Assets.UCC
{
    public struct GroupedSyncHelpers
    {
        public SyncData RootFrame { get; set; }
        public List<SyncHelper> SyncHelpers { get; set; }
    }
}
#endif