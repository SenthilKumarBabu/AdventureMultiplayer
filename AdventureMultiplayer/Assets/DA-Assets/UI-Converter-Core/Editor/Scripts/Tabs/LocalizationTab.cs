using DA_Assets.DAI;
using DA_Assets.UCC.Extensions;
using DA_Assets.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using DA_Assets.Tools;


#if DALOC_EXISTS
using DA_Assets.DAL;
#endif

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    internal class LocalizationTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
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
                scriptableObject.Localize(FcuLocKey.label_localization_settings),
                scriptableObject.Localize(FcuLocKey.tooltip_localization_settings)
            );
            titleEl.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.LocalizationSettings;
                var s = monoBeh.Settings.LocalizationSettings;
                s.LocalizationComponent = d.LocalizationComponent;
                s.LocKeyCaseType = d.LocKeyCaseType;
                s.LocKeyMaxLenght = d.LocKeyMaxLenght;
                s.CsvSeparator = d.CsvSeparator;
                s.LocFolderPath = d.LocFolderPath;
                s.LocFileName = d.LocFileName;
                scriptableObject.RefreshTabs();
            });
            parent.Add(titleEl);
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            var settings = monoBeh.Settings.LocalizationSettings;
            var settingsContainer = new VisualElement();

#if DALOC_EXISTS
            VisualElement daLocContainer = null;
#endif

            var locComponentField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_loc_component), settings.LocalizationComponent);
            locComponentField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_loc_component);

            void ApplyLocalizationSelection(LocalizationComponent requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;

#if DALOC_EXISTS == false
                if (requestedValue == LocalizationComponent.DALocalizator)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(LocalizationComponent.DALocalizator)));
                    }

                    validatedValue = LocalizationComponent.None;
                }
#endif

#if I2LOC_EXISTS == false
                if (requestedValue == LocalizationComponent.I2Localization)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(LocalizationComponent.I2Localization)));
                    }

                    validatedValue = LocalizationComponent.None;
                }
#endif

                if (updateField && validatedValue != requestedValue)
                {
                    locComponentField.SetValueWithoutNotify(validatedValue);
                }

                settings.LocalizationComponent = validatedValue;
                settingsContainer.style.display = validatedValue == LocalizationComponent.None ? DisplayStyle.None : DisplayStyle.Flex;

#if DALOC_EXISTS
                if (daLocContainer != null)
                {
                    daLocContainer.style.display = validatedValue == LocalizationComponent.DALocalizator || monoBeh.IsDebug() ? DisplayStyle.Flex : DisplayStyle.None;
                }
#endif
            }

            locComponentField.RegisterValueChangedCallback(evt =>
            {
                ApplyLocalizationSelection((LocalizationComponent)evt.newValue, updateField: true, logErrors: true);
            });
            locComponentField.AddResetMenu(settings, FcuDefaults.LocalizationSettings, s => s.LocalizationComponent, (s, v) => { s.LocalizationComponent = v; ApplyLocalizationSelection(v, updateField: true, logErrors: false); });
            formContainer.Add(locComponentField);

            formContainer.Add(settingsContainer);

#if DALOC_EXISTS
            daLocContainer = new VisualElement();
            daLocContainer.style.display = settings.LocalizationComponent == LocalizationComponent.DALocalizator || monoBeh.IsDebug() ? DisplayStyle.Flex : DisplayStyle.None;
            settingsContainer.Add(daLocContainer);

            daLocContainer.Add(uitk.Space10());

            var helpBox = uitk.HelpBox(new HelpBoxData
            {
                Message = scriptableObject.Localize(FcuLocKey.localization_warning_serialize_first, nameof(settings.Localizator)),
                MessageType = MessageType.Warning
            });
            helpBox.style.display = settings.Localizator == null ? DisplayStyle.Flex : DisplayStyle.None;

            var localizatorField = new ObjectField(scriptableObject.Localize(FcuLocKey.label_localizator));
            localizatorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_localizator);
            localizatorField.objectType = typeof(LocalizatorBase);
            localizatorField.allowSceneObjects = false;
            localizatorField.value = settings.Localizator;
            localizatorField.RegisterValueChangedCallback(evt =>
            {
                settings.Localizator = (ScriptableObject)evt.newValue;
                helpBox.style.display = settings.Localizator == null ? DisplayStyle.Flex : DisplayStyle.None;
            });

            daLocContainer.Add(localizatorField);
            daLocContainer.Add(uitk.Space10());
            daLocContainer.Add(helpBox);
#endif
            settingsContainer.Add(uitk.Space10());

            var keyMaxLengthField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_loc_key_max_lenght));
            keyMaxLengthField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_loc_key_max_lenght);
            keyMaxLengthField.value = settings.LocKeyMaxLenght;
            keyMaxLengthField.RegisterValueChangedCallback(evt => settings.LocKeyMaxLenght = evt.newValue);
            keyMaxLengthField.AddResetMenu(settings, FcuDefaults.LocalizationSettings, s => s.LocKeyMaxLenght, (s, v) => s.LocKeyMaxLenght = v);
            settingsContainer.Add(keyMaxLengthField);
            settingsContainer.Add(uitk.Space10());

            var caseTypeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_loc_case_type), settings.LocKeyCaseType);
            caseTypeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_loc_case_type);
            caseTypeField.RegisterValueChangedCallback(evt => settings.LocKeyCaseType = (LocalizationKeyCaseType)evt.newValue);
            caseTypeField.AddResetMenu(settings, FcuDefaults.LocalizationSettings, s => s.LocKeyCaseType, (s, v) => s.LocKeyCaseType = v);
            settingsContainer.Add(caseTypeField);
            settingsContainer.Add(uitk.Space10());

#if DALOC_EXISTS
            IMGUIContainer culturePopupContainer = null;

            GenericMenu selectLangMenu = LanguageCollector.CreateLanguageMenu((cultureCode) =>
            {
                monoBeh.Settings.LocalizationSettings.CurrentFigmaLayoutCulture = cultureCode;
                culturePopupContainer?.MarkDirtyRepaint();
            });

            culturePopupContainer = new IMGUIContainer(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    scriptableObject.Localize(FcuLocKey.label_figma_layout_culture),
                    scriptableObject.Localize(FcuLocKey.tooltip_figma_layout_culture)));
                uitk.Colorize(() =>
                {
                    if (EditorGUILayout.DropdownButton(
                   new GUIContent(monoBeh.Settings.LocalizationSettings.CurrentFigmaLayoutCulture),
                   FocusType.Keyboard))
                    {
                        selectLangMenu.ShowAsContext();
                    }
                });
                EditorGUILayout.EndHorizontal();
            });

            settingsContainer.Add(culturePopupContainer);
            settingsContainer.Add(uitk.Space10());
#endif

            var separatorField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_csv_separator), settings.CsvSeparator);
            separatorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_csv_separator);
            separatorField.RegisterValueChangedCallback(evt => settings.CsvSeparator = (CsvSeparator)evt.newValue);
            separatorField.AddResetMenu(settings, FcuDefaults.LocalizationSettings, s => s.CsvSeparator, (s, v) => s.CsvSeparator = v);
            settingsContainer.Add(separatorField);
            settingsContainer.Add(uitk.Space10());

            var folderPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_loc_folder_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_loc_folder_path),
                initialValue: settings.LocFolderPath,
                onPathChanged: (newValue) => settings.LocFolderPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_folder),
                    settings.LocFolderPath,
                    ""),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_folder)
            );
            folderPathContainer.AddFolderResetMenu(
                () => settings.LocFolderPath,
                FcuDefaults.LocalizationSettings.LocFolderPath,
                v => settings.LocFolderPath = v);
            settingsContainer.Add(folderPathContainer);
            settingsContainer.Add(uitk.Space10());

            var fileNameField = new TextField(scriptableObject.Localize(FcuLocKey.label_loc_file_name));
            fileNameField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_loc_file_name);
            fileNameField.value = settings.LocFileName;
            fileNameField.RegisterValueChangedCallback(evt => settings.LocFileName = evt.newValue);
            fileNameField.AddResetMenu(settings, FcuDefaults.LocalizationSettings, s => s.LocFileName, (s, v) => s.LocFileName = v);
            settingsContainer.Add(fileNameField);

            ApplyLocalizationSelection(settings.LocalizationComponent, updateField: true, logErrors: false);
        }
    }
}