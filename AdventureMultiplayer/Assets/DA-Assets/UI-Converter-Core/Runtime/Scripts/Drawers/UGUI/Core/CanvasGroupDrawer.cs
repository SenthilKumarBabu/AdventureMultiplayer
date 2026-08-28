#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using UnityEngine;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class CanvasGroupDrawer : FcuBase
    {
        public void Draw(Node fobject)
        {
            if (fobject.Data.FcuImageType == FcuImageType.Downloadable && !fobject.Data.ManualWhiteColor)
                return;

            fobject.Data.GameObject.TryAddComponent(out CanvasGroup canvasGroup);
            canvasGroup.alpha = (float)fobject.Opacity;
        }
    }
}
#endif