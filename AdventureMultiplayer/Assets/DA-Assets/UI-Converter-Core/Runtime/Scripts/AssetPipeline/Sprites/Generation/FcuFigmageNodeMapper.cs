#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Figmage;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.UCC
{
    static class FcuFigmageNodeMapper
    {
        public static FigmageNode ToFigmageNode(Node source)
        {
            return new FigmageNode
            {
                Id = source.Id,
                Name = source.Name,
                AbsoluteBoundingBox = ToFigmageRect(source.AbsoluteBoundingBox),
                AbsoluteRenderBounds = ToFigmageRect(source.AbsoluteRenderBounds),
                Size = source.Size,
                Rotation = source.Rotation ?? 0f,
                RelativeTransform = ToRelativeTransform(source.RelativeTransform),
                Opacity = source.Opacity ?? 1f,
                CornerRadius = source.CornerRadius ?? 0f,
                CornerRadiuses = source.CornerRadiuses != null ? new List<float>(source.CornerRadiuses) : null,
                CornerSmoothing = source.CornerSmoothing,
                StrokeWeight = source.StrokeWeight,
                StrokeAlign = ToFigmageStrokeAlign(source.StrokeAlign),
                StrokeJoin = ToFigmageStrokeJoin(source.StrokeJoin),
                StrokeCap = ToFigmageStrokeCap(source.StrokeCap),
                DashPattern = source.StrokeDashes != null ? new List<float>(source.StrokeDashes) : null,
                IndividualStrokeWeights = ToFigmageStrokeWeights(source.IndividualStrokeWeights),
                Fills = ToFigmagePaints(source.Fills),
                Strokes = ToFigmagePaints(source.Strokes),
                Effects = ToFigmageEffects(source.Effects),
                FillGeometry = ToFigmageGeometry(source.FillGeometry),
                StrokeGeometry = ToFigmageGeometry(source.StrokeGeometry),
                Children = ToFigmageChildren(source.Children)
            };
        }

        static FigmageRect ToFigmageRect(BoundingBox source)
        {
            return new FigmageRect(source.X ?? 0f, source.Y ?? 0f, source.Width ?? 0f, source.Height ?? 0f);
        }

        static List<List<float>> ToRelativeTransform(List<List<float?>> source)
        {
            if (source == null)
                return null;

            List<List<float>> result = new List<List<float>>(source.Count);
            for (int y = 0; y < source.Count; y++)
            {
                List<float?> row = source[y];
                if (row == null)
                {
                    result.Add(null);
                    continue;
                }

                List<float> mappedRow = new List<float>(row.Count);
                for (int x = 0; x < row.Count; x++)
                    mappedRow.Add(row[x] ?? 0f);
                result.Add(mappedRow);
            }

            return result;
        }

        static FigmageStrokeAlign ToFigmageStrokeAlign(StrokeAlign source)
        {
            switch (source)
            {
                case StrokeAlign.INSIDE:
                    return FigmageStrokeAlign.Inside;
                case StrokeAlign.OUTSIDE:
                    return FigmageStrokeAlign.Outside;
                default:
                    return FigmageStrokeAlign.Center;
            }
        }

        static FigmageStrokeJoin ToFigmageStrokeJoin(string source)
        {
            if (string.Equals(source, "ROUND", StringComparison.OrdinalIgnoreCase))
                return FigmageStrokeJoin.Round;

            if (string.Equals(source, "BEVEL", StringComparison.OrdinalIgnoreCase))
                return FigmageStrokeJoin.Bevel;

            return FigmageStrokeJoin.Miter;
        }

        static FigmageStrokeCap ToFigmageStrokeCap(StrokeCap source)
        {
            switch (source)
            {
                case StrokeCap.ROUND:
                    return FigmageStrokeCap.Round;
                case StrokeCap.SQUARE:
                    return FigmageStrokeCap.Square;
                case StrokeCap.LINE_ARROW:
                    return FigmageStrokeCap.ArrowLines;
                case StrokeCap.TRIANGLE_ARROW:
                    return FigmageStrokeCap.TriangleFilled;
                default:
                    return FigmageStrokeCap.None;
            }
        }

        static FigmageStrokeWeights ToFigmageStrokeWeights(IndividualStrokeWeights source)
        {
            return new FigmageStrokeWeights(source.Top, source.Bottom, source.Left, source.Right);
        }

        static List<FigmagePaint> ToFigmagePaints(List<Paint> source)
        {
            List<FigmagePaint> result = new List<FigmagePaint>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(ToFigmagePaint(source[i]));

            return result;
        }

        static FigmagePaint ToFigmagePaint(Paint source)
        {
            return new FigmagePaint
            {
                Type = ToFigmagePaintType(source.Type),
                Color = source.Color,
                Visible = source.Visible != false,
                BlendMode = source.BlendMode,
                Opacity = source.Opacity ?? 1f,
                GradientHandlePositions = source.GradientHandlePositions != null ? new List<Vector2>(source.GradientHandlePositions) : null,
                GradientStops = ToFigmageGradientStops(source.GradientStops)
            };
        }

        static FigmagePaintType ToFigmagePaintType(PaintType source)
        {
            switch (source)
            {
                case PaintType.GRADIENT_LINEAR:
                    return FigmagePaintType.GradientLinear;
                case PaintType.GRADIENT_RADIAL:
                    return FigmagePaintType.GradientRadial;
                case PaintType.GRADIENT_ANGULAR:
                    return FigmagePaintType.GradientAngular;
                case PaintType.GRADIENT_DIAMOND:
                    return FigmagePaintType.GradientDiamond;
                default:
                    return FigmagePaintType.Solid;
            }
        }

        static List<FigmageGradientStop> ToFigmageGradientStops(List<GradientStop> source)
        {
            if (source == null)
                return null;

            List<FigmageGradientStop> result = new List<FigmageGradientStop>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(new FigmageGradientStop(source[i].Position, source[i].Color));

            return result;
        }

        static List<FigmageEffect> ToFigmageEffects(List<Effect> source)
        {
            List<FigmageEffect> result = new List<FigmageEffect>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
                result.Add(ToFigmageEffect(source[i]));

            return result;
        }

        static FigmageEffect ToFigmageEffect(Effect source)
        {
            return new FigmageEffect
            {
                Type = ToFigmageEffectType(source.Type),
                Visible = source.Visible != false,
                Color = source.Color,
                Opacity = source.Opacity ?? 1f,
                BlendMode = source.BlendMode,
                Offset = source.Offset,
                Radius = source.Radius,
                Spread = source.Spread ?? 0f,
                BlurType = source.BlurType,
                StartRadius = source.StartRadius ?? 0f,
                StartOffset = source.StartOffset,
                EndOffset = source.EndOffset
            };
        }

        static FigmageEffectType ToFigmageEffectType(EffectType source)
        {
            switch (source)
            {
                case EffectType.INNER_SHADOW:
                    return FigmageEffectType.InnerShadow;
                case EffectType.LAYER_BLUR:
                    return FigmageEffectType.LayerBlur;
                case EffectType.BACKGROUND_BLUR:
                    return FigmageEffectType.BackgroundBlur;
                default:
                    return FigmageEffectType.DropShadow;
            }
        }

        static List<FigmageGeometry> ToFigmageGeometry(List<FillGeometry> source)
        {
            if (source == null)
                return null;

            List<FigmageGeometry> result = new List<FigmageGeometry>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(new FigmageGeometry { Path = source[i].Path });

            return result;
        }

        static List<FigmageNode> ToFigmageChildren(List<Node> source)
        {
            if (source == null)
                return null;

            List<FigmageNode> result = new List<FigmageNode>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(ToFigmageNode(source[i]));

            return result;
        }
    }
}
#endif