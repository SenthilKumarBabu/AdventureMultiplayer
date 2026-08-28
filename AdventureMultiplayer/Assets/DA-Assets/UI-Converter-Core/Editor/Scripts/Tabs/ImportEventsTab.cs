using DA_Assets.DAI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal class ImportEventsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        internal VisualElement Draw()
        {
            var root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            Label title = new Label(scriptableObject.Localize(FcuLocKey.label_import_events));
            title.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_import_events);
            title.style.fontSize = DAI_UitkConstants.FontSizeTitle;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(title);
            parent.Add(uitk.Space10());

            var formContainer = new VisualElement();
            formContainer.style.backgroundColor = uitk.ColorScheme.GROUP;
            UIHelpers.SetDefaultRadius(formContainer);
            UIHelpers.SetDefaultPadding(formContainer);
            parent.Add(formContainer);

            SerializedProperty eventsProp = scriptableObject.SerializedObject.FindProperty(nameof(ConverterBase.Events));

            if (eventsProp == null)
            {
                formContainer.Add(new HelpBox(scriptableObject.Localize(FcuLocKey.importevents_error_property_not_found), HelpBoxMessageType.Error));
                return;
            }

            var onInstantiateContainer = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(eventsProp.FindPropertyRelative(nameof(FcuEvents.OnObjectInstantiate)));
                });
            });
            formContainer.Add(onInstantiateContainer);
            formContainer.Add(uitk.Space10());

            var onAddComponentContainer = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(eventsProp.FindPropertyRelative(nameof(FcuEvents.OnAddComponent)));
                });
            });
            formContainer.Add(onAddComponentContainer);
            formContainer.Add(uitk.Space10());

            var onImportStartContainer = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(eventsProp.FindPropertyRelative(nameof(FcuEvents.OnImportStart)));
                });
            });
            formContainer.Add(onImportStartContainer);
            formContainer.Add(uitk.Space10());

            var onImportCompleteContainer = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(eventsProp.FindPropertyRelative(nameof(FcuEvents.OnImportComplete)));
                });
            });
            formContainer.Add(onImportCompleteContainer);
            formContainer.Add(uitk.Space10());

            var onImportFailContainer = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(eventsProp.FindPropertyRelative(nameof(FcuEvents.OnImportFail)));
                });
            });
            formContainer.Add(onImportFailContainer);
        }
    }
}