using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.Figmage
{
    [CustomPropertyDrawer(typeof(FigmagePaint))]
    public sealed class FigmagePaintDrawer : PropertyDrawer
    {
        const float RowHeight = 34f;
        const float IconSize = 22f;
        const float Gap = 8f;
        const float TypeFieldMinWidth = 120f;

        internal static readonly Color FieldBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        internal static readonly Color FieldBorder = new Color(1f, 1f, 1f, 0.12f);
        internal static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        internal static readonly Color MutedColor = new Color(0.72f, 0.72f, 0.72f, 1f);

        static GUIStyle numberStyle;

        internal static GUIStyle NumberStyle
        {
            get
            {
                if (numberStyle == null)
                {
                    numberStyle = new GUIStyle(EditorStyles.numberField)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = FontStyle.Bold
                    };
                    numberStyle.normal.textColor = TextColor;
                }

                return numberStyle;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return RowHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty typeProp = property.FindPropertyRelative("Type");
            SerializedProperty visibleProp = property.FindPropertyRelative("Visible");

            FigmagePaintType type = (FigmagePaintType)typeProp.enumValueIndex;
            Rect row = new Rect(position.x, position.y + 4f, position.width, 26f);
            Rect swatchRect = new Rect(row.x + 2f, row.y + 2f, IconSize, IconSize);
            Rect visibilityRect = new Rect(row.xMax - 24f, row.y + 3f, 20f, 20f);
            Rect dropdownRect = new Rect(swatchRect.xMax + Gap, row.y, Mathf.Max(TypeFieldMinWidth, visibilityRect.x - swatchRect.xMax - Gap), 26f);

            DrawPaintSwatch(swatchRect, property, type);
            if (GUI.Button(swatchRect, GUIContent.none, GUIStyle.none))
                FigmagePaintSettingsPopup.Show(GUIUtility.GUIToScreenRect(swatchRect), property.serializedObject, property.propertyPath);

            DrawDropdown(dropdownRect, GetPaintLabel(type));
            if (GUI.Button(dropdownRect, GUIContent.none, GUIStyle.none))
                FigmagePaintTypePopup.Show(GUIUtility.GUIToScreenRect(dropdownRect), property.serializedObject, property.propertyPath);

            FigmageEffectDrawer.DrawEyeIcon(visibilityRect, visibleProp.boolValue ? TextColor : MutedColor, visibleProp.boolValue);
            if (GUI.Button(visibilityRect, GUIContent.none, GUIStyle.none))
            {
                visibleProp.boolValue = !visibleProp.boolValue;
                FigmageEffectDrawer.ApplyAndRefresh(property.serializedObject);
            }
        }

        internal static void DrawDropdown(Rect rect, string label)
        {
            DrawFieldBox(rect);
            Rect labelRect = new Rect(rect.x + 10f, rect.y, rect.width - 30f, rect.height);
            GUI.Label(labelRect, label, BuildLabelStyle(TextColor, FontStyle.Bold, TextAnchor.MiddleLeft));
            DrawChevron(new Rect(rect.xMax - 22f, rect.y + 9f, 8f, 8f));
        }

        static void DrawOpacity(Rect rect, SerializedProperty opacityProp, SerializedObject serializedObject)
        {
            DrawFieldBox(rect);

            EditorGUI.BeginChangeCheck();
            int nextPercent = EditorGUI.IntField(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 24f, 18f), GUIContent.none, Mathf.RoundToInt(Mathf.Clamp01(opacityProp.floatValue) * 100f), NumberStyle);
            GUI.Label(new Rect(rect.xMax - 18f, rect.y, 14f, rect.height), "%", BuildLabelStyle(MutedColor, FontStyle.Bold, TextAnchor.MiddleLeft));
            if (!EditorGUI.EndChangeCheck())
                return;

            opacityProp.floatValue = Mathf.Clamp01(nextPercent / 100f);
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
        }

        static void DrawPaintSwatch(Rect rect, SerializedProperty paint, FigmagePaintType type)
        {
            DrawRoundedRect(rect, new Color(1f, 1f, 1f, 0.08f), 4f);
            Rect fillRect = new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f);

            if (type == FigmagePaintType.Solid)
            {
                Color color = paint.FindPropertyRelative("Color").colorValue;
                color.a = 1f;
                DrawRoundedRect(fillRect, color, 3f);
                return;
            }

            Color start = Color.white;
            Color end = Color.black;
            SerializedProperty stops = paint.FindPropertyRelative("GradientStops");
            if (stops != null && stops.arraySize > 0)
            {
                start = stops.GetArrayElementAtIndex(0).FindPropertyRelative("Color").colorValue;
                end = stops.GetArrayElementAtIndex(stops.arraySize - 1).FindPropertyRelative("Color").colorValue;
            }

            DrawGradientPreview(fillRect, start, end);
        }

        static void DrawGradientPreview(Rect rect, Color start, Color end)
        {
            int segments = Mathf.Max(1, Mathf.CeilToInt(rect.width));
            for (int i = 0; i < segments; i++)
            {
                float x0 = rect.xMin + i;
                float x1 = i == segments - 1
                    ? rect.xMax
                    : rect.xMin + i + 1.25f;
                Rect part = Rect.MinMaxRect(x0, rect.yMin, x1, rect.yMax);
                Color color = Color.Lerp(start, end, (i + 0.5f) / segments);
                color.a = 1f;
                EditorGUI.DrawRect(part, color);
            }
        }

        internal static void DrawFieldBox(Rect rect)
        {
            EditorGUI.DrawRect(rect, FieldBg);
            DrawBorder(rect, FieldBorder);
        }

        internal static void DrawChevron(Rect rect)
        {
            Handles.BeginGUI();
            Handles.color = MutedColor;
            Vector3 a = new Vector3(rect.x, rect.y + 1f, 0f);
            Vector3 b = new Vector3(rect.center.x, rect.yMax - 1f, 0f);
            Vector3 c = new Vector3(rect.xMax, rect.y + 1f, 0f);
            Handles.DrawAAPolyLine(2f, a, b, c);
            Handles.EndGUI();
        }

        internal static void DrawStrokeWeightIcon(Rect rect, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(2f, new Vector3(rect.x, rect.y + 3f, 0f), new Vector3(rect.xMax, rect.y + 3f, 0f));
            Handles.DrawAAPolyLine(2f, new Vector3(rect.x, rect.center.y, 0f), new Vector3(rect.xMax, rect.center.y, 0f));
            Handles.DrawAAPolyLine(2f, new Vector3(rect.x, rect.yMax - 3f, 0f), new Vector3(rect.xMax, rect.yMax - 3f, 0f));
            Handles.EndGUI();
        }

        internal static void DrawSlidersIcon(Rect rect, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            float x0 = rect.x + 4f;
            float x1 = rect.xMax - 4f;
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? x0 : x1;
                Handles.DrawAAPolyLine(2f, new Vector3(x, rect.y + 2f, 0f), new Vector3(x, rect.yMax - 2f, 0f));
                Handles.DrawSolidDisc(new Vector3(x, rect.y + (i == 0 ? 8f : 14f), 0f), Vector3.forward, 2f);
            }
            Handles.EndGUI();
        }

        internal static void DrawStrokeSidesIcon(Rect rect, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Rect square = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f);
            Handles.DrawSolidRectangleWithOutline(square, Color.clear, color);
            Handles.DrawSolidRectangleWithOutline(new Rect(square.x + 3f, square.y + 3f, square.width - 6f, square.height - 6f), Color.clear, new Color(color.r, color.g, color.b, 0.45f));
            Handles.EndGUI();
        }

        static void DrawBorder(Rect rect, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, color);
            Handles.EndGUI();
        }

        static void DrawRoundedRect(Rect rect, Color color, float radius)
        {
            EditorGUI.DrawRect(rect, color);
        }

        internal static GUIStyle BuildLabelStyle(Color color, FontStyle fontStyle, TextAnchor alignment)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = alignment,
                fontStyle = fontStyle
            };
            style.normal.textColor = color;
            return style;
        }

        internal static string GetPaintLabel(FigmagePaintType type)
        {
            switch (type)
            {
                case FigmagePaintType.GradientLinear:
                    return "Linear";
                case FigmagePaintType.GradientRadial:
                    return "Radial";
                case FigmagePaintType.GradientAngular:
                    return "Angular";
                case FigmagePaintType.GradientDiamond:
                    return "Diamond";
                default:
                    return "Solid";
            }
        }
    }

    sealed class FigmagePaintTypePopup : EditorWindow
    {
        static readonly FigmagePaintType[] Types =
        {
            FigmagePaintType.Solid,
            FigmagePaintType.GradientLinear,
            FigmagePaintType.GradientRadial,
            FigmagePaintType.GradientAngular,
            FigmagePaintType.GradientDiamond
        };

        SerializedObject serializedObject;
        string propertyPath;

        public static void Show(Rect screenRect, SerializedObject source, string path)
        {
            FigmagePaintTypePopup window = CreateInstance<FigmagePaintTypePopup>();
            window.serializedObject = new SerializedObject(source.targetObjects);
            window.propertyPath = path;
            window.ShowAsDropDown(screenRect, new Vector2(300f, 206f));
            window.Focus();
        }

        public void CreateGUI()
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                Close();
                return;
            }

            VisualElement root = rootVisualElement;
            FigmageEffectTypePopup.StylePopupRoot(root);

            FigmagePaintType current = GetCurrentType();
            for (int i = 0; i < Types.Length; i++)
            {
                FigmagePaintType type = Types[i];
                Button row = new Button(() => SelectType(type));
                row.text = (type == current ? "\u2713  " : "   ") + FigmagePaintDrawer.GetPaintLabel(type);
                FigmageEffectTypePopup.StylePopupRow(row);
                root.Add(row);
            }
        }

        FigmagePaintType GetCurrentType()
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyPath))
                return FigmagePaintType.Solid;

            serializedObject.Update();
            SerializedProperty paint = serializedObject.FindProperty(propertyPath);
            if (paint == null)
                return FigmagePaintType.Solid;

            return (FigmagePaintType)paint.FindPropertyRelative("Type").enumValueIndex;
        }

        void SelectType(FigmagePaintType type)
        {
            serializedObject.Update();
            SerializedProperty paint = serializedObject.FindProperty(propertyPath);
            paint.FindPropertyRelative("Type").enumValueIndex = (int)type;
            EnsurePaintDefaults(paint, type);
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            Close();
        }

        internal static void EnsurePaintDefaults(SerializedProperty paint, FigmagePaintType type)
        {
            if (type == FigmagePaintType.Solid)
            {
                paint.FindPropertyRelative("Color").colorValue = Color.white;
                return;
            }

            SerializedProperty handles = paint.FindPropertyRelative("GradientHandlePositions");
            if (handles.arraySize < 3)
            {
                handles.arraySize = 3;
                handles.GetArrayElementAtIndex(0).vector2Value = new Vector2(0f, 0.5f);
                handles.GetArrayElementAtIndex(1).vector2Value = new Vector2(1f, 0.5f);
                handles.GetArrayElementAtIndex(2).vector2Value = new Vector2(0f, 1f);
            }

            SerializedProperty stops = paint.FindPropertyRelative("GradientStops");
            if (stops.arraySize > 0)
                return;

            stops.arraySize = 2;
            stops.GetArrayElementAtIndex(0).FindPropertyRelative("Position").floatValue = 0f;
            stops.GetArrayElementAtIndex(0).FindPropertyRelative("Color").colorValue = Color.white;
            stops.GetArrayElementAtIndex(1).FindPropertyRelative("Position").floatValue = 1f;
            stops.GetArrayElementAtIndex(1).FindPropertyRelative("Color").colorValue = Color.black;
        }
    }

    sealed class FigmagePaintSettingsPopup : EditorWindow
    {
        SerializedObject serializedObject;
        string propertyPath;
        int targetId;

        public static void Show(Rect screenRect, SerializedObject source, string path)
        {
            FigmagePaintSettingsPopup window = CreateInstance<FigmagePaintSettingsPopup>();
            window.serializedObject = new SerializedObject(source.targetObjects);
            window.propertyPath = path;
            window.targetId = source.targetObject.GetInstanceID();
            FigmageEditor.BeginGradientHandleEditing(window.targetId, path);
            window.ShowAsDropDown(screenRect, new Vector2(360f, 420f));
            window.Focus();
        }

        public void CreateGUI()
        {
            BuildGUI();
        }

        void BuildGUI()
        {
            if (serializedObject == null || string.IsNullOrWhiteSpace(propertyPath))
            {
                Close();
                return;
            }

            VisualElement root = rootVisualElement;
            root.Clear();
            FigmageEffectTypePopup.StylePopupRoot(root);
            root.style.paddingLeft = 14f;
            root.style.paddingRight = 14f;

            serializedObject.Update();
            SerializedProperty paint = serializedObject.FindProperty(propertyPath);
            if (paint == null)
            {
                Close();
                return;
            }

            FigmagePaintType type = (FigmagePaintType)paint.FindPropertyRelative("Type").enumValueIndex;

            VisualElement header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            Label title = new Label("Custom");
            title.style.flexGrow = 1f;
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = FigmagePaintDrawer.TextColor;
            Button close = new Button(CloseAndEndEditing) { text = "x" };
            close.style.width = 28f;
            close.style.height = 26f;
            FigmageEffectTypePopup.StylePopupRow(close);
            close.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(title);
            header.Add(close);
            root.Add(header);

            AddTypeDropdown(root, type);
            AddBoundField(root, paint.FindPropertyRelative("Opacity"));
            AddBoundField(root, paint.FindPropertyRelative("BlendMode"));

            if (type == FigmagePaintType.Solid)
            {
                AddBoundField(root, paint.FindPropertyRelative("Color"));
            }
            else
            {
#if UNITY_2022_1_OR_NEWER
                root.Add(new FigmageGradientScaleElement(serializedObject, propertyPath));
#endif
                AddBoundField(root, paint.FindPropertyRelative("GradientStops"));
            }

            root.Bind(serializedObject);
        }

        void CloseAndEndEditing()
        {
            FigmageEditor.EndGradientHandleEditing(targetId, propertyPath);
            Close();
        }

        void AddTypeDropdown(VisualElement root, FigmagePaintType type)
        {
            Button button = null;
            button = new Button(() => ShowPaintTypeMenu(button));
            button.text = FigmagePaintDrawer.GetPaintLabel(type);
            button.style.width = 146f;
            button.style.height = 34f;
            button.style.marginTop = 12f;
            button.style.marginBottom = 8f;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.paddingLeft = 10f;
            button.style.backgroundColor = FigmagePaintDrawer.FieldBg;
            button.style.color = FigmagePaintDrawer.TextColor;
            root.Add(button);
        }

        void ShowPaintTypeMenu(VisualElement anchor)
        {
            GenericMenu menu = new GenericMenu();
            FigmagePaintType current = GetCurrentType();
            AddPaintTypeMenuItem(menu, current, FigmagePaintType.Solid);
            AddPaintTypeMenuItem(menu, current, FigmagePaintType.GradientLinear);
            AddPaintTypeMenuItem(menu, current, FigmagePaintType.GradientRadial);
            AddPaintTypeMenuItem(menu, current, FigmagePaintType.GradientAngular);
            AddPaintTypeMenuItem(menu, current, FigmagePaintType.GradientDiamond);
            menu.DropDown(GUIUtility.GUIToScreenRect(anchor.worldBound));
        }

        void AddPaintTypeMenuItem(GenericMenu menu, FigmagePaintType current, FigmagePaintType type)
        {
            menu.AddItem(new GUIContent(FigmagePaintDrawer.GetPaintLabel(type)), type == current, () => SelectPaintType(type));
        }

        FigmagePaintType GetCurrentType()
        {
            serializedObject.Update();
            SerializedProperty paint = serializedObject.FindProperty(propertyPath);
            if (paint == null)
                return FigmagePaintType.Solid;

            return (FigmagePaintType)paint.FindPropertyRelative("Type").enumValueIndex;
        }

        void SelectPaintType(FigmagePaintType type)
        {
            serializedObject.Update();
            SerializedProperty paint = serializedObject.FindProperty(propertyPath);
            if (paint == null)
                return;

            paint.FindPropertyRelative("Type").enumValueIndex = (int)type;
            FigmagePaintTypePopup.EnsurePaintDefaults(paint, type);
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            SceneView.RepaintAll();
            BuildGUI();
        }

        void AddBoundField(VisualElement root, SerializedProperty property)
        {
            PropertyField field = new PropertyField(property);
            field.style.marginTop = 8f;
            field.RegisterCallback<SerializedPropertyChangeEvent>(_ => FigmageEffectDrawer.ApplyAndRefresh(serializedObject));
            root.Add(field);
        }
    }

#if UNITY_2022_1_OR_NEWER
    sealed class FigmageGradientScaleElement : VisualElement
    {
        const float BarHeight = 42f;
        const float HandleWidth = 22f;
        const float HandleHeight = 36f;

        readonly SerializedObject serializedObject;
        readonly string propertyPath;
        int draggedStopIndex = -1;

        public FigmageGradientScaleElement(SerializedObject serializedObject, string propertyPath)
        {
            this.serializedObject = serializedObject;
            this.propertyPath = propertyPath;

            style.height = 74f;
            style.marginTop = 12f;
            style.marginBottom = 12f;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<ContextClickEvent>(OnContextClick);
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect bar = GetBarRect();
            Painter2D painter = context.painter2D;
            painter.fillColor = FigmagePaintDrawer.FieldBg;
            FillRect(painter, bar);

            DrawGradientSegments(context, bar);
            DrawStopHandles(context, bar);
        }

        void DrawGradientSegments(MeshGenerationContext context, Rect bar)
        {
            SerializedProperty stops = GetStops();
            if (stops == null || stops.arraySize == 0)
            {
                context.painter2D.fillColor = Color.white;
                FillRect(context.painter2D, bar);
                return;
            }

            int segments = Mathf.Max(1, Mathf.CeilToInt(bar.width));
            for (int i = 0; i < segments; i++)
            {
                float x0 = bar.xMin + i;
                float x1 = i == segments - 1
                    ? bar.xMax
                    : bar.xMin + i + 1.25f;
                Rect segment = Rect.MinMaxRect(
                    x0,
                    bar.yMin,
                    x1,
                    bar.yMax);
                context.painter2D.fillColor = EvaluateGradient(stops, (i + 0.5f) / segments);
                FillRect(context.painter2D, segment);
            }
        }

        void DrawStopHandles(MeshGenerationContext context, Rect bar)
        {
            SerializedProperty stops = GetStops();
            if (stops == null)
                return;

            for (int i = 0; i < stops.arraySize; i++)
            {
                SerializedProperty stop = stops.GetArrayElementAtIndex(i);
                float position = Mathf.Clamp01(stop.FindPropertyRelative("Position").floatValue);
                Color color = stop.FindPropertyRelative("Color").colorValue;
                color.a = 1f;

                float x = Mathf.Lerp(bar.xMin, bar.xMax, position);
                Rect marker = new Rect(x - HandleWidth * 0.5f, bar.yMin - 8f, HandleWidth, HandleHeight);
                DrawHandle(context, marker, color);
            }
        }

        static void DrawHandle(MeshGenerationContext context, Rect rect, Color color)
        {
            Painter2D painter = context.painter2D;
            painter.fillColor = new Color(0.06f, 0.35f, 0.75f, 1f);
            FillRect(painter, new Rect(rect.x, rect.y, rect.width, rect.height - 6f));

            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.center.x - 5f, rect.yMax - 8f));
            painter.LineTo(new Vector2(rect.center.x, rect.yMax));
            painter.LineTo(new Vector2(rect.center.x + 5f, rect.yMax - 8f));
            painter.ClosePath();
            painter.Fill();

            Rect swatch = new Rect(rect.x + 4f, rect.y + 5f, rect.width - 8f, rect.width - 8f);
            painter.fillColor = color;
            FillRect(painter, swatch);
        }

        static void FillRect(Painter2D painter, Rect rect)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1)
                return;

            if (evt.button != 0)
                return;

            Rect bar = GetBarRect();
            int stopIndex = FindStopAt(evt.localPosition, bar);
            if (stopIndex < 0 && bar.Contains(evt.localPosition))
                stopIndex = AddStopAt(GetPositionFromPoint(evt.localPosition, bar));

            if (stopIndex < 0)
                return;

            draggedStopIndex = stopIndex;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnContextClick(ContextClickEvent evt)
        {
            Rect bar = GetBarRect();
            int stopIndex = FindStopAt(evt.localMousePosition, bar);
            if (stopIndex < 0)
                return;

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Redistribute stops"), false, RedistributeStops);

            SerializedProperty stops = GetStops();
            if (stops != null && stops.arraySize > 2)
                menu.AddItem(new GUIContent("Delete stop"), false, () => DeleteStop(stopIndex));
            else
                menu.AddDisabledItem(new GUIContent("Delete stop"));

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (draggedStopIndex < 0 || !this.HasPointerCapture(evt.pointerId))
                return;

            MoveStop(draggedStopIndex, GetPositionFromPoint(evt.localPosition, GetBarRect()));
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (draggedStopIndex < 0)
                return;

            draggedStopIndex = -1;
            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        int FindStopAt(Vector2 localPosition, Rect bar)
        {
            SerializedProperty stops = GetStops();
            if (stops == null)
                return -1;

            for (int i = stops.arraySize - 1; i >= 0; i--)
            {
                float position = Mathf.Clamp01(stops.GetArrayElementAtIndex(i).FindPropertyRelative("Position").floatValue);
                float x = Mathf.Lerp(bar.xMin, bar.xMax, position);
                Rect hit = new Rect(x - HandleWidth * 0.65f, bar.yMin - 12f, HandleWidth * 1.3f, HandleHeight + 8f);
                if (hit.Contains(localPosition))
                    return i;
            }

            return -1;
        }

        void RedistributeStops()
        {
            serializedObject.Update();
            SerializedProperty stops = GetStops();
            if (stops == null || stops.arraySize <= 1)
                return;

            float denominator = stops.arraySize - 1f;
            for (int i = 0; i < stops.arraySize; i++)
                stops.GetArrayElementAtIndex(i).FindPropertyRelative("Position").floatValue = i / denominator;

            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            MarkDirtyRepaint();
        }

        void DeleteStop(int index)
        {
            serializedObject.Update();
            SerializedProperty stops = GetStops();
            if (stops == null || stops.arraySize <= 2 || index < 0 || index >= stops.arraySize)
                return;

            stops.DeleteArrayElementAtIndex(index);
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            MarkDirtyRepaint();
        }

        int AddStopAt(float position)
        {
            serializedObject.Update();
            SerializedProperty stops = GetStops();
            if (stops == null)
                return -1;

            int index = stops.arraySize;
            Color color = EvaluateGradient(stops, position);
            stops.InsertArrayElementAtIndex(index);
            SerializedProperty stop = stops.GetArrayElementAtIndex(index);
            stop.FindPropertyRelative("Position").floatValue = Mathf.Clamp01(position);
            stop.FindPropertyRelative("Color").colorValue = color;
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            MarkDirtyRepaint();
            return index;
        }

        void MoveStop(int index, float position)
        {
            serializedObject.Update();
            SerializedProperty stops = GetStops();
            if (stops == null || index < 0 || index >= stops.arraySize)
                return;

            stops.GetArrayElementAtIndex(index).FindPropertyRelative("Position").floatValue = Mathf.Clamp01(position);
            FigmageEffectDrawer.ApplyAndRefresh(serializedObject);
            MarkDirtyRepaint();
        }

        SerializedProperty GetStops()
        {
            SerializedProperty paint = serializedObject.FindProperty(propertyPath);
            return paint?.FindPropertyRelative("GradientStops");
        }

        Rect GetBarRect()
        {
            return new Rect(12f, 28f, Mathf.Max(1f, contentRect.width - 24f), BarHeight);
        }

        static float GetPositionFromPoint(Vector2 localPosition, Rect bar)
        {
            return Mathf.InverseLerp(bar.xMin, bar.xMax, localPosition.x);
        }

        static Color EvaluateGradient(SerializedProperty stops, float position)
        {
            if (stops == null || stops.arraySize == 0)
                return Color.white;

            SerializedProperty previous = stops.GetArrayElementAtIndex(0);
            SerializedProperty next = previous;
            float previousPosition = previous.FindPropertyRelative("Position").floatValue;
            float nextPosition = previousPosition;

            for (int i = 0; i < stops.arraySize; i++)
            {
                SerializedProperty current = stops.GetArrayElementAtIndex(i);
                float currentPosition = current.FindPropertyRelative("Position").floatValue;
                if (currentPosition <= position && currentPosition >= previousPosition)
                {
                    previous = current;
                    previousPosition = currentPosition;
                }

                if (currentPosition >= position && (next == previous || currentPosition <= nextPosition || nextPosition < position))
                {
                    next = current;
                    nextPosition = currentPosition;
                }
            }

            Color previousColor = previous.FindPropertyRelative("Color").colorValue;
            Color nextColor = next.FindPropertyRelative("Color").colorValue;
            previousColor.a = 1f;
            nextColor.a = 1f;

            if (Mathf.Abs(nextPosition - previousPosition) <= 0.0001f)
                return previousColor;

            return Color.Lerp(previousColor, nextColor, Mathf.InverseLerp(previousPosition, nextPosition, position));
        }
    }
#endif
}
