#if UNITY_EDITOR
#if VECTOR_GRAPHICS_EXISTS
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using Unity.VectorGraphics;
using UnityEngine;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class SvgImageDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            target.TryAddGraphic(out SVGImage img);

            img.sprite = sprite;
            img.material = monoBeh.Config.VectorMaterials.UnlitVectorGradientUI;
            img.raycastTarget = monoBeh.Settings.SvgImageSettings.RaycastTarget;
            img.preserveAspect = monoBeh.Settings.SvgImageSettings.PreserveAspect;
            img.raycastPadding = monoBeh.Settings.SvgImageSettings.RaycastPadding;

            monoBeh.CanvasDrawer.ImageDrawer.UnityImageDrawer.SetColor(fobject, img);
            monoBeh.CanvasDrawer.ImageDrawer.TryAddCornerRounder(fobject, target);
        }
    }
}
#endif
#endif