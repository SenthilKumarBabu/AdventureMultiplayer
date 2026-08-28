using DA_Assets.DAI;
using DA_Assets.UpdateChecker.Models;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.Figmage
{
    [CustomEditor(typeof(Figmage)), CanEditMultipleObjects]
    public sealed class FigmageEditor : Editor
    {
        const float SceneHandleSize = 0.08f;
        const float SceneSwatchSize = 0.12f;
        const float SceneShadowOffset = 0.0125f;

        [SerializeField] DAInspectorUITK uitk;
        [SerializeField] Texture2D cornerTopLeftIcon;
        [SerializeField] Texture2D cornerTopRightIcon;
        [SerializeField] Texture2D cornerBottomLeftIcon;
        [SerializeField] Texture2D cornerBottomRightIcon;
        [SerializeField] Texture2D cornerAllIcon;
        [SerializeField] float bakeScale = 1f;

        readonly Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();

        static int activeGradientTargetId;
        static string activeGradientPropertyPath;

        SerializedProperty opacityProp;
        SerializedProperty cornerRadiusProp;
        SerializedProperty cornerRadiusesProp;
        SerializedProperty cornerSmoothingProp;
        SerializedProperty fillsProp;
        SerializedProperty strokesProp;
        SerializedProperty strokeWeightProp;
        SerializedProperty strokeAlignProp;
        SerializedProperty individualStrokeWeightsProp;
        SerializedProperty effectsProp;
        SerializedProperty shaderEnabledProp;
        SerializedProperty bakedSpriteGuidProp;
        SerializedProperty overflowAnchorsProp;

        void OnEnable()
        {
            opacityProp = serializedObject.FindProperty("opacity");
            cornerRadiusProp = serializedObject.FindProperty("cornerRadius");
            cornerRadiusesProp = serializedObject.FindProperty("cornerRadiuses");
            cornerSmoothingProp = serializedObject.FindProperty("cornerSmoothing");
            fillsProp = serializedObject.FindProperty("fills");
            strokesProp = serializedObject.FindProperty("strokes");
            strokeWeightProp = serializedObject.FindProperty("strokeWeight");
            strokeAlignProp = serializedObject.FindProperty("strokeAlign");
            individualStrokeWeightsProp = serializedObject.FindProperty("individualStrokeWeights");
            effectsProp = serializedObject.FindProperty("effects");
            shaderEnabledProp = serializedObject.FindProperty("shaderEnabled");
            bakedSpriteGuidProp = serializedObject.FindProperty("bakedSpriteGuid");
            overflowAnchorsProp = serializedObject.FindProperty("overflowAnchors");
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = uitk != null
                ? uitk.CreateRoot(uitk.ColorScheme.BG, false)
                : new VisualElement { style = { paddingLeft = 10, paddingRight = 10, paddingTop = 10, paddingBottom = 10 } };

            AddInspectorBlock(root, DrawGeneralInspector);
            AddSpace(root);
            AddInspectorBlock(root, DrawCornerControls);
            AddSpace(root);
            AddInspectorBlock(root, DrawFillInspector);
            AddSpace(root);
            AddInspectorBlock(root, DrawStrokeInspector);
            AddSpace(root);
            AddInspectorBlock(root, DrawEffectsInspector);
            AddSpace(root);
            AddInspectorBlock(root, DrawBakeInspector);

            if (uitk != null)
            {
                root.Add(uitk.Space(12));
                root.Add(uitk.CreateFooterWithVersionInfo(
                    FigmageConfig.Instance.Localizator.Language,
                    new DAInspectorUITK.FooterAssetInfo
                    {
                        AssetType = AssetType.FIGMAGE,
                        ProductVersion = FigmageConfig.Instance.ProductVersion
                    }));
            }

            return root;
        }

        void DrawGeneralInspector()
        {
            EditorGUILayout.Slider(opacityProp, 0f, 1f);
            EditorGUILayout.PropertyField(shaderEnabledProp);
        }

        void DrawFillInspector()
        {
            GetPaintList(fillsProp, "Fill", false).DoLayoutList();
        }

        void DrawStrokeInspector()
        {
            GetPaintList(strokesProp, "Stroke", true).DoLayoutList();
            DrawStrokeControls();
        }

        void DrawEffectsInspector()
        {
            GetEffectList(effectsProp).DoLayoutList();
        }

        void DrawBakeInspector()
        {
            DrawBakedSpriteField();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(overflowAnchorsProp);
            }

            bakeScale = EditorGUILayout.Slider("Bake Scale", bakeScale, 0.25f, 4f);

            if (GUILayout.Button(new GUIContent("Bake", "Render current Figmage to a PNG sprite asset."), GUILayout.Height(28f)))
                BakeSelected();
        }

        void DrawSerializedBlock(System.Action draw)
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            Color previousBackground = GUI.backgroundColor;
            if (uitk != null)
                GUI.backgroundColor = uitk.ColorScheme.IMGUI;

            try
            {
                draw();
            }
            finally
            {
                GUI.backgroundColor = previousBackground;
            }

            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (changed)
                RefreshTargets();
        }

        void AddInspectorBlock(VisualElement root, System.Action draw)
        {
            IMGUIContainer container = new IMGUIContainer(() => DrawSerializedBlock(draw));

            if (uitk == null)
            {
                root.Add(container);
                return;
            }

            (VisualElement blockRoot, VisualElement content) = InsetGroup();
            content.Add(container);
            root.Add(blockRoot);
        }

        void AddSpace(VisualElement root)
        {
            if (uitk != null)
                root.Add(uitk.Space10());
        }

        (VisualElement root, VisualElement content) InsetGroup(
            int paddingTop = 10,
            int paddingBottom = 10,
            int paddingLeft = 10,
            int paddingRight = 10)
        {
            VisualElement wrap = new VisualElement
            {
                style =
                {
                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(StyleKeyword.Auto)
                }
            };

            VisualElement group = new VisualElement
            {
                style =
                {
                    backgroundColor = new StyleColor(uitk.ColorScheme.GROUP),
                    borderLeftColor = uitk.ColorScheme.OUTLINE,
                    borderRightColor = uitk.ColorScheme.OUTLINE,
                    borderTopColor = uitk.ColorScheme.OUTLINE,
                    borderBottomColor = uitk.ColorScheme.OUTLINE,
                    borderLeftWidth = new StyleFloat(1f),
                    borderRightWidth = new StyleFloat(1f),
                    borderTopWidth = new StyleFloat(1f),
                    borderBottomWidth = new StyleFloat(1f),
                    borderTopLeftRadius = new StyleLength(14),
                    borderTopRightRadius = new StyleLength(14),
                    borderBottomLeftRadius = new StyleLength(14),
                    borderBottomRightRadius = new StyleLength(14),
                    paddingTop = new StyleLength(paddingTop),
                    paddingBottom = new StyleLength(paddingBottom),
                    paddingLeft = new StyleLength(paddingLeft),
                    paddingRight = new StyleLength(paddingRight),
                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(StyleKeyword.Auto),
                    overflow = new StyleEnum<Overflow>(Overflow.Hidden)
                }
            };

            VisualElement content = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column),
                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(StyleKeyword.Auto)
                }
            };

            group.Add(content);
            wrap.Add(group);
            return (wrap, content);
        }

        void DrawBakedSpriteField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                string path = AssetDatabase.GUIDToAssetPath(bakedSpriteGuidProp.stringValue);
                Sprite sprite = string.IsNullOrWhiteSpace(path)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Sprite>(path);
                EditorGUILayout.ObjectField("Baked Sprite", sprite, typeof(Sprite), false);
            }
        }

        void DrawCornerControls()
        {
            bool independent = cornerRadiusesProp.arraySize >= 4;
            bool nextIndependent = EditorGUILayout.Toggle("Independent", independent);
            if (nextIndependent != independent)
                SetIndependentCorners(nextIndependent, independent);

            if (nextIndependent)
            {
                EnsureCornerRadiuses();
                DrawIndependentCornerLayout();
            }
            else
            {
                DrawSingleCornerLayout();
            }

            EditorGUILayout.Slider(cornerSmoothingProp, 0f, 1f);
        }

        void SetIndependentCorners(bool nextIndependent, bool wasIndependent)
        {
            if (nextIndependent)
            {
                EnsureCornerRadiuses();
                return;
            }

            cornerRadiusProp.floatValue = wasIndependent && cornerRadiusesProp.arraySize > 0
                ? cornerRadiusesProp.GetArrayElementAtIndex(0).floatValue
                : cornerRadiusProp.floatValue;
            cornerRadiusesProp.arraySize = 0;
        }

        void EnsureCornerRadiuses()
        {
            float fallback = Mathf.Max(0f, cornerRadiusProp.floatValue);
            while (cornerRadiusesProp.arraySize < 4)
            {
                int index = cornerRadiusesProp.arraySize;
                cornerRadiusesProp.InsertArrayElementAtIndex(index);
                cornerRadiusesProp.GetArrayElementAtIndex(index).floatValue = fallback;
            }
        }

        void DrawIndependentCornerLayout()
        {
            Rect block = GUILayoutUtility.GetRect(1f, 94f, GUILayout.ExpandWidth(true));
            DrawCornerField(new Rect(block.x + 20f, block.y + 10f, 112f, 24f), cornerRadiusesProp.GetArrayElementAtIndex(0), cornerTopLeftIcon, true);
            DrawCornerField(new Rect(block.xMax - 132f, block.y + 10f, 112f, 24f), cornerRadiusesProp.GetArrayElementAtIndex(1), cornerTopRightIcon, false);
            DrawCornerField(new Rect(block.x + 20f, block.y + 58f, 112f, 24f), cornerRadiusesProp.GetArrayElementAtIndex(3), cornerBottomLeftIcon, true);
            DrawCornerField(new Rect(block.xMax - 132f, block.y + 58f, 112f, 24f), cornerRadiusesProp.GetArrayElementAtIndex(2), cornerBottomRightIcon, false);
        }

        void DrawSingleCornerLayout()
        {
            Rect block = GUILayoutUtility.GetRect(1f, 42f, GUILayout.ExpandWidth(true));
            DrawCornerField(new Rect(block.x + 20f, block.y + 8f, 112f, 24f), cornerRadiusProp, cornerAllIcon, true);
        }

        static void DrawCornerField(Rect rect, SerializedProperty radius, Texture2D icon, bool iconOnLeft)
        {
            Rect iconRect = iconOnLeft
                ? new Rect(rect.x, rect.y, 24f, 24f)
                : new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
            Rect fieldRect = iconOnLeft
                ? new Rect(iconRect.xMax + 10f, rect.y + 1f, 74f, 22f)
                : new Rect(rect.x, rect.y + 1f, 74f, 22f);

            DrawCornerIcon(iconRect, icon);
            HandleCornerDrag(iconRect, radius);

            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.FloatField(fieldRect, GUIContent.none, radius.floatValue);
            if (EditorGUI.EndChangeCheck())
                radius.floatValue = Mathf.Max(0f, value);
        }

        static void DrawCornerIcon(Rect rect, Texture2D icon)
        {
            if (icon != null)
            {
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
                return;
            }

            Handles.BeginGUI();
            Handles.color = FigmagePaintDrawer.TextColor;
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, FigmagePaintDrawer.TextColor);
            Handles.EndGUI();
        }

        static void HandleCornerDrag(Rect rect, SerializedProperty radius)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);
            Event current = Event.current;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.SlideArrow);

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && rect.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        radius.floatValue = Mathf.Max(0f, radius.floatValue + current.delta.x - current.delta.y);
                        current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
            }
        }

        void DrawStrokeControls()
        {
            Rect row = EditorGUILayout.GetControlRect(false, 32f);
            row.y += 4f;
            row.height = 26f;

            Rect alignRect = new Rect(row.x, row.y, Mathf.Min(214f, row.width * 0.43f), row.height);
            Rect weightRect = new Rect(alignRect.xMax + 10f, row.y, Mathf.Min(214f, row.width - alignRect.width - 10f), row.height);

            DrawStrokeAlignDropdown(alignRect);
            DrawStrokeWeightField(weightRect);
        }

        void DrawStrokeAlignDropdown(Rect rect)
        {
            FigmagePaintDrawer.DrawFieldBox(rect);
            FigmageStrokeAlign align = (FigmageStrokeAlign)strokeAlignProp.enumValueIndex;
            GUI.Label(new Rect(rect.x + 10f, rect.y, rect.width - 30f, rect.height), align.ToString(), FigmagePaintDrawer.BuildLabelStyle(FigmagePaintDrawer.TextColor, FontStyle.Bold, TextAnchor.MiddleLeft));
            FigmagePaintDrawer.DrawChevron(new Rect(rect.xMax - 22f, rect.y + 9f, 8f, 8f));
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                FigmageStrokeAlignPopup.Show(GUIUtility.GUIToScreenRect(rect), serializedObject);
        }

        void DrawStrokeWeightField(Rect rect)
        {
            FigmagePaintDrawer.DrawFieldBox(rect);
            Rect iconRect = new Rect(rect.x + 10f, rect.y + 5f, 20f, 16f);
            FigmagePaintDrawer.DrawStrokeWeightIcon(iconRect, FigmagePaintDrawer.MutedColor);

            bool wasUniform = StrokeWeightsAreUniform();
            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.FloatField(new Rect(rect.x + 34f, rect.y + 3f, rect.width - 40f, 20f), GUIContent.none, strokeWeightProp.floatValue, FigmagePaintDrawer.NumberStyle);
            if (!EditorGUI.EndChangeCheck())
                return;

            strokeWeightProp.floatValue = Mathf.Max(0f, value);
            if (wasUniform)
                SetAllStrokeWeights(strokeWeightProp.floatValue);
        }

        bool StrokeWeightsAreUniform()
        {
            float weight = Mathf.Max(0f, strokeWeightProp.floatValue);
            return Mathf.Approximately(individualStrokeWeightsProp.FindPropertyRelative("Top").floatValue, weight) &&
                   Mathf.Approximately(individualStrokeWeightsProp.FindPropertyRelative("Bottom").floatValue, weight) &&
                   Mathf.Approximately(individualStrokeWeightsProp.FindPropertyRelative("Left").floatValue, weight) &&
                   Mathf.Approximately(individualStrokeWeightsProp.FindPropertyRelative("Right").floatValue, weight);
        }

        void SetAllStrokeWeights(float value)
        {
            individualStrokeWeightsProp.FindPropertyRelative("Top").floatValue = value;
            individualStrokeWeightsProp.FindPropertyRelative("Bottom").floatValue = value;
            individualStrokeWeightsProp.FindPropertyRelative("Left").floatValue = value;
            individualStrokeWeightsProp.FindPropertyRelative("Right").floatValue = value;
        }

        ReorderableList GetPaintList(SerializedProperty property, string title, bool stroke)
        {
            string key = property.propertyPath;
            if (lists.TryGetValue(key, out ReorderableList existing))
                return existing;

            ReorderableList list = new ReorderableList(serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);
            };
            list.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= property.arraySize)
                    return;

                SerializedProperty paint = property.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUI.GetPropertyHeight(paint, GUIContent.none, true);
                EditorGUI.PropertyField(rect, paint, GUIContent.none, true);
            };
            list.elementHeightCallback = index =>
            {
                if (index < 0 || index >= property.arraySize)
                    return EditorGUIUtility.singleLineHeight + 6f;

                return EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(index), GUIContent.none, true) + 4f;
            };
            list.onAddCallback = _ =>
            {
                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                ResetPaint(property.GetArrayElementAtIndex(index), stroke);
                serializedObject.ApplyModifiedProperties();
                RefreshTargets();
            };
            list.onRemoveCallback = reorderableList =>
            {
                if (reorderableList.index >= 0 && reorderableList.index < property.arraySize)
                    property.DeleteArrayElementAtIndex(reorderableList.index);
                serializedObject.ApplyModifiedProperties();
                RefreshTargets();
            };
            list.onReorderCallback = _ =>
            {
                serializedObject.ApplyModifiedProperties();
                RefreshTargets();
            };

            lists[key] = list;
            return list;
        }

        ReorderableList GetEffectList(SerializedProperty property)
        {
            string key = property.propertyPath;
            if (lists.TryGetValue(key, out ReorderableList existing))
                return existing;

            ReorderableList list = new ReorderableList(serializedObject, property, true, true, true, true);
            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Effects", EditorStyles.boldLabel);
            };
            list.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= property.arraySize)
                    return;

                SerializedProperty effect = property.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUI.GetPropertyHeight(effect, GUIContent.none, true);
                EditorGUI.PropertyField(rect, effect, GUIContent.none, true);
            };
            list.elementHeightCallback = index =>
            {
                if (index < 0 || index >= property.arraySize)
                    return EditorGUIUtility.singleLineHeight + 6f;

                return EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(index), GUIContent.none, true) + 4f;
            };
            list.onAddCallback = _ =>
            {
                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                ResetEffect(property.GetArrayElementAtIndex(index));
                serializedObject.ApplyModifiedProperties();
                RefreshTargets();
            };
            list.onRemoveCallback = reorderableList =>
            {
                if (reorderableList.index >= 0 && reorderableList.index < property.arraySize)
                    property.DeleteArrayElementAtIndex(reorderableList.index);
                serializedObject.ApplyModifiedProperties();
                RefreshTargets();
            };
            list.onReorderCallback = _ =>
            {
                serializedObject.ApplyModifiedProperties();
                RefreshTargets();
            };

            lists[key] = list;
            return list;
        }

        static void ResetPaint(SerializedProperty paint, bool stroke)
        {
            paint.FindPropertyRelative("Type").enumValueIndex = (int)FigmagePaintType.Solid;
            paint.FindPropertyRelative("Color").colorValue = Color.white;
            paint.FindPropertyRelative("Visible").boolValue = true;
            paint.FindPropertyRelative("BlendMode").stringValue = "NORMAL";
            paint.FindPropertyRelative("Opacity").floatValue = 1f;
            paint.FindPropertyRelative("GradientHandlePositions").arraySize = 0;
            paint.FindPropertyRelative("GradientStops").arraySize = 0;
        }

        static void ResetEffect(SerializedProperty effect)
        {
            effect.FindPropertyRelative("Type").enumValueIndex = (int)FigmageEffectType.DropShadow;
            effect.FindPropertyRelative("Visible").boolValue = true;
            effect.FindPropertyRelative("Color").colorValue = new Color(0f, 0f, 0f, 0.25f);
            effect.FindPropertyRelative("BlendMode").stringValue = "NORMAL";
            effect.FindPropertyRelative("Opacity").floatValue = 1f;
            effect.FindPropertyRelative("Offset").vector2Value = new Vector2(0f, 4f);
            effect.FindPropertyRelative("Radius").floatValue = 8f;
            effect.FindPropertyRelative("Spread").floatValue = 0f;
        }

        void OnSceneGUI()
        {
            Figmage figmage = (Figmage)target;
            RectTransform rectTransform = figmage.transform as RectTransform;
            if (rectTransform == null || !TryGetActiveGradient(figmage, out FigmagePaint paint))
                return;

            EnsureGradientData(paint);

            Vector2 startHandle = paint.GradientHandlePositions[0];
            Vector2 endHandle = paint.GradientHandlePositions[1];
            Vector2 widthHandle = paint.GradientHandlePositions[2];
            Vector3 startWorld = ToWorld(rectTransform, startHandle);
            Vector3 endWorld = ToWorld(rectTransform, endHandle);
            Vector3 widthWorld = ToWorld(rectTransform, widthHandle);

            DrawLineWithShadow(startWorld, endWorld, Color.white);
            DrawLineWithShadow(startWorld, widthWorld, Color.white);
            DrawGradientStopSwatches(paint, startWorld, endWorld);

            EditorGUI.BeginChangeCheck();
            Vector3 movedStart = DrawHandle(startWorld);
            Vector3 movedEnd = DrawHandle(endWorld);
            Vector3 movedWidth = DrawHandle(widthWorld);
            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(figmage, "Move Figmage Gradient Handles");
            Vector2 newStart = ToHandlePosition(rectTransform, movedStart);
            Vector2 newEnd = ToHandlePosition(rectTransform, movedEnd);
            Vector2 newWidth = ToHandlePosition(rectTransform, movedWidth);
            MoveGradientHandle(paint.GradientHandlePositions, rectTransform.rect, startHandle, endHandle, widthHandle, newStart, newEnd, newWidth);
            figmage.RefreshImmediate();
            EditorUtility.SetDirty(figmage);
        }

        internal static void BeginGradientHandleEditing(int targetId, string propertyPath)
        {
            activeGradientTargetId = targetId;
            activeGradientPropertyPath = propertyPath;
            SceneView.RepaintAll();
        }

        internal static void EndGradientHandleEditing(int targetId, string propertyPath)
        {
            if (activeGradientTargetId != targetId || activeGradientPropertyPath != propertyPath)
                return;

            activeGradientTargetId = 0;
            activeGradientPropertyPath = null;
            SceneView.RepaintAll();
        }

        static bool TryGetActiveGradient(Figmage figmage, out FigmagePaint paint)
        {
            paint = null;
            if (activeGradientTargetId != figmage.GetInstanceID() || string.IsNullOrWhiteSpace(activeGradientPropertyPath))
                return false;

            if (!TryGetPaintByPropertyPath(figmage, activeGradientPropertyPath, out paint))
                return false;

            return paint != null && paint.Visible && paint.Type != FigmagePaintType.Solid;
        }

        static bool TryGetPaintByPropertyPath(Figmage figmage, string propertyPath, out FigmagePaint paint)
        {
            paint = null;
            List<FigmagePaint> paints;
            if (propertyPath.StartsWith("fills."))
                paints = figmage.Fills;
            else if (propertyPath.StartsWith("strokes."))
                paints = figmage.Strokes;
            else
                return false;

            if (paints == null || !TryGetArrayIndex(propertyPath, out int index) || index < 0 || index >= paints.Count)
                return false;

            paint = paints[index];
            return true;
        }

        static bool TryGetArrayIndex(string propertyPath, out int index)
        {
            index = -1;
            int open = propertyPath.LastIndexOf('[');
            int close = propertyPath.LastIndexOf(']');
            if (open < 0 || close <= open + 1)
                return false;

            return int.TryParse(propertyPath.Substring(open + 1, close - open - 1), out index);
        }

        static void EnsureGradientData(FigmagePaint paint)
        {
            if (paint.GradientHandlePositions == null)
                paint.GradientHandlePositions = new List<Vector2>();

            while (paint.GradientHandlePositions.Count < 3)
            {
                if (paint.GradientHandlePositions.Count == 0)
                    paint.GradientHandlePositions.Add(new Vector2(0f, 0.5f));
                else if (paint.GradientHandlePositions.Count == 1)
                    paint.GradientHandlePositions.Add(new Vector2(1f, 0.5f));
                else
                    paint.GradientHandlePositions.Add(new Vector2(0f, 1f));
            }

            if (paint.GradientStops == null || paint.GradientStops.Count == 0)
            {
                paint.GradientStops = new List<FigmageGradientStop>
                {
                    new FigmageGradientStop(0f, Color.white),
                    new FigmageGradientStop(1f, Color.black)
                };
            }
        }

        static void MoveGradientHandle(List<Vector2> handles, Rect rect, Vector2 start, Vector2 end, Vector2 width, Vector2 movedStart, Vector2 movedEnd, Vector2 movedWidth)
        {
            const float dragEpsilon = 0.000001f;
            if ((movedStart - start).sqrMagnitude > dragEpsilon)
            {
                handles[0] = movedStart;
                handles[2] = ProjectWidthHandle(rect, movedStart, end, width);
                return;
            }

            if ((movedEnd - end).sqrMagnitude > dragEpsilon)
            {
                Vector2 startLocal = ToLocalGradientPoint(rect, start);
                Vector2 axis = ToLocalGradientPoint(rect, end) - startLocal;
                Vector2 movedAxis = ToLocalGradientPoint(rect, movedEnd) - startLocal;
                if (axis.sqrMagnitude <= dragEpsilon || movedAxis.sqrMagnitude <= dragEpsilon)
                {
                    handles[1] = movedEnd;
                    return;
                }

                Vector2 widthAxis = ProjectWidthHandleLocal(startLocal, startLocal + axis, ToLocalGradientPoint(rect, width)) - startLocal;
                handles[1] = movedEnd;
                handles[2] = FromLocalGradientPoint(rect, startLocal + Rotate(widthAxis, Vector2.SignedAngle(axis, movedAxis)) * (movedAxis.magnitude / axis.magnitude));
                return;
            }

            if ((movedWidth - width).sqrMagnitude > dragEpsilon)
                handles[2] = ProjectWidthHandle(rect, start, end, movedWidth);
        }

        static Vector2 ProjectWidthHandle(Rect rect, Vector2 start, Vector2 end, Vector2 movedWidth)
        {
            return FromLocalGradientPoint(rect, ProjectWidthHandleLocal(
                ToLocalGradientPoint(rect, start),
                ToLocalGradientPoint(rect, end),
                ToLocalGradientPoint(rect, movedWidth)));
        }

        static Vector2 ProjectWidthHandleLocal(Vector2 start, Vector2 end, Vector2 movedWidth)
        {
            Vector2 axis = end - start;
            if (axis.sqrMagnitude <= 0.000001f)
                return movedWidth;

            Vector2 normal = new Vector2(-axis.y, axis.x).normalized;
            float signedWidth = Vector2.Dot(movedWidth - start, normal);
            return start + normal * signedWidth;
        }

        static Vector2 ToLocalGradientPoint(Rect rect, Vector2 handle)
        {
            return new Vector2(
                rect.xMin + rect.width * handle.x,
                rect.yMin + rect.height * (1f - handle.y));
        }

        static Vector2 FromLocalGradientPoint(Rect rect, Vector2 local)
        {
            return new Vector2(
                (local.x - rect.xMin) / rect.width,
                1f - ((local.y - rect.yMin) / rect.height));
        }

        static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }

        static void DrawLineWithShadow(Vector3 start, Vector3 end, Color color)
        {
            Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
            float handleSize = HandleUtility.GetHandleSize((start + end) * 0.5f) * SceneShadowOffset;
            Vector3 shadowOffset = Mathf.Abs(start.y - end.y) > Mathf.Abs(start.x - end.x)
                ? new Vector3(handleSize, 0f, 0f)
                : new Vector3(0f, -handleSize, 0f);

            Handles.color = shadowColor;
            Handles.DrawLine(start + shadowOffset, end + shadowOffset);
            Handles.color = color;
            Handles.DrawLine(start, end);
        }

        static Vector3 DrawHandle(Vector3 position)
        {
            int handleId = GUIUtility.GetControlID(FocusType.Passive);
            float handleSize = HandleUtility.GetHandleSize(position) * SceneHandleSize;
            float shadowSize = HandleUtility.GetHandleSize(position) * SceneShadowOffset;
            Vector3 shadowOffset = new Vector3(shadowSize, -shadowSize, 0f);

            Handles.color = new Color(0f, 0f, 0f, 0.5f);
            Handles.SphereHandleCap(0, position + shadowOffset, Quaternion.identity, handleSize, EventType.Repaint);

            Handles.color = Color.white;
#if UNITY_2022_3_OR_NEWER
            return Handles.FreeMoveHandle(handleId, position, handleSize, Vector3.zero, Handles.SphereHandleCap);
#else
            return Handles.FreeMoveHandle(handleId, position, Quaternion.identity, handleSize, Vector3.zero, Handles.SphereHandleCap);
#endif
        }

        static void DrawGradientStopSwatches(FigmagePaint paint, Vector3 start, Vector3 end)
        {
            if (paint.GradientStops == null)
                return;

            for (int i = 0; i < paint.GradientStops.Count; i++)
            {
                FigmageGradientStop stop = paint.GradientStops[i];
                Vector3 position = Vector3.LerpUnclamped(start, end, stop.Position);
                DrawColorSwatch(position, stop.Color);
            }
        }

        static void DrawColorSwatch(Vector3 position, Color color)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * SceneSwatchSize;
            Vector3 up = SceneView.currentDrawingSceneView != null
                ? SceneView.currentDrawingSceneView.camera.transform.up
                : Vector3.up;
            Vector3 swatchPosition = position + up * handleSize * 1.7f;
            float borderSize = handleSize * 1.7f;
            float fillSize = borderSize * 0.72f;
            float shadowSize = HandleUtility.GetHandleSize(swatchPosition) * SceneShadowOffset;
            Vector3 shadowOffset = new Vector3(shadowSize, -shadowSize, 0f);

            Handles.color = new Color(0f, 0f, 0f, 0.5f);
            Handles.CubeHandleCap(0, swatchPosition + shadowOffset, Quaternion.identity, borderSize, EventType.Repaint);

            Handles.color = Color.white;
            Handles.CubeHandleCap(0, swatchPosition, Quaternion.identity, borderSize, EventType.Repaint);

            color.a = 1f;
            Handles.color = color;
            Handles.CubeHandleCap(0, swatchPosition, Quaternion.identity, fillSize, EventType.Repaint);
        }

        static Vector3 ToWorld(RectTransform rectTransform, Vector2 handle)
        {
            Rect rect = rectTransform.rect;
            Vector3 local = new Vector3(
                rect.xMin + rect.width * handle.x,
                rect.yMin + rect.height * (1f - handle.y),
                0f);
            return rectTransform.TransformPoint(local);
        }

        static Vector2 ToHandlePosition(RectTransform rectTransform, Vector3 world)
        {
            Rect rect = rectTransform.rect;
            Vector3 local = rectTransform.InverseTransformPoint(world);
            return new Vector2(
                (local.x - rect.xMin) / rect.width,
                1f - ((local.y - rect.yMin) / rect.height));
        }

        void BakeSelected()
        {
            serializedObject.ApplyModifiedProperties();

            Figmage figmage = (Figmage)target;
            string path = ResolveBakePath(figmage);

            if (string.IsNullOrWhiteSpace(path))
                return;

            BakeToSprite(figmage, Mathf.Max(0.01f, bakeScale), path);
        }

        static string ResolveBakePath(Figmage figmage)
        {
            string existingPath = AssetDatabase.GUIDToAssetPath(figmage.BakedSpriteGuid);
            if (!string.IsNullOrWhiteSpace(existingPath))
                return existingPath;

            return EditorUtility.SaveFilePanelInProject(
                "Bake Figmage",
                figmage.name + ".png",
                "png",
                "Choose where to save the baked Figmage sprite.");
        }

        public static Sprite BakeToSprite(Figmage figmage, float scale, string path)
        {
            figmage.BakePng(path, Mathf.Max(0.01f, scale));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Sprite sprite = LoadSpriteAsset(path);
            Undo.RecordObject(figmage.Image, "Bake Figmage Sprite");
            Undo.RecordObject(figmage, "Bake Figmage Sprite");
            figmage.BakedSpriteGuid = AssetDatabase.AssetPathToGUID(path);
            figmage.Image.sprite = sprite;
            figmage.Image.material = null;
            figmage.Image.color = Color.white;
            figmage.ShaderEnabled = false;
            figmage.ApplyCurrentOverflow();
            EditorUtility.SetDirty(figmage);
            EditorUtility.SetDirty(figmage.Image);
            return sprite;
        }

        static Sprite LoadSpriteAsset(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite nestedSprite)
                    return nestedSprite;
            }

            return null;
        }

        void RefreshTargets()
        {
            foreach (Object selectedTarget in targets)
            {
                if (selectedTarget is Figmage figmage)
                {
                    figmage.Refresh();
                    EditorUtility.SetDirty(figmage);
                }
            }
        }

    }
}
