#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
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
        private static void PopulateLink(UniText text, Node fobject, Color32 baseColor)
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
                if (!string.IsNullOrEmpty(defaultStyle.Hyperlink.Url))
                {
                    var modifier = CreateLinkModifier(baseColor);
                    RegisterRangeRule(text, modifier)
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = defaultStyle.Hyperlink.Url });
                }
                return;
            }

            var data = new System.Collections.Generic.List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);

            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                string url = GetHyperlinkUrl(eff);
                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    string nextUrl = GetHyperlinkUrl(next);

                    if (nextUrl != url) break;
                    i++;
                }

                if (!string.IsNullOrEmpty(url))
                {
                    data.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = url });
                }
            }

            RegisterIfHasData(text, CreateLinkModifier(baseColor), data);
        }

        private static LinkModifier CreateLinkModifier(Color32 baseColor)
        {
            var modifier = new LinkModifier();
            modifier.LinkColor = baseColor;
            modifier.EnableUnderline = false;

            return modifier;
        }
#endif
    }
}
#endif