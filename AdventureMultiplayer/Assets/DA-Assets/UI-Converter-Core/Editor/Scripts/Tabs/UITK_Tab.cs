using DA_Assets.DAI;
using UnityEditor;
using UnityEngine.UIElements;

#if ULB_EXISTS
using DA_Assets.ULB;
#endif

namespace DA_Assets.UCC
{
    internal class UITK_Tab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        public VisualElement Draw()
        {
            var root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            root.Add(uitk.CreateTitle(
                scriptableObject.Localize(FcuLocKey.label_ui_toolkit_tab),
                scriptableObject.Localize(FcuLocKey.tooltip_ui_toolkit_tab)
            ));
            root.Add(uitk.Space10());

            DrawUITKSettings(root);

            return root;
        }

        private void DrawUITKSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var settings = monoBeh.Settings.UITK_Settings;

#if ULB_EXISTS
            var linkingModeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_uitk_linking_mode), settings.UitkLinkingMode);
            linkingModeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_uitk_linking_mode);
            linkingModeField.RegisterValueChangedCallback(evt => settings.UitkLinkingMode = (UitkLinkingMode)evt.newValue);
            linkingModeField.AddResetMenu(settings, FcuDefaults.UITK_Settings, s => s.UitkLinkingMode, (s, v) => s.UitkLinkingMode = v);
            panel.Add(linkingModeField);
            panel.Add(uitk.ItemSeparator());
#endif

            var folderPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_uitk_output_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_uitk_output_path),
                initialValue: settings.UitkOutputPath,
                onPathChanged: (newValue) => settings.UitkOutputPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_folder),
                    settings.UitkOutputPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_folder));
            folderPathContainer.AddFolderResetMenu(
                () => settings.UitkOutputPath,
                FcuDefaults.UITK_Settings.UitkOutputPath,
                v => settings.UitkOutputPath = v);
            panel.Add(folderPathContainer);
        }
    }
}