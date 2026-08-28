using DA_Assets.DAI;
using UnityEditor;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal class PrefabCreatorTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        public VisualElement Draw()
        {
            var root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            var titleEl = uitk.CreateTitle(
                scriptableObject.Localize(FcuLocKey.label_prefab_creator),
                scriptableObject.Localize(FcuLocKey.tooltip_prefab_creator));
            titleEl.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.PrefabSettings;
                var s = monoBeh.Settings.PrefabSettings;
                s.PrefabsPath = d.PrefabsPath;
                s.TextPrefabNameType = d.TextPrefabNameType;
                scriptableObject.RefreshTabs();
            });
            parent.Add(titleEl);
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            var settings = monoBeh.Settings.PrefabSettings;

            var folderPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_prefabs_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_prefabs_path),
                initialValue: settings.PrefabsPath,
                onPathChanged: (newValue) => settings.PrefabsPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_prefabs_folder),
                    settings.PrefabsPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_prefabs_folder)
            );
            folderPathContainer.AddFolderResetMenu(
                () => settings.PrefabsPath,
                FcuDefaults.PrefabSettings.PrefabsPath,
                v => settings.PrefabsPath = v);

            formContainer.Add(folderPathContainer);
            formContainer.Add(uitk.ItemSeparator());

            var nameTypeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_prefab_naming_mode), settings.TextPrefabNameType);

            nameTypeField.RegisterValueChangedCallback(evt =>
            {
                var newValue = (TextPrefabNameType)evt.newValue;
                settings.TextPrefabNameType = newValue;
            });
            nameTypeField.AddResetMenu(settings, FcuDefaults.PrefabSettings, s => s.TextPrefabNameType, (s, v) => s.TextPrefabNameType = v);

            formContainer.Add(nameTypeField);
            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var createButton = uitk.Button(scriptableObject.Localize(FcuLocKey.common_button_create), () =>
            {
                monoBeh.EditorEventHandlers.CreatePrefabs_OnClick();
            });
            formContainer.Add(createButton);
        }
    }
}