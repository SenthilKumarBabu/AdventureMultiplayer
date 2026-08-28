#if UNITY_EDITOR
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
        private static void PopulateEllipsis(UniText text, Node fobject)
        {
            if (fobject.Style.TextTruncation == TextTruncation.ENDING)
                RegisterRangeRule(text, new EllipsisModifier())
                    .data.Add(new RangeRule.Data { range = string.Empty, parameter = "1" });
        }
#endif
    }
}
#endif