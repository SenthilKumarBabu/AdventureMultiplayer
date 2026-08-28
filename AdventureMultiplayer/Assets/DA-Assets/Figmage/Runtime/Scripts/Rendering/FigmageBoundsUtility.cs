using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.Figmage
{
    public static class FigmageBoundsUtility
    {
        const float BlurOverflowMultiplier = 2f;

        public static Rect CalculateRenderBounds(FigmageNode node)
        {
            Rect contentBounds = ToRect(node.AbsoluteBoundingBox);
            Rect renderBounds = IsValid(node.AbsoluteRenderBounds)
                ? ToRect(node.AbsoluteRenderBounds)
                : contentBounds;

            if (!Approximately(renderBounds, contentBounds))
                return renderBounds;

            float strokeOverflow = GetStrokeOverflow(node);
            if (strokeOverflow > 0f)
                renderBounds = Encapsulate(renderBounds, Expand(contentBounds, strokeOverflow));

            if (node.Effects == null)
                return renderBounds;

            foreach (FigmageEffect effect in node.Effects)
            {
                if (effect == null || !effect.Visible)
                    continue;

                switch (effect.Type)
                {
                    case FigmageEffectType.DropShadow:
                        renderBounds = Encapsulate(renderBounds, CalculateDropShadowBounds(contentBounds, effect, strokeOverflow));
                        break;
                    case FigmageEffectType.LayerBlur:
                        renderBounds = Encapsulate(renderBounds, Expand(contentBounds, effect.Radius * BlurOverflowMultiplier));
                        break;
                }
            }

            return renderBounds;
        }

        static float GetStrokeOverflow(FigmageNode node)
        {
            if (node.StrokeWeight <= 0f || !HasVisiblePaint(node.Strokes))
                return 0f;

            switch (node.StrokeAlign)
            {
                case FigmageStrokeAlign.Center:
                    return node.StrokeWeight * 0.5f;
                case FigmageStrokeAlign.Outside:
                    return node.StrokeWeight;
                default:
                    return 0f;
            }
        }

        static bool HasVisiblePaint(List<FigmagePaint> paints)
        {
            if (paints == null)
                return false;

            for (int i = 0; i < paints.Count; i++)
            {
                if (paints[i] != null && paints[i].Visible)
                    return true;
            }

            return false;
        }

        static Rect CalculateDropShadowBounds(Rect contentBounds, FigmageEffect effect, float strokeOverflow)
        {
            float shadowOverflow = strokeOverflow + Mathf.Max(0f, effect.Spread) + effect.Radius * BlurOverflowMultiplier;
            Rect shadowBounds = Expand(contentBounds, shadowOverflow);
            shadowBounds.position += effect.Offset;
            return shadowBounds;
        }

        static Rect Expand(Rect rect, float amount)
        {
            if (amount <= 0f)
                return rect;

            return new Rect(
                rect.xMin - amount,
                rect.yMin - amount,
                rect.width + amount * 2f,
                rect.height + amount * 2f);
        }

        static Rect Encapsulate(Rect current, Rect other)
        {
            float xMin = Mathf.Min(current.xMin, other.xMin);
            float yMin = Mathf.Min(current.yMin, other.yMin);
            float xMax = Mathf.Max(current.xMax, other.xMax);
            float yMax = Mathf.Max(current.yMax, other.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        static Rect ToRect(FigmageRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        static bool IsValid(FigmageRect rect)
        {
            return rect.Width > 0f && rect.Height > 0f;
        }

        static bool Approximately(Rect a, Rect b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                   Mathf.Approximately(a.y, b.y) &&
                   Mathf.Approximately(a.width, b.width) &&
                   Mathf.Approximately(a.height, b.height);
        }
    }
}
