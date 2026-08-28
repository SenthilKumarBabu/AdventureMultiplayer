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
        private static void PopulateParagraphSpacing(UniText text, Node fobject)
        {
            string parameter = UniTextModifierParameterBuilder.BuildParagraphSpacingParameter(fobject.Style.ParagraphSpacing);
            if (parameter == null)
                return;

            RegisterRangeRule(text, new ParagraphSpacingModifier())
                .data.Add(new RangeRule.Data { range = string.Empty, parameter = parameter });
        }
#endif
    }
}
#endif
