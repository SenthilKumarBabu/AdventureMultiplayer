using DA_Assets.DAI;
using UnityEngine;
using UnityEngine.UIElements;
using DA_Assets.Logging;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    internal class ShadowsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
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
            Label title = new Label(scriptableObject.Localize(FcuLocKey.label_shadows_tab));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_shadows_tab);
            title.style.fontSize = DAI_UitkConstants.FontSizeTitle;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.AddSectionResetMenu(() =>
            {
                monoBeh.Settings.ShadowSettings.ShadowComponent = FcuDefaults.ShadowSettings.ShadowComponent;
                scriptableObject.RefreshTabs();
            });
            parent.Add(title);
            parent.Add(uitk.Space10());

            var formContainer = new VisualElement();
            formContainer.style.backgroundColor = uitk.ColorScheme.GROUP;
            UIHelpers.SetDefaultRadius(formContainer);
            UIHelpers.SetDefaultPadding(formContainer);
            parent.Add(formContainer);

            var shadowComponentField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_shadow_type), monoBeh.Settings.ShadowSettings.ShadowComponent);
            shadowComponentField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_shadow_type);

            void ApplyShadowComponentSelection(ShadowComponent requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;

#if LETAI_TRUESHADOW == false
                if (requestedValue == ShadowComponent.TrueShadow)
                {
                    if (logErrors)
                    {
                        Debug.LogError(scriptableObject.Localize(FcuLocKey.log_asset_not_imported, nameof(ShadowComponent.TrueShadow)));
                    }

                    validatedValue = ShadowComponent.Baked;
                }
#endif

                if (updateField)
                {
                    if ((ShadowComponent)shadowComponentField.value != validatedValue)
                    {
                        shadowComponentField.SetValueWithoutNotify(validatedValue);
                    }
                }

                monoBeh.Settings.ShadowSettings.ShadowComponent = validatedValue;
            }

            shadowComponentField.RegisterValueChangedCallback(evt =>
            {
                ApplyShadowComponentSelection((ShadowComponent)evt.newValue, updateField: true, logErrors: true);
            });
            shadowComponentField.AddResetMenu(monoBeh.Settings.ShadowSettings, FcuDefaults.ShadowSettings, s => s.ShadowComponent, (s, v) => s.ShadowComponent = v);
            formContainer.Add(shadowComponentField);

            ApplyShadowComponentSelection(monoBeh.Settings.ShadowSettings.ShadowComponent, updateField: true, logErrors: false);
        }
    }
}