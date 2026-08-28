#if UNITY_EDITOR
using DA_Assets.UCC.Model;

#if UNITEXT
using LightSide;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private static void PopulateVerticalTrim(UniText text, Node fobject)
        {
            if (!fobject.Style.LeadingTrim.HasValue)
                return;

            string parameter = UniTextModifierParameterBuilder.BuildTextBoxTrimParameter(fobject.Style.LeadingTrim.Value);
            if (parameter == null)
                return;

            RegisterRangeRule(text, new TextBoxTrimModifier())
                .data.Add(new RangeRule.Data { range = string.Empty, parameter = parameter });
        }
#endif
    }
}
#endif
