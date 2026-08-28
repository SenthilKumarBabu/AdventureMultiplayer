using DA_Assets.DAI;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace DA_Assets.UCC
{
    internal class ButtonsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            root.Add(uitk.CreateTitle(
                 scriptableObject.Localize(FcuLocKey.label_buttons_tab),
                 scriptableObject.Localize(FcuLocKey.tooltip_buttons_tab)));
            root.Add(uitk.Space10());

            DrawGeneralSettings(root);
            root.Add(uitk.Space10());

            DrawUnityButtonSettings(root);

#if DABUTTON_EXISTS
            if (monoBeh.UsingDAButton())
            {
                root.Add(uitk.Space10());
                DrawDABSection(root);
            }
#endif

            return root;
        }

        private void DrawGeneralSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var buttonComponentField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_button_type), monoBeh.Settings.ButtonSettings.ButtonComponent);
            buttonComponentField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_button_type);
            buttonComponentField.RegisterValueChangedCallback(evt => monoBeh.Settings.ButtonSettings.ButtonComponent = (ButtonComponent)evt.newValue);
            buttonComponentField.AddResetMenu(monoBeh.Settings.ButtonSettings, FcuDefaults.ButtonSettings, s => s.ButtonComponent, (s, v) => s.ButtonComponent = v);
            panel.Add(buttonComponentField);
            panel.Add(uitk.ItemSeparator());

            var transitionField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_transition_type), monoBeh.Settings.ButtonSettings.TransitionType);
            transitionField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_transition_type);
            transitionField.RegisterValueChangedCallback(evt => monoBeh.Settings.ButtonSettings.TransitionType = (ButtonTransitionType)evt.newValue);
            transitionField.AddResetMenu(monoBeh.Settings.ButtonSettings, FcuDefaults.ButtonSettings, s => s.TransitionType, (s, v) => s.TransitionType = v);
            panel.Add(transitionField);
        }

        private void DrawUnityButtonSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            var settings = monoBeh.Settings.ButtonSettings.UnityButtonSettings;

            Label header = new Label(scriptableObject.Localize(FcuLocKey.label_button_settings));
            header.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_button_settings);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.UnityButtonSettings;
                settings.NormalColor = d.NormalColor;
                settings.HighlightedColor = d.HighlightedColor;
                settings.PressedColor = d.PressedColor;
                settings.SelectedColor = d.SelectedColor;
                settings.DisabledColor = d.DisabledColor;
                settings.ColorMultiplier = d.ColorMultiplier;
                settings.FadeDuration = d.FadeDuration;
                scriptableObject.RefreshTabs();
            });
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var normalColorField = uitk.ColorField(scriptableObject.Localize(FcuLocKey.label_normal_color));
            normalColorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_normal_color);
            normalColorField.value = settings.NormalColor;
            normalColorField.RegisterValueChangedCallback(evt => settings.NormalColor = evt.newValue);
            normalColorField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.NormalColor, (s, v) => s.NormalColor = v);
            panel.Add(normalColorField);
            panel.Add(uitk.ItemSeparator());

            var highlightedColorField = uitk.ColorField(scriptableObject.Localize(FcuLocKey.label_highlighted_color));
            highlightedColorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_highlighted_color);
            highlightedColorField.value = settings.HighlightedColor;
            highlightedColorField.RegisterValueChangedCallback(evt => settings.HighlightedColor = evt.newValue);
            highlightedColorField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.HighlightedColor, (s, v) => s.HighlightedColor = v);
            panel.Add(highlightedColorField);
            panel.Add(uitk.ItemSeparator());

            var pressedColorField = uitk.ColorField(scriptableObject.Localize(FcuLocKey.label_pressed_color));
            pressedColorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_pressed_color);
            pressedColorField.value = settings.PressedColor;
            pressedColorField.RegisterValueChangedCallback(evt => settings.PressedColor = evt.newValue);
            pressedColorField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.PressedColor, (s, v) => s.PressedColor = v);
            panel.Add(pressedColorField);
            panel.Add(uitk.ItemSeparator());

            var selectedColorField = uitk.ColorField(scriptableObject.Localize(FcuLocKey.label_selected_color));
            selectedColorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_selected_color);
            selectedColorField.value = settings.SelectedColor;
            selectedColorField.RegisterValueChangedCallback(evt => settings.SelectedColor = evt.newValue);
            selectedColorField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.SelectedColor, (s, v) => s.SelectedColor = v);
            panel.Add(selectedColorField);
            panel.Add(uitk.ItemSeparator());

            var disabledColorField = uitk.ColorField(scriptableObject.Localize(FcuLocKey.label_disabled_color));
            disabledColorField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_disabled_color);
            disabledColorField.value = settings.DisabledColor;
            disabledColorField.RegisterValueChangedCallback(evt => settings.DisabledColor = evt.newValue);
            disabledColorField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.DisabledColor, (s, v) => s.DisabledColor = v);
            panel.Add(disabledColorField);
            panel.Add(uitk.ItemSeparator());

            var colorMultiplierField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_color_multiplier));
            colorMultiplierField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_color_multiplier);
            colorMultiplierField.value = settings.ColorMultiplier;
            colorMultiplierField.RegisterValueChangedCallback(evt => settings.ColorMultiplier = evt.newValue);
            colorMultiplierField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.ColorMultiplier, (s, v) => s.ColorMultiplier = v);
            panel.Add(colorMultiplierField);
            panel.Add(uitk.ItemSeparator());

            var fadeDurationField = uitk.FloatField(scriptableObject.Localize(FcuLocKey.label_fade_duration));
            fadeDurationField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_fade_duration);
            fadeDurationField.value = settings.FadeDuration;
            fadeDurationField.RegisterValueChangedCallback(evt => settings.FadeDuration = evt.newValue);
            fadeDurationField.AddResetMenu(settings, FcuDefaults.UnityButtonSettings, s => s.FadeDuration, (s, v) => s.FadeDuration = v);
            panel.Add(fadeDurationField);
        }

#if DABUTTON_EXISTS
        private void DrawDABSection(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label title = new Label(scriptableObject.Localize(FcuLocKey.label_dabutton_settings));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_dabutton_settings);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            string pathToDabSettings = $"{nameof(monoBeh.Settings)}.{nameof(monoBeh.Settings.ButtonSettings)}.{nameof(monoBeh.Settings.ButtonSettings.DAB_Settings)}";
            SerializedProperty dabSettingsProp = scriptableObject.SerializedObject.FindProperty(pathToDabSettings);

            if (dabSettingsProp == null)
            {
                panel.Add(new HelpBox("Could not find DAB_Settings property. Check the path.", HelpBoxMessageType.Error));
                return;
            }

            var scalePropsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.ScaleProperties)));
            });
            panel.Add(scalePropsField);
            panel.Add(uitk.ItemSeparator());

            var scaleAnimsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.ScaleAnimations)));
            });
            panel.Add(scaleAnimsField);
            panel.Add(uitk.ItemSeparator());

            var colorAnimsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.ColorAnimations)));
            });
            panel.Add(colorAnimsField);
            panel.Add(uitk.ItemSeparator());

            var spriteAnimsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.SpriteAnimations)));
            });
            panel.Add(spriteAnimsField);
        }
#endif
    }
}