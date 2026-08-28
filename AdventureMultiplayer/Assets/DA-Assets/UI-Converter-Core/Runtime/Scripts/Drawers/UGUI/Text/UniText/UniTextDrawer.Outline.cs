#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using System;
using DA_Assets.UCC.Extensions;

#if UNITEXT
using LightSide;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static UniTextRenderMode GetRenderMode(Node fobject)
        {
            if (!TryGetVisibleOutline(fobject, out _))
                return UniTextRenderMode.SDF;

            return string.Equals(fobject.StrokeJoin, "ROUND", StringComparison.OrdinalIgnoreCase)
                ? UniTextRenderMode.SDF
                : UniTextRenderMode.MSDF;
        }

        private static bool TryGetVisibleOutline(Node fobject, out Paint stroke)
        {
            stroke = default;

            if (fobject.StrokeWeight <= 0f || fobject.Strokes == null)
                return false;

            for (int i = 0; i < fobject.Strokes.Count; i++)
            {
                Paint candidate = fobject.Strokes[i];
                if (!candidate.IsVisible())
                    continue;

                if (candidate.Type != PaintType.SOLID && !IsGradientPaint(candidate))
                    continue;

                stroke = candidate;
                return true;
            }

            return false;
        }

        private static void PopulateOutline(UniText text, Node fobject)
        {
            if (!TryGetVisibleOutline(fobject, out Paint stroke))
                return;

            OutlineModifier modifier = new OutlineModifier { FixedPixelSize = true };
            string parameter;

            if (stroke.Type == PaintType.SOLID)
            {
                string hex = Color32ToHex(stroke.Color);
                parameter = FormattableString.Invariant($"{hex},{fobject.StrokeWeight}");
            }
            else if (TryBuildGradientReference(stroke, out string gradientName, out string shape, out float angle))
            {
                modifier.Provider = new GlobalSettingsGradientProvider();
                parameter = FormattableString.Invariant($"{gradientName},{fobject.StrokeWeight},{shape},{angle:F0}");
            }
            else
            {
                return;
            }

            RegisterRangeRule(text, modifier)
                .data.Add(new RangeRule.Data { range = string.Empty, parameter = parameter });
        }
#endif
    }
}
#endif