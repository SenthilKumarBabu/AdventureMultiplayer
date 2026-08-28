#if UNITY_2021_3_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace DA_Assets.ImgOverflow
{
    [CustomEditor(typeof(ImageOverflow))]
    public class ImageOverflowEditor : Editor
    {
        private bool _editMode = false;
        private bool _shiftPressed = false;

        private ImageOverflow _monoBeh;
        private RectTransform _rectTransform;
        private Sprite _sprite;

        private const float _shadowOffset = 0.0125f;

        private GUIStyle _labelStyle => GetLabelStyle();
        private GUIStyle _labelShadowStyle => GetLabelShadowStyle();
        private Color _gizmoColor => GetGizmoColor();

        private SerializedProperty _areaAnchors;
        private SerializedProperty _debugMode;
        private SerializedProperty _leftPercent;
        private SerializedProperty _rightPercent;
        private SerializedProperty _topPercent;
        private SerializedProperty _bottomPercent;
        private SerializedProperty _leftPixels;
        private SerializedProperty _rightPixels;
        private SerializedProperty _topPixels;
        private SerializedProperty _bottomPixels;

        private static GUIContent __editModeIcon;
        private static GUIContent _editModeIcon
        {
            get
            {
                if (__editModeIcon == null)
                {
                    __editModeIcon = new GUIContent(
                        EditorGUIUtility.IconContent("EditCollider").image,
                        ImageOverflowLocKey.tooltip_edit_mode_button.Localize()
                    );
                }

                return __editModeIcon;
            }
        }

        private bool EditModeButton()
        {
            Vector2 btnSize = new Vector2(25, 25);
            Rect rect = EditorGUILayout.GetControlRect(true, btnSize.y);
            Rect buttonRect = new Rect(rect.xMin, rect.yMin, btnSize.x, btnSize.y);

            GUIContent iconContent = new GUIContent(_editModeIcon);
            _editMode = GUI.Toggle(buttonRect, _editMode, iconContent, "EditModeSingleButton");
            return _editMode;
        }

        private void OnEnable()
        {
            _monoBeh = (ImageOverflow)target;
            _rectTransform = _monoBeh.GetComponent<RectTransform>();

            _areaAnchors = serializedObject.FindProperty("_areaAnchors");
            _debugMode = serializedObject.FindProperty("_debugMode");
            _leftPercent = serializedObject.FindProperty("_leftPercent");
            _rightPercent = serializedObject.FindProperty("_rightPercent");
            _topPercent = serializedObject.FindProperty("_topPercent");
            _bottomPercent = serializedObject.FindProperty("_bottomPercent");
            _leftPixels = serializedObject.FindProperty("_leftPixels");
            _rightPixels = serializedObject.FindProperty("_rightPixels");
            _topPixels = serializedObject.FindProperty("_topPixels");
            _bottomPixels = serializedObject.FindProperty("_bottomPixels");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();

            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(ImageOverflowLocKey.label_edit_area_anchors.Localize()), GUI.skin.label, GUILayout.Width(EditorGUIUtility.labelWidth));
            labelRect.y += 1;
            GUI.Label(labelRect, ImageOverflowLocKey.label_edit_area_anchors.Localize());

            if (EditModeButton())
            {
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(_areaAnchors);

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(_debugMode);

            if (_debugMode.boolValue)
            {
                GUILayout.Space(10);

                EditorGUILayout.PropertyField(_leftPercent);
                EditorGUILayout.PropertyField(_rightPercent);
                EditorGUILayout.PropertyField(_topPercent);
                EditorGUILayout.PropertyField(_bottomPercent);

                EditorGUILayout.PropertyField(_leftPixels);
                EditorGUILayout.PropertyField(_rightPixels);
                EditorGUILayout.PropertyField(_topPixels);
                EditorGUILayout.PropertyField(_bottomPixels);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (!_editMode)
                return;

            if (_monoBeh.Image.sprite != _sprite)
            {
                UpdateSprite();
            }

            if (_sprite == null)
                return;

            Vector2 spriteSize = new Vector2(_sprite.texture.width, _sprite.texture.height);

            Event e = Event.current;

            _shiftPressed = e.shift;

            Borders areaAnchors = _monoBeh.AreaAnchors;

            Vector3[] rtCorners = new Vector3[4];
            _rectTransform.GetWorldCorners(rtCorners);

            Vector3 rtTopLeft = rtCorners[1];
            Vector3 rtTopRight = rtCorners[2];
            Vector3 rtBottomRight = rtCorners[3];
            Vector3 rtBottomLeft = rtCorners[0];

            float rtWidth = Vector3.Distance(rtTopLeft, rtTopRight);
            float rtHeight = Vector3.Distance(rtTopLeft, rtBottomLeft);

            Vector3 texTopLeft = rtTopLeft + _rectTransform.TransformVector(new Vector3(-areaAnchors.left * rtWidth, areaAnchors.top * rtHeight, 0));
            Vector3 texTopRight = rtTopRight + _rectTransform.TransformVector(new Vector3(areaAnchors.right * rtWidth, areaAnchors.top * rtHeight, 0));
            Vector3 texBottomRight = rtBottomRight + _rectTransform.TransformVector(new Vector3(areaAnchors.right * rtWidth, -areaAnchors.bottom * rtHeight, 0));
            Vector3 texBottomLeft = rtBottomLeft + _rectTransform.TransformVector(new Vector3(-areaAnchors.left * rtWidth, -areaAnchors.bottom * rtHeight, 0));
            Vector3 texCenter = (texTopLeft + texBottomRight) / 2;

            DrawRectangle(texTopLeft, texTopRight, texBottomLeft, texBottomRight, _gizmoColor);

            EditorGUI.BeginChangeCheck();

            DrawHandle(texTopLeft, out Vector3 handleTopLeft, out int topLeftId, out bool topLeftMoving);
            DrawHandle(texTopRight, out Vector3 handleTopRight, out int topRightId, out bool topRightMoving);
            DrawHandle(texBottomRight, out Vector3 handleBottomRight, out int bottomRightId, out bool bottomRightMoving);
            DrawHandle(texBottomLeft, out Vector3 handleBottomLeft, out int bottomLeftId, out bool bottomLeftMoving);
            DrawHandle(texCenter, out Vector3 handleCenter, out int centerId, out bool centerMoving);

            Undo.RecordObject(_monoBeh, ImageOverflowLocKey.undo_adjust_area_anchors.Localize());
            EditorGUI.EndChangeCheck();

            RectangleSize texSize;
            GetTexSize(
                rtTopLeft,
                rtTopRight,
                rtBottomLeft,
                rtWidth,
                rtHeight,
                texTopLeft,
                texTopRight,
                texBottomLeft,
                out texSize);

            Vector3 texTopRightNew = default;
            Vector3 texTopLeftNew = default;
            Vector3 texBottomRightNew = default;
            Vector3 texBottomLeftNew = default;

            float spriteCoof = spriteSize.x / spriteSize.y;
            RectangleSize newTexSize;

            if (topLeftMoving || HandleUtility.nearestControl == topLeftId)
            {
                FitTopLeftByMousePos(
                      texTopRight,
                      texBottomRight,
                      out texTopRightNew,
                      out texTopLeftNew,
                      out texBottomLeftNew,
                      spriteCoof,
                      out newTexSize);

                DrawRectangle(texTopLeftNew, texTopRightNew, texBottomLeftNew, texBottomRight, Color.red);
            }
            else if (topRightMoving || HandleUtility.nearestControl == topRightId)
            {
                FitTopRightByMousePos(
                    texTopLeft,
                    texBottomRight,
                    spriteCoof,
                    out texTopRightNew,
                    out texTopLeftNew,
                    out texBottomRightNew,
                    out newTexSize);

                DrawRectangle(texTopLeftNew, texTopRightNew, texBottomLeft, texBottomRightNew, Color.red);
            }
            else if (bottomLeftMoving || HandleUtility.nearestControl == bottomLeftId)
            {
                FitBottomLeftByMousePos(
                    texTopLeft,
                    texTopRight,
                    texBottomRight,
                    out texTopLeftNew,
                    out texBottomRightNew,
                    out texBottomLeftNew,
                    spriteCoof,
                    out newTexSize);

                DrawRectangle(texTopLeftNew, texTopRight, texBottomLeftNew, texBottomRightNew, Color.red);
            }
            else if (bottomRightMoving || HandleUtility.nearestControl == bottomRightId)
            {
                FitBottomRightByMousePos(
                    texTopLeft,
                    texTopRight,
                    out texTopRightNew,
                    out texBottomRightNew,
                    out texBottomLeftNew,
                    spriteCoof,
                    out newTexSize);

                DrawRectangle(texTopLeft, texTopRightNew, texBottomLeftNew, texBottomRightNew, Color.red);
            }

            Vector3 deltaTopLeft = default;
            Vector3 deltaTopRight = default;
            Vector3 deltaBottomLeft = default;
            Vector3 deltaBottomRight = default;
            Vector3 deltaCenter = default;

            if (_shiftPressed)
            {
                if (centerMoving)
                {
                    deltaCenter = _rectTransform.InverseTransformVector(handleCenter - texCenter);
                }
                else if (topLeftMoving)
                {
                    deltaTopLeft = _rectTransform.InverseTransformVector(texTopLeftNew - rtTopLeft);
                }
                else if (topRightMoving)
                {
                    deltaTopRight = _rectTransform.InverseTransformVector(texTopRightNew - rtTopRight);
                }
                else if (bottomLeftMoving)
                {
                    deltaBottomLeft = _rectTransform.InverseTransformVector(texBottomLeftNew - rtBottomLeft);
                }
                else if (bottomRightMoving)
                {
                    deltaBottomRight = _rectTransform.InverseTransformVector(texBottomRightNew - rtBottomRight);
                }
            }
            else
            {
                if (centerMoving)
                {
                    deltaCenter = _rectTransform.InverseTransformVector(handleCenter - texCenter);
                }
                else if (topLeftMoving)
                {
                    deltaTopLeft = _rectTransform.InverseTransformVector(handleTopLeft - rtTopLeft);
                }
                else if (topRightMoving)
                {
                    deltaTopRight = _rectTransform.InverseTransformVector(handleTopRight - rtTopRight);
                }
                else if (bottomLeftMoving)
                {
                    deltaBottomLeft = _rectTransform.InverseTransformVector(handleBottomLeft - rtBottomLeft);
                }
                else if (bottomRightMoving)
                {
                    deltaBottomRight = _rectTransform.InverseTransformVector(handleBottomRight - rtBottomRight);
                }
            }

            if (centerMoving)
            {
                _monoBeh.AreaAnchors = new Borders
                {
                    left = _monoBeh.AreaAnchors.left - deltaCenter.x / rtWidth,
                    right = _monoBeh.AreaAnchors.right + deltaCenter.x / rtWidth,
                    top = _monoBeh.AreaAnchors.top + deltaCenter.y / rtHeight,
                    bottom = _monoBeh.AreaAnchors.bottom - deltaCenter.y / rtHeight,
                };
            }
            else if (topLeftMoving)
            {
                _monoBeh.AreaAnchors = new Borders
                {
                    left = -deltaTopLeft.x / rtWidth,
                    right = _monoBeh.AreaAnchors.right,
                    top = deltaTopLeft.y / rtHeight,
                    bottom = _monoBeh.AreaAnchors.bottom,
                };
            }
            else if (topRightMoving)
            {
                _monoBeh.AreaAnchors = new Borders
                {
                    left = _monoBeh.AreaAnchors.left,
                    right = deltaTopRight.x / rtWidth,
                    top = deltaTopRight.y / rtHeight,
                    bottom = _monoBeh.AreaAnchors.bottom,
                };
            }
            else if (bottomLeftMoving)
            {
                _monoBeh.AreaAnchors = new Borders
                {
                    left = -deltaBottomLeft.x / rtWidth,
                    right = _monoBeh.AreaAnchors.right,
                    top = _monoBeh.AreaAnchors.top,
                    bottom = -deltaBottomLeft.y / rtHeight,
                };
            }
            else if (bottomRightMoving)
            {
                _monoBeh.AreaAnchors = new Borders
                {
                    left = _monoBeh.AreaAnchors.left,
                    right = deltaBottomRight.x / rtWidth,
                    top = _monoBeh.AreaAnchors.top,
                    bottom = -deltaBottomRight.y / rtHeight,
                };
            }

            DisplayPixelDistances(rtCorners, texTopLeft, texTopRight, texBottomLeft, texBottomRight);
        }

        private static void FitBottomLeftByMousePos(Vector3 texTopLeft, Vector3 texTopRight, Vector3 texBottomRight, out Vector3 texTopLeftNew, out Vector3 texBottomRightNew, out Vector3 texBottomLeftNew, float spriteCoof, out RectangleSize newTexSize)
        {
            Vector2 myPoint = GetCanvasMousePos();
            float widthBasedOnX = texTopRight.x - myPoint.x;
            float heightBasedOnY = texTopRight.y - myPoint.y;

            float widthFromHeight = heightBasedOnY * spriteCoof;
            float heightFromWidth = widthBasedOnX / spriteCoof;

            if (widthFromHeight < widthBasedOnX)
            {
                newTexSize.width = widthFromHeight;
                newTexSize.height = heightBasedOnY;
            }
            else
            {
                newTexSize.width = widthBasedOnX;
                newTexSize.height = heightFromWidth;
            }

            texTopLeftNew = new Vector3(texBottomRight.x - newTexSize.width, texTopLeft.y);
            texBottomRightNew = new Vector3(texBottomRight.x, texTopRight.y - newTexSize.height);
            texBottomLeftNew = new Vector3(texBottomRight.x - newTexSize.width, texTopRight.y - newTexSize.height);
        }

        private static void FitTopLeftByMousePos(
            Vector3 texTopRight,
            Vector3 texBottomRight,
            out Vector3 texTopRightNew,
            out Vector3 texTopLeftNew,
            out Vector3 texBottomLeftNew,
            float spriteCoof,
            out RectangleSize newTexSize)
        {
            Vector2 myPoint = GetCanvasMousePos();
            float widthBasedOnX = texTopRight.x - myPoint.x;
            float heightBasedOnY = myPoint.y - texBottomRight.y;

            float widthFromHeight = heightBasedOnY * spriteCoof;
            float heightFromWidth = widthBasedOnX / spriteCoof;

            if (widthFromHeight < widthBasedOnX)
            {
                newTexSize.width = widthFromHeight;
                newTexSize.height = heightBasedOnY;
            }
            else
            {
                newTexSize.width = widthBasedOnX;
                newTexSize.height = heightFromWidth;
            }

            texTopRightNew = new Vector3(texTopRight.x, texBottomRight.y + newTexSize.height);
            texTopLeftNew = new Vector3(texTopRight.x - newTexSize.width, texBottomRight.y + newTexSize.height);
            texBottomLeftNew = new Vector3(texTopRight.x - newTexSize.width, texBottomRight.y);
        }

        private static void FitBottomRightByMousePos(Vector3 texTopLeft, Vector3 texTopRight, out Vector3 texTopRightNew, out Vector3 texBottomRightNew, out Vector3 texBottomLeftNew, float spriteCoof, out RectangleSize newTexSize)
        {
            Vector2 myPoint = GetCanvasMousePos();
            float widthBasedOnX = myPoint.x - texTopLeft.x;
            float heightBasedOnY = texTopRight.y - myPoint.y;

            float widthFromHeight = heightBasedOnY * spriteCoof;
            float heightFromWidth = widthBasedOnX / spriteCoof;

            if (widthFromHeight < widthBasedOnX)
            {
                newTexSize.width = widthFromHeight;
                newTexSize.height = heightBasedOnY;
            }
            else
            {
                newTexSize.width = widthBasedOnX;
                newTexSize.height = heightFromWidth;
            }

            texTopRightNew = new Vector3(texTopLeft.x + newTexSize.width, texTopRight.y);
            texBottomRightNew = new Vector3(texTopLeft.x + newTexSize.width, texTopRight.y - newTexSize.height);
            texBottomLeftNew = new Vector3(texTopLeft.x, texTopRight.y - newTexSize.height);
        }

        private void GetTexSize(Vector3 rtTopLeft, Vector3 rtTopRight, Vector3 rtBottomLeft, float rtWidth, float rtHeight, Vector3 texTopLeft, Vector3 texTopRight, Vector3 texBottomLeft, out RectangleSize texSize)
        {
            float leftPx = _monoBeh.AreaAnchors.left * rtWidth;
            float rightPx = _monoBeh.AreaAnchors.right * rtWidth;
            float topPx = _monoBeh.AreaAnchors.top * rtHeight;
            float bottomPx = _monoBeh.AreaAnchors.bottom * rtHeight;

            if (texTopLeft.x < rtTopLeft.x && texTopRight.x > rtTopRight.x)
            {
                texSize.width = rtWidth + Mathf.Abs(leftPx) + Mathf.Abs(rightPx);
            }
            else if (texTopLeft.x < rtTopLeft.x)
            {
                texSize.width = rtWidth + Mathf.Abs(leftPx) - Mathf.Abs(rightPx);
            }
            else if (texTopRight.x > rtTopRight.x)
            {
                texSize.width = rtWidth - Mathf.Abs(leftPx) + rightPx;
            }
            else
            {
                texSize.width = rtWidth - Mathf.Abs(leftPx) - Mathf.Abs(rightPx);
            }

            if (texBottomLeft.y < rtBottomLeft.y && texTopLeft.y > rtTopLeft.y)
            {
                texSize.height = rtHeight + Mathf.Abs(bottomPx) + Mathf.Abs(topPx);
            }
            else if (texBottomLeft.y < rtBottomLeft.y)
            {
                texSize.height = rtHeight + Mathf.Abs(bottomPx) - Mathf.Abs(topPx);
            }
            else
            {
                texSize.height = rtHeight - Mathf.Abs(bottomPx) + topPx;
            }

            texSize.width = Mathf.Abs(texSize.width);
            texSize.height = Mathf.Abs(texSize.height);
        }

        private static void FitTopRightByMousePos(Vector3 texTopLeft, Vector3 texBottomRight, float spriteCoof, out Vector3 texTopRightNew, out Vector3 texTopLeftNew, out Vector3 texBottomRightNew, out RectangleSize newTexSize)
        {
            Vector2 myPoint = GetCanvasMousePos();
            float widthBasedOnX = myPoint.x - texTopLeft.x;
            float heightBasedOnY = myPoint.y - texBottomRight.y;

            float widthFromHeight = heightBasedOnY * spriteCoof;
            float heightFromWidth = widthBasedOnX / spriteCoof;

            if (widthFromHeight < widthBasedOnX)
            {
                newTexSize.width = widthFromHeight;
                newTexSize.height = heightBasedOnY;
            }
            else
            {
                newTexSize.width = widthBasedOnX;
                newTexSize.height = heightFromWidth;
            }

            texTopRightNew = new Vector3(texTopLeft.x + newTexSize.width, texBottomRight.y + newTexSize.height);
            texTopLeftNew = new Vector3(texTopLeft.x, texBottomRight.y + newTexSize.height);
            texBottomRightNew = new Vector3(texTopLeft.x + newTexSize.width, texBottomRight.y);
        }

        private void DrawRectangle(Vector3 texTopLeft, Vector3 texTopRight, Vector3 texBottomLeft, Vector3 texBottomRight, Color color)
        {
            DrawLineWithShadow(texTopLeft, texTopRight, color);
            DrawLineWithShadow(texTopRight, texBottomRight, color);
            DrawLineWithShadow(texBottomRight, texBottomLeft, color);
            DrawLineWithShadow(texBottomLeft, texTopLeft, color);
        }

        private void DisplayPixelDistances(Vector3[] rtCorners, Vector3 texTopLeft, Vector3 texTopRight, Vector3 texBottomLeft, Vector3 texBottomRight)
        {
            Vector3 centerTop = (texTopLeft + texTopRight) / 2;
            Vector3 centerBottom = (texBottomLeft + texBottomRight) / 2;
            Vector3 centerLeft = (texTopLeft + texBottomLeft) / 2;
            Vector3 centerRight = (texTopRight + texBottomRight) / 2;

            Vector3 rectTopEdge = new Vector3(centerTop.x, rtCorners[1].y, centerTop.z);
            Vector3 rectBottomEdge = new Vector3(centerBottom.x, rtCorners[0].y, centerBottom.z);
            Vector3 rectLeftEdge = new Vector3(rtCorners[0].x, centerLeft.y, centerLeft.z);
            Vector3 rectRightEdge = new Vector3(rtCorners[3].x, centerRight.y, centerRight.z);

            Vector3 rtTopLeft = rtCorners[1];
            Vector3 rtTopRight = rtCorners[2];
            Vector3 rtBottomRight = rtCorners[3];
            Vector3 rtBottomLeft = rtCorners[0];

            DrawLineAndExtend(centerTop, rectTopEdge, rtTopLeft, rtTopRight);
            DrawLineAndExtend(centerBottom, rectBottomEdge, rtBottomLeft, rtBottomRight);
            DrawLineAndExtend(centerLeft, rectLeftEdge, rtBottomLeft, rtTopLeft);
            DrawLineAndExtend(centerRight, rectRightEdge, rtBottomRight, rtTopRight);

            DisplayDistance(centerTop, rectTopEdge);
            DisplayDistance(centerBottom, rectBottomEdge);
            DisplayDistance(centerLeft, rectLeftEdge);
            DisplayDistance(centerRight, rectRightEdge);
        }

        private void DrawLineAndExtend(Vector3 start, Vector3 end, Vector3 edgeStart, Vector3 edgeEnd)
        {
            DrawLineWithShadow(start, end, _gizmoColor);

            if (start.x < edgeStart.x || start.x > edgeEnd.x || start.y < edgeStart.y || start.y > edgeEnd.y)
            {
                Vector3 intersection = end;

                if (start.x < edgeStart.x)
                {
                    intersection.x = edgeStart.x;
                }
                else if (start.x > edgeEnd.x)
                {
                    intersection.x = edgeEnd.x;
                }

                if (start.y < edgeStart.y)
                {
                    intersection.y = edgeStart.y;
                }
                else if (start.y > edgeEnd.y)
                {
                    intersection.y = edgeEnd.y;
                }

                DrawLineWithShadow(end, intersection, _gizmoColor);
            }
        }

        private void DrawLineWithShadow(Vector3 start, Vector3 end, Color color)
        {
            Color shadowColor = new Color(0, 0, 0, 0.3f);
            float handleSize = HandleUtility.GetHandleSize((start + end) / 2) * _shadowOffset;
            Vector3 shadowOffset;

            if (Mathf.Abs(start.y - end.y) > Mathf.Abs(start.x - end.x))
            {
                shadowOffset = new Vector3(handleSize, 0, 0);
            }
            else
            {
                shadowOffset = new Vector3(0, -handleSize, 0);
            }

            Handles.color = shadowColor;
            Handles.DrawLine(start + shadowOffset, end + shadowOffset);

            Handles.color = color;
            Handles.DrawLine(start, end);
        }

        private void DisplayDistance(Vector3 start, Vector3 end)
        {
            float distance = Vector3.Distance(start, end);
            Vector3 midpoint = (start + end) / 2;
            DrawLabel(midpoint, ImageOverflowLocKey.label_distance_in_pixels.Localize(distance));
        }

        private void DrawLabel(Vector3 position, string text)
        {
            float shadowSize = HandleUtility.GetHandleSize(position) * _shadowOffset;
            Vector3 shadowOffset = new Vector3(shadowSize, -shadowSize, 0);

            Handles.Label(position + shadowOffset, text, _labelShadowStyle);
            Handles.Label(position, text, _labelStyle);
        }

        private Color GetGizmoColor()
        {
            switch (_monoBeh.GizmosColor)
            {
                case GizmosColor.Red:
                    return Color.red;
                case GizmosColor.Blue:
                    return Color.blue;
                case GizmosColor.White:
                    return Color.white;
                default:
                    return Color.green;
            }
        }

        private GUIStyle GetLabelStyle()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = _gizmoColor;
            return labelStyle;
        }
        private GUIStyle GetLabelShadowStyle()
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.black;
            return labelStyle;
        }

        private static Vector2 GetCanvasMousePos()
        {
            Event e = Event.current;
            Vector2 mousePosition = e.mousePosition;
            Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            Plane xyPlane = new Plane(Vector3.forward, Vector3.zero);
            xyPlane.Raycast(worldRay, out float distance);
            Vector3 worldMousePos = worldRay.GetPoint(distance);
            return new Vector2(worldMousePos.x, worldMousePos.y);
        }

        private void DrawHandle(Vector3 pos, out Vector3 handlePos, out int handleId, out bool moving)
        {
            float handleSize = HandleUtility.GetHandleSize(_rectTransform.position) * 0.1f;
            handleId = GUIUtility.GetControlID(FocusType.Passive);

            float shadowSize = HandleUtility.GetHandleSize(pos) * _shadowOffset;
            Vector3 shadowOffset = new Vector3(shadowSize, -shadowSize, 0);

            Color shadowColor = new Color(0, 0, 0, 0.5f);
            Handles.color = shadowColor;
            Handles.CubeHandleCap(0, pos + shadowOffset, Quaternion.identity, handleSize, EventType.Repaint);

            Handles.color = _gizmoColor;
#if UNITY_2022_3_OR_NEWER
            handlePos = Handles.FreeMoveHandle(handleId, pos, handleSize, Vector3.zero, Handles.CubeHandleCap);
#else
            handlePos = Handles.FreeMoveHandle(handleId, pos, Quaternion.identity, handleSize, Vector3.zero, Handles.CubeHandleCap);
#endif
            moving = GUIUtility.hotControl == handleId;
        }

        private void UpdateSprite()
        {
            string assetPath = AssetDatabase.GetAssetPath(_monoBeh.Image.sprite);
            _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
#endif
