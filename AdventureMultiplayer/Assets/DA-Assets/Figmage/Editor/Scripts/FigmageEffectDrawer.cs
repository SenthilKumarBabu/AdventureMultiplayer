using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.Figmage
{
    [CustomPropertyDrawer(typeof(FigmageEffect))]
    public sealed class FigmageEffectDrawer : PropertyDrawer
    {
        const float RowHeight = 34f;
        const float IconSize = 20f;
        const float Gap = 8f;
        const float TypeFieldMinWidth = 120f;

        static readonly Color RowBorder = new Color(1f, 1f, 1f, 0.12f);
        static readonly Color FieldBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        static readonly Color FieldBorder = new Color(1f, 1f, 1f, 0.12f);
        static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        static readonly Color MutedColor = new Color(0.72f, 0.72f, 0.72f, 1f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return RowHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProp = property.FindPropertyRelative("Type");
            SerializedProperty visibleProp = property.FindPropertyRelative("Visible");

            FigmageEffectType type = (FigmageEffectType)typeProp.enumValueIndex;
            Rect row = new Rect(position.x, position.y + 4f, position.width, 26f);
            Rect iconRect = new Rect(row.x + 2f, row.y + 3f, IconSize, IconSize);
            Rect visibilityRect = new Rect(row.xMax - 24f, row.y + 3f, IconSize, IconSize);
            Rect dropdownRect = new Rect(iconRect.xMax + Gap, row.y, Mathf.Max(TypeFieldMinWidth, visibilityRect.x - iconRect.xMax - Gap * 2f), 26f);

            DrawEffectIcon(iconRect, type);
            if (GUI.Button(iconRect, GUIContent.none, GUIStyle.none))
                FigmageEffectSettingsPopup.Show(GUIUtility.GUIToScreenRect(iconRect), property.serializedObject, property.propertyPath);

            DrawDropdown(dropdownRect, GetEffectLabel(type));
            if (GUI.Button(dropdownRect, GUIContent.none, GUIStyle.none))
                FigmageEffectTypePopup.Show(GUIUtility.GUIToScreenRect(dropdownRect), property.serializedObject, property.propertyPath);

            DrawVisibilityButton(visibilityRect, visibleProp, property.serializedObject);
        }

        static void DrawDropdown(Rect rect, string label)
        {
            EditorGUI.DrawRect(rect, FieldBg);
            DrawBorder(rect, FieldBorder);

            Rect labelRect = new Rect(rect.x + 10f, rect.y, rect.width - 28f, rect.height);
            GUI.Label(labelRect, label, BuildLabelStyle(TextColor, FontStyle.Bold, TextAnchor.MiddleLeft));

            Rect arrowRect = new Rect(rect.xMax - 20f, rect.y + 9f, 8f, 8f);
            Handles.BeginGUI();
            Handles.color = MutedColor;
            Vector3 a = new Vector3(arrowRect.x, arrowRect.y + 1f, 0f);
            Vector3 b = new Vector3(arrowRect.center.x, arrowRect.yMax - 1f, 0f);
            Vector3 c = new Vector3(arrowRect.xMax, arrowRect.y + 1f, 0f);
            Handles.DrawAAPolyLine(2f, a, b, c);
            Handles.EndGUI();
        }

        static void DrawVisibilityButton(Rect rect, SerializedProperty visibleProp, SerializedObject serializedObject)
        {
            DrawEyeIcon(rect, visibleProp.boolValue ? TextColor : MutedColor, visibleProp.boolValue);
            if (!GUI.Button(rect, GUIContent.none, GUIStyle.none))
                return;

            visibleProp.boolValue = !visibleProp.boolValue;
            ApplyAndRefresh(serializedObject);
        }

        static void DrawEffectIcon(Rect rect, FigmageEffectType type)
        {
            Handles.BeginGUI();
            switch (type)
            {
                case FigmageEffectType.LayerBlur:
                    DrawDottedIcon(rect);
                    break;
                case FigmageEffectType.InnerShadow:
                    DrawInsetSquareIcon(rect);
                    break;
                case FigmageEffectType.BackgroundBlur:
                    DrawDottedIcon(rect, 0.55f);
                    break;
                default:
                    DrawShadowSquareIcon(rect);
                    break;
            }
            Handles.EndGUI();
        }

        static void DrawDottedIcon(Rect rect, float alpha = 1f)
        {
            Handles.color = new Color(TextColor.r, TextColor.g, TextColor.b, alpha);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    float dotAlpha = ((x + y) % 2 == 0) ? alpha : alpha * 0.55f;
                    Handles.color = new Color(TextColor.r, TextColor.g, TextColor.b, dotAlpha);
                    Handles.DrawSolidDisc(new Vector3(rect.x + 4f + x * 4f, rect.y + 4f + y * 4f, 0f), Vector3.forward, 1.15f);
                }
            }
        }

        static void DrawShadowSquareIcon(Rect rect)
        {
            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            Handles.DrawSolidRectangleWithOutline(new Rect(rect.x + 4f, rect.y + 5f, 14f, 14f), Color.clear, Handles.color);
            Handles.color = TextColor;
            Handles.DrawSolidRectangleWithOutline(new Rect(rect.x + 2f, rect.y + 3f, 14f, 14f), Color.clear, Handles.color);
        }

        static void DrawInsetSquareIcon(Rect rect)
        {
            Rect square = new Rect(rect.x + 3f, rect.y + 3f, 14f, 14f);
            Handles.color = TextColor;
            Handles.DrawSolidRectangleWithOutline(square, Color.clear, Handles.color);
            Handles.color = new Color(1f, 1f, 1f, 0.38f);
            Handles.DrawLine(new Vector3(square.x + 3f, square.yMax - 3f, 0f), new Vector3(square.xMax - 3f, square.yMax - 3f, 0f));
            Handles.DrawLine(new Vector3(square.x + 3f, square.y + 3f, 0f), new Vector3(square.x + 3f, square.yMax - 3f, 0f));
        }

        internal static void DrawEyeIcon(Rect rect, Color color, bool open)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Vector3 left = new Vector3(rect.x + 2f, rect.center.y, 0f);
            Vector3 top = new Vector3(rect.center.x, rect.y + 5f, 0f);
            Vector3 right = new Vector3(rect.xMax - 2f, rect.center.y, 0f);
            Vector3 bottom = new Vector3(rect.center.x, rect.yMax - 5f, 0f);
            Handles.DrawAAPolyLine(2f, left, top, right, bottom, left);
            if (open)
                Handles.DrawSolidDisc(new Vector3(rect.center.x, rect.center.y, 0f), Vector3.forward, 2.5f);
            else
                Handles.DrawAAPolyLine(2f, new Vector3(rect.x + 3f, rect.yMax - 3f, 0f), new Vector3(rect.xMax - 3f, rect.y + 3f, 0f));
            Handles.EndGUI();
        }

        static void DrawBorder(Rect rect, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, color);
            Handles.EndGUI();
        }

        static GUIStyle BuildLabelStyle(Color color, FontStyle fontStyle, TextAnchor alignment)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = alignment,
                fontStyle = fontStyle
            };
            style.normal.textColor = color;
            return style;
        }

        internal static string GetEffectLabel(FigmageEffectType type)
        {
            switch (type)
            {
                case FigmageEffectType.InnerShadow:
                    return "Inner shadow";
                case FigmageEffectType.LayerBlur:
                    return "Layer blur";
                case FigmageEffectType.BackgroundBlur:
                    return "Background blur";
                default:
                    return "Drop shadow";
            }
        }

        internal static void ApplyAndRefresh(SerializedObject serializedObject)
        {
            serializedObject.ApplyModifiedProperties();
            foreach (UnityEngine.Object targetObject in serializedObject.targetObjects)
            {
                if (targetObject is Figmage figmage)
                {
                    figmage.Refresh();
                    EditorUtility.SetDirty(figmage);
                }
            }

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    sealed class FigmageEffectTypePopup : EditorWindow
    {
        static readonly FigmageEffectType[] Types =
        {
            FigmageEffectType.InnerShadow,
            FigmageEffectType.DropShadow,
            FigmageEffectType.LayerBlur
        };

        SerializedObject serializedObject;
        string propertyPath;

        public static void Show(Rect screenRect, SerializedObject source, string path)
        {
            FigmageEffectTypePopup window = CreateInstance<FigmageEffectTypePopup>();
            window.serializedObject = new SerializedObject(source.targetObjects);
            window.propertyPath = path;
            window.ShowAsDropDown(screenRect, new Vector2(300f, 172f));
            window.Focus();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            StylePopupRoot(root);

            FigmageEffectType current = GetCurrentType();
            for (int i = 0; i < Types.Length; i++)
            {
                FigmageEffectType type = Types[i];
                Button row = new Button(() => SelectType(type));
                row.text = (type == current ? "\u2713  " : "   ") + FigmageEffectDrawer.GetEffectLabel(type);
                StylePopupRow(row);
                root.Add(row);
            }
        }

        FigmageEffectType GetCurrentType()
        {
            serializedObject.Update();
            SerializedProperty effect = serializedObject.FindProperty(propertyPath);
            return (FigmageEffectType)effect.FindPropertyRelative("Type").enumValueIndex;
        }

        void SelectType(FigmageEffectType type)
        {
            serializedObject.Update();
            SerializedProperty effect = serializedObject.FindProperty(propertyPath);
            effect.FindPropertyRelative("Type").enumValueIndex = (int)type;
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            Close();
        }

        internal static void StylePopupRoot(VisualElement root)
        {
            root.style.backgroundColor = new Color(0.11f, 0.11f, 0.11f, 1f);
            root.style.paddingLeft = 18f;
            root.style.paddingRight = 18f;
            root.style.paddingTop = 14f;
            root.style.paddingBottom = 14f;
        }

        internal static void StylePopupRow(Button row)
        {
            row.style.height = 34f;
            row.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.style.backgroundColor = Color.clear;
            row.style.borderBottomWidth = 0f;
            row.style.borderTopWidth = 0f;
            row.style.borderLeftWidth = 0f;
            row.style.borderRightWidth = 0f;
            row.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            row.style.fontSize = 14f;
            row.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
    }

    sealed class FigmageEffectSettingsPopup : EditorWindow
    {
        SerializedObject serializedObject;
        string propertyPath;

        public static void Show(Rect screenRect, SerializedObject source, string path)
        {
            FigmageEffectSettingsPopup window = CreateInstance<FigmageEffectSettingsPopup>();
            window.serializedObject = new SerializedObject(source.targetObjects);
            window.propertyPath = path;
            window.ShowAsDropDown(screenRect, new Vector2(360f, 238f));
            window.Focus();
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            FigmageEffectTypePopup.StylePopupRoot(root);
            root.style.paddingLeft = 14f;
            root.style.paddingRight = 14f;

            serializedObject.Update();
            SerializedProperty effect = serializedObject.FindProperty(propertyPath);
            FigmageEffectType type = (FigmageEffectType)effect.FindPropertyRelative("Type").enumValueIndex;

            VisualElement header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            Label title = new Label(FigmageEffectDrawer.GetEffectLabel(type));
            title.style.flexGrow = 1f;
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            Button close = new Button(Close) { text = "x" };
            close.style.width = 28f;
            close.style.height = 26f;
            FigmageEffectTypePopup.StylePopupRow(close);
            close.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(title);
            header.Add(close);
            root.Add(header);

            AddBoundField(root, effect.FindPropertyRelative("Opacity"));
            AddBoundField(root, effect.FindPropertyRelative("Radius"));

            if (type == FigmageEffectType.DropShadow || type == FigmageEffectType.InnerShadow)
            {
                AddBoundField(root, effect.FindPropertyRelative("BlendMode"));
                AddBoundField(root, effect.FindPropertyRelative("Color"));
                AddBoundField(root, effect.FindPropertyRelative("Offset"));
                AddBoundField(root, effect.FindPropertyRelative("Spread"));
            }
            else if (type == FigmageEffectType.LayerBlur)
            {
                AddBoundField(root, effect.FindPropertyRelative("BlurType"));
                AddBoundField(root, effect.FindPropertyRelative("StartRadius"));
                AddBoundField(root, effect.FindPropertyRelative("StartOffset"));
                AddBoundField(root, effect.FindPropertyRelative("EndOffset"));
            }

            root.Bind(serializedObject);
        }

        void AddBoundField(VisualElement root, SerializedProperty property)
        {
            PropertyField field = new PropertyField(property);
            field.style.marginTop = 8f;
            field.RegisterCallback<SerializedPropertyChangeEvent>(_ => FigmageEffectDrawer.ApplyAndRefresh(serializedObject));
            root.Add(field);
        }
    }
}
