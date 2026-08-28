#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITEXT
using LightSide;
using Style = DA_Assets.UCC.Model.Style;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static void PopulateGradient(UniText text, Node fobject)
        {
            Style defaultStyle = fobject.Style;
            var overrides = fobject.CharacterStyleOverrides;
            var overrideTable = fobject.StyleOverrideTable;
            string chars = fobject.GetText();
            int len = chars.Length;
            string defaultParameter = TryBuildDefaultGradientParameter(fobject, defaultStyle);

            bool hasOverrides = overrides != null && overrides.Count > 0
                                && overrideTable != null && overrideTable.Count > 0;

            if (!hasOverrides)
            {
                if (string.IsNullOrEmpty(defaultParameter))
                    return;

                RegisterRangeRule(text, new GradientModifier())
                    .data.Add(new RangeRule.Data { range = string.Empty, parameter = defaultParameter });
                return;
            }

            var data = new List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);

            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                string parameter = TryBuildGradientParameter(eff, defaultStyle, defaultParameter);
                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    string nextParameter = TryBuildGradientParameter(next, defaultStyle, defaultParameter);
                    if (!string.Equals(nextParameter, parameter, StringComparison.Ordinal))
                        break;

                    i++;
                }

                if (!string.IsNullOrEmpty(parameter))
                    data.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = parameter });
            }

            RegisterIfHasData(text, new GradientModifier(), data);
        }

        private static string TryBuildDefaultGradientParameter(Node fobject, Style defaultStyle)
        {
            string parameter = TryBuildGradientParameter(defaultStyle);
            if (!string.IsNullOrEmpty(parameter))
                return parameter;

            FFill fill = fobject.Data.Graphic.Fill;
            if (!fill.HasGradient)
                return null;

            return TryBuildGradientParameter(fill.GradientPaint);
        }

        private static string TryBuildGradientParameter(Style style, Style defaultStyle, string defaultParameter)
        {
            string parameter = TryBuildGradientParameter(style);
            if (!string.IsNullOrEmpty(parameter))
                return parameter;

            if (!string.IsNullOrEmpty(defaultParameter) && ReferenceEquals(style.Fills, defaultStyle.Fills))
                return defaultParameter;

            return null;
        }

        private static string TryBuildGradientParameter(Style style)
        {
            Paint? gradientPaint = GetGradientPaint(style);
            if (!gradientPaint.HasValue)
                return null;

            return TryBuildGradientParameter(gradientPaint.Value);
        }

        private static string TryBuildGradientParameter(Paint paint)
        {
            if (!TryBuildGradientReference(paint, out string gradientName, out string shape, out float angle))
                return null;

            if (Math.Abs(angle) < 0.01f && shape == "linear")
                return gradientName;

            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F0}", gradientName, shape, angle);
        }

        private static bool TryBuildGradientReference(Paint paint, out string gradientName, out string shape, out float angle)
        {
            gradientName = null;
            shape = null;
            angle = 0f;

            if (!IsGradientPaint(paint))
                return false;

            Gradient unityGradient = FigmaGradientToUnity(paint);
            if (unityGradient == null)
                return false;

            shape = GetGradientShape(paint.Type);
            angle = GetGradientAngle(paint);
            gradientName = GenerateGradientName(paint);

            EnsureGradientRegistered(gradientName, unityGradient);
            return true;
        }

        private static Paint? GetGradientPaint(Style style)
        {
            if (style.Fills == null)
                return null;

            foreach (Paint fill in style.Fills)
            {
                if (fill.Visible == false)
                    continue;

                if (IsGradientPaint(fill))
                    return fill;
            }

            return null;
        }

        private static bool IsGradientPaint(Paint paint)
        {
            if (paint.Visible == false)
                return false;

            if (paint.GradientStops.IsEmpty())
                return false;

            switch (paint.Type)
            {
                case PaintType.GRADIENT_LINEAR:
                case PaintType.GRADIENT_RADIAL:
                case PaintType.GRADIENT_ANGULAR:
                case PaintType.GRADIENT_DIAMOND:
                    return true;
                default:
                    return false;
            }
        }

        private static Gradient FigmaGradientToUnity(Paint paint)
        {
            var stops = paint.GradientStops;
            if (stops == null || stops.Count == 0)
                return null;

            var colorKeys = new GradientColorKey[stops.Count];
            var alphaKeys = new GradientAlphaKey[stops.Count];

            for (int i = 0; i < stops.Count; i++)
            {
                colorKeys[i] = new GradientColorKey(stops[i].Color, stops[i].Position);
                alphaKeys[i] = new GradientAlphaKey(stops[i].Color.a, stops[i].Position);
            }

            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private static string GetGradientShape(PaintType paintType)
        {
            switch (paintType)
            {
                case PaintType.GRADIENT_RADIAL:
                case PaintType.GRADIENT_DIAMOND:
                    return "radial";
                case PaintType.GRADIENT_ANGULAR:
                    return "angular";
                default:
                    return "linear";
            }
        }

        private static float GetGradientAngle(Paint paint)
        {
            if (paint.GradientHandlePositions == null || paint.GradientHandlePositions.Count < 2)
                return 0f;

            Vector2 p0 = paint.GradientHandlePositions[0];
            Vector2 p1 = paint.GradientHandlePositions[1];

            float dx = p1.x - p0.x;

            float dy = -(p1.y - p0.y);




            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;


            if (angle < 0f) angle += 360f;
            return angle;
        }

        private static string GenerateGradientName(Paint paint)
        {

            int hash = paint.Type.GetHashCode();
            if (paint.GradientStops != null)
            {
                foreach (var stop in paint.GradientStops)
                {
                    hash = hash * 31 + stop.Color.GetHashCode();
                    hash = hash * 31 + stop.Position.GetHashCode();
                }
            }

            return $"fcu_{(hash & 0x7FFFFFFF):X8}";
        }

        private static void EnsureGradientRegistered(string name, Gradient gradient)
        {
            var gradientsAsset = LightSide.UniTextSettings.Gradients;

            if (gradientsAsset == null)
            {
                Debug.LogWarning("[UniTextDrawer] UniTextSettings.Gradients is not assigned. Cannot register gradient.");
                return;
            }


            if (gradientsAsset.TryGetGradient(name, out _))
                return;

            gradientsAsset.Add(name, gradient);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssetIfDirty(gradientsAsset);
#endif
        }
#endif
    }
}
#endif