using DA_Assets.DAI;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using DA_Assets.Tools;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using DA_Assets.UCC.Extensions;
using DA_Assets.Singleton;
using DA_Assets.Extensions;
using DA_Assets.UCC;

namespace DA_Assets.UCC
{
    internal class MainSettingsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            var titleEl = uitk.CreateTitle(
                scriptableObject.Localize(FcuLocKey.label_main_settings),
                scriptableObject.Localize(FcuLocKey.tooltip_main_settings));
            titleEl.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.MainSettings;
                var s = monoBeh.Settings.MainSettings;
                s.UIFramework = d.UIFramework;
                s.ImportMode = d.ImportMode;
                s.PositioningMode = d.PositioningMode;
                s.PivotType = d.PivotType;
                s.GameObjectLayer = d.GameObjectLayer;
                s.GameObjectNameMaxLenght = d.GameObjectNameMaxLenght;
                s.TextObjectNameMaxLenght = d.TextObjectNameMaxLenght;
                s.UseDuplicateFinder = d.UseDuplicateFinder;
                s.UseLayoutUpdater = d.UseLayoutUpdater;
                s.DestroySyncHelpersAfterImport = d.DestroySyncHelpersAfterImport;
                s.DrawLayoutGrids = d.DrawLayoutGrids;
                s.RawImport = d.RawImport;
                s.Https = d.Https;
                scriptableObject.RefreshTabs();
            });
            parent.Add(titleEl);
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            if (scriptableObject.SerializedObject == null)
            {
                formContainer.Add(new HelpBox("SerializedObject is null. Cannot draw MainSettingsTab.", HelpBoxMessageType.Warning));
                return;
            }

            string pathToMainSettings = $"{nameof(monoBeh.Settings)}.{nameof(monoBeh.Settings.MainSettings)}";
            SerializedProperty mainSettingsProp = scriptableObject.SerializedObject.FindProperty(pathToMainSettings);

            if (mainSettingsProp == null)
            {
                formContainer.Add(new HelpBox(scriptableObject.Localize(FcuLocKey.mainsettings_error_property_not_found), HelpBoxMessageType.Error));
                return;
            }

            EnumField uiFrameworkField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_ui_framework), monoBeh.Settings.MainSettings.UIFramework);
            uiFrameworkField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_ui_framework);

            void ApplyUiFrameworkSelection(UIFramework requestedValue, bool updateField, bool logErrors, bool refreshTabs)
            {
                var validatedValue = requestedValue;

#if (FCU_EXISTS && FCU_UITK_EXT_EXISTS) == false
                if (requestedValue == UIFramework.UITK)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(UIFramework.UITK)));
                    }

                    validatedValue = UIFramework.UGUI;
                }
#endif

#if NOVA_UI_EXISTS == false
                if (requestedValue == UIFramework.NOVA)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(UIFramework.NOVA)));
                    }

                    validatedValue = UIFramework.UGUI;
                }
#endif

                if (updateField)
                {
                    if ((UIFramework)uiFrameworkField.value != validatedValue)
                    {
                        uiFrameworkField.SetValueWithoutNotify(validatedValue);
                    }
                }

                bool changed = monoBeh.Settings.MainSettings.UIFramework != validatedValue;
                monoBeh.Settings.MainSettings.UIFramework = validatedValue;

                if (refreshTabs && (changed || validatedValue != requestedValue))
                {
                    scriptableObject.RefreshTabs();
                }
            }

            uiFrameworkField.RegisterValueChangedCallback(evt =>
            {
                ApplyUiFrameworkSelection((UIFramework)evt.newValue, updateField: true, logErrors: true, refreshTabs: true);
            });
            uiFrameworkField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.UIFramework, (s, v) => { s.UIFramework = v; ApplyUiFrameworkSelection(v, updateField: true, logErrors: false, refreshTabs: true); });
            formContainer.Add(uiFrameworkField);

            ApplyUiFrameworkSelection(monoBeh.Settings.MainSettings.UIFramework, updateField: true, logErrors: false, refreshTabs: false);

            formContainer.Add(uitk.ItemSeparator());
            ImportMode? forcedImportMode = monoBeh.ForcedImportMode;
            if (forcedImportMode.HasValue)
                monoBeh.Settings.MainSettings.ImportMode = forcedImportMode.Value;

            EnumField importModeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_import_mode), monoBeh.Settings.MainSettings.ImportMode);
            importModeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_import_mode);
            importModeField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.ImportMode = (ImportMode)evt.newValue;
                if (forcedImportMode.HasValue)
                    importModeField.SetValueWithoutNotify(forcedImportMode.Value);
                scriptableObject.Inspector?.RebuildUI();
            });
            if (forcedImportMode.HasValue)
                importModeField.SetEnabled(false);
            else
                importModeField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.ImportMode, (s, v) => s.ImportMode = v);
            formContainer.Add(importModeField);


            if (monoBeh.IsUGUI() || monoBeh.IsNova() || monoBeh.IsDebug())
            {
                formContainer.Add(uitk.ItemSeparator());
                LayerField gameObjectLayerField = uitk.LayerField(scriptableObject.Localize(FcuLocKey.label_go_layer), monoBeh.Settings.MainSettings.GameObjectLayer);
                gameObjectLayerField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_go_layer);
                gameObjectLayerField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.Settings.MainSettings.GameObjectLayer = evt.newValue;
                });
                gameObjectLayerField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.GameObjectLayer, (s, v) => s.GameObjectLayer = v);
                formContainer.Add(gameObjectLayerField);
            }

            if (monoBeh.IsUGUI() || monoBeh.IsDebug())
            {
                formContainer.Add(uitk.ItemSeparator());
                EnumField positioningModeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_positioning_mode), monoBeh.Settings.MainSettings.PositioningMode);
                positioningModeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_positioning_mode);
                positioningModeField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.Settings.MainSettings.PositioningMode = (PositioningMode)evt.newValue;
                });
                positioningModeField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.PositioningMode, (s, v) => s.PositioningMode = v);
                formContainer.Add(positioningModeField);

                formContainer.Add(uitk.ItemSeparator());
                EnumField pivotTypeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_pivot_type), monoBeh.Settings.MainSettings.PivotType);
                pivotTypeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pivot_type);
                pivotTypeField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.Settings.MainSettings.PivotType = (PivotType)evt.newValue;
                });
                pivotTypeField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.PivotType, (s, v) => s.PivotType = v);
                formContainer.Add(pivotTypeField);
            }

            formContainer.Add(uitk.ItemSeparator());
            IntegerField gameObjectNameMaxLenghtField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_go_name_max_length));
            gameObjectNameMaxLenghtField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_go_name_max_length);
            gameObjectNameMaxLenghtField.value = monoBeh.Settings.MainSettings.GameObjectNameMaxLenght;
            gameObjectNameMaxLenghtField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.GameObjectNameMaxLenght = evt.newValue;
            });
            gameObjectNameMaxLenghtField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.GameObjectNameMaxLenght, (s, v) => s.GameObjectNameMaxLenght = v);
            formContainer.Add(gameObjectNameMaxLenghtField);

            formContainer.Add(uitk.ItemSeparator());
            IntegerField textObjectNameMaxLenghtField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_text_name_max_length));
            textObjectNameMaxLenghtField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_text_name_max_length);
            textObjectNameMaxLenghtField.value = monoBeh.Settings.MainSettings.TextObjectNameMaxLenght;
            textObjectNameMaxLenghtField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.TextObjectNameMaxLenght = evt.newValue;
            });
            textObjectNameMaxLenghtField.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.TextObjectNameMaxLenght, (s, v) => s.TextObjectNameMaxLenght = v);
            formContainer.Add(textObjectNameMaxLenghtField);

            formContainer.Add(uitk.ItemSeparator());
            Toggle useDuplicateFinderToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_use_duplicate_finder));
            useDuplicateFinderToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_use_duplicate_finder);
            useDuplicateFinderToggle.value = monoBeh.Settings.MainSettings.UseDuplicateFinder;
            useDuplicateFinderToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.UseDuplicateFinder = evt.newValue;
            });
            useDuplicateFinderToggle.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.UseDuplicateFinder, (s, v) => s.UseDuplicateFinder = v);
            formContainer.Add(useDuplicateFinderToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle useLayoutUpdaterToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_use_layout_updater));
            useLayoutUpdaterToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_use_layout_updater);
            useLayoutUpdaterToggle.value = monoBeh.Settings.MainSettings.UseLayoutUpdater;
            useLayoutUpdaterToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.UseLayoutUpdater = evt.newValue;
                useLayoutUpdaterToggle.SetValueWithoutNotify(monoBeh.Settings.MainSettings.UseLayoutUpdater);
            });
            useLayoutUpdaterToggle.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.UseLayoutUpdater, (s, v) =>
            {
                s.UseLayoutUpdater = v;
                useLayoutUpdaterToggle.SetValueWithoutNotify(s.UseLayoutUpdater);
            });
            formContainer.Add(useLayoutUpdaterToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle destroySyncHelpersAfterImportToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_destroy_sync_helpers_after_import));
            destroySyncHelpersAfterImportToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_destroy_sync_helpers_after_import);
            destroySyncHelpersAfterImportToggle.value = monoBeh.Settings.MainSettings.DestroySyncHelpersAfterImport;
            destroySyncHelpersAfterImportToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.DestroySyncHelpersAfterImport = evt.newValue;
            });
            destroySyncHelpersAfterImportToggle.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.DestroySyncHelpersAfterImport, (s, v) => s.DestroySyncHelpersAfterImport = v);
            formContainer.Add(destroySyncHelpersAfterImportToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle drawLayoutGridsToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_draw_layout_grids));
            drawLayoutGridsToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_draw_layout_grids);
            drawLayoutGridsToggle.value = monoBeh.Settings.MainSettings.DrawLayoutGrids;
            drawLayoutGridsToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.DrawLayoutGrids = evt.newValue;
            });
            drawLayoutGridsToggle.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.DrawLayoutGrids, (s, v) => s.DrawLayoutGrids = v);
            formContainer.Add(drawLayoutGridsToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle rawImportToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_raw_import));
            rawImportToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_raw_import);
            rawImportToggle.value = monoBeh.Settings.MainSettings.RawImport;
            rawImportToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.RawImport = evt.newValue;
            });
            rawImportToggle.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.RawImport, (s, v) => s.RawImport = v);
            formContainer.Add(rawImportToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle httpsToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_https_setting));
            httpsToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_https_setting);
            httpsToggle.value = monoBeh.Settings.MainSettings.Https;
            httpsToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.Https = evt.newValue;
            });
            httpsToggle.AddResetMenu(monoBeh.Settings.MainSettings, FcuDefaults.MainSettings, s => s.Https, (s, v) => s.Https = v);
            formContainer.Add(httpsToggle);

            scriptableObject.AddPlatformMainSettings(formContainer);

            formContainer.Add(uitk.ItemSeparator());


            var langValues = (DALanguage[])Enum.GetValues(typeof(DALanguage));
            var langChoices = langValues.Select(l => l.GetDisplayName()).ToList();
            int currentLangIndex = Array.IndexOf(langValues, monoBeh.Config.Localizator.Language);
            if (currentLangIndex < 0) currentLangIndex = 0;

            DALanguage defaultLanguage = DALanguage.en;
            string defaultLangDisplay = defaultLanguage.GetDisplayName();

            var languageField = uitk.DropdownField(scriptableObject.Localize(FcuLocKey.label_ui_language));
            languageField.choices = langChoices;
            languageField.index = currentLangIndex;
            languageField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_ui_language);
            languageField.RegisterValueChangedCallback(evt =>
            {
                int idx = langChoices.IndexOf(evt.newValue);
                if (idx >= 0)
                {
                    DALanguage selected = langValues[idx];
                    monoBeh.Config.Localizator.Language = selected;
                    FuitkConfigReflectionHelper.SetLanguage(selected);
                }
            });
            languageField.AddDropdownResetMenu(
                () => monoBeh.Config.Localizator.Language.GetDisplayName(),
                defaultLangDisplay,
                v =>
                {
                    int idx = langChoices.IndexOf(v);
                    if (idx >= 0)
                    {
                        DALanguage selected = langValues[idx];
                        monoBeh.Config.Localizator.Language = selected;
                        FuitkConfigReflectionHelper.SetLanguage(selected);
                        languageField.SetValueWithoutNotify(v);
                    }
                });
            formContainer.Add(languageField);
        }
    }
}