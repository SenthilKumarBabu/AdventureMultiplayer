using DA_Assets.DAI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal partial class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        public void DrawDefaultTextSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(scriptableObject.Localize(FcuLocKey.label_unity_text_settings));
            header.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_unity_text_settings);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.UnityTextSettings;
                var s = monoBeh.Settings.UnityTextSettings;
                s.BestFit = d.BestFit;
                s.FontLineSpacing = d.FontLineSpacing;
                s.HorizontalWrapMode = d.HorizontalWrapMode;
                s.VerticalWrapMode = d.VerticalWrapMode;
                scriptableObject.RefreshTabs();
            });
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.UnityTextSettings;

            var bestFitToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_best_fit));
            bestFitToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_best_fit);
            bestFitToggle.value = settings.BestFit;
            bestFitToggle.RegisterValueChangedCallback(evt =>
            {
                settings.BestFit = evt.newValue;
                if (settings.VerticalWrapMode == VerticalWrapMode.Overflow)
                {
                    settings.BestFit = false;
                    bestFitToggle.SetValueWithoutNotify(false);
                }
            });
            bestFitToggle.AddResetMenu(settings, FcuDefaults.UnityTextSettings, s => s.BestFit, (s, v) => s.BestFit = v);
            panel.Add(bestFitToggle);
            panel.Add(uitk.ItemSeparator());

            var lineSpacingField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_line_spacing));
            lineSpacingField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_line_spacing);
            lineSpacingField.value = settings.FontLineSpacing;
            lineSpacingField.RegisterValueChangedCallback(evt => settings.FontLineSpacing = evt.newValue);
            lineSpacingField.AddResetMenu(settings, FcuDefaults.UnityTextSettings, s => s.FontLineSpacing, (s, v) => s.FontLineSpacing = v);
            panel.Add(lineSpacingField);
            panel.Add(uitk.ItemSeparator());

            var hOverflowField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_horizontal_overflow), settings.HorizontalWrapMode);
            hOverflowField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_horizontal_overflow);
            hOverflowField.RegisterValueChangedCallback(evt => settings.HorizontalWrapMode = (HorizontalWrapMode)evt.newValue);
            hOverflowField.AddResetMenu(settings, FcuDefaults.UnityTextSettings, s => s.HorizontalWrapMode, (s, v) => s.HorizontalWrapMode = v);
            panel.Add(hOverflowField);
            panel.Add(uitk.ItemSeparator());

            var vOverflowField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_vertical_overflow), settings.VerticalWrapMode);
            vOverflowField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_vertical_overflow);
            vOverflowField.RegisterValueChangedCallback(evt =>
            {
                settings.VerticalWrapMode = (VerticalWrapMode)evt.newValue;
                if (settings.VerticalWrapMode == VerticalWrapMode.Overflow)
                {
                    settings.BestFit = false;
                    bestFitToggle.SetValueWithoutNotify(false);
                }
            });
            vOverflowField.AddResetMenu(settings, FcuDefaults.UnityTextSettings, s => s.VerticalWrapMode, (s, v) => s.VerticalWrapMode = v);
            panel.Add(vOverflowField);
        }
    }
}