using DA_Assets.Constants;
using DA_Assets.Logging;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.UCC
{
    internal class ContextMenuItems
    {
        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + FcuConfig.SetFcuToSyncHelpers, false, 10)]
        private static void SetFcuToSyncHelpers_OnClick()
        {
            if (TryGetSelectedConverter(out ConverterBase fcu))
            {
                fcu.EditorEventHandlers.SetFcuToSyncHelpers_OnClick();
            }
            else
            {
                Debug.LogError(FcuLocKey.log_component_not_selected_in_hierarchy.Localize(nameof(ConverterBase)));
            }
        }

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + FcuConfig.DestroySyncHelpers, false, 14)]
        private static void DestroySyncHelpers_OnClick()
        {
            if (TryGetSelectedConverter(out ConverterBase fcu))
            {
                fcu.EditorEventHandlers.DestroySyncHelpers_OnClick();
            }
            else
            {
                Debug.LogError(FcuLocKey.log_component_not_selected_in_hierarchy.Localize(nameof(ConverterBase)));
            }
        }

        private static bool TryGetSelectedConverter(out ConverterBase converter)
        {
            GameObject selectedGameObject = Selection.activeGameObject;
            converter = selectedGameObject == null
                ? null
                : selectedGameObject.GetComponentInParent<ConverterBase>(true);
            return converter != null;
        }
    }
}
