using DA_Assets.DAI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal partial class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private void DrawGoogleFontsSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(scriptableObject.Localize(FcuLocKey.label_google_fonts_settings));
            header.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_google_fonts_settings);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            {
                var apiKeyContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                var apiKeyField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_google_fonts_api_key));
                apiKeyField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_google_fonts_api_key, scriptableObject.Localize(FcuLocKey.label_google_fonts_api_key));
                apiKeyField.value = monoBeh.Config.GoogleFontsApiKey;
                apiKeyField.style.flexGrow = 1;

                apiKeyField.RegisterValueChangedCallback(evt => monoBeh.Config.GoogleFontsApiKey = evt.newValue);
                apiKeyContainer.Add(apiKeyField);

                var getApiKeyButton = uitk.Button(scriptableObject.Localize(FcuLocKey.label_get_google_api_key), () =>
                {
                    Application.OpenURL("https://developers.google.com/fonts/docs/developer_api#identifying_your_application_to_google");
                });
                getApiKeyButton.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_get_google_api_key);

                getApiKeyButton.style.maxWidth = 100;
                getApiKeyButton.style.maxHeight = 18;
                getApiKeyButton.style.marginTop = 1;

                UIHelpers.SetRadius(getApiKeyButton, 3);
                apiKeyContainer.Add(getApiKeyButton);
                panel.Add(apiKeyContainer);
            }

        }
    }
}