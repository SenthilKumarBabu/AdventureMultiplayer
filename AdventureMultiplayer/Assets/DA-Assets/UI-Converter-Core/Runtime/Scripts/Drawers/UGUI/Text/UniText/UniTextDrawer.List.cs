#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System.Collections.Generic;

#if UNITEXT
using LightSide;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static void PopulateLists(UniText text, Node fobject)
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListData(fobject.GetText(), fobject.LineTypes, fobject.LineIndentations);
            RegisterIfHasData(text, new ListModifier(), data);
        }

        private static void PopulateListSpacing(UniText text, Node fobject)
        {
            List<RangeRule.Data> data = UniTextModifierParameterBuilder.BuildListSpacingData(fobject.GetText(), fobject.LineTypes, fobject.Style.ListSpacing);
            RegisterIfHasData(text, new ParagraphSpacingModifier(), data);
        }
#endif
    }
}
#endif
