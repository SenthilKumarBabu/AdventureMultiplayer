using DA_Assets.DAI;
using DA_Assets.UCC;
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    [CustomEditor(typeof(FigmaConverterUnity)), CanEditMultipleObjects]
    public class FcuEditor : ConverterEditorBase
    {
        private AuthTab _authTab;

        protected override void PopulatePlatformTabs(FcuSettingsWindow window, List<FcuSettingsWindow.ITab> tabs, int insertIndex)
        {
            var authTab = monoBeh.Link(ref _authTab, window);
            tabs.Insert(insertIndex, new FcuSettingsWindow.UITKTab(
                FcuLocKey.label_figma_auth.Localize(),
                FcuLocKey.tooltip_figma_auth.Localize(),
                authTab.Draw()));
        }

        protected override void PopulatePlatformMainSettings(FcuSettingsWindow window, VisualElement container)
        {
            container.Add(uitk.ItemSeparator());
            EnumField figmaEnvField = uitk.EnumField(
                FcuLocKey.label_figma_environment.Localize(),
                monoBeh.Settings.MainSettings.FigmaEnvironment);
            figmaEnvField.tooltip = FcuLocKey.tooltip_figma_environment.Localize();
            figmaEnvField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.FigmaEnvironment = (FigmaEnvironment)evt.newValue;
            });
            figmaEnvField.AddResetMenu(
                monoBeh.Settings.MainSettings,
                FcuDefaults.MainSettings,
                s => s.FigmaEnvironment,
                (s, v) => s.FigmaEnvironment = v);
            container.Add(figmaEnvField);
        }
    }
}
