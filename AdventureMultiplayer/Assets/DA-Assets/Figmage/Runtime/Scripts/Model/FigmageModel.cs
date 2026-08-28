using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DA_Assets.Figmage
{
    public enum FigmagePaintType
    {
        Solid = 0,
        GradientLinear = 1,
        GradientRadial = 2,
        GradientAngular = 3,
        GradientDiamond = 4
    }

    public enum FigmageStrokeAlign
    {
        Inside = 0,
        Center = 1,
        Outside = 2
    }

    public enum FigmageStrokeJoin
    {
        Miter = 0,
        Bevel = 1,
        Round = 2
    }

    public enum FigmageStrokeCap
    {
        None = 0,
        Round = 1,
        Square = 2,
        ArrowLines = 3,
        ArrowEquilateral = 4,
        DiamondFilled = 5,
        TriangleFilled = 6,
        CircleFilled = 7
    }

    public enum FigmageEffectType
    {
        DropShadow = 0,
        InnerShadow = 1,
        LayerBlur = 2,
        BackgroundBlur = 3
    }

    [Serializable]
    public struct FigmageRect
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public FigmageRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    [Serializable]
    public struct FigmageGradientStop
    {
        public Color Color;
        [Range(0f, 1f)] public float Position;

        public FigmageGradientStop(float position, Color color)
        {
            Position = Mathf.Clamp01(position);
            Color = color;
        }
    }

    [Serializable]
    public struct FigmageStrokeWeights
    {
        public float Top;
        public float Bottom;
        public float Left;
        public float Right;

        public FigmageStrokeWeights(float top, float bottom, float left, float right)
        {
            Top = Mathf.Max(0f, top);
            Bottom = Mathf.Max(0f, bottom);
            Left = Mathf.Max(0f, left);
            Right = Mathf.Max(0f, right);
        }

        public static FigmageStrokeWeights Uniform(float weight)
        {
            float clamped = Mathf.Max(0f, weight);
            return new FigmageStrokeWeights(clamped, clamped, clamped, clamped);
        }
    }

    [Serializable]
    public sealed class FigmagePaint
    {
        public FigmagePaintType Type;
        public Color Color;
        public bool Visible = true;
        public string BlendMode;
        [Range(0f, 1f)] public float Opacity = 1f;
        public List<Vector2> GradientHandlePositions;
        public List<FigmageGradientStop> GradientStops;

        public static FigmagePaint Solid(Color color, float opacity = 1f, string blendMode = "NORMAL")
        {
            return new FigmagePaint
            {
                Type = FigmagePaintType.Solid,
                Color = color,
                Opacity = opacity,
                BlendMode = blendMode,
                Visible = true
            };
        }

        public static FigmagePaint LinearGradient(
            IEnumerable<Vector2> gradientHandlePositions,
            IEnumerable<FigmageGradientStop> gradientStops,
            float opacity = 1f,
            string blendMode = "NORMAL")
        {
            return new FigmagePaint
            {
                Type = FigmagePaintType.GradientLinear,
                Opacity = opacity,
                BlendMode = blendMode,
                Visible = true,
                GradientHandlePositions = gradientHandlePositions?.Take(3).ToList() ?? new List<Vector2>(),
                GradientStops = gradientStops?.ToList() ?? new List<FigmageGradientStop>()
            };
        }
    }

    [Serializable]
    public sealed class FigmageEffect
    {
        public FigmageEffectType Type;
        public bool Visible = true;
        public Color Color;
        [Range(0f, 1f)] public float Opacity = 1f;
        public string BlendMode;
        public Vector2 Offset;
        public float Radius;
        public float Spread;
        public string BlurType;
        public float StartRadius;
        public Vector2 StartOffset;
        public Vector2 EndOffset;
    }

    [Serializable]
    public struct FigmageGeometry
    {
        public string Path;
    }

    [Serializable]
    public sealed class FigmageNode
    {
        public string Id;
        public string Name;
        public FigmageRect AbsoluteBoundingBox;
        public FigmageRect AbsoluteRenderBounds;
        public Vector2 Size;
        public float Rotation;
        [NonSerialized] public List<List<float>> RelativeTransform;
        [Range(0f, 1f)] public float Opacity = 1f;
        public float CornerRadius;
        public List<float> CornerRadiuses;
        [Range(0f, 1f)] public float CornerSmoothing;
        public float StrokeWeight;
        public FigmageStrokeAlign StrokeAlign = FigmageStrokeAlign.Center;
        public FigmageStrokeJoin StrokeJoin = FigmageStrokeJoin.Miter;
        public FigmageStrokeCap StrokeCap = FigmageStrokeCap.None;
        public float StrokeMiterLimit = 4f;
        public List<float> DashPattern;
        public FigmageStrokeWeights IndividualStrokeWeights;
        public List<FigmagePaint> Fills = new List<FigmagePaint>();
        public List<FigmagePaint> Strokes = new List<FigmagePaint>();
        public List<FigmageEffect> Effects = new List<FigmageEffect>();
        public List<FigmageGeometry> FillGeometry;
        public List<FigmageGeometry> StrokeGeometry;
        public List<FigmageNode> Children;
    }
}
