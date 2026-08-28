#if UNITY_EDITOR
using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.UCC
{
    [ResourcePath("")]
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/FcuConfig")]
    public sealed class FcuConfig : ConvConfigBase<FcuConfig>
    {
        public const string ProductName = "Figma Converter for Unity";
        public const string ProductNameShort = "FCU";
        public const string DestroyChilds = "Destroy childs";
        public const string SetFcuToSyncHelpers = "Set current FCU to SyncHelpers";
        public const string CompareTwoObjects = "Compare two selected objects";
        public const string DestroyLastImported = "Destroy last imported frames";
        public const string DestroySyncHelpers = "Destroy SyncHelpers";
        public const string CreatePrefabs = "Create Prefabs";
        public const string UpdatePrefabs = "Update Prefabs";
        public const string Create = "Create";
        public const string OptimizeSyncHelpers = "Optimize SyncHelpers";
        public const string GenerateScripts = "Generate scripts";
    }
}
#endif