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
        private void PopulateBold(UniText text, Node fobject)
        {
            Style defaultStyle = fobject.Style;
            var overrides = fobject.CharacterStyleOverrides;
            var overrideTable = fobject.StyleOverrideTable;
            bool hasOverrides = overrides != null && overrides.Count > 0
                                && overrideTable != null && overrideTable.Count > 0;

            if (!hasOverrides)
            {
                FontMetadata defaultFont = defaultStyle.GetFontMetadata();

                if (monoBeh.FontLoader.NeedsUniTextBoldModifier(defaultFont))
                {
                    RegisterRangeRule(text, new BoldModifier())
                        .data.Add(new RangeRule.Data { range = string.Empty, parameter = defaultFont.Weight.ToString() });
                }

                return;
            }

            var data = BuildRunRanges(
                fobject,
                selector: style => style.GetFontMetadata(),
                toParameter: metadata => metadata.Weight.ToString(),
                include: metadata => monoBeh.FontLoader.NeedsUniTextBoldModifier(metadata));

            RegisterIfHasData(text, new BoldModifier(), data);
        }
#endif
    }
}
#endif