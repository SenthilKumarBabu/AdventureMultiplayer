#if UNITY_EDITOR
using DA_Assets.Extensions;
#if DA_UI_COMPONENTS_EXISTS
using DA_Assets.UI;
#endif
using DA_Assets.UCC.Model;
using System;
using UnityEngine;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class LayoutGridDrawer : FcuBase
    {
        public void Draw(Node fobject)
        {
            if (fobject.LayoutGrids.IsEmpty())
                return;

#if DA_UI_COMPONENTS_EXISTS

            FigmaLayoutGrid[] oldComponents = fobject.Data.GameObject.GetComponents<FigmaLayoutGrid>();
            foreach (FigmaLayoutGrid old in oldComponents)
            {
                old.Destroy();
            }


            foreach (var layoutGrid in fobject.LayoutGrids)
            {
                fobject.Data.GameObject.TryAddComponent(out FigmaLayoutGrid component, supportMultiInstance: true);


                LayoutGridPattern pattern = LayoutGridPattern.GRID;
                if (layoutGrid.Pattern == "ROWS")
                    pattern = LayoutGridPattern.ROWS;
                else if (layoutGrid.Pattern == "COLUMNS")
                    pattern = LayoutGridPattern.COLUMNS;


                LayoutGridAlignment alignment = LayoutGridAlignment.MIN;
                if (!string.IsNullOrEmpty(layoutGrid.Alignment))
                {
                    if (layoutGrid.Alignment == "MAX")
                        alignment = LayoutGridAlignment.MAX;
                    else if (layoutGrid.Alignment == "STRETCH")
                        alignment = LayoutGridAlignment.STRETCH;
                    else if (layoutGrid.Alignment == "CENTER")
                        alignment = LayoutGridAlignment.CENTER;
                }

                component.Pattern = pattern;
                component.SectionSize = layoutGrid.SectionSize;
                component.Visible = layoutGrid.Visible;
                component.Color = layoutGrid.Color;
                component.Alignment = alignment;
                component.GutterSize = layoutGrid.GutterSize;
                component.Offset = layoutGrid.Offset;
                component.Count = layoutGrid.Count;
            }
#endif
        }
    }
}
#endif