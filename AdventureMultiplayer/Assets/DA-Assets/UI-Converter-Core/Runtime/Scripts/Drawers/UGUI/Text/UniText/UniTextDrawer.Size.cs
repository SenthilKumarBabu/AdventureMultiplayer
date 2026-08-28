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
        private static void PopulateSize(UniText text, Node fobject)
        {
            Style defaultStyle = fobject.Style;
            var overrides = fobject.CharacterStyleOverrides;
            var overrideTable = fobject.StyleOverrideTable;
            string chars = fobject.GetText();
            int len = chars.Length;

            bool hasOverrides = overrides != null && overrides.Count > 0
                                && overrideTable != null && overrideTable.Count > 0;



            if (!hasOverrides)
                return;

            float defaultFontSize = defaultStyle.FontSize;
            var data = new System.Collections.Generic.List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);

            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                float fs = eff.FontSize;
                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    if (next.FontSize != fs) break;
                    i++;
                }

                if (fs > 0 && fs != defaultFontSize)
                    data.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = fs.ToString("G") });
            }

            RegisterIfHasData(text, new SizeModifier(), data);
        }
#endif
    }
}
#endif