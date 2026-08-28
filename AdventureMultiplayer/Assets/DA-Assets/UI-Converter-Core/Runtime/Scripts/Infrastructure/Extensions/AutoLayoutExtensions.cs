#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.UCC.Extensions
{
    public static class AutoLayoutExtensions
    {
        public static bool ShouldReverseAutoLayoutDrawOrder(this Node fobject)
        {
            if (fobject.ItemReverseZIndex != true)
                return false;

            if (fobject.LayoutWrap == LayoutWrap.WRAP)
                return false;

            if (fobject.LayoutMode != LayoutMode.HORIZONTAL &&
                fobject.LayoutMode != LayoutMode.VERTICAL)
            {
                return false;
            }

            return true;
        }

        public static bool TryFixSizeWithStroke(this Node fobject, float currentY, out float newY)
        {
            newY = 0;

            if (currentY > 0)
                return false;

            if (fobject.Strokes.IsEmpty())
                return false;

            if (!fobject.Strokes.Any(x => x.Visible.ToBoolNullTrue()))
                return false;

            newY = fobject.StrokeWeight;
            return true;
        }

        public static bool IsInsideAutoLayout(this Node parent, out HorizontalOrVerticalLayoutGroup layoutGroup)
        {
            layoutGroup = null;

            if (!parent.ContainsTag(FcuTag.AutoLayoutGroup))
                return false;

            if (parent.Data?.GameObject == null)
                return false;

            if (!parent.Data.GameObject.TryGetComponentSafe(out layoutGroup))
                return false;

            return true;
        }

        public static RectOffsetCustom AdjustPadding(this RectOffsetCustom padding, Node fobject, FRect parentRect, IReadOnlyList<Node> children)
        {
            float totalHorizontalPadding = padding.left + padding.right;
            float totalVerticalPadding = padding.top + padding.bottom;

            Vector2 parentSize = parentRect.size;
            Vector2[] childSizes = GetAutoLayoutChildSizes(parentRect, children);

            float maxChildWidth = childSizes.IsEmpty() ? 0 : childSizes.Max(c => c.x);
            float maxChildHeight = childSizes.IsEmpty() ? 0 : childSizes.Max(c => c.y);

            float contentWidth = maxChildWidth;
            float contentHeight = maxChildHeight;

            if (!childSizes.IsEmpty() && fobject.LayoutWrap != LayoutWrap.WRAP)
            {
                float gap = 0f;

                if (childSizes.Length > 1 && fobject.PrimaryAxisAlignItems != PrimaryAxisAlignItem.SPACE_BETWEEN)
                {
                    gap = fobject.ItemSpacing.ToFloat() * (childSizes.Length - 1);
                }

                switch (fobject.LayoutMode)
                {
                    case LayoutMode.HORIZONTAL:
                        contentWidth = childSizes.Sum(c => c.x) + gap;
                        contentHeight = maxChildHeight;
                        break;
                    case LayoutMode.VERTICAL:
                        contentWidth = maxChildWidth;
                        contentHeight = childSizes.Sum(c => c.y) + gap;
                        break;
                }
            }

            if (contentWidth + totalHorizontalPadding > parentSize.x && totalHorizontalPadding != 0)
            {
                float excessWidth = (contentWidth + totalHorizontalPadding) - parentSize.x;

                float leftRatio = padding.left / totalHorizontalPadding;
                float rightRatio = padding.right / totalHorizontalPadding;

                padding.left = Mathf.Max(0, padding.left - Mathf.CeilToInt(leftRatio * excessWidth));
                padding.right = Mathf.Max(0, padding.right - Mathf.CeilToInt(rightRatio * excessWidth));
            }

            if (contentHeight + totalVerticalPadding > parentSize.y && totalVerticalPadding != 0)
            {
                float excessHeight = (contentHeight + totalVerticalPadding) - parentSize.y;

                float topRatio = padding.top / totalVerticalPadding;
                float bottomRatio = padding.bottom / totalVerticalPadding;

                padding.top = Mathf.Max(0, padding.top - Mathf.CeilToInt(topRatio * excessHeight));
                padding.bottom = Mathf.Max(0, padding.bottom - Mathf.CeilToInt(bottomRatio * excessHeight));
            }

            return padding;
        }

        private static Vector2[] GetAutoLayoutChildSizes(FRect parentRect, IReadOnlyList<Node> children)
        {
            if (children == null || children.Count == 0)
                return Array.Empty<Vector2>();

            List<Vector2> childSizes = new List<Vector2>();

            foreach (Node child in children)
            {
                if (!ShouldUseChildForAutoLayoutSize(parentRect, child, children))
                    continue;

                childSizes.Add(child.Data.FRect.size);
            }

            return childSizes.ToArray();
        }

        public static bool ShouldUseChildForAutoLayoutSize(FRect parentRect, Node child, IReadOnlyList<Node> siblings)
        {
            if (ReferenceEquals(child, null))
                return false;

            if (child.LayoutPositioning == LayoutPositioning.ABSOLUTE)
                return false;

            if (child.Data?.GameObject != null &&
                child.Data.GameObject.TryGetComponentSafe(out LayoutElement layoutElement) &&
                layoutElement.ignoreLayout)
            {
                return false;
            }

            if (IsBackgroundLikeLayoutChild(parentRect, child, siblings))
                return false;

            return true;
        }

        private static bool IsBackgroundLikeLayoutChild(FRect parentRect, Node child, IReadOnlyList<Node> siblings)
        {
            if (!child.ContainsTag(FcuTag.Image))
                return false;

            bool hasOtherContent = siblings.Any(x =>
                !ReferenceEquals(x, child) &&
                !ReferenceEquals(x, null) &&
                x.LayoutPositioning != LayoutPositioning.ABSOLUTE &&
                !x.ContainsTag(FcuTag.Image));

            if (!hasOtherContent)
                return false;

            FRect childRect = child.Data.FRect;

            const float tolerance = 2f;

            float parentLeft = parentRect.position.x;
            float parentRight = parentRect.position.x + parentRect.size.x;
            float parentTop = parentRect.position.y;
            float parentBottom = parentRect.position.y + parentRect.size.y;

            float childLeft = childRect.position.x;
            float childRight = childRect.position.x + childRect.size.x;
            float childTop = childRect.position.y;
            float childBottom = childRect.position.y + childRect.size.y;

            bool coversHorizontally =
                childLeft <= parentLeft + tolerance &&
                childRight >= parentRight - tolerance;

            bool coversVertically =
                childTop <= parentTop + tolerance &&
                childBottom >= parentBottom - tolerance;

            return coversHorizontally && coversVertically;
        }


        public static TextAnchor GetHorLayoutAnchor(this Node fobject)
        {
            string aligment = "";
            aligment += fobject.PrimaryAxisAlignItems;
            aligment += " ";
            aligment += fobject.CounterAxisAlignItems;

            switch (aligment)
            {
                case "NONE NONE":
                    return TextAnchor.UpperLeft;
                case "SPACE_BETWEEN NONE":
                    return TextAnchor.UpperCenter;
                case "CENTER NONE":
                    return TextAnchor.UpperCenter;
                case "MAX NONE":
                    return TextAnchor.UpperRight;
                case "NONE CENTER":
                    return TextAnchor.MiddleLeft;
                case "NONE BASELINE":
                    return TextAnchor.MiddleLeft;
                case "SPACE_BETWEEN CENTER":
                    return TextAnchor.MiddleCenter;
                case "CENTER CENTER":
                    return TextAnchor.MiddleCenter;
                case "CENTER BASELINE":
                    return TextAnchor.MiddleCenter;
                case "MAX CENTER":
                    return TextAnchor.MiddleRight;
                case "MAX BASELINE":
                    return TextAnchor.MiddleRight;
                case "NONE MAX":
                    return TextAnchor.LowerLeft;
                case "SPACE_BETWEEN MAX":
                    return TextAnchor.LowerCenter;
                case "CENTER MAX":
                    return TextAnchor.LowerCenter;
                case "MAX MAX":
                    return TextAnchor.LowerRight;
            }

            Debug.LogError(FcuLocKey.log_unknown_aligment.Localize(aligment, fobject.Data.NameHierarchy));
            return TextAnchor.UpperLeft;
        }

        public static TextAnchor GetVertLayoutAnchor(this Node fobject)
        {
            string aligment = "";
            aligment += fobject.PrimaryAxisAlignItems;
            aligment += " ";
            aligment += fobject.CounterAxisAlignItems;

            switch (aligment)
            {
                case "NONE NONE":
                    return TextAnchor.UpperLeft;
                case "NONE CENTER":
                    return TextAnchor.UpperCenter;
                case "NONE MAX":
                    return TextAnchor.UpperRight;
                case "CENTER NONE":
                    return TextAnchor.MiddleLeft;
                case "SPACE_BETWEEN NONE":
                    return TextAnchor.MiddleLeft;
                case "CENTER CENTER":
                    return TextAnchor.MiddleCenter;
                case "SPACE_BETWEEN CENTER":
                    return TextAnchor.MiddleCenter;
                case "CENTER MAX":
                    return TextAnchor.MiddleRight;
                case "SPACE_BETWEEN MAX":
                    return TextAnchor.MiddleRight;
                case "MAX NONE":
                    return TextAnchor.LowerLeft;
                case "MAX CENTER":
                    return TextAnchor.LowerCenter;
                case "MAX MAX":
                    return TextAnchor.LowerRight;
            }

            Debug.LogError(FcuLocKey.log_unknown_aligment.Localize(aligment, fobject.Data.NameHierarchy));
            return TextAnchor.UpperLeft;
        }

#if UNITY_2021_3_OR_NEWER
        public static (string alignItems, string justifyContent) ToUITK(this TextAnchor textAnchor, bool isHorizontal = true)
        {

            var (primary, secondary) = textAnchor switch
            {
                TextAnchor.UpperLeft => ("flex-start", "flex-start"),
                TextAnchor.UpperCenter => ("flex-start", "center"),
                TextAnchor.UpperRight => ("flex-start", "flex-end"),

                TextAnchor.MiddleLeft => ("center", "flex-start"),
                TextAnchor.MiddleCenter => ("center", "center"),
                TextAnchor.MiddleRight => ("center", "flex-end"),

                TextAnchor.LowerLeft => ("flex-end", "flex-start"),
                TextAnchor.LowerCenter => ("flex-end", "center"),
                TextAnchor.LowerRight => ("flex-end", "flex-end"),
                _ => throw new NotImplementedException(),
            };


            return isHorizontal ? (primary, secondary) : (secondary, primary);
        }
#endif
        public static (bool childControlWidth, bool childControlHeight) GetChildControlByLayoutMode(this Node fobject, LayoutMode layoutMode)
        {
            bool childControlWidth = false;
            bool childControlHeight = false;

            HashSet<float?> layoutGrows = new HashSet<float?>();
            HashSet<LayoutAlign> layoutAligns = new HashSet<LayoutAlign>();

            foreach (Node child in fobject.Children)
            {
                if (child.LayoutPositioning == LayoutPositioning.ABSOLUTE)
                    continue;

                layoutGrows.Add(child.LayoutGrow);
                layoutAligns.Add(child.LayoutAlign);
            }

            if (layoutGrows.Count == 1)
            {
                if (layoutGrows.First() == 1)
                {
                    if (layoutMode == LayoutMode.VERTICAL)
                    {
                        childControlHeight = true;
                    }
                    else if (layoutMode == LayoutMode.HORIZONTAL)
                    {
                        childControlWidth = true;
                    }
                }
            }

            if (layoutAligns.Count == 1)
            {
                if (layoutAligns.First() == LayoutAlign.STRETCH)
                {
                    if (layoutMode == LayoutMode.VERTICAL)
                    {
                        childControlWidth = true;
                    }
                    else if (layoutMode == LayoutMode.HORIZONTAL)
                    {
                        childControlHeight = true;
                    }
                }
            }

            return (childControlWidth, childControlHeight);
        }

        public static float GetHorSpacing(this Node fobject)
        {
            float result = 0f;
            int state = 0;

            if (fobject.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
            {
                if (fobject.Data.ChildIndexes.IsEmpty())
                {
                    state = 1;
                    result = 0;
                }
                else if (fobject.Data.ChildIndexes.Count == 1)
                {
                    state = 2;
                    result = 0;
                }
                else
                {
                    state = 3;
                    List<Node> layoutChildren = fobject.Children
                        .Where(child => child.LayoutPositioning != LayoutPositioning.ABSOLUTE)
                        .ToList();

                    int childCount = layoutChildren.Count;
                    if (childCount <= 1)
                    {
                        state = 2;
                        result = 0;
                        return result;
                    }

                    int spacingCount = childCount - 1;
                    float parentWidth = fobject.Data.FRect.size.x;

                    float allChildsWidth = 0;

                    foreach (Node child in layoutChildren)
                    {
                        allChildsWidth += child.Data.FRect.size.x;
                    }

                    float spacingWidth = (parentWidth - allChildsWidth) / spacingCount;
                    result = spacingWidth;
                }
            }
            else
            {
                if (TryGetHorSpacingFromChildBounds(fobject, out float spacing))
                    return spacing;

                state = 4;
                result = fobject.ItemSpacing.ToFloat();
            }

            FcuLogger.Debug($"{nameof(GetHorSpacing)} | {state} | {result} | {fobject.Data.NameHierarchy}", FcuDebugSettingsFlags.LogAutoLayoutExtensions);

            return result;
        }

        public static float GetVertSpacing(this Node fobject)
        {
            if (fobject.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
            {
                if (fobject.Data.ChildIndexes.IsEmpty())
                {
                    return 0;
                }
                else if (fobject.Data.ChildIndexes.Count == 1)
                {
                    return 0;
                }
                else
                {
                    List<Node> layoutChildren = fobject.Children
                        .Where(child => child.LayoutPositioning != LayoutPositioning.ABSOLUTE)
                        .ToList();

                    int childCount = layoutChildren.Count;
                    if (childCount <= 1)
                    {
                        return 0;
                    }

                    int spacingCount = childCount - 1;
                    float parentHeight = fobject.Data.FRect.size.y;

                    float allChildsHeight = 0;

                    foreach (Node child in layoutChildren)
                    {
                        allChildsHeight += child.Data.FRect.size.y;
                    }

                    float spacingWidth = (parentHeight - allChildsHeight) / spacingCount;
                    return spacingWidth;
                }
            }
            else
            {
                if (TryGetVertSpacingFromChildBounds(fobject, out float spacing))
                    return spacing;

                return fobject.ItemSpacing.ToFloat();
            }
        }

        private static bool TryGetHorSpacingFromChildBounds(Node fobject, out float spacing)
        {
            spacing = 0f;

            if (fobject.LayoutWrap == LayoutWrap.WRAP)
                return false;

            List<Node> layoutChildren = fobject.Children
                .Where(child => child.LayoutPositioning != LayoutPositioning.ABSOLUTE)
                .ToList();

            if (layoutChildren.Count < 2)
                return false;

            Node first = layoutChildren[0];
            Node second = layoutChildren[1];

            if (first.AbsoluteBoundingBox.X.HasValue == false ||
                first.AbsoluteBoundingBox.Width.HasValue == false ||
                second.AbsoluteBoundingBox.X.HasValue == false)
            {
                return false;
            }

            spacing = second.AbsoluteBoundingBox.X.Value -
                (first.AbsoluteBoundingBox.X.Value + first.AbsoluteBoundingBox.Width.Value);

            return true;
        }

        private static bool TryGetVertSpacingFromChildBounds(Node fobject, out float spacing)
        {
            spacing = 0f;

            if (fobject.LayoutWrap == LayoutWrap.WRAP)
                return false;

            List<Node> layoutChildren = fobject.Children
                .Where(child => child.LayoutPositioning != LayoutPositioning.ABSOLUTE)
                .ToList();

            if (layoutChildren.Count < 2)
                return false;

            Node first = layoutChildren[0];
            Node second = layoutChildren[1];

            if (first.AbsoluteBoundingBox.Y.HasValue == false ||
                first.AbsoluteBoundingBox.Height.HasValue == false ||
                second.AbsoluteBoundingBox.Y.HasValue == false)
            {
                return false;
            }

            spacing = second.AbsoluteBoundingBox.Y.Value -
                (first.AbsoluteBoundingBox.Y.Value + first.AbsoluteBoundingBox.Height.Value);

            return true;
        }
    }
}
#endif