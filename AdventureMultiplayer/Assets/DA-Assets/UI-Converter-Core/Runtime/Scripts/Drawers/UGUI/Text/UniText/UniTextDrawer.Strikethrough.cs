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
        private static void PopulateStrikethrough(UniText text, Node fobject)
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
                if (defaultStyle.TextDecoration == TextDecoration.STRIKETHROUGH)
                    RegisterRangeRule(text, new StrikethroughModifier())
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = string.Empty });
                return;
            }

            var data = new System.Collections.Generic.List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);

            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                TextDecoration td = eff.TextDecoration;
                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    if (next.TextDecoration != td) break;
                    i++;
                }

                if (td == TextDecoration.STRIKETHROUGH)
                    data.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = string.Empty });
            }

            RegisterIfHasData(text, new StrikethroughModifier(), data);
        }
#endif
    }
}
#endif