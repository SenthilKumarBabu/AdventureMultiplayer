#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.UCC.Extensions
{
    public static class TransformExtensions
    {
        private const string SCALE_CONSTRAINT = "SCALE";

        public static List<Transform> GetAllParents(this Transform child)
        {
            List<Transform> parents = new List<Transform>();
            Transform currentParent = child.parent;

            while (currentParent != null)
            {
                parents.Add(currentParent);
                currentParent = currentParent.parent;
            }

            return parents;
        }

        public static float GetAngleFromMatrix(this Node fobject)
        {
            float rotRad;
            float rotDeg;
            float a;
            float b;

            if (fobject.RelativeTransform.IsEmpty() ||
                fobject.RelativeTransform.Count < 2 ||
                fobject.RelativeTransform[1].Count < 2 ||
                fobject.RelativeTransform[1][0] == null ||
                fobject.RelativeTransform[1][1] == null)
            {
                return 0;
            }
            else
            {
                a = (float)fobject.RelativeTransform[1][0];
                b = (float)fobject.RelativeTransform[1][1];
                rotRad = Mathf.Atan2(a, b).Round(FcuConfig.Rounding.AngleFromMatrix);
            }

            rotRad = -1 * rotRad;
            rotDeg = rotRad * (180f / (float)Mathf.PI);

            FcuLogger.Debug($"{nameof(GetAngleFromMatrix)} | {fobject.Data.NameHierarchy} | rotDeg: {rotDeg}\nb: {b}\na: {a}\nrotRad: {rotRad}", FcuDebugSettingsFlags.LogAngle);

            return rotDeg;
        }

        public static float GetAngleFromField(this Node fobject)
        {
            float rotRad;
            float rotDeg;
            float a = 0f;
            float b = 0f;

            rotRad = fobject.Rotation.HasValue ? fobject.Rotation.Value.Round(FcuConfig.Rounding.AngleFromMatrix) : 0;
            rotRad = -1 * rotRad;
            rotDeg = rotRad * (180f / (float)Mathf.PI);

            FcuLogger.Debug($"{nameof(GetAngleFromField)} | {fobject.Data.NameHierarchy} | hasValue: {fobject.Rotation.HasValue} | rotDeg: {rotDeg}\nb: {b}\na: {a}\nrotRad: {rotRad}", FcuDebugSettingsFlags.LogAngle);

            return rotDeg;
        }

        public static float GetFigmaRotationAngle(this Node fobject)
        {
            if (!ReferenceEquals(fobject, null) && fobject.Data != null && fobject.Data.HasTransformComputationCache)
                return fobject.Data.CachedFigmaRotationAngle;

            float angle = fobject.GetAngleFromField();

            if (angle == 0)
                angle = fobject.GetAngleFromMatrix();

            return angle;
        }

        public static AnchorType GetFigmaAnchor(this Node fobject)
        {
            string anchor = fobject.Constraints.Vertical + " " + fobject.Constraints.Horizontal;

            AnchorType anchorPreset;

            switch (anchor)
            {
                case "TOP LEFT":
                    anchorPreset = AnchorType.TopLeft;
                    break;
                case "BOTTOM LEFT":
                    anchorPreset = AnchorType.BottomLeft;
                    break;
                case "TOP_BOTTOM LEFT":
                    anchorPreset = AnchorType.VertStretchLeft;
                    break;
                case "CENTER LEFT":
                    anchorPreset = AnchorType.MiddleLeft;
                    break;
                case "SCALE LEFT":
                    anchorPreset = AnchorType.VertStretchLeft;
                    break;
                case "TOP RIGHT":
                    anchorPreset = AnchorType.TopRight;
                    break;
                case "BOTTOM RIGHT":
                    anchorPreset = AnchorType.BottomRight;
                    break;
                case "TOP_BOTTOM RIGHT":
                    anchorPreset = AnchorType.VertStretchRight;
                    break;
                case "CENTER RIGHT":
                    anchorPreset = AnchorType.MiddleRight;
                    break;
                case "SCALE RIGHT":
                    anchorPreset = AnchorType.VertStretchRight;
                    break;
                case "TOP LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchTop;
                    break;
                case "BOTTOM LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchBottom;
                    break;
                case "TOP_BOTTOM LEFT_RIGHT":
                    anchorPreset = AnchorType.StretchAll;
                    break;
                case "CENTER LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchMiddle;
                    break;
                case "SCALE LEFT_RIGHT":
                    anchorPreset = AnchorType.HorStretchMiddle;
                    break;
                case "TOP CENTER":
                    anchorPreset = AnchorType.TopCenter;
                    break;
                case "BOTTOM CENTER":
                    anchorPreset = AnchorType.BottomCenter;
                    break;
                case "TOP_BOTTOM CENTER":
                    anchorPreset = AnchorType.VertStretchCenter;
                    break;
                case "CENTER CENTER":
                    anchorPreset = AnchorType.MiddleCenter;
                    break;
                case "SCALE CENTER":
                    anchorPreset = AnchorType.VertStretchCenter;
                    break;
                case "TOP SCALE":
                    anchorPreset = AnchorType.HorStretchTop;
                    break;
                case "BOTTOM SCALE":
                    anchorPreset = AnchorType.HorStretchBottom;
                    break;
                case "TOP_BOTTOM SCALE":
                    anchorPreset = AnchorType.VertStretchCenter;
                    break;
                case "CENTER SCALE":
                    anchorPreset = AnchorType.HorStretchMiddle;
                    break;
                case "SCALE SCALE":
                    anchorPreset = AnchorType.StretchAll;
                    break;
                default:
                    anchorPreset = AnchorType.MiddleCenter;
                    break;
            }

            return anchorPreset;
        }

        public static bool HasFigmaScaleConstraint(this Node fobject)
        {
            return fobject.Constraints.Horizontal == SCALE_CONSTRAINT ||
                   fobject.Constraints.Vertical == SCALE_CONSTRAINT;
        }

        public static bool TryGetFigmaScaleAnchors(this Node fobject, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = default;
            anchorMax = default;

            if (!fobject.HasFigmaScaleConstraint() || fobject.Data == null)
                return false;

            if (TryGetSpriteFRectScaleAnchors(fobject, out anchorMin, out anchorMax))
                return true;

            Vector2 parentSize = fobject.Data.Parent.Size;
            if (parentSize.x <= 0 || parentSize.y <= 0)
                return false;

            if (!TryGetFigmaLocalPosition(fobject, out Vector2 localPosition))
                return false;

            Vector2 size = fobject.Size;

            anchorMin = new Vector2(
                localPosition.x / parentSize.x,
                (parentSize.y - localPosition.y - size.y) / parentSize.y);

            anchorMax = new Vector2(
                (localPosition.x + size.x) / parentSize.x,
                (parentSize.y - localPosition.y) / parentSize.y);

            return true;
        }

        public static void ApplyFigmaScaleAnchors(this RectTransform rectTransform, Node fobject)
        {
            if (!fobject.TryGetFigmaScaleAnchors(out Vector2 scaleAnchorMin, out Vector2 scaleAnchorMax))
                return;

            bool scaleHorizontal = fobject.Constraints.Horizontal == SCALE_CONSTRAINT;
            bool scaleVertical = fobject.Constraints.Vertical == SCALE_CONSTRAINT;

            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;

            if (scaleHorizontal)
            {
                anchorMin.x = scaleAnchorMin.x;
                anchorMax.x = scaleAnchorMax.x;
            }

            if (scaleVertical)
            {
                anchorMin.y = scaleAnchorMin.y;
                anchorMax.y = scaleAnchorMax.y;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;

            if (scaleHorizontal)
            {
                offsetMin.x = 0;
                offsetMax.x = 0;
            }

            if (scaleVertical)
            {
                offsetMin.y = 0;
                offsetMax.y = 0;
            }

            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static bool TryGetSpriteFRectScaleAnchors(Node fobject, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = default;
            anchorMax = default;

            if (!fobject.IsSprite() ||
                fobject.Data == null ||
                fobject.Data.Parent.Data == null)
                return false;

            FRect parentRect = fobject.Data.Parent.Data.FRect;
            FRect childRect = fobject.Data.FRect;

            Vector2 parentSize = parentRect.size;
            Vector2 size = childRect.size;

            if (parentSize.x <= 0 || parentSize.y <= 0 ||
                size.x <= 0 || size.y <= 0)
                return false;

            float localX = childRect.position.x - parentRect.position.x;
            float localY = parentRect.position.y - childRect.position.y;

            anchorMin = new Vector2(
                localX / parentSize.x,
                (parentSize.y - localY - size.y) / parentSize.y);

            anchorMax = new Vector2(
                (localX + size.x) / parentSize.x,
                (parentSize.y - localY) / parentSize.y);

            return true;
        }

        private static bool TryGetFigmaLocalPosition(Node fobject, out Vector2 localPosition)
        {
            localPosition = default;

            if (fobject.RelativeTransform.IsEmpty() ||
                fobject.RelativeTransform.Count < 2 ||
                fobject.RelativeTransform[0] == null ||
                fobject.RelativeTransform[1] == null ||
                fobject.RelativeTransform[0].Count < 3 ||
                fobject.RelativeTransform[1].Count < 3 ||
                fobject.RelativeTransform[0][2] == null ||
                fobject.RelativeTransform[1][2] == null)
            {
                return false;
            }

            localPosition = new Vector2(
                (float)fobject.RelativeTransform[0][2],
                (float)fobject.RelativeTransform[1][2]);

            return true;
        }
    }
}
#endif