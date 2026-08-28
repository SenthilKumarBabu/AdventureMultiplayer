using DA_Assets.DAI;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using DA_Assets.Logging;

namespace DA_Assets.UCC
{
    [Serializable]
    internal partial class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private VisualElement _dynamicSettingsContainer;

        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            var titleEl = uitk.CreateTitle(
                scriptableObject.Localize(FcuLocKey.label_text_and_fonts),
                scriptableObject.Localize(FcuLocKey.tooltip_text_and_fonts));
            titleEl.AddSectionResetMenu(() =>
            {
                monoBeh.Settings.TextFontsSettings.TextComponent = FcuDefaults.TextFontsSettings.TextComponent;
                monoBeh.Settings.TextFontsSettings.LineHeightMode = FcuDefaults.TextFontsSettings.LineHeightMode;
                scriptableObject.RefreshTabs();
            });
            root.Add(titleEl);
            root.Add(uitk.Space10());

            DrawGeneralSettings(root);
            root.Add(uitk.Space10());

            DrawGoogleFontsSettings(root);
            root.Add(uitk.Space10());

            _dynamicSettingsContainer = new VisualElement();
            root.Add(_dynamicSettingsContainer);
            UpdateDynamicSettings();
            root.Add(uitk.Space10());

            DrawPathSettings(root);

#if TextMeshPro
            root.Add(uitk.Space10());
            DrawFontGenerationSettings(root);
#endif

            return root;
        }

        private void DrawGeneralSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var textComponentField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_text_component), monoBeh.Settings.TextFontsSettings.TextComponent);
            textComponentField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_text_component);
            textComponentField.RegisterValueChangedCallback(evt =>
            {
                var requestedValue = (TextComponent)evt.newValue;
                var validatedValue = requestedValue;

                switch (requestedValue)
                {
                    case TextComponent.UnityEngine_UI_Text:
                        break;

                    case TextComponent.TextMeshPro:
#if TextMeshPro == false
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(TextComponent.TextMeshPro)));
                        validatedValue = TextComponent.UnityEngine_UI_Text;
#endif
                        break;

                    case TextComponent.RTL_TextMeshPro:
#if RTLTMP_EXISTS == false
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(TextComponent.RTL_TextMeshPro)));
                        validatedValue = TextComponent.UnityEngine_UI_Text;
#endif
                        break;

                    case TextComponent.UI_Toolkit_Text:
                        break;

                    case TextComponent.UniText:
#if UNITEXT == false
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(TextComponent.UniText)));
                        validatedValue = TextComponent.UnityEngine_UI_Text;
#endif
                        break;
                }

                if (monoBeh.Settings.MainSettings.UIFramework == UIFramework.UITK &&
                    validatedValue != TextComponent.UI_Toolkit_Text)
                {
                    Debug.LogError(scriptableObject.Localize(FcuLocKey.label_cannot_select_setting, validatedValue, monoBeh.Settings.MainSettings.UIFramework));
                    validatedValue = TextComponent.UI_Toolkit_Text;
                }

                if (validatedValue != requestedValue)
                {
                    textComponentField.SetValueWithoutNotify(validatedValue);
                }

                monoBeh.Settings.TextFontsSettings.TextComponent = validatedValue;
                UpdateDynamicSettings();
            });
            textComponentField.AddResetMenu(monoBeh.Settings.TextFontsSettings, FcuDefaults.TextFontsSettings, s => s.TextComponent, (s, v) => { s.TextComponent = v; UpdateDynamicSettings(); });
            panel.Add(textComponentField);
            panel.Add(uitk.ItemSeparator());

            var lineHeightModeField = uitk.EnumField("Line Height Mode", monoBeh.Settings.TextFontsSettings.LineHeightMode);
            lineHeightModeField.tooltip = "Controls how FCU rewrites TMP and UITK Face Info line-height fields. CapHeightToBaseline changes capLine, ascentLine, descentLine, and lineHeight. baseline and all other Face Info fields stay unchanged.";
            lineHeightModeField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.TextFontsSettings.LineHeightMode = (LineHeightMode)evt.newValue;
            });
            lineHeightModeField.AddResetMenu(
                monoBeh.Settings.TextFontsSettings,
                FcuDefaults.TextFontsSettings,
                s => s.LineHeightMode,
                (s, v) => s.LineHeightMode = v);
            panel.Add(lineHeightModeField);
        }

        private void UpdateDynamicSettings()
        {
            _dynamicSettingsContainer.Clear();

            switch (monoBeh.Settings.TextFontsSettings.TextComponent)
            {
                case TextComponent.UnityEngine_UI_Text:
                    DrawDefaultTextSettings(_dynamicSettingsContainer);
                    break;
                case TextComponent.TextMeshPro:
                case TextComponent.RTL_TextMeshPro:
                    DrawTextMeshSettingsSection(_dynamicSettingsContainer);
                    break;
                case TextComponent.UI_Toolkit_Text:
                    DrawUitkTextSettings(_dynamicSettingsContainer);
                    break;
                case TextComponent.UniText:
                    DrawUniTextSettings(_dynamicSettingsContainer);
                    break;
            }
        }
    }
}