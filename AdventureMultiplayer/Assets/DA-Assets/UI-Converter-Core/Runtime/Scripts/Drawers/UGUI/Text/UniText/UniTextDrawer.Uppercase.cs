#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Globalization;

#if UNITEXT
using LightSide;
using Style = DA_Assets.UCC.Model.Style;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static void PopulateTextCase(UniText text, Node fobject)
        {
            Style defaultStyle = fobject.Style;
            var overrides = fobject.CharacterStyleOverrides;
            var overrideTable = fobject.StyleOverrideTable;
            string chars = fobject.GetText();
            int len = chars.Length;

            bool hasOverrides = overrides != null && overrides.Count > 0
                                && overrideTable != null && overrideTable.Count > 0;

            if (!hasOverrides)
            {
                ApplyGlobalTextCase(text, defaultStyle.TextCase);
                return;
            }

            var upperData = new List<RangeRule.Data>();
            var lowerData = new List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);




            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                TextCase tc = eff.TextCase;
                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    if (next.TextCase != tc) break;
                    i++;
                }

                switch (tc)
                {
                    case TextCase.UPPER:
                        upperData.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = string.Empty });
                        break;
                    case TextCase.LOWER:
                        lowerData.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = string.Empty });
                        break;

                }
            }

            RegisterIfHasData(text, new UppercaseModifier(), upperData);
            RegisterIfHasData(text, new LowercaseModifier(), lowerData);
        }

        private static void ApplyGlobalTextCase(UniText text, TextCase textCase)
        {
            switch (textCase)
            {
                case TextCase.UPPER:
                    RegisterRangeRule(text, new UppercaseModifier())
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = string.Empty });
                    break;
                case TextCase.LOWER:
                    RegisterRangeRule(text, new LowercaseModifier())
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = string.Empty });
                    break;

            }
        }

        internal static string ApplyTitleCase(string input, TextCase textCase)
        {
            if (textCase != TextCase.TITLE || string.IsNullOrEmpty(input))
                return input;

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());
        }
#endif
    }
}
#endif