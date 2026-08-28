#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using System.IO;

namespace DA_Assets.UCC.Extensions
{
    public static class NodeExtensionsAssembly
    {
        public static bool IsSupportedRenderSize(this Vector2 sourceSize, float imageScale, out Vector2Int spriteSize, out Vector2Int renderSize)
        {
            spriteSize = (sourceSize * imageScale).ToVector2Int();

            int maxRenderSize = FcuConfig.MaxRenderSize;
            renderSize = spriteSize;

            if (renderSize.x <= maxRenderSize && renderSize.y <= maxRenderSize)
            {
                return true;
            }

            return false;
        }

        public static bool IsSvgExtension(this Node fobject)
        {
            if (fobject.Data.SpritePath.IsEmpty())
            {
                return false;
            }

            string spriteExt = Path.GetExtension(fobject.Data.SpritePath);
            if (spriteExt.StartsWith(".") && spriteExt.Length > 1)
                spriteExt = spriteExt.Remove(0, 1);

            bool isSvgSprite = spriteExt.ToLower() == ImageFormat.SVG.ToLower();
            return isSvgSprite;
        }

        public static void ExecuteWithTemporaryParent(this Node fobject, Transform tempChildsParent, Func<Node, GameObject> targetSelector, Action action)
        {
            GameObject target = targetSelector(fobject);
            List<Transform> children = new List<Transform>();
            List<int> siblingIndices = new List<int>();

            foreach (Transform child in target.transform)
            {
                children.Add(child);
                siblingIndices.Add(child.GetSiblingIndex());
            }

            foreach (Transform child in children)
            {
                child.SetParent(tempChildsParent);
            }

            action.Invoke();

            for (int i = 0; i < children.Count; i++)
            {
                children[i].SetParent(target.transform);
                children[i].SetSiblingIndex(siblingIndices[i]);
            }
        }

        public static bool IsSprite(this SyncData data)
        {
            return data.FcuImageType == FcuImageType.Downloadable || data.FcuImageType == FcuImageType.Generative;
        }

        public static bool IsSprite(this Node fobject)
        {
            return fobject.Data.FcuImageType == FcuImageType.Downloadable || fobject.Data.FcuImageType == FcuImageType.Generative;
        }

        public static bool IsCircle(this Node fobject)
        {
            if (!HasEqualRoundedSize(fobject))
                return false;

            if (fobject.Type == NodeType.ELLIPSE)
                return true;

            if (fobject.Type != NodeType.RECTANGLE)
                return false;

            return HasCircleCornerRadius(fobject);
        }

        private static bool HasEqualRoundedSize(Node fobject)
        {
            return fobject.Size.x.Round(FcuConfig.Rounding.IsCircle) == fobject.Size.y.Round(FcuConfig.Rounding.IsCircle);
        }

        private static bool HasCircleCornerRadius(Node fobject)
        {
            float side = fobject.Size.x.Round(FcuConfig.Rounding.IsCircle);
            float circleRadius = (side / 2f).Round(FcuConfig.Rounding.IsCircle);

            if (fobject.CornerRadiuses.IsEmpty())
            {
                float radius = fobject.CornerRadius.ToFloat().Round(FcuConfig.Rounding.IsCircle);
                return radius >= circleRadius;
            }

            if (fobject.CornerRadiuses.Count < 4)
                return false;

            float firstRadius = fobject.CornerRadiuses[0].Round(FcuConfig.Rounding.IsCircle);

            for (int i = 1; i < 4; i++)
            {
                if (fobject.CornerRadiuses[i].Round(FcuConfig.Rounding.IsCircle) != firstRadius)
                    return false;
            }

            return firstRadius >= circleRadius;
        }

        public static bool IsRectangle(this Node fobject)
        {
            if (fobject.Type == NodeType.RECTANGLE ||
                fobject.Type == NodeType.FRAME ||
                fobject.Type == NodeType.COMPONENT ||
                fobject.Type == NodeType.INSTANCE ||
                fobject.Type == NodeType.GROUP)
                return true;

            if (fobject.Type == NodeType.LINE && fobject.IsSupportedLine())
                return false;

            if (!fobject.Children.IsEmpty())
                return false;

            return true;
        }


        public static void SetFlagToAllChilds(this Node parent, Action<Node> action)
        {
            if (parent.IsDefault() || parent.Children.IsEmpty())
                return;

            foreach (Node child in parent.Children)
            {
                action(child);
                SetFlagToAllChilds(child, action);
            }
        }

        public static List<GradientAlphaKey> ToGradientAlphaKeys(this Paint gradient)
        {
            List<GradientAlphaKey> gradientColorKeys = new List<GradientAlphaKey>();

            if (gradient.GradientStops.IsEmpty())
            {
                return gradientColorKeys;
            }

            foreach (GradientStop gradientStop in gradient.GradientStops)
            {
                gradientColorKeys.Add(new GradientAlphaKey
                {
                    alpha = gradientStop.Color.a,
                    time = gradientStop.Position
                });
            }

            return gradientColorKeys;
        }

        public static List<GradientColorKey> ToGradientColorKeys(this Paint gradient)
        {
            List<GradientColorKey> gradientColorKeys = new List<GradientColorKey>();

            if (gradient.GradientStops.IsEmpty())
            {
                return gradientColorKeys;
            }

            foreach (GradientStop gradientStop in gradient.GradientStops)
            {
                gradientColorKeys.Add(new GradientColorKey
                {
                    color = gradientStop.Color,
                    time = gradientStop.Position
                });
            }

            return gradientColorKeys;
        }

        public static string GetText(this Node fobject)
        {




            if (fobject.Characters.IsEmpty())
                return string.Empty;

            return fobject.Characters
                .Replace("\\r", " ")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }


        public static bool IsSupportedLine(this Node fobject)
        {
            if (fobject.StrokeCap == StrokeCap.SQUARE)
                return true;

            if (fobject.StrokeCap == StrokeCap.ROUND && fobject.StrokeWeight >= 2f)
                return true;

            return false;
        }

        public static bool HasVisibleProperty<T>(this Node fobject, Expression<Func<Node, IEnumerable<T>>> propertySelector) where T : IVisible
        {
            var func = propertySelector.Compile();
            IEnumerable<T> collection = func(fobject);
            return !collection.IsEmpty() && collection.Any(item => item.Visible.ToBoolNullTrue());
        }

        public static bool TryGetLocalPosition(this Node fobject, out Vector2 rtPos)
        {
            try
            {
                float x = Mathf.Round(fobject.RelativeTransform[0][2].ToFloat() * 100f) / 100f;
                float y = Mathf.Round(-fobject.RelativeTransform[1][2].ToFloat() * 100f) / 100f;

                rtPos = new Vector2(x, y);
                return true;
            }
            catch
            {
                rtPos = new Vector2(0, 0);
                return false;
            }
        }
    }
}
#endif