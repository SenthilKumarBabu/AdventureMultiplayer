using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace DA_Assets.Figmage
{
    sealed partial class FigmageRenderer
    {
        static void Traverse(FigmageNode node, Action<FigmageNode> visitor)
        {
            visitor?.Invoke(node);

            if (node.Children == null)
            {
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                Traverse(node.Children[i], visitor);
            }
        }

        static bool IsVisiblePaint(FigmagePaint paint)
        {
            return paint != null && paint.Visible;
        }

        static bool IsVisibleEffect(FigmageEffect effect)
        {
            return effect != null && effect.Visible;
        }

        static int MapPaintKind(FigmagePaintType paintType)
        {
            switch (paintType)
            {
                case FigmagePaintType.Solid:
                    return LayerKindSolid;
                case FigmagePaintType.GradientLinear:
                    return LayerKindLinearGradient;
                case FigmagePaintType.GradientRadial:
                    return LayerKindRadialGradient;
                case FigmagePaintType.GradientAngular:
                    return LayerKindAngularGradient;
                case FigmagePaintType.GradientDiamond:
                    return LayerKindDiamondGradient;
                default:
                    return -1;
            }
        }

        static int MapBlendMode(string blendMode)
        {
            switch ((blendMode ?? string.Empty).ToUpperInvariant())
            {
                case BlendModeScreen:
                    return BlendModeIndexScreen;
                case BlendModeLinearDodge:
                case BlendModePlusLighter:
                    return BlendModeIndexLinearDodge;
                case BlendModeOverlay:
                    return BlendModeIndexOverlay;
                default:
                    return BlendModeIndexNormal;
            }
        }

        static float MapStrokeAlign(FigmageStrokeAlign strokeAlign)
        {
            switch (strokeAlign)
            {
                case FigmageStrokeAlign.Inside:
                    return StrokeAlignInsideValue;
                case FigmageStrokeAlign.Center:
                    return StrokeAlignCenterValue;
                case FigmageStrokeAlign.Outside:
                    return StrokeAlignOutsideValue;
                default:
                    return StrokeAlignCenterValue;
            }
        }

        static float GetOuterStrokeExpansion(float strokeAlign, float scaledStrokeWeight)
        {
            if (strokeAlign > StrokeAlignOutsideThreshold)
            {
                return scaledStrokeWeight * 2f;
            }

            if (strokeAlign > StrokeAlignCenterThreshold)
            {
                return scaledStrokeWeight;
            }

            return 0f;
        }

        bool TryBuildRenderSpec(FigmageNode node, float scale, out ShaderRenderSpec spec, out string error)
        {
            spec = null;
            error = null;

            if (!TryGetRect(node.AbsoluteBoundingBox, out Rect contentBounds))
            {
                error = MissingAbsoluteBoundingBoxError;
                return false;
            }

            Rect renderBounds = TryGetRect(node.AbsoluteRenderBounds, out Rect renderRect)
                ? renderRect
                : contentBounds;
            Vector2 localSize = ResolveNodeSize(node, contentBounds);
            float rotationRadians = ExtractRotationRadians(node);
            float strokeAlign = MapStrokeAlign(node.StrokeAlign);
            Vector4 unscaledCornerRadii = GetCornerRadii(node, 1f, localSize);
            Vector4[] fillPathPoints = ExtractPathPoints(node.FillGeometry, localSize, unscaledCornerRadii, node.CornerSmoothing);
            Vector4[] strokePathPoints = ExtractPathPoints(node.StrokeGeometry, localSize, unscaledCornerRadii, node.CornerSmoothing);
            Bounds sampledBounds = SamplePathBounds(fillPathPoints, localSize.x * scale, localSize.y * scale, rotationRadians);

            if (localSize.x <= 0f || localSize.y <= 0f || renderBounds.width <= 0f || renderBounds.height <= 0f)
            {
                error = EmptyNodeBoundsError;
                return false;
            }

            List<LayerSpec> fills = BuildLayerSpecs(node.Fills);
            List<LayerSpec> strokes = HasRenderableStroke(node, strokePathPoints)
                ? BuildLayerSpecs(node.Strokes)
                : new List<LayerSpec>();
            if (strokes.Count > 1 && strokes[1].BlendMode == BlendModeIndexScreen)
            {
                strokes[1].Opacity = Mathf.Clamp01(strokes[1].Opacity * SecondaryScreenStrokeOpacityScale);
            }

            if (fills.Count == 0 && strokes.Count == 0)
            {
                error = MissingSupportedPaintsError;
                return false;
            }

            spec = new ShaderRenderSpec
            {
                CanvasSize = new Vector2Int(
                    Mathf.Max(1, Mathf.CeilToInt(renderBounds.width * scale)),
                    Mathf.Max(1, Mathf.CeilToInt(renderBounds.height * scale))),
                ShapeSize = new Vector2(
                    Mathf.Max(1f, localSize.x * scale),
                    Mathf.Max(1f, localSize.y * scale)),
                ShapeOffset = ComputeShapeCenterOffset(contentBounds, renderBounds, scale) - (Vector2)sampledBounds.center,
                CornerRadii = unscaledCornerRadii * scale,
                CornerSmoothing = Mathf.Clamp01(node.CornerSmoothing),
                ShapeOpacity = Mathf.Clamp01(node.Opacity),
                ShapeRotationRadians = rotationRadians,
                StrokeWeight = Mathf.Max(0f, node.StrokeWeight * scale),
                StrokeAlign = strokeAlign,
                Antialiasing = 1f,
                SampledBoundsCenter = (Vector2)sampledBounds.center,
                SampledBoundsSize = (Vector2)sampledBounds.size,
                FillPathPoints = fillPathPoints,
                StrokePathPoints = strokePathPoints,
                Fills = fills,
                Strokes = strokes,
                DropShadows = GetShadowSpecs(node, FigmageEffectType.DropShadow, scale),
                InnerShadow = GetShadowSpec(node, FigmageEffectType.InnerShadow, scale),
                LayerBlur = GetLayerBlurSpec(node, scale)
            };

            spec.LayerBlurTuningMode = ResolveLayerBlurTuningMode(node);

            return true;
        }

        static bool HasRenderableStroke(FigmageNode node, Vector4[] strokePathPoints)
        {
            bool hasStrokePath = strokePathPoints != null && strokePathPoints.Length > 1;
            return (node.StrokeWeight > 0f || hasStrokePath) && node.Strokes != null && node.Strokes.Any(IsVisiblePaint);
        }

        static int ResolveLayerBlurTuningMode(FigmageNode node)
        {
            if (node.Effects == null)
                return LayerBlurTuningModeDefault;

            int layerBlurIndex = node.Effects.FindIndex(x => IsVisibleEffect(x) && x.Type == FigmageEffectType.LayerBlur);
            if (layerBlurIndex < 0)
                return LayerBlurTuningModeDefault;
            FigmageEffect layerBlur = node.Effects[layerBlurIndex];
            if (layerBlur.StartRadius <= 0f)
                return LayerBlurTuningModeDefault;

            List<FigmagePaint> visibleFills = (node.Fills ?? new List<FigmagePaint>()).Where(IsVisiblePaint).ToList();
            List<FigmageEffect> visibleDropShadows = node.Effects.Where(x => IsVisibleEffect(x) && x.Type == FigmageEffectType.DropShadow).ToList();
            bool hasVisibleInnerShadow = node.Effects.Any(x => IsVisibleEffect(x) && x.Type == FigmageEffectType.InnerShadow);

            if (layerBlur.StartRadius > 0f &&
                visibleDropShadows.Count == 2 &&
                !hasVisibleInnerShadow &&
                visibleFills.Count == 1 &&
                string.Equals(visibleFills[0].BlendMode, BlendModeNormal, StringComparison.OrdinalIgnoreCase) &&
                node.StrokeAlign == FigmageStrokeAlign.Center)
            {
                Vector2 firstOffset = visibleDropShadows[0].Offset;
                Vector2 secondOffset = visibleDropShadows[1].Offset;
                bool sameXOffset = Mathf.Abs(firstOffset.x - secondOffset.x) <= OpposingShadowOffsetEpsilon;
                bool opposingYOffset = firstOffset.y * secondOffset.y < 0f;

                if (sameXOffset && opposingYOffset)
                    return LayerBlurTuningModeOverlay;
            }

            if (visibleDropShadows.Count == 1 &&
                visibleFills.Count == 1 &&
                !string.Equals(visibleFills[0].BlendMode, BlendModeNormal, StringComparison.OrdinalIgnoreCase))
            {
                return LayerBlurTuningModeShadow;
            }

            return LayerBlurTuningModeDefault;
        }

        static bool TryGetRect(FigmageRect box, out Rect rect)
        {
            rect = default;

            if (box.Width <= 0f || box.Height <= 0f)
            {
                return false;
            }

            rect = new Rect(box.X, box.Y, box.Width, box.Height);
            return true;
        }

        static void UpdateShapeOffsetForCanvas(ShaderRenderSpec spec, ExportEntry entry, float scale)
        {
            if (entry == null ||
                !entry.UseExternalCanvasBounds ||
                !TryGetRect(entry.ContentBounds, out Rect contentBounds) ||
                !TryGetRect(entry.CanvasBounds, out Rect canvasBounds))
            {
                spec.ShapeOffset = -spec.SampledBoundsCenter;
                return;
            }

            bool canvasTighterThanContent =
                canvasBounds.width < contentBounds.width - 0.01f ||
                canvasBounds.height < contentBounds.height - 0.01f;

            if (canvasTighterThanContent)
            {
                spec.ShapeOffset = HasCanvasExpandingEffects(spec)
                    ? ComputeShapeCenterOffset(contentBounds, canvasBounds, scale)
                    : -spec.SampledBoundsCenter;
                return;
            }

            Vector2 canvasCenterOffset = ComputeShapeCenterOffset(contentBounds, canvasBounds, scale);
            spec.ShapeOffset = canvasCenterOffset;
        }

        static Vector2 ComputeShapeCenterOffset(Rect contentBounds, Rect renderBounds, float scale)
        {
            float localCenterX = (contentBounds.x - renderBounds.x) + contentBounds.width * 0.5f;
            float localCenterYTop = (contentBounds.y - renderBounds.y) + contentBounds.height * 0.5f;
            // Shader space expects the shape center relative to the canvas center.
            // If the content sits further right/down inside renderBounds because Figma
            // has left/top overflow, the offset must also be right/down. The previous
            // sign here mirrored asymmetric overflow and produced PNG padding on the
            // opposite sides from Figma absoluteRenderBounds.
            float offsetX = (localCenterX - renderBounds.width * 0.5f) * scale;
            float offsetY = (localCenterYTop - renderBounds.height * 0.5f) * scale;
            return new Vector2(offsetX, offsetY);
        }

        static bool HasCanvasExpandingEffects(ShaderRenderSpec spec)
        {
            return spec.DropShadows.Count > 0 || spec.LayerBlur.Enabled;
        }

        static Vector2 ResolveNodeSize(FigmageNode node, Rect bbox)
        {
            if (node.Size.x > 0f && node.Size.y > 0f)
            {
                return node.Size;
            }

            return new Vector2(
                Mathf.Max(1f, bbox.width),
                Mathf.Max(1f, bbox.height));
        }

        static Vector4 GetCornerRadii(FigmageNode node, float scale, Vector2 localSize)
        {
            List<float> radii = node.CornerRadiuses;
            if (radii != null && radii.Count == 4)
            {
                return NormalizeCornerRadii(
                    Mathf.Max(0f, radii[0] * scale),
                    Mathf.Max(0f, radii[1] * scale),
                    Mathf.Max(0f, radii[2] * scale),
                    Mathf.Max(0f, radii[3] * scale),
                    localSize * scale);
            }

            float radius = Mathf.Max(0f, node.CornerRadius * scale);
            return NormalizeCornerRadii(radius, radius, radius, radius, localSize * scale);
        }

        static Vector4 NormalizeCornerRadii(float topLeft, float topRight, float bottomRight, float bottomLeft, Vector2 size)
        {
            float scale = 1f;
            scale = MinPositiveScale(scale, size.x, topLeft + topRight);
            scale = MinPositiveScale(scale, size.y, topRight + bottomRight);
            scale = MinPositiveScale(scale, size.x, bottomLeft + bottomRight);
            scale = MinPositiveScale(scale, size.y, topLeft + bottomLeft);

            return new Vector4(
                topLeft * scale,
                topRight * scale,
                bottomRight * scale,
                bottomLeft * scale);
        }

        static float MinPositiveScale(float current, float length, float radiusSum)
        {
            if (radiusSum <= 0f)
                return current;

            return Mathf.Min(current, Mathf.Max(0f, length) / radiusSum);
        }

        static float ExtractRotationRadians(FigmageNode node)
        {
            if (node.RelativeTransform != null && node.RelativeTransform.Count >= 2 &&
                node.RelativeTransform[0] != null && node.RelativeTransform[0].Count >= 1 &&
                node.RelativeTransform[1] != null && node.RelativeTransform[1].Count >= 1)
            {
                float m00 = node.RelativeTransform[0][0];
                float m10 = node.RelativeTransform[1][0];
                return Mathf.Atan2(-m10, m00);
            }

            float rotation = node.Rotation;
            return Mathf.Abs(rotation) <= FullRotationRadians + ArcComputationEpsilon
                ? -rotation
                : -rotation * Mathf.Deg2Rad;
        }

        static Vector4[] ExtractPathPoints(List<FigmageGeometry> geometry, Vector2 size, Vector4 cornerRadii, float cornerSmoothing)
        {
            if (size.x <= 0f || size.y <= 0f)
            {
                return Array.Empty<Vector4>();
            }

            string path = ExtractGeometryPath(geometry);
            if (string.IsNullOrWhiteSpace(path) && ShouldUseSmoothedCornerPath(cornerRadii, cornerSmoothing))
            {
                path = BuildSmoothedRoundedRectPath(size.x, size.y, cornerRadii, cornerSmoothing);
            }

            return string.IsNullOrWhiteSpace(path)
                ? Array.Empty<Vector4>()
                : SampleSvgPath(path, size.x, size.y);
        }

        static string ExtractGeometryPath(List<FigmageGeometry> geometry)
        {
            if (geometry == null)
            {
                return null;
            }

            List<string> paths = null;
            for (int i = 0; i < geometry.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(geometry[i].Path))
                {
                    paths ??= new List<string>();
                    paths.Add(geometry[i].Path);
                }
            }

            return paths == null ? null : string.Join(" ", paths);
        }

        static bool ShouldUseSmoothedCornerPath(Vector4 cornerRadii, float cornerSmoothing)
        {
            return cornerSmoothing > 0f &&
                   (cornerRadii.x > 0f || cornerRadii.y > 0f || cornerRadii.z > 0f || cornerRadii.w > 0f);
        }

        static string BuildSmoothedRoundedRectPath(float width, float height, Vector4 radii, float cornerSmoothing)
        {
            float[] budgets = CalculateCornerSmoothingBudgets(radii, width, height);
            SmoothedCornerPathParams topLeft = GetSmoothedCornerPathParams(radii.x, cornerSmoothing, budgets[0]);
            SmoothedCornerPathParams topRight = GetSmoothedCornerPathParams(radii.y, cornerSmoothing, budgets[1]);
            SmoothedCornerPathParams bottomRight = GetSmoothedCornerPathParams(radii.z, cornerSmoothing, budgets[2]);
            SmoothedCornerPathParams bottomLeft = GetSmoothedCornerPathParams(radii.w, cornerSmoothing, budgets[3]);

            return FormattableString.Invariant($@"
                M {width - topRight.P} 0
                {DrawTopRightPath(topRight)}
                L {width} {height - bottomRight.P}
                {DrawBottomRightPath(bottomRight)}
                L {bottomLeft.P} {height}
                {DrawBottomLeftPath(bottomLeft)}
                L 0 {topLeft.P}
                {DrawTopLeftPath(topLeft)}
                Z");
        }

        static SmoothedCornerPathParams GetSmoothedCornerPathParams(float cornerRadius, float cornerSmoothing, float budget)
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSmoothing = Mathf.Clamp01(cornerSmoothing);
            if (cornerRadius <= 0f || budget <= 0f)
            {
                return new SmoothedCornerPathParams { P = 0f };
            }

            float p = (1f + cornerSmoothing) * cornerRadius;
            float maxCornerSmoothing = budget / cornerRadius - 1f;
            cornerSmoothing = Mathf.Min(cornerSmoothing, maxCornerSmoothing);
            p = Mathf.Min(p, budget);

            float arcMeasure = 90f * (1f - cornerSmoothing);
            float arcSectionLength = Mathf.Sin(arcMeasure * 0.5f * Mathf.Deg2Rad) * cornerRadius * Mathf.Sqrt(2f);
            float angleAlpha = (90f - arcMeasure) * 0.5f;
            float p3ToP4Distance = cornerRadius * Mathf.Tan(angleAlpha * 0.5f * Mathf.Deg2Rad);
            float angleBeta = 45f * cornerSmoothing;
            float c = p3ToP4Distance * Mathf.Cos(angleBeta * Mathf.Deg2Rad);
            float d = c * Mathf.Tan(angleBeta * Mathf.Deg2Rad);
            float b = (p - arcSectionLength - c - d) / 3f;
            float a = 2f * b;

            return new SmoothedCornerPathParams
            {
                A = a,
                B = b,
                C = c,
                D = d,
                P = p,
                CornerRadius = cornerRadius,
                ArcSectionLength = arcSectionLength
            };
        }

        static float[] CalculateCornerSmoothingBudgets(Vector4 radii, float width, float height)
        {
            float[] radiusByCorner =
            {
                radii.x,
                radii.y,
                radii.z,
                radii.w
            };
            float[] budgets = { -1f, -1f, -1f, -1f };
            int[] order = { 0, 1, 2, 3 };
            Array.Sort(order, (a, b) => radiusByCorner[b].CompareTo(radiusByCorner[a]));

            for (int i = 0; i < order.Length; i++)
            {
                int corner = order[i];
                float radius = radiusByCorner[corner];
                float first = GetAdjacentBudget(corner, 0, radiusByCorner, budgets, width, height);
                float second = GetAdjacentBudget(corner, 1, radiusByCorner, budgets, width, height);
                float budget = Mathf.Min(first, second);
                budgets[corner] = Mathf.Max(0f, budget);
            }

            return budgets;
        }

        static float GetAdjacentBudget(int corner, int adjacentIndex, float[] radii, float[] budgets, float width, float height)
        {
            GetAdjacentCorner(corner, adjacentIndex, out int adjacentCorner, out bool horizontalSide);
            float radius = radii[corner];
            float adjacentRadius = radii[adjacentCorner];
            if (radius <= 0f && adjacentRadius <= 0f)
                return 0f;

            float sideLength = horizontalSide ? width : height;
            if (budgets[adjacentCorner] >= 0f)
                return sideLength - budgets[adjacentCorner];

            return radius / Mathf.Max(radius + adjacentRadius, 0.0001f) * sideLength;
        }

        static void GetAdjacentCorner(int corner, int adjacentIndex, out int adjacentCorner, out bool horizontalSide)
        {
            switch (corner)
            {
                case 0:
                    adjacentCorner = adjacentIndex == 0 ? 1 : 3;
                    horizontalSide = adjacentIndex == 0;
                    break;
                case 1:
                    adjacentCorner = adjacentIndex == 0 ? 0 : 2;
                    horizontalSide = adjacentIndex == 0;
                    break;
                case 2:
                    adjacentCorner = adjacentIndex == 0 ? 3 : 1;
                    horizontalSide = adjacentIndex == 0;
                    break;
                default:
                    adjacentCorner = adjacentIndex == 0 ? 2 : 0;
                    horizontalSide = adjacentIndex == 0;
                    break;
            }
        }

        static string DrawTopRightPath(SmoothedCornerPathParams p)
        {
            if (p.CornerRadius <= 0f)
                return FormattableString.Invariant($"l {p.P} 0");

            return string.Format(
                CultureInfo.InvariantCulture,
                "c {0} 0 {1} 0 {2} {3} a {4} {4} 0 0 1 {5} {5} c {3} {6} {3} {7} {3} {8}",
                p.A, p.A + p.B, p.A + p.B + p.C, p.D, p.CornerRadius, p.ArcSectionLength, p.C, p.B + p.C, p.A + p.B + p.C);
        }

        static string DrawBottomRightPath(SmoothedCornerPathParams p)
        {
            if (p.CornerRadius <= 0f)
                return FormattableString.Invariant($"l 0 {p.P}");

            return string.Format(
                CultureInfo.InvariantCulture,
                "c 0 {0} 0 {1} {2} {3} a {4} {4} 0 0 1 {5} {6} c {7} {8} {9} {8} {10} {8}",
                p.A, p.A + p.B, -p.D, p.A + p.B + p.C, p.CornerRadius, -p.ArcSectionLength, p.ArcSectionLength,
                -p.C, p.D, -(p.B + p.C), -(p.A + p.B + p.C));
        }

        static string DrawBottomLeftPath(SmoothedCornerPathParams p)
        {
            if (p.CornerRadius <= 0f)
                return FormattableString.Invariant($"l {-p.P} 0");

            return string.Format(
                CultureInfo.InvariantCulture,
                "c {0} 0 {1} 0 {2} {3} a {4} {4} 0 0 1 {5} {5} c {3} {6} {3} {7} {3} {8}",
                -p.A, -(p.A + p.B), -(p.A + p.B + p.C), -p.D, p.CornerRadius, -p.ArcSectionLength,
                -p.C, -(p.B + p.C), -(p.A + p.B + p.C));
        }

        static string DrawTopLeftPath(SmoothedCornerPathParams p)
        {
            if (p.CornerRadius <= 0f)
                return FormattableString.Invariant($"l 0 {-p.P}");

            return string.Format(
                CultureInfo.InvariantCulture,
                "c 0 {0} 0 {1} {2} {3} a {4} {4} 0 0 1 {5} {6} c {7} {8} {9} {8} {10} {8}",
                -p.A, -(p.A + p.B), p.D, -(p.A + p.B + p.C), p.CornerRadius, p.ArcSectionLength, -p.ArcSectionLength,
                p.C, -p.D, p.B + p.C, p.A + p.B + p.C);
        }

        struct SmoothedCornerPathParams
        {
            public float A;
            public float B;
            public float C;
            public float D;
            public float P;
            public float CornerRadius;
            public float ArcSectionLength;
        }

        static Bounds SamplePathBounds(Vector4[] pathPoints, float width, float height, float rotationRadians)
        {
            if (pathPoints == null || pathPoints.Length == 0)
            {
                return new Bounds(Vector3.zero, new Vector3(width, height, 0f));
            }

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            float cos = Mathf.Cos(-rotationRadians);
            float sin = Mathf.Sin(-rotationRadians);

            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector2 point = pathPoints[i];
                Vector2 local = new Vector2(
                    (point.x - 0.5f) * width,
                    (point.y - 0.5f) * height);

                Vector2 rotated = new Vector2(
                    cos * local.x - sin * local.y,
                    sin * local.x + cos * local.y);

                min = Vector2.Min(min, rotated);
                max = Vector2.Max(max, rotated);
            }

            Vector2 center = (min + max) * 0.5f;
            Vector2 size = max - min;
            return new Bounds(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));
        }

        List<LayerSpec> BuildLayerSpecs(List<FigmagePaint> paints)
        {
            List<LayerSpec> result = new List<LayerSpec>();
            if (paints == null)
            {
                return result;
            }

            foreach (FigmagePaint paint in paints)
            {
                if (!IsVisiblePaint(paint))
                {
                    continue;
                }

                int kind = MapPaintKind(paint.Type);
                if (kind < 0)
                {
                    continue;
                }

                LayerSpec layer = new LayerSpec
                {
                    Kind = kind,
                    Opacity = Mathf.Clamp01(paint.Opacity),
                    BlendMode = MapBlendMode(paint.BlendMode),
                    Stops = BuildStopSpecs(paint),
                    TransformRowA = BuildGradientRowA(paint),
                    TransformRowB = BuildGradientRowB(paint)
                };

                if (kind == LayerKindSolid)
                {
                    Color solidColor = ToLinearColor(paint.Color);
                    layer.Stops = new List<StopSpec>
                    {
                        new StopSpec
                        {
                            Position = 0f,
                            Alpha = solidColor.a,
                            Color = solidColor
                        }
                    };
                }

                result.Add(layer);
            }

            return result;
        }

        static List<StopSpec> BuildStopSpecs(FigmagePaint paint)
        {
            List<StopSpec> stops = new List<StopSpec>();
            if (paint.GradientStops == null)
            {
                return stops;
            }

            foreach (FigmageGradientStop stop in paint.GradientStops.OrderBy(x => x.Position))
            {
                Color linearColor = ToLinearColor(stop.Color);
                stops.Add(new StopSpec
                {
                    Position = Mathf.Clamp01(stop.Position),
                    Alpha = Mathf.Clamp01(linearColor.a),
                    Color = new Color(linearColor.r, linearColor.g, linearColor.b, 1f)
                });
            }

            return stops;
        }

        static Vector3 BuildGradientRowA(FigmagePaint paint)
        {
            GetGradientRows(paint, out Vector3 rowA, out _);
            return rowA;
        }

        static Vector3 BuildGradientRowB(FigmagePaint paint)
        {
            GetGradientRows(paint, out _, out Vector3 rowB);
            return rowB;
        }

        static void GetGradientRows(FigmagePaint paint, out Vector3 rowA, out Vector3 rowB)
        {
            rowA = new Vector3(1f, 0f, 0f);
            rowB = new Vector3(0f, 1f, 0f);

            if (paint.GradientHandlePositions != null && paint.GradientHandlePositions.Count >= 3)
            {
                Vector2 p0 = paint.GradientHandlePositions[0];
                Vector2 p1 = paint.GradientHandlePositions[1];
                Vector2 p2 = paint.GradientHandlePositions[2];

                float m00 = p1.x - p0.x;
                float m01 = p2.x - p0.x;
                float m10 = p1.y - p0.y;
                float m11 = p2.y - p0.y;
                float det = m00 * m11 - m01 * m10;
                if (Mathf.Abs(det) < GradientDeterminantEpsilon)
                {
                    return;
                }

                float inv00 = m11 / det;
                float inv01 = -m01 / det;
                float inv10 = -m10 / det;
                float inv11 = m00 / det;

                rowA = new Vector3(inv00, inv01, -(inv00 * p0.x + inv01 * p0.y));
                rowB = new Vector3(inv10, inv11, -(inv10 * p0.x + inv11 * p0.y));
            }
        }

        static List<ShadowSpec> GetShadowSpecs(FigmageNode node, FigmageEffectType FigmageEffectType, float scale)
        {
            List<ShadowSpec> shadows = new List<ShadowSpec>();

            if (node.Effects == null)
            {
                return shadows;
            }

            foreach (FigmageEffect effect in node.Effects)
            {
                if (!IsVisibleEffect(effect) || effect.Type != FigmageEffectType)
                {
                    continue;
                }

                Color color = new Color(effect.Color.r, effect.Color.g, effect.Color.b, effect.Color.a);
                color.a *= Mathf.Clamp01(effect.Opacity);

                shadows.Add(new ShadowSpec
                {
                    Enabled = true,
                    BlurRadius = Mathf.Max(0f, effect.Radius * scale),
                    Spread = effect.Spread * scale,
                    Offset = new Vector2(effect.Offset.x * scale, effect.Offset.y * scale),
                    BlendMode = MapBlendMode(effect.BlendMode),
                    Color = color
                });
            }

            return shadows;
        }

        static ShadowSpec GetShadowSpec(FigmageNode node, FigmageEffectType FigmageEffectType, float scale)
        {
            List<ShadowSpec> shadows = GetShadowSpecs(node, FigmageEffectType, scale);
            if (shadows.Count == 0)
            {
                return ShadowSpec.Disabled;
            }

            return shadows[0];
        }

        static LayerBlurSpec GetLayerBlurSpec(FigmageNode node, float scale)
        {
            if (node.Effects == null)
            {
                return LayerBlurSpec.Disabled;
            }

            foreach (FigmageEffect effect in node.Effects)
            {
                if (!IsVisibleEffect(effect) || effect.Type != FigmageEffectType.LayerBlur)
                {
                    continue;
                }

                bool progressive = string.Equals(effect.BlurType, ProgressiveBlurType, StringComparison.OrdinalIgnoreCase);
                Vector2 startOffset = progressive ? effect.StartOffset : Vector2.zero;
                Vector2 endOffset = progressive ? effect.EndOffset : Vector2.right;
                if (Vector2.SqrMagnitude(endOffset - startOffset) <= ProgressiveBlurOffsetEpsilonSqr)
                {
                    progressive = false;
                }

                float layerBlurRadiusScale = ResolveLayerBlurRadiusScale();
                return new LayerBlurSpec
                {
                    Enabled = true,
                    Progressive = progressive,
                    Radius = Mathf.Max(0f, effect.Radius * scale * layerBlurRadiusScale),
                    StartRadius = Mathf.Max(0f, effect.StartRadius * scale * layerBlurRadiusScale * ResolveLayerBlurStartRadiusScale()),
                    StartOffset = startOffset,
                    EndOffset = endOffset
                };
            }

            return LayerBlurSpec.Disabled;
        }

        static Color ToLinearColor(Color color)
        {
            Color converted = new Color(color.r, color.g, color.b, color.a).linear;
            converted.a = color.a;
            return converted;
        }
    }
}
