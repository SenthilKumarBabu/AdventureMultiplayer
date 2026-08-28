using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.Figmage
{
    sealed class FigmageStrokeAlignPopup : EditorWindow
    {
        static readonly FigmageStrokeAlign[] Values =
        {
            FigmageStrokeAlign.Center,
            FigmageStrokeAlign.Inside,
            FigmageStrokeAlign.Outside
        };

        SerializedObject serializedObject;

        public static void Show(Rect screenRect, SerializedObject source)
        {
            FigmageStrokeAlignPopup window = CreateInstance<FigmageStrokeAlignPopup>();
            window.serializedObject = new SerializedObject(source.targetObjects);
            window.ShowAsDropDown(screenRect, new Vector2(236f, 128f));
            window.Focus();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            FigmageEffectTypePopup.StylePopupRoot(root);

            serializedObject.Update();
            SerializedProperty strokeAlign = serializedObject.FindProperty("strokeAlign");
            FigmageStrokeAlign current = (FigmageStrokeAlign)strokeAlign.enumValueIndex;

            for (int i = 0; i < Values.Length; i++)
            {
                FigmageStrokeAlign value = Values[i];
                Button row = new Button(() => Select(value));
                row.text = (value == current ? "\u2713  " : "   ") + value;
                FigmageEffectTypePopup.StylePopupRow(row);
                root.Add(row);
            }
        }

        void Select(FigmageStrokeAlign value)
        {
            serializedObject.Update();
            serializedObject.FindProperty("strokeAlign").enumValueIndex = (int)value;
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            Close();
        }
    }

}
