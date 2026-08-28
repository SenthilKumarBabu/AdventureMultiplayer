using DA_Assets.DAI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal partial class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private void DrawUniTextSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(scriptableObject.Localize(FcuLocKey.label_unitext_settings));
            header.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_unitext_settings);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.UniTextSettings;

            var autoSizeToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_auto_size));
            autoSizeToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_auto_size);
            autoSizeToggle.value = settings.AutoSize;
            autoSizeToggle.RegisterValueChangedCallback(evt => settings.AutoSize = evt.newValue);
            panel.Add(autoSizeToggle);
            panel.Add(uitk.ItemSeparator());

            var wordWrapToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_wrapping));
            wordWrapToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_unitext_word_wrap);
            wordWrapToggle.value = settings.WordWrap;
            wordWrapToggle.RegisterValueChangedCallback(evt => settings.WordWrap = evt.newValue);
            panel.Add(wordWrapToggle);
            panel.Add(uitk.ItemSeparator());

            var raycastTargetToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_raycast_target));
            raycastTargetToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_raycast_target);
            raycastTargetToggle.value = settings.RaycastTarget;
            raycastTargetToggle.RegisterValueChangedCallback(evt => settings.RaycastTarget = evt.newValue);
            panel.Add(raycastTargetToggle);
        }
    }
}