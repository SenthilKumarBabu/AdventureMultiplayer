#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;

#if UNITEXT
using LightSide;
using Style = DA_Assets.UCC.Model.Style;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static void PopulateLineHeightAndLetterSpacing(UniText text, Node fobject)
        {
            Style defaultStyle = fobject.Style;
            System.Collections.Generic.List<int> overrides = fobject.CharacterStyleOverrides;
            System.Collections.Generic.Dictionary<string, Style> overrideTable = fobject.StyleOverrideTable;
            string chars = fobject.GetText();
            int len = chars.Length;

            bool hasOverrides = overrides != null && overrides.Count > 0
                                && overrideTable != null && overrideTable.Count > 0;

            if (!hasOverrides)
            {
                string lineHeightParameter = UniTextModifierParameterBuilder.BuildLineHeightParameter(defaultStyle);
                if (lineHeightParameter != null)
                    RegisterRangeRule(text, new LineHeightModifier())
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = lineHeightParameter });

                if (defaultStyle.LetterSpacing != 0)
                    RegisterRangeRule(text, new LetterSpacingModifier())
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = defaultStyle.LetterSpacing.ToString("G") });

                return;
            }

            var lhData = new System.Collections.Generic.List<RangeRule.Data>();
            var lsData = new System.Collections.Generic.List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);

            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                string lh = UniTextModifierParameterBuilder.BuildLineHeightParameter(eff);
                float ls = eff.LetterSpacing;
                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    if (UniTextModifierParameterBuilder.BuildLineHeightParameter(next) != lh || next.LetterSpacing != ls) break;
                    i++;
                }

                string rangeStr = $"{runStart}..{i}";

                if (lh != null)
                    lhData.Add(new RangeRule.Data { range = rangeStr, parameter = lh });

                if (ls != 0)
                    lsData.Add(new RangeRule.Data { range = rangeStr, parameter = ls.ToString("G") });
            }

            RegisterIfHasData(text, new LineHeightModifier(), lhData);
            RegisterIfHasData(text, new LetterSpacingModifier(), lsData);
        }
#endif
    }
}
#endif
