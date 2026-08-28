#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if TextMeshPro && RTLTMP_EXISTS
using RTLTMPro;
#endif

#if TextMeshPro
using TMPro;
#endif

#if NOVA_UI_EXISTS
using Nova.TMP;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class TextMeshDrawer : FcuBase
    {
        [SerializeField] List<Node> texts;
        public List<Node> Texts => texts;

        public void DrawRTL(Node fobject)
        {
#if TextMeshPro && RTLTMP_EXISTS
            fobject.Data.GameObject.TryAddGraphic(out RTLTextMeshPro text);

            text.Farsi = monoBeh.Settings.TextMeshSettings.Farsi;
            text.ForceFix = monoBeh.Settings.TextMeshSettings.ForceFix;
            text.PreserveNumbers = monoBeh.Settings.TextMeshSettings.PreserveNumbers;
            text.FixTags = monoBeh.Settings.TextMeshSettings.FixTags;

            Draw(fobject, text);
#endif
        }

        public void DrawTMP(Node fobject)
        {
#if TextMeshPro
            TMP_Text text;

            fobject.Data.GameObject.TryAddGraphic(out TextMeshProUGUI uguiText);
            text = uguiText;

            Draw(fobject, text);
#endif
        }

        public void DrawNovaTMP(Node fobject)
        {
#if TextMeshPro && NOVA_UI_EXISTS
            TMP_Text text;

            fobject.Data.GameObject.TryAddGraphic(out TextMeshProTextBlock novaText);
            text = novaText;

            Draw(fobject, text);
#endif
        }

#if TextMeshPro
        private void Draw(Node fobject, TMP_Text text)
        {
            string str = fobject.GetText();

            text.text = str;

            text.overrideColorTags = monoBeh.Settings.TextMeshSettings.OverrideTags;
            text.enableAutoSizing = monoBeh.Settings.TextMeshSettings.AutoSize;

#if UNITY_2023_1_OR_NEWER
            if (monoBeh.Settings.TextMeshSettings.Wrapping)
            {
                text.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
#else
            text.enableWordWrapping = monoBeh.Settings.TextMeshSettings.Wrapping;
#endif

            if (monoBeh.IsNova())
            {
                text.isOrthographic = monoBeh.Settings.TextMeshSettings.OrthographicMode;
            }

            text.richText = monoBeh.Settings.TextMeshSettings.RichText;
            text.raycastTarget = monoBeh.Settings.TextMeshSettings.RaycastTarget;
            text.parseCtrlCharacters = monoBeh.Settings.TextMeshSettings.ParseEscapeCharacters;
            text.useMaxVisibleDescender = monoBeh.Settings.TextMeshSettings.VisibleDescender;

#if UNITY_6000_0_OR_NEWER
            var fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>();
            if (monoBeh.Settings.TextMeshSettings.Kerning)
            {
                fontFeatures.Add(UnityEngine.TextCore.OTL_FeatureTag.kern);
            }
            text.fontFeatures = fontFeatures;
#elif UNITY_2023_4_OR_NEWER == false
            text.enableKerning = monoBeh.Settings.TextMeshSettings.Kerning;
#endif

            text.extraPadding = monoBeh.Settings.TextMeshSettings.ExtraPadding;
            text.horizontalMapping = monoBeh.Settings.TextMeshSettings.HorizontalMapping;
            text.verticalMapping = monoBeh.Settings.TextMeshSettings.VerticalMapping;
            text.geometrySortingOrder = monoBeh.Settings.TextMeshSettings.GeometrySorting;

            if (monoBeh.Settings.TextMeshSettings.Shader != null)
            {
                text.fontMaterial.shader = monoBeh.Settings.TextMeshSettings.Shader;
            }

            SetFont(text, fobject);

            text.alignment = fobject.GetTextAnchor().ToTextMeshAnchor();


            if (fobject.Style.TextAlignHorizontal == TextAlignHorizontal.JUSTIFIED)
            {
                switch (fobject.Style.TextAlignVertical)
                {
                    case TextAlignVertical.TOP:
                        text.alignment = TextAlignmentOptions.TopJustified;
                        break;
                    case TextAlignVertical.BOTTOM:
                        text.alignment = TextAlignmentOptions.BottomJustified;
                        break;
                    default:
                        text.alignment = TextAlignmentOptions.Justified;
                        break;
                }
            }

            SetFontSize(text, fobject);
            SetFontCase(text, fobject);
            SetOverflowMode(text, fobject);
            SetColor(text, fobject);

            if (fobject.Style.MaxLines.HasValue && fobject.Style.MaxLines.Value > 0)
            {
                text.maxVisibleLines = fobject.Style.MaxLines.Value;
            }
        }

        private const float _sizeFixForNonOrthographicNovaText = 10f;

        private void SetFontSize(TMP_Text text, Node fobject)
        {
            if (monoBeh.Settings.TextMeshSettings.AutoSize)
            {
                text.fontSizeMin = 1;
                text.fontSizeMax = fobject.Style.FontSize;
            }
            else
            {
                text.fontSize = fobject.Style.FontSize;
            }

            if (monoBeh.IsNova())
            {
                if (monoBeh.Settings.TextMeshSettings.OrthographicMode == false)
                {
                    text.fontSize *= _sizeFixForNonOrthographicNovaText;
                }
            }

            if (!fobject.Style.FontFamily.IsEmpty())
            {
                text.characterSpacing = (float)Math.Round(fobject.Style.LetterSpacing / text.fontSize * 100, monoBeh.Config.Rounding.FontParams);


                float lineSpacingValue;
                string unit = fobject.Style.LineHeightUnit;

                if (!string.IsNullOrEmpty(unit) && unit == "FONT_SIZE_%" && fobject.Style.LineHeightPercentFontSize.HasValue)
                {

                    lineSpacingValue = fobject.Style.LineHeightPercentFontSize.Value - text.font.faceInfo.lineHeight;
                }
                else
                {

                    lineSpacingValue = fobject.Style.LineHeightPx / fobject.Style.FontSize * 100 - text.font.faceInfo.lineHeight;
                }

                text.lineSpacing = (float)Math.Round(lineSpacingValue, monoBeh.Config.Rounding.FontParams);
                text.paragraphSpacing = (float)Math.Round(fobject.Style.ParagraphSpacing / fobject.Style.FontSize * 100, monoBeh.Config.Rounding.FontParams);
            }
        }

        private void SetFont(TMP_Text text, Node fobject)
        {
            TMP_FontAsset font = monoBeh.FontLoader.GetFontFromArray(fobject, monoBeh.FontLoader.TmpFonts);
            text.font = font;
        }

        private void SetFontCase(TMP_Text text, Node fobject)
        {
            FontStyles textDecoration = FontStyles.Normal;
            FontStyles textCase = FontStyles.Normal;
            FontStyles textItalic = FontStyles.Normal;
            FontStyles textBold = FontStyles.Normal;

            if (fobject.Data.HasFontAsset == false)
            {
                if (fobject.Style.Italic.ToBoolNullFalse())
                {
                    textItalic = FontStyles.Italic;
                }

                if (fobject.Style.FontWeight > 600)
                {
                    textBold = FontStyles.Bold;
                }
            }

            switch (fobject.Style.TextDecoration)
            {
                case TextDecoration.UNDERLINE:
                    textDecoration = FontStyles.Underline;
                    break;
                case TextDecoration.STRIKETHROUGH:
                    textDecoration = FontStyles.Strikethrough;
                    break;
            }

            switch (fobject.Style.TextCase)
            {
                case TextCase.UPPER:
                    textCase = FontStyles.UpperCase;
                    break;
                case TextCase.LOWER:
                    textCase = FontStyles.LowerCase;
                    break;
                case TextCase.TITLE:
                    textCase = FontStyles.Normal;
                    break;
                case TextCase.SMALL_CAPS:
                case TextCase.SMALL_CAPS_FORCED:
                    textCase = FontStyles.SmallCaps;
                    break;
            }

            FontStyles final = textDecoration | textCase | textItalic | textBold;

            text.fontStyle = final;
        }

        private void SetOverflowMode(TMP_Text text, Node fobject)
        {
            TextOverflowModes textTurncate = monoBeh.Settings.TextMeshSettings.Overflow;

            if (fobject.Style.TextAutoResize == TextAutoResize.NONE || fobject.Style.TextAutoResize == TextAutoResize.TRUNCATE)
            {
                if (fobject.Style.TextTruncation == TextTruncation.ENDING)
                {
                    textTurncate = TextOverflowModes.Ellipsis;
                }
            }

            text.overflowMode = textTurncate;
        }

        private void SetColor(TMP_Text text, Node fobject)
        {
            FGraphic graphic = fobject.Data.Graphic;

            text.enableVertexGradient = false;

            if (graphic.Fill.HasSolid)
            {
                text.color = graphic.Fill.SolidPaint.Color;
            }
            else if (graphic.Fill.HasGradient)
            {
                Paint gradientPaint = graphic.Fill.GradientPaint;
                List<GradientColorKey> gradientColorKeys = gradientPaint.ToGradientColorKeys();

                if (!gradientColorKeys.IsEmpty())
                {
                    if (gradientPaint.Type == PaintType.GRADIENT_LINEAR
                        && gradientColorKeys.Count >= 2
                        && IsVerticalGradient(gradientPaint)
                        && !fobject.Characters.Contains("\n"))
                    {

                        Color startColor = gradientColorKeys.First().color;
                        Color endColor = gradientColorKeys.Last().color;

                        bool isTopToBottom = IsTopToBottomGradient(gradientPaint);
                        Color topColor = isTopToBottom ? startColor : endColor;
                        Color bottomColor = isTopToBottom ? endColor : startColor;

                        text.color = Color.white;
                        text.enableVertexGradient = true;
                        text.colorGradient = new VertexGradient(topColor, topColor, bottomColor, bottomColor);
                    }
                    else
                    {

                        text.color = gradientColorKeys.First().color;
                    }
                }
            }

            if (graphic.HasStroke && fobject.StrokeAlign == StrokeAlign.INSIDE)
            {
                float normalizedWidth = fobject.StrokeWeight / text.preferredHeight;
                text.outlineWidth = normalizedWidth;

                if (graphic.Stroke.HasSolid)
                {
                    text.outlineColor = graphic.Stroke.SolidPaint.Color;
                }
                else if (graphic.Stroke.HasGradient)
                {
                    List<GradientColorKey> gradientColorKeys = graphic.Stroke.GradientPaint.ToGradientColorKeys();

                    if (!gradientColorKeys.IsEmpty())
                    {
                        text.outlineColor = gradientColorKeys.First().color;
                    }
                }
            }
            else
            {
                text.outlineWidth = 0;
            }
        }

        private const float _verticalAngleTolerance = 15f;

        private static bool IsVerticalGradient(Paint paint)
        {
            if (paint.GradientHandlePositions == null || paint.GradientHandlePositions.Count < 2)
                return true;

            Vector2 p0 = paint.GradientHandlePositions[0];
            Vector2 p1 = paint.GradientHandlePositions[1];

            float dx = p1.x - p0.x;
            float dy = p1.y - p0.y;

            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;


            bool isDown = Mathf.Abs(angle - 90f) <= _verticalAngleTolerance;
            bool isUp = Mathf.Abs(angle - 270f) <= _verticalAngleTolerance;

            return isDown || isUp;
        }

        private static bool IsTopToBottomGradient(Paint paint)
        {
            if (paint.GradientHandlePositions == null || paint.GradientHandlePositions.Count < 2)
                return true;

            Vector2 p0 = paint.GradientHandlePositions[0];
            Vector2 p1 = paint.GradientHandlePositions[1];


            return (p1.y - p0.y) >= 0f;
        }
#endif
    }
}
#endif