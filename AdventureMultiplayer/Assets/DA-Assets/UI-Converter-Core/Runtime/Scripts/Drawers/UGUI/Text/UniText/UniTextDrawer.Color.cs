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
        private static void PopulateColor(UniText text, Node fobject, Color32 baseColor)
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

            var data = new System.Collections.Generic.List<RangeRule.Data>();
            int[] charToOverrideIndex = BuildCharToOverrideIndexMap(chars);

            int i = 0;
            while (i < len)
            {
                Style eff = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                Color32 col = GetSolidFillColor32(eff);


                if (col.r == 0 && col.g == 0 && col.b == 0 && col.a == 0)
                    col = baseColor;

                int runStart = i++;

                while (i < len)
                {
                    Style next = GetEffectiveStyle(i, defaultStyle, overrides, overrideTable, charToOverrideIndex);
                    Color32 nextCol = GetSolidFillColor32(next);
                    if (nextCol.r == 0 && nextCol.g == 0 && nextCol.b == 0 && nextCol.a == 0)
                        nextCol = baseColor;
                    if (!Color32Equals(nextCol, col)) break;
                    i++;
                }

                if (!Color32Equals(col, baseColor))
                    data.Add(new RangeRule.Data { range = $"{runStart}..{i}", parameter = Color32ToHex(col) });
            }

            RegisterIfHasData(text, new ColorModifier(), data);
        }
#endif
    }
}
#endif