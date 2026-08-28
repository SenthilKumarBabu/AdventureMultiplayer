using DA_Assets.DAI;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal partial class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private void DrawUitkTextSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(scriptableObject.Localize(FcuLocKey.label_uitk_text_settings));
            header.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_uitk_text_settings);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.UitkTextSettings;
                var s = monoBeh.Settings.UitkTextSettings;
                s.WhiteSpace = d.WhiteSpace;
                s.TextOverflow = d.TextOverflow;
                s.AutoSize = d.AutoSize;
                s.Focusable = d.Focusable;
                s.EnableRichText = d.EnableRichText;
                s.EmojiFallbackSupport = d.EmojiFallbackSupport;
                s.ParseEscapeSequences = d.ParseEscapeSequences;
                s.Selectable = d.Selectable;
                s.DoubleClickSelectsWord = d.DoubleClickSelectsWord;
                s.TripleClickSelectsLine = d.TripleClickSelectsLine;
                s.DisplayTooltipWhenElided = d.DisplayTooltipWhenElided;
                scriptableObject.RefreshTabs();
            });
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.UitkTextSettings;

            var whiteSpaceField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_white_space), settings.WhiteSpace);
            whiteSpaceField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_white_space);
            whiteSpaceField.RegisterValueChangedCallback(evt => settings.WhiteSpace = (WhiteSpace)evt.newValue);
            whiteSpaceField.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.WhiteSpace, (s, v) => s.WhiteSpace = v);
            panel.Add(whiteSpaceField);
            panel.Add(uitk.ItemSeparator());

            var textOverflowField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_text_overflow), settings.TextOverflow);
            textOverflowField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_text_overflow);
            textOverflowField.RegisterValueChangedCallback(evt => settings.TextOverflow = (TextOverflow)evt.newValue);
            textOverflowField.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.TextOverflow, (s, v) => s.TextOverflow = v);
            panel.Add(textOverflowField);
            panel.Add(uitk.ItemSeparator());

#if UNITY_2022_3_OR_NEWER
            var languageDirectionField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_language_direction), settings.LanguageDirection);
            languageDirectionField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_language_direction);
            languageDirectionField.RegisterValueChangedCallback(evt => settings.LanguageDirection = (LanguageDirection)evt.newValue);
            languageDirectionField.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.LanguageDirection, (s, v) => s.LanguageDirection = v);
            panel.Add(languageDirectionField);
            panel.Add(uitk.ItemSeparator());
#endif
            var autoSizeToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_auto_size));
            autoSizeToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_auto_size);
            autoSizeToggle.value = settings.AutoSize;
            autoSizeToggle.RegisterValueChangedCallback(evt => settings.AutoSize = evt.newValue);
            autoSizeToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.AutoSize, (s, v) => s.AutoSize = v);
            panel.Add(autoSizeToggle);
            panel.Add(uitk.ItemSeparator());

            var focusableToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_focusable));
            focusableToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_focusable);
            focusableToggle.value = settings.Focusable;
            focusableToggle.RegisterValueChangedCallback(evt => settings.Focusable = evt.newValue);
            focusableToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.Focusable, (s, v) => s.Focusable = v);
            panel.Add(focusableToggle);
            panel.Add(uitk.ItemSeparator());

            var richTextToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_rich_text));
            richTextToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_rich_text);
            richTextToggle.value = settings.EnableRichText;
            richTextToggle.RegisterValueChangedCallback(evt => settings.EnableRichText = evt.newValue);
            richTextToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.EnableRichText, (s, v) => s.EnableRichText = v);
            panel.Add(richTextToggle);
            panel.Add(uitk.ItemSeparator());

            var emojiFallbackToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_emoji_fallback_support));
            emojiFallbackToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_emoji_fallback_support);
            emojiFallbackToggle.value = settings.EmojiFallbackSupport;
            emojiFallbackToggle.RegisterValueChangedCallback(evt => settings.EmojiFallbackSupport = evt.newValue);
            emojiFallbackToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.EmojiFallbackSupport, (s, v) => s.EmojiFallbackSupport = v);
            panel.Add(emojiFallbackToggle);
            panel.Add(uitk.ItemSeparator());

            var parseEscapeToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_parse_escape_characters));
            parseEscapeToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_parse_escape_characters);
            parseEscapeToggle.value = settings.ParseEscapeSequences;
            parseEscapeToggle.RegisterValueChangedCallback(evt => settings.ParseEscapeSequences = evt.newValue);
            parseEscapeToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.ParseEscapeSequences, (s, v) => s.ParseEscapeSequences = v);
            panel.Add(parseEscapeToggle);
            panel.Add(uitk.ItemSeparator());

            var selectableToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_selectable));
            selectableToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_selectable);
            selectableToggle.value = settings.Selectable;
            selectableToggle.RegisterValueChangedCallback(evt => settings.Selectable = evt.newValue);
            selectableToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.Selectable, (s, v) => s.Selectable = v);
            panel.Add(selectableToggle);
            panel.Add(uitk.ItemSeparator());

            var doubleClickToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_double_click_selects_word));
            doubleClickToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_double_click_selects_word);
            doubleClickToggle.value = settings.DoubleClickSelectsWord;
            doubleClickToggle.RegisterValueChangedCallback(evt => settings.DoubleClickSelectsWord = evt.newValue);
            doubleClickToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.DoubleClickSelectsWord, (s, v) => s.DoubleClickSelectsWord = v);
            panel.Add(doubleClickToggle);
            panel.Add(uitk.ItemSeparator());

            var tripleClickToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_triple_click_selects_line));
            tripleClickToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_triple_click_selects_line);
            tripleClickToggle.value = settings.TripleClickSelectsLine;
            tripleClickToggle.RegisterValueChangedCallback(evt => settings.TripleClickSelectsLine = evt.newValue);
            tripleClickToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.TripleClickSelectsLine, (s, v) => s.TripleClickSelectsLine = v);
            panel.Add(tripleClickToggle);
            panel.Add(uitk.ItemSeparator());

            var tooltipWhenElidedToggle = uitk.Toggle(scriptableObject.Localize(FcuLocKey.label_display_tooltip_when_elided));
            tooltipWhenElidedToggle.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_display_tooltip_when_elided);
            tooltipWhenElidedToggle.value = settings.DisplayTooltipWhenElided;
            tooltipWhenElidedToggle.RegisterValueChangedCallback(evt => settings.DisplayTooltipWhenElided = evt.newValue);
            tooltipWhenElidedToggle.AddResetMenu(settings, FcuDefaults.UitkTextSettings, s => s.DisplayTooltipWhenElided, (s, v) => s.DisplayTooltipWhenElided = v);
            panel.Add(tooltipWhenElidedToggle);
        }
    }
}