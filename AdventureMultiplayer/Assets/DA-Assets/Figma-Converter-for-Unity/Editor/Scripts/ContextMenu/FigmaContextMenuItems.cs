using DA_Assets.Constants;
using DA_Assets.UCC;
using UnityEditor;

namespace DA_Assets.FCU
{
    internal static class FigmaContextMenuItems
    {
        [MenuItem("Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + FcuConfig.Create + " " + FcuConfig.ProductName, false, 0)]
        private static void CreateFcu_OnClick()
        {
            AssetTools.CreateConverterOnScene<FigmaConverterUnity>();
        }
    }
}
