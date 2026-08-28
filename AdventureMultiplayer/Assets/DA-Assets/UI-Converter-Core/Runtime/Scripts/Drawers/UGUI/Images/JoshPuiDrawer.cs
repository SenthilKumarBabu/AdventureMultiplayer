#if UNITY_EDITOR
#if JOSH_PUI_EXISTS
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class JoshPuiDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            target.TryAddGraphic(out ProceduralImage img);

            img.sprite = sprite;
            img.type = monoBeh.Settings.JoshPuiSettings.Type;
            img.raycastTarget = monoBeh.Settings.JoshPuiSettings.RaycastTarget;
            img.preserveAspect = monoBeh.Settings.JoshPuiSettings.PreserveAspect;
            img.FalloffDistance = monoBeh.Settings.JoshPuiSettings.FalloffDistance;
#if UNITY_2020_1_OR_NEWER
            img.raycastPadding = monoBeh.Settings.JoshPuiSettings.RaycastPadding;
#endif
            if (fobject.Type == NodeType.ELLIPSE)
            {
                target.TryAddComponent(out RoundModifier roundModifier);
            }
            else
            {
                if (fobject.CornerRadiuses != null)
                {
                    target.TryAddComponent(out FreeModifier freeModifier);
                    freeModifier.Radius = monoBeh.GraphicHelpers.GetCornerRadius(fobject);
                }
                else
                {
                    target.TryAddComponent(out UniformModifier uniformModifier);
                    uniformModifier.Radius = fobject.CornerRadius.ToFloat();
                }
            }

            SetColor(fobject, img);
        }

        public void SetColor(Node fobject, ProceduralImage img)
        {
            FGraphic graphic = fobject.Data.Graphic;

            FcuLogger.Debug($"SetUnityImageColor | {fobject.Data.NameHierarchy} | {fobject.Data.FcuImageType} | hasFills: {graphic.HasFill} | hasStroke: {graphic.HasStroke}", FcuDebugSettingsFlags.LogComponentDrawer);

            if (fobject.IsDrawableType())
            {
                monoBeh.CanvasDrawer.ImageDrawer.SetProceduralColor(fobject, img,
                setStrokeOnlyWidth: () =>
                {
                    img.BorderWidth = fobject.StrokeWeight;
                    fobject.SetReason(ReasonKey.Stroke_BorderWidth);
                },
                setStroke: () =>
                {
                    switch (graphic.Stroke.Align)
                    {
                        case StrokeAlign.OUTSIDE:
                            {
                                monoBeh.CanvasDrawer.ImageDrawer.AddDAOutline(fobject);
                            }
                            break;
                        default:
                            {
                                monoBeh.CanvasDrawer.ImageDrawer.RemoveDAOutline(fobject.Data.GameObject);
                                fobject.SetReason(ReasonKey.Stroke_Ignored);
                                break;
                            }
                    }
                });
            }
            else
            {
                monoBeh.CanvasDrawer.ImageDrawer.UnityImageDrawer.SetColor(fobject, img);
            }
        }
    }
}
#endif
#endif