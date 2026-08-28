#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.UCC.Extensions
{
    public static class TextExtensions
    {
        public static FontStyle GetFontWeight(this Node fobject)
        {
            string fontStyleRaw = fobject.Style.FontPostScriptName;

            if (fontStyleRaw.IsEmpty() == false)
            {
                if (fontStyleRaw.Contains(FontStyle.Bold.ToString()))
                {
                    if (fobject.Style.Italic.ToBoolNullFalse())
                    {
                        return FontStyle.BoldAndItalic;
                    }
                    else
                    {
                        return FontStyle.Bold;
                    }
                }
                else if (fobject.Style.Italic.ToBoolNullFalse())
                {
                    return FontStyle.Italic;
                }
            }

            return FontStyle.Normal;
        }

        public static TextAnchor GetTextAnchor(this Node fobject)
        {
            string textAligment = fobject.Style.TextAlignVertical + " " + fobject.Style.TextAlignHorizontal;

            switch (textAligment)
            {
                case "BOTTOM CENTER":
                    return TextAnchor.LowerCenter;
                case "BOTTOM LEFT":
                    return TextAnchor.LowerLeft;
                case "BOTTOM RIGHT":
                    return TextAnchor.LowerRight;
                case "CENTER CENTER":
                    return TextAnchor.MiddleCenter;
                case "CENTER LEFT":
                    return TextAnchor.MiddleLeft;
                case "CENTER RIGHT":
                    return TextAnchor.MiddleRight;
                case "TOP CENTER":
                    return TextAnchor.UpperCenter;
                case "TOP LEFT":
                    return TextAnchor.UpperLeft;
                case "TOP RIGHT":
                    return TextAnchor.UpperRight;
                default:
                    return TextAnchor.MiddleCenter;
            }
        }
#if TextMeshPro
        public static TextAlignmentOptions ToTextMeshAnchor(this TextAnchor textAnchor)
        {
            switch (textAnchor)
            {
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }
#endif

        public static string ToUITKAnchor(this TextAnchor textAnchor)
        {
            switch (textAnchor)
            {
                case TextAnchor.LowerCenter:
                    return "lower-center";
                case TextAnchor.LowerLeft:
                    return "lower-left";
                case TextAnchor.LowerRight:
                    return "lower-right";
                case TextAnchor.MiddleCenter:
                    return "middle-center";
                case TextAnchor.MiddleLeft:
                    return "middle-left";
                case TextAnchor.MiddleRight:
                    return "middle-right";
                case TextAnchor.UpperCenter:
                    return "upper-center";
                case TextAnchor.UpperLeft:
                    return "upper-left";
                case TextAnchor.UpperRight:
                    return "upper-right";
                default:
                    return "middle-center";
            }
        }

        public static string GetText(this GameObject go)
        {
#if TextMeshPro
            if (go.TryGetComponent(out TMP_Text tmpText))
            {
                return tmpText.text;
            }
#endif
#if UNITEXT
            if (go.TryGetComponent(out LightSide.UniText uniText))
            {
                return uniText.Text;
            }
#endif
            if (go.TryGetComponent(out UnityEngine.UI.Text unityText))
            {
                return unityText.text;
            }

            return null;
        }

        public static void SetText(this GameObject go, string text)
        {
            bool set = SetNovaText();
            set = SetTMPText();
            if (!set) set = SetUniText();

            if (!set)
            {
                SetUnityText();
            }

            bool SetNovaText()
            {
#if NOVA_UI_EXISTS
                if (go.TryGetComponentSafe(out Nova.TextBlock textBlock))
                {
                    textBlock.Text = text;
                    return true;
                }
#endif
                return false;
            }

            bool SetTMPText()
            {
#if TextMeshPro
                if (go.TryGetComponentSafe(out TMP_Text tmpText))
                {
                    tmpText.text = text;
                    return true;
                }
#endif
                return false;
            }

            bool SetUniText()
            {
#if UNITEXT
                if (go.TryGetComponentSafe(out LightSide.UniText uniTextComp))
                {
                    uniTextComp.Text = text;
                    return true;
                }
#endif
                return false;
            }

            void SetUnityText()
            {
                if (go.TryGetComponent(out UnityEngine.UI.Text unityText))
                {
                    unityText.text = text;
                }
            }
        }

        internal readonly struct FontWeightBucket
        {
            public readonly int Weight;
            public readonly string Name;

            public FontWeightBucket(int weight, string name)
            {
                Weight = weight;
                Name = name;
            }
        }

        internal readonly struct FontWeightKeyword
        {
            public readonly string Keyword;
            public readonly int Weight;
            public readonly Func<int, bool> Allow;

            public FontWeightKeyword(string keyword, int weight, Func<int, bool> allow = null)
            {
                Keyword = keyword;
                Weight = weight;
                Allow = allow;
            }
        }

        internal static readonly FontWeightBucket[] FontWeightBuckets = new[]
        {
            new FontWeightBucket(100,  "Thin"),
            new FontWeightBucket(200,  "ExtraLight"),
            new FontWeightBucket(300,  "Light"),
            new FontWeightBucket(400,  "Regular"),
            new FontWeightBucket(500,  "Medium"),
            new FontWeightBucket(600,  "SemiBold"),
            new FontWeightBucket(700,  "Bold"),
            new FontWeightBucket(800,  "ExtraBold"),
            new FontWeightBucket(900,  "Black"),
            new FontWeightBucket(1000, "ExtraBlack"),
        };

        internal static readonly FontWeightKeyword[] FontWeightKeywords = new[]
        {
            new FontWeightKeyword("extrablack", 1000),
            new FontWeightKeyword("extralight", 200),
            new FontWeightKeyword("ultralight", 200),
            new FontWeightKeyword("extrabold",  800),
            new FontWeightKeyword("ultrabold",  800),
            new FontWeightKeyword("semibold",   600),
            new FontWeightKeyword("demibold",   600),
            new FontWeightKeyword("medium",     500, allow: w => w <= 499),
            new FontWeightKeyword("regular",    400),
            new FontWeightKeyword("book",       400),
            new FontWeightKeyword("basic",      400),
            new FontWeightKeyword("black",      900),
            new FontWeightKeyword("light",      300),
            new FontWeightKeyword("bold",       700),
            new FontWeightKeyword("thin",       100),
        };

        public static string FontWeightToString(this int weight)
        {
            if (weight <= 0)
                return weight.ToString();

            foreach (FontWeightBucket bucket in FontWeightBuckets)
            {
                int lower = bucket.Weight == 100 ? 1 : bucket.Weight;
                int upper = bucket.Weight + 99;

                if (weight >= lower && weight <= upper)
                    return bucket.Name;
            }

            return weight.ToString();
        }

        public static string FontNameToString(this FontMetadata fontMetadata,
            bool includeWeight = true,
            bool includeItalic = true,
            FontSubset? fontSubset = null,
            bool format = false)
        {
            string fullName = fontMetadata.Family;

            if (includeWeight)
                fullName += $"-{fontMetadata.Weight.FontWeightToString()}";

            if (includeItalic)
                if (fontMetadata.FontStyle == FontStyle.Italic)
                    fullName += $"-{FontStyle.Italic}";

            if (fontSubset != null)
                fullName += $"-{fontSubset}";

            if (format)
            {
                fullName = fullName.FormatFontName();
            }

            return fullName;
        }

        public static FontMetadata GetFontMetadata(this Style style)
        {
            int fontWeight = NormalizeFontWeight(style.FontWeight, style.FontStyle, style.FontPostScriptName);

            return new FontMetadata
            {
                Family = style.FontFamily,
                Weight = fontWeight,
                FontStyle = style.Italic.ToBoolNullFalse() ? FontStyle.Italic : FontStyle.Normal,
            };
        }

        public static FontMetadata GetFontMetadata(this Style overrideStyle, Style defaultStyle)
        {
            string family = overrideStyle.FontFamily.IsEmpty() ? defaultStyle.FontFamily : overrideStyle.FontFamily;
            int fontWeight = overrideStyle.FontWeight > 0 ? overrideStyle.FontWeight : defaultStyle.FontWeight;
            string styleName = overrideStyle.FontStyle.IsEmpty() ? defaultStyle.FontStyle : overrideStyle.FontStyle;
            string postScriptName = overrideStyle.FontPostScriptName.IsEmpty() ? defaultStyle.FontPostScriptName : overrideStyle.FontPostScriptName;

            bool italic = overrideStyle.Italic.HasValue
                ? overrideStyle.Italic.Value
                : defaultStyle.Italic.ToBoolNullFalse();

            fontWeight = NormalizeFontWeight(fontWeight, styleName, postScriptName);

            return new FontMetadata
            {
                Family = family,
                Weight = fontWeight,
                FontStyle = italic ? FontStyle.Italic : FontStyle.Normal,
            };
        }

        public static List<FontMetadata> GetAllFontMetadata(this Node fobject)
        {
            var ordered = new List<FontMetadata>();
            var unique = new HashSet<FontMetadata>();

            AddUnique(fobject.Style.GetFontMetadata());

            if (!fobject.StyleOverrideTable.IsEmpty())
            {
                foreach (Style overrideStyle in fobject.StyleOverrideTable.Values)
                {
                    AddUnique(overrideStyle.GetFontMetadata(fobject.Style));
                }
            }

            if (ordered.Count <= 1)
                return ordered;

            return ordered
                .Take(1)
                .Concat(ordered
                    .Skip(1)
                    .OrderBy(x => x.Family, StringComparer.Ordinal)
                    .ThenBy(x => x.Weight)
                    .ThenBy(x => x.FontStyle))
                .ToList();

            void AddUnique(FontMetadata metadata)
            {
                if (metadata.Family.IsEmpty())
                    return;

                if (unique.Add(metadata))
                    ordered.Add(metadata);
            }
        }

        public static FontMetadata GetFontMetadata(this Node fobject)
        {
            return fobject.Style.GetFontMetadata();
        }

        private static int NormalizeFontWeight(int fontWeight, string styleName, string postScriptName)
        {
            string style = $"{styleName} {postScriptName}".FormatFontName();

            if (style.IsEmpty())
                return fontWeight;

            foreach (FontWeightKeyword kw in FontWeightKeywords)
            {
                if (!style.Contains(kw.Keyword))
                    continue;

                if (kw.Allow != null && !kw.Allow(fontWeight))
                    continue;

                return kw.Weight;
            }

            return fontWeight;
        }

        public static string FontNameToString(this Node fobject,
            bool includeWeight = true,
            bool includeItalic = true,
            FontSubset? fontSubset = null,
            bool format = false)
        {
            FontMetadata fm = fobject.GetFontMetadata();
            string fn = fm.FontNameToString(includeWeight, includeItalic, fontSubset, format);
            return fn;
        }

#if UNITEXT
        public static LightSide.HorizontalAlignment GetUniTextHAlign(this Node fobject)
        {
            switch (fobject.Style.TextAlignHorizontal)
            {
                case TextAlignHorizontal.CENTER:
                    return LightSide.HorizontalAlignment.Center;
                case TextAlignHorizontal.RIGHT:
                    return LightSide.HorizontalAlignment.Right;
                case TextAlignHorizontal.JUSTIFIED:
                    return LightSide.HorizontalAlignment.Justify;
                default:
                    return LightSide.HorizontalAlignment.Left;
            }
        }

        public static LightSide.VerticalAlignment GetUniTextVAlign(this Node fobject)
        {
            switch (fobject.Style.TextAlignVertical)
            {
                case TextAlignVertical.CENTER:
                    return LightSide.VerticalAlignment.Middle;
                case TextAlignVertical.BOTTOM:
                    return LightSide.VerticalAlignment.Bottom;
                default:
                    return LightSide.VerticalAlignment.Top;
            }
        }
#endif
    }
}
#endif