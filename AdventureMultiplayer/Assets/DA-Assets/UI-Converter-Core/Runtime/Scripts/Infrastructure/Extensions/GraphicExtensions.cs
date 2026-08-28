#if UNITY_EDITOR
using DA_Assets.UCC.Attributes;
using DA_Assets.UCC.Model;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

#pragma warning disable CS0219

namespace DA_Assets.UCC.Extensions
{
    public static class GraphicExtensions
    {


        public static bool GetBoundingSize(this Node fobject, out Vector2 size)
        {
            size = default;

            float? x = fobject.AbsoluteBoundingBox.Width;
            float? y = fobject.AbsoluteBoundingBox.Height;

            if (x == null || y == null)
            {
                return false;
            }

            float xR = (float)Math.Round(x.Value, FcuConfig.Rounding.BoundingSize);
            float yR = (float)Math.Round(y.Value, FcuConfig.Rounding.BoundingSize);

            size = new Vector2(xR, yR);
            return true;
        }

        public static bool GetBoundingPosition(this Node fobject, out Vector2 position)
        {
            position = default;

            float? x = fobject.AbsoluteBoundingBox.X;
            float? y = fobject.AbsoluteBoundingBox.Y;

            if (x == null || y == null)
            {
                return false;
            }

            float xR = (float)Math.Round(x.Value, FcuConfig.Rounding.BoundingSize);
            float yR = (float)Math.Round(y.Value, FcuConfig.Rounding.BoundingSize);

            position = new Vector2(xR, yR);
            return true;
        }

        public static bool GetRenderSize(this Node fobject, out Vector2 size)
        {
            size = default;

            float? x = fobject.AbsoluteRenderBounds.Width;
            float? y = fobject.AbsoluteRenderBounds.Height;

            if (x == null || y == null)
            {
                return false;
            }

            float xR = (float)Math.Round(x.Value, FcuConfig.Rounding.BoundingSize);
            float yR = (float)Math.Round(y.Value, FcuConfig.Rounding.BoundingSize);

            size = new Vector2(xR, yR);
            return true;
        }

        public static bool GetRenderPosition(this Node fobject, out Vector2 position)
        {
            position = default;

            float? x = fobject.AbsoluteRenderBounds.X;
            float? y = fobject.AbsoluteRenderBounds.Y;

            if (x == null || y == null)
            {
                return false;
            }

            float xR = (float)Math.Round(x.Value, FcuConfig.Rounding.BoundingSize);
            float yR = (float)Math.Round(y.Value, FcuConfig.Rounding.BoundingSize);

            position = new Vector2(xR, yR);
            return true;
        }

        public static bool IsZeroSize(this Node fobject)
        {
            if (fobject.AbsoluteBoundingBox.Width <= 0 || fobject.AbsoluteBoundingBox.Height <= 0)
            {
                return true;
            }

            return false;
        }

        public static bool IsVisible(this Node fobject) =>
            fobject.Visible.ToBoolNullTrue() && fobject.Opacity != 0;

        public static bool IsVisible(this Paint paint) => paint.Visible.ToBoolNullTrue();

        public static bool IsVisible(this Effect effect) => effect.Visible.ToBoolNullTrue();

        public static bool IsSingleLinearGradient(this Node fobject, out Paint paint)
        {
            paint = default;
            bool result = false;
            int reason = 0;

            if (fobject.Fills.IsEmpty())
            {
                result = false;
                reason = 1;
            }
            else
            {
                IEnumerable<Paint> enabledFills = fobject.Fills.Where(x => x.IsVisible());
                IEnumerable<Paint> linearFills = fobject.Fills.Where(x => x.IsVisible() && x.Type == PaintType.GRADIENT_LINEAR);

                if (!fobject.Strokes.IsEmpty())
                {
                    result = false;
                    reason = 2;
                }
                else if (!fobject.Effects.IsEmpty())
                {
                    result = false;
                    reason = 3;
                }
                else if (enabledFills.Count() > 1)
                {
                    result = false;
                    reason = 4;
                }
                else if (linearFills.Count() == 1)
                {
                    paint = linearFills.First();
                    result = true;
                    reason = 4;
                }
            }

            return result;
        }

        public static bool ContainsImageEmojiVideo(this Node fobject)
        {
            if (fobject.Fills.IsEmpty())
                return false;

            foreach (Paint paint in fobject.Fills)
            {
                if (!paint.IsVisible())
                    return false;

                if (paint.ImageRef.IsEmpty() == false || paint.GifRef.IsEmpty() == false)
                    return true;

                switch (paint.Type)
                {
                    case PaintType.IMAGE:
                    case PaintType.EMOJI:
                    case PaintType.VIDEO:
                        return true;
                }
            }

            return false;
        }

        public static bool IsSingleColor(this Node fobject, out Color color)
        {
            HashSet<Color32> rgbValues = new HashSet<Color32>(new RgbOnlyComparer());
            List<bool> flags = new List<bool>();

            IsSingleColorRecursive(fobject, flags, rgbValues);

            if (flags.Count > 0)
            {
                color = default;
                return false;
            }

            if (rgbValues.Count == 1)
            {
                Color32 c32 = rgbValues.First();
                color = new Color(c32.r / 255f, c32.g / 255f, c32.b / 255f, 1f);
                return true;
            }
            else
            {
                color = default;
                return false;
            }
        }

        private class RgbOnlyComparer : IEqualityComparer<Color32>
        {
            public bool Equals(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;
            public int GetHashCode(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;
        }

        private static void IsSingleColorRecursive(Node fobject, List<bool> flags, HashSet<Color32> rgbValues)
        {
            if (fobject.Fills.IsEmpty() == false)
            {
                foreach (Paint paint in fobject.Fills)
                {
                    if (!paint.IsVisible())
                        continue;

                    if (paint.ImageRef.IsEmpty() == false || paint.GifRef.IsEmpty() == false)
                    {
                        flags.Add(true);
                        return;
                    }

                    if (paint.Type.ToString().Contains("SOLID") == false)
                    {
                        flags.Add(true);
                        return;
                    }

                    rgbValues.Add(paint.Color);
                }
            }

            if (fobject.Strokes.IsEmpty() == false)
            {
                foreach (Paint paint in fobject.Strokes)
                {
                    if (!paint.IsVisible())
                        continue;

                    if (paint.ImageRef.IsEmpty() == false || paint.GifRef.IsEmpty() == false)
                    {
                        flags.Add(true);
                        return;
                    }

                    if (paint.Type.ToString().Contains("SOLID") == false)
                    {
                        flags.Add(true);
                        return;
                    }

                    rgbValues.Add(paint.Color);
                }
            }

            if (fobject.Effects.IsEmpty() == false)
            {
                foreach (Effect effect in fobject.Effects)
                {
                    if (!effect.IsVisible())
                        continue;

                    switch (effect.Type)
                    {

                        case EffectType.LAYER_BLUR:
                        case EffectType.BACKGROUND_BLUR:
                            break;


                        case EffectType.DROP_SHADOW:
                        case EffectType.INNER_SHADOW:
                            rgbValues.Add(effect.Color);
                            break;

                        default:
                            flags.Add(true);
                            return;
                    }
                }
            }

            if (fobject.Children.IsEmpty())
                return;

            foreach (Node item in fobject.Children)
            {
                if (item.Type == NodeType.TEXT)
                    continue;

                IsSingleColorRecursive(item, flags, rgbValues);
            }
        }

        public static bool ContainsRoundedCorners(this Node fobject)
        {
            return fobject.CornerRadius > 0 ||
                   (fobject.CornerRadiuses?.Any(radius => radius > 0)).ToBoolNullFalse() ||
                   fobject.TryGetVectorRoundedRectSourceBorder(out _);
        }

        public static bool IsArcDataFilled(this Node fobject)
        {
            if (fobject.ArcData.Equals(default(ArcData)))
            {
                return false;
            }

            return fobject.ArcData.EndingAngle < 6.28f;
        }

        public static bool IsRotationInvariantSolidCircle(this Node fobject)
        {
            if (!fobject.IsCircle() || fobject.IsArcDataFilled())
                return false;

            bool hasSolidPaint = false;

            if (!ContainsOnlySolidPaints(fobject.Fills, ref hasSolidPaint))
                return false;

            if (!ContainsOnlySolidPaints(fobject.Strokes, ref hasSolidPaint))
                return false;

            if (!hasSolidPaint)
                return false;

            return !HasAsymmetricShadow(fobject);
        }

        public static bool IsScaleInvariantSolidFillCircle(this Node fobject)
        {
            if (!fobject.IsCircle() || fobject.IsArcDataFilled())
                return false;

            if (!fobject.Effects.IsEmpty() && fobject.Effects.Any(effect => effect.IsVisible()))
                return false;

            if (!HasVisibleSolidFill(fobject))
                return false;

            bool hasSolidPaint = false;

            return ContainsOnlySolidPaints(fobject.Fills, ref hasSolidPaint) &&
                   ContainsOnlySolidPaints(fobject.Strokes, ref hasSolidPaint) &&
                   hasSolidPaint &&
                   fobject.Data.Graphic.HasSingleColor;
        }

        public static bool IsScaleInvariantAutoSlice9CircleSource(this Node fobject)
        {
            if (!fobject.IsScaleInvariantAutoSlice9())
                return false;

            if (!HasVisibleSolidFill(fobject))
                return false;

            bool hasSolidPaint = false;

            return ContainsOnlySolidPaints(fobject.Fills, ref hasSolidPaint) &&
                   ContainsOnlySolidPaints(fobject.Strokes, ref hasSolidPaint) &&
                   hasSolidPaint &&
                   fobject.Data.Graphic.HasSingleColor;
        }

        public static bool IsScaleInvariantAutoSlice9(this Node fobject)
        {
            if (!fobject.ContainsTag(FcuTag.AutoSlice9))
                return false;

            if (!fobject.Effects.IsEmpty() && fobject.Effects.Any(effect => effect.IsVisible()))
                return false;

            return HasUniformAutoSlice9Corners(fobject);
        }

        public static bool HasOnlySolidVisibleFillAndStroke(this Node fobject)
        {
            bool hasSolidPaint = false;

            return ContainsOnlySolidPaints(fobject.Fills, ref hasSolidPaint) &&
                   ContainsOnlySolidPaints(fobject.Strokes, ref hasSolidPaint) &&
                   hasSolidPaint;
        }

        public static bool CanMinimizeAutoSlice9Sprite(this Node fobject)
        {
            return fobject.Data.ManualWhiteColor || fobject.HasOnlySolidVisibleFillAndStroke();
        }

        public static bool IsDrawableSolidOverlayContainer(this Node fobject)
        {
            return fobject.TryGetDrawableSolidOverlayPaint(out _, out _);
        }

        public static bool TryGetDrawableSolidOverlayPaint(this Node fobject, out Paint paint, out Color color)
        {
            paint = default;
            color = default;

            if (!fobject.IsRectangle() || fobject.Children.IsEmpty())
                return false;

            if (HasVisiblePaints(fobject.Fills) || HasVisiblePaints(fobject.Strokes))
                return false;

            if (!fobject.Effects.IsEmpty() && fobject.Effects.Any(effect => effect.IsVisible()))
                return false;

            if (!fobject.GetBoundingPosition(out Vector2 parentPosition) ||
                !fobject.GetBoundingSize(out Vector2 parentSize))
                return false;

            bool found = false;

            foreach (Node child in fobject.Children)
            {
                if (!child.IsVisible())
                    continue;

                if (!TryGetFullSizeSolidOverlayChild(child, parentPosition, parentSize, out Paint childPaint, out Color childColor))
                    return false;

                if (found)
                {
                    if (!Approximately(childColor, color))
                        return false;

                    if (color.a < 0.999f)
                        return false;
                }
                else
                {
                    paint = childPaint;
                    color = childColor;
                    found = true;
                }
            }

            return found;
        }

        private static bool HasUniformAutoSlice9Corners(Node fobject)
        {
            if (fobject.TryGetVectorRoundedRectSourceBorder(out Vector4 vectorBorder))
                return IsUniformVector4(vectorBorder);

            GetCornerRadii(fobject, out float topLeft, out float topRight, out float bottomRight, out float bottomLeft);

            float maxRadius = Mathf.Min(fobject.Size.x, fobject.Size.y) / 2f;
            topLeft = Mathf.Min(topLeft, maxRadius);
            topRight = Mathf.Min(topRight, maxRadius);
            bottomRight = Mathf.Min(bottomRight, maxRadius);
            bottomLeft = Mathf.Min(bottomLeft, maxRadius);

            return Mathf.Approximately(topLeft, topRight) &&
                   Mathf.Approximately(topLeft, bottomRight) &&
                   Mathf.Approximately(topLeft, bottomLeft);
        }

        public static bool TryGetAutoSlice9SourceBorder(this Node fobject, out Vector4 border)
        {
            border = default;

            if (!fobject.ContainsTag(FcuTag.AutoSlice9))
                return false;

            if (fobject.TryGetVectorRoundedRectSourceBorder(out border))
                return true;

            float topLeft;
            float topRight;
            float bottomRight;
            float bottomLeft;
            GetCornerRadii(fobject, out topLeft, out topRight, out bottomRight, out bottomLeft);

            float extra = fobject.Data.Graphic.HasStroke ? fobject.StrokeWeight : 0f;
            extra += GetMaxAutoSliceEffectExtent(fobject);

            float halfWidth = fobject.Size.x / 2f;
            float halfHeight = fobject.Size.y / 2f;

            float left = Mathf.Min(Mathf.Max(topLeft, bottomLeft) + extra, halfWidth);
            float right = Mathf.Min(Mathf.Max(topRight, bottomRight) + extra, halfWidth);
            float top = Mathf.Min(Mathf.Max(topLeft, topRight) + extra, halfHeight);
            float bottom = Mathf.Min(Mathf.Max(bottomLeft, bottomRight) + extra, halfHeight);

            border = new Vector4(left, bottom, right, top);
            return true;
        }

        public static bool TryGetVectorRoundedRectSourceBorder(this Node fobject, out Vector4 border)
        {
            border = default;

            if (fobject.Type != NodeType.VECTOR)
                return false;

            if (fobject.FillGeometry.IsEmpty())
                return false;

            if (!TryGetLeadingVectorRoundRectRadius(fobject.FillGeometry[0].Path, out float radius))
                return false;

            border = new Vector4(radius, radius, radius, radius);
            return true;
        }

        private static bool TryGetLeadingVectorRoundRectRadius(string path, out float radius)
        {
            radius = 0f;

            if (string.IsNullOrWhiteSpace(path))
                return false;

            string[] parts = TokenizePath(path);

            if (parts.Length < 7 || parts[0] != "M" || parts[3] != "L" || parts[6] != "C")
                return false;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float moveX))
                return false;

            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float moveY))
                return false;

            if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float lineX))
                return false;

            if (!float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float lineY))
                return false;

            const float epsilon = 0.01f;
            float pathWidth = GetPathWidth(parts);

            if (Mathf.Abs(moveY) > epsilon || Mathf.Abs(lineY) > epsilon)
                return false;

            if (pathWidth <= epsilon || moveX <= epsilon || lineX <= moveX || lineX >= pathWidth - epsilon)
                return false;

            float leftRadius = moveX;
            float rightRadius = pathWidth - lineX;
            radius = Mathf.Max(leftRadius, rightRadius);

            return radius > epsilon;
        }

        private static string[] TokenizePath(string path)
        {
            return Regex.Matches(
                    path,
                    @"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")
                .Cast<Match>()
                .Select(match => match.Value)
                .ToArray();
        }

        private static float GetPathWidth(string[] parts)
        {
            float maxX = 0f;
            int numericIndex = 0;

            foreach (string part in parts)
            {
                if (!float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    continue;

                if (numericIndex % 2 == 0)
                {
                    maxX = Mathf.Max(maxX, value);
                }

                numericIndex++;
            }

            return maxX;
        }

        private static bool ContainsOnlySolidPaints(List<Paint> paints, ref bool hasSolidPaint)
        {
            if (paints.IsEmpty())
                return true;

            foreach (Paint paint in paints)
            {
                if (!paint.IsVisible())
                    continue;

                if (paint.ImageRef.IsEmpty() == false || paint.GifRef.IsEmpty() == false)
                    return false;

                if (paint.Type != PaintType.SOLID)
                    return false;

                hasSolidPaint = true;
            }

            return true;
        }

        private static bool TryGetFullSizeSolidOverlayChild(
            Node child,
            Vector2 parentPosition,
            Vector2 parentSize,
            out Paint paint,
            out Color color)
        {
            paint = default;
            color = default;

            if (child.Type != NodeType.RECTANGLE && child.Type != NodeType.FRAME)
                return false;

            if (!child.Children.IsEmpty() && child.Children.Any(x => x.IsVisible()))
                return false;

            if (HasVisiblePaints(child.Strokes))
                return false;

            if (!child.Effects.IsEmpty() && child.Effects.Any(effect => effect.IsVisible()))
                return false;

            if (child.ContainsImageEmojiVideo())
                return false;

            if (!child.GetBoundingPosition(out Vector2 childPosition) ||
                !child.GetBoundingSize(out Vector2 childSize))
                return false;

            if (!Approximately(childPosition, parentPosition) || !Approximately(childSize, parentSize))
                return false;

            List<Paint> visibleFills = child.Fills.IsEmpty()
                ? new List<Paint>()
                : child.Fills.Where(x => x.IsVisible()).ToList();

            if (visibleFills.Count != 1)
                return false;

            paint = visibleFills[0];

            if (paint.Type != PaintType.SOLID ||
                paint.ImageRef.IsEmpty() == false ||
                paint.GifRef.IsEmpty() == false)
                return false;

            color = paint.Color;

            if (child.Opacity.HasValue)
                color.a *= child.Opacity.Value;

            return true;
        }

        private static bool HasVisiblePaints(List<Paint> paints)
        {
            return !paints.IsEmpty() && paints.Any(paint => paint.IsVisible());
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                   Mathf.Approximately(a.y, b.y);
        }

        private static bool Approximately(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r) &&
                   Mathf.Approximately(a.g, b.g) &&
                   Mathf.Approximately(a.b, b.b) &&
                   Mathf.Approximately(a.a, b.a);
        }

        private static bool HasVisibleSolidFill(Node fobject)
        {
            if (fobject.Fills.IsEmpty())
                return false;

            return fobject.Fills.Any(paint => paint.IsVisible() && paint.Type == PaintType.SOLID);
        }

        private static bool HasAsymmetricShadow(Node fobject)
        {
            if (fobject.Effects.IsEmpty())
                return false;

            foreach (Effect effect in fobject.Effects)
            {
                if (!effect.IsVisible() || !effect.IsShadowType())
                    continue;

                if (Mathf.Abs(effect.Offset.x) > 0.001f || Mathf.Abs(effect.Offset.y) > 0.001f)
                    return true;
            }

            return false;
        }

        private static void GetCornerRadii(
            Node fobject,
            out float topLeft,
            out float topRight,
            out float bottomRight,
            out float bottomLeft)
        {
            if (fobject.CornerRadiuses.IsEmpty())
            {
                float radius = fobject.CornerRadius.ToFloat();
                topLeft = radius;
                topRight = radius;
                bottomRight = radius;
                bottomLeft = radius;
                return;
            }

            topLeft = fobject.CornerRadiuses[0];
            topRight = fobject.CornerRadiuses.Count > 1 ? fobject.CornerRadiuses[1] : topLeft;
            bottomRight = fobject.CornerRadiuses.Count > 2 ? fobject.CornerRadiuses[2] : topRight;
            bottomLeft = fobject.CornerRadiuses.Count > 3 ? fobject.CornerRadiuses[3] : bottomRight;
        }

        private static float GetMaxAutoSliceEffectExtent(Node fobject)
        {
            if (fobject.Effects.IsEmpty())
                return 0f;

            float maxEffectExtent = 0f;

            foreach (Effect effect in fobject.Effects)
            {
                if (!effect.IsVisible())
                    continue;

                switch (effect.Type)
                {
                    case EffectType.DROP_SHADOW:
                    case EffectType.INNER_SHADOW:
                        maxEffectExtent = Mathf.Max(maxEffectExtent, effect.Radius + (effect.Spread ?? 0f));
                        break;
                    case EffectType.LAYER_BLUR:
                        maxEffectExtent = Mathf.Max(maxEffectExtent, effect.Radius);
                        break;
                }
            }

            return maxEffectExtent;
        }

        private static bool IsUniformVector4(Vector4 value)
        {
            return Mathf.Approximately(value.x, value.y) &&
                   Mathf.Approximately(value.x, value.z) &&
                   Mathf.Approximately(value.x, value.w);
        }

        public static Paint GetFirstSolidPaint(this List<Paint> paints)
        {
            if (paints == null)
                return default;

            Paint fill = default;

            foreach (Paint paint in paints)
            {
                if (!paint.IsVisible())
                    continue;

                if (paint.Type == PaintType.SOLID)
                {
                    fill = paint;
                    fill.Color = fill.Color.SetAlpha(paint.Opacity);
                    break;
                }
            }

            return fill;
        }

        public static bool HasOnlyVisibleSolidPaints(this List<Paint> paints)
        {
            return paints.TryGetSolidCompositeColor(out _);
        }

        public static bool TryGetSolidCompositeColor(this List<Paint> paints, out Color color)
        {
            color = default;

            if (paints == null)
                return false;

            bool hasVisiblePaint = false;
            Color result = default;

            foreach (Paint paint in paints)
            {
                if (!paint.IsVisible())
                    continue;

                if (paint.Type != PaintType.SOLID)
                    return false;

                hasVisiblePaint = true;
                Color source = paint.Color;
                source.a = Mathf.Clamp01(source.a * (paint.Opacity ?? 1f));
                result = BlendSourceOver(result, source);
            }

            if (!hasVisiblePaint)
                return false;

            color = result;
            return true;
        }

        private static Color BlendSourceOver(Color destination, Color source)
        {
            float alpha = source.a + destination.a * (1f - source.a);

            if (Mathf.Approximately(alpha, 0f))
                return default;

            float red = (source.r * source.a + destination.r * destination.a * (1f - source.a)) / alpha;
            float green = (source.g * source.a + destination.g * destination.a * (1f - source.a)) / alpha;
            float blue = (source.b * source.a + destination.b * destination.a * (1f - source.a)) / alpha;

            return new Color(red, green, blue, alpha);
        }

        public static int GetPriority(this PaintType paintType)
        {
            Type type = paintType.GetType();

            MemberInfo[] memberInfo = type.GetMember(paintType.ToString());

            if (memberInfo.Length > 0)
            {
                PaintPriorityAttribute attribute = memberInfo[0].GetCustomAttribute<PaintPriorityAttribute>();

                if (attribute != null)
                {
                    return attribute.Priority;
                }
            }

            return 0;
        }
    }
}
#endif