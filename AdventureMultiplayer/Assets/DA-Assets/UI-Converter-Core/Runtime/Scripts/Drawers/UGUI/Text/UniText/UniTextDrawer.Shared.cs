#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
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
        private static Style GetEffectiveStyle(
            int charIndex,
            Style defaultStyle,
            List<int> overrides,
            Dictionary<string, Style> overrideTable,
            int[] charToOverrideIndex = null)
        {
            int overrideIndex = charIndex;
            if (charToOverrideIndex != null && charIndex >= 0 && charIndex < charToOverrideIndex.Length)
                overrideIndex = charToOverrideIndex[charIndex];

            if (overrideIndex >= overrides.Count)
                return defaultStyle;

            int key = overrides[overrideIndex];
            if (key == 0)
                return defaultStyle;

            if (overrideTable.TryGetValue(key.ToString(), out Style ov))
            {
                Style result = defaultStyle;

                result.LetterSpacing = ov.LetterSpacing;

                if (ov.LineHeightPx > 0)
                    result.LineHeightPx = ov.LineHeightPx;

#pragma warning disable CS0618
                if (ov.LineHeightPercent.HasValue)
                    result.LineHeightPercent = ov.LineHeightPercent;
#pragma warning restore CS0618

                if (ov.LineHeightPercentFontSize.HasValue)
                    result.LineHeightPercentFontSize = ov.LineHeightPercentFontSize;

                if (!ov.LineHeightUnit.IsEmpty())
                    result.LineHeightUnit = ov.LineHeightUnit;

                if (ov.FontSize > 0)
                    result.FontSize = ov.FontSize;

                if (!ov.FontFamily.IsEmpty())
                    result.FontFamily = ov.FontFamily;

                if (!ov.FontStyle.IsEmpty())
                    result.FontStyle = ov.FontStyle;

                if (!ov.FontPostScriptName.IsEmpty())
                    result.FontPostScriptName = ov.FontPostScriptName;

                if (ov.FontWeight > 0)
                    result.FontWeight = ov.FontWeight;

                if (ov.Italic.HasValue)
                    result.Italic = ov.Italic;

                result.TextDecoration = ov.TextDecoration;
                result.TextCase = ov.TextCase;

                if (ov.Fills != null && ov.Fills.Count > 0)
                    result.Fills = ov.Fills;

                if (!string.IsNullOrEmpty(ov.Hyperlink.Url))
                    result.Hyperlink = ov.Hyperlink;

                return result;
            }

            return defaultStyle;
        }

        private static int[] BuildCharToOverrideIndexMap(string chars)
        {
            if (string.IsNullOrEmpty(chars))
                return System.Array.Empty<int>();

            int[] map = new int[chars.Length];
            int overrideIndex = 0;
            int i = 0;

            while (i < chars.Length)
            {
                map[i] = overrideIndex;

                if (char.IsHighSurrogate(chars[i])
                    && i + 1 < chars.Length
                    && char.IsLowSurrogate(chars[i + 1]))
                {
                    map[i + 1] = overrideIndex;
                    i += 2;
                }
                else
                {
                    i++;
                }

                overrideIndex++;
            }

            return map;
        }

        private static Color32 GetSolidFillColor32(Style style)
        {
            if (style.Fills != null)
            {
                foreach (Paint fill in style.Fills)
                {
                    if (fill.Type == PaintType.SOLID)
                        return fill.Color;
                }
            }

            return new Color32(0, 0, 0, 0);
        }

        private static string GetHyperlinkUrl(Style style)
        {
            return string.IsNullOrEmpty(style.Hyperlink.Url) ? null : style.Hyperlink.Url;
        }

        private static string Color32ToHex(Color32 c)
        {
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
        }

        private static bool Color32Equals(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        private static List<RangeRule.Data> BuildRunRanges<T>(
            Node fobject,
            System.Func<Style, T> selector,
            System.Func<T, string> toParameter,
            System.Func<T, bool> include,
            System.Collections.Generic.IEqualityComparer<T> comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            var result = new List<RangeRule.Data>();

            Style defaultStyle = fobject.Style;
            var overrides = fobject.CharacterStyleOverrides;
            var overrideTable = fobject.StyleOverrideTable;
            string chars = fobject.GetText();
            int len = chars.Length;

            bool hasOverrides = overrides != null && overrides.Count > 0
                                && overrideTable != null && overrideTable.Count > 0;

            if (!hasOverrides)
                return result;

            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);
            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                T value = selector(eff);
                int runStart = i;
                i++;

                while (i < len)
                {
                    T next = selector(GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex));
                    if (!comparer.Equals(next, value))
                        break;

                    i++;
                }

                if (include(value))
                    result.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = toParameter(value) });
            }

            return result;
        }
#endif
    }

#if UNITEXT
    public static class UniTextModifierParameterBuilder
    {
        public static string BuildLineHeightParameter(Style style)
        {
            string unit = style.LineHeightUnit;

            if (unit == "FONT_SIZE_%" && style.LineHeightPercentFontSize.HasValue && style.LineHeightPercentFontSize.Value > 0f)
                return $",scaled,{(style.LineHeightPercentFontSize.Value / 100f).ToString("G")},leadingAbove";

#pragma warning disable CS0618
            if (unit == "INTRINSIC_%" && style.LineHeightPercent.HasValue && style.LineHeightPercent.Value > 0f)
                return $"{style.LineHeightPercent.Value.ToString("G")}%,content,1.2,leadingAbove";
#pragma warning restore CS0618

            if (style.LineHeightPx > 0f)
                return $"{style.LineHeightPx.ToString("G")},content,1.2,leadingAbove";

            return null;
        }

        public static string BuildTextBoxTrimParameter(LeadingTrim leadingTrim)
        {
            if (leadingTrim == LeadingTrim.CAP_HEIGHT)
                return "capHeight,baseline";

            return null;
        }

        public static string BuildParagraphSpacingParameter(float paragraphSpacing)
        {
            if (paragraphSpacing <= 0f)
                return null;

            return paragraphSpacing.ToString("G");
        }

        public static List<RangeRule.Data> BuildListData(string chars, List<string> lineTypes, List<int> lineIndentations)
        {
            var result = new List<RangeRule.Data>();

            if (string.IsNullOrEmpty(chars) || lineTypes == null || lineTypes.Count == 0)
                return result;

            var orderedNumbers = new Dictionary<int, int>();
            int lineIndex = 0;
            int lineStart = 0;

            while (lineStart <= chars.Length && lineIndex < lineTypes.Count)
            {
                int lineEnd = chars.IndexOf('\n', lineStart);
                if (lineEnd < 0)
                    lineEnd = chars.Length;

                string lineType = lineTypes[lineIndex];
                int level = GetLineIndentation(lineIndentations, lineIndex);

                if (lineType == "UNORDERED")
                {
                    ResetListCountersAtOrBelow(orderedNumbers, level);

                    if (lineEnd > lineStart)
                        result.Add(new RangeRule.Data { range = $"{lineStart}..{lineEnd}", parameter = level.ToString("G") });
                }
                else if (lineType == "ORDERED")
                {
                    ResetListCountersBelow(orderedNumbers, level);
                    int number = GetNextOrderedNumber(orderedNumbers, level);

                    if (lineEnd > lineStart)
                        result.Add(new RangeRule.Data { range = $"{lineStart}..{lineEnd}", parameter = $"{level.ToString("G")},{number.ToString("G")}" });
                }
                else
                {
                    orderedNumbers.Clear();
                }

                if (lineEnd == chars.Length)
                    break;

                lineStart = lineEnd + 1;
                lineIndex++;
            }

            return result;
        }

        public static List<RangeRule.Data> BuildListSpacingData(string chars, List<string> lineTypes, float listSpacing)
        {
            var result = new List<RangeRule.Data>();

            if (string.IsNullOrEmpty(chars) || listSpacing <= 0f || lineTypes == null || lineTypes.Count == 0)
                return result;

            int lineIndex = 0;
            int lineStart = 0;

            while (lineStart <= chars.Length && lineIndex < lineTypes.Count)
            {
                int lineEnd = chars.IndexOf('\n', lineStart);
                if (lineEnd < 0)
                    lineEnd = chars.Length;

                string lineType = lineTypes[lineIndex];
                if (IsListLineType(lineType) && lineEnd > lineStart)
                    result.Add(new RangeRule.Data { range = $"{lineStart}..{lineEnd}", parameter = listSpacing.ToString("G") });

                if (lineEnd == chars.Length)
                    break;

                lineStart = lineEnd + 1;
                lineIndex++;
            }

            return result;
        }

        private static int GetLineIndentation(List<int> lineIndentations, int lineIndex)
        {
            if (lineIndentations == null || lineIndex >= lineIndentations.Count)
                return 0;

            return Math.Max(0, lineIndentations[lineIndex]);
        }

        private static int GetNextOrderedNumber(Dictionary<int, int> orderedNumbers, int level)
        {
            orderedNumbers.TryGetValue(level, out int number);
            number++;
            orderedNumbers[level] = number;
            return number;
        }

        private static bool IsListLineType(string lineType)
        {
            return lineType == "ORDERED" || lineType == "UNORDERED";
        }

        private static void ResetListCountersAtOrBelow(Dictionary<int, int> orderedNumbers, int level)
        {
            ResetListCounters(orderedNumbers, level, true);
        }

        private static void ResetListCountersBelow(Dictionary<int, int> orderedNumbers, int level)
        {
            ResetListCounters(orderedNumbers, level, false);
        }

        private static void ResetListCounters(Dictionary<int, int> orderedNumbers, int level, bool includeLevel)
        {
            var keysToRemove = new List<int>();
            foreach (int key in orderedNumbers.Keys)
            {
                if (includeLevel ? key >= level : key > level)
                    keysToRemove.Add(key);
            }

            foreach (int key in keysToRemove)
            {
                orderedNumbers.Remove(key);
            }
        }
    }
#endif
}
#endif
