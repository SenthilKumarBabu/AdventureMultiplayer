#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using UnityEngine.UI;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class HorLayoutDrawer : FcuBase
    {
        public void Draw(Node fobject)
        {
            fobject.Data.GameObject.TryAddComponent(out HorizontalLayoutGroup layoutGroup);

            layoutGroup.childAlignment = fobject.GetHorLayoutAnchor();
            layoutGroup.padding = fobject.Data.FRect.padding.ToRectOffset();
#if UNITY_2020_1_OR_NEWER
            layoutGroup.reverseArrangement = fobject.ShouldReverseAutoLayoutDrawOrder();
#endif
            layoutGroup.childScaleWidth = false;
            layoutGroup.childScaleHeight = false;

            var childControl = fobject.GetChildControlByLayoutMode(LayoutMode.HORIZONTAL);

            if (fobject.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
            {

                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = childControl.childControlHeight;
                layoutGroup.spacing = 0;
            }
            else
            {
                layoutGroup.childControlWidth = childControl.childControlWidth;
                layoutGroup.childControlHeight = childControl.childControlHeight;
                layoutGroup.spacing = fobject.GetHorSpacing();
            }

            if (layoutGroup.childControlWidth)
            {
                layoutGroup.childForceExpandWidth = true;
            }
            else if (fobject.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
            {
                layoutGroup.childForceExpandWidth = true;
            }
            else
            {
                layoutGroup.childForceExpandWidth = false;
            }

            if (layoutGroup.childControlHeight)
            {
                layoutGroup.childForceExpandHeight = true;
            }
            else if (fobject.CounterAxisAlignItems == CounterAxisAlignItem.SPACE_BETWEEN)
            {
                layoutGroup.childForceExpandHeight = true;
            }
            else
            {
                layoutGroup.childForceExpandHeight = false;
            }


            fobject.Data.GameObject.TryAddComponent(out ContentSizeFitter csf);

            if (fobject.PrimaryAxisSizingMode == PrimaryAxisSizingMode.AUTO ||
                fobject.PrimaryAxisSizingMode == PrimaryAxisSizingMode.NONE)
            {
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            if (fobject.CounterAxisSizingMode == CounterAxisSizingMode.AUTO ||
                fobject.CounterAxisSizingMode == CounterAxisSizingMode.NONE)
            {
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            ApplyFigmaPreferredSize(fobject, csf);
        }

        private void ApplyFigmaPreferredSize(Node fobject, ContentSizeFitter csf)
        {
            if (csf.horizontalFit != ContentSizeFitter.FitMode.PreferredSize &&
                csf.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
            {
                return;
            }

            fobject.Data.GameObject.TryAddComponent(out LayoutElement layoutElement);

            if (csf.horizontalFit == ContentSizeFitter.FitMode.PreferredSize)
            {
                layoutElement.preferredWidth = fobject.Size.x;
            }

            if (csf.verticalFit == ContentSizeFitter.FitMode.PreferredSize)
            {
                layoutElement.preferredHeight = fobject.Size.y;
            }
        }
    }
}
#endif