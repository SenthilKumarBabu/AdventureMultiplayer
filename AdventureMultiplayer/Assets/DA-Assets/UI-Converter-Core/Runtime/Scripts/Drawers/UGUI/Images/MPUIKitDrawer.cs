#if UNITY_EDITOR
#if MPUIKIT_EXISTS
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using MPUIKIT;
using System;
using System.Reflection;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class MPUIKitDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            target.TryAddGraphic(out MPImage img);
            SetCorners(fobject, img);

            SetColor(fobject, img);

            img.sprite = sprite;
            img.type = monoBeh.Settings.MPUIKitSettings.Type;
            img.raycastTarget = monoBeh.Settings.MPUIKitSettings.RaycastTarget;
            img.preserveAspect = monoBeh.Settings.MPUIKitSettings.PreserveAspect;
            img.FalloffDistance = monoBeh.Settings.MPUIKitSettings.FalloffDistance;

#if UNITY_2020_1_OR_NEWER
            img.raycastPadding = monoBeh.Settings.MPUIKitSettings.RaycastPadding;
#endif

            MethodInfo initMethod = typeof(MPImage).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            initMethod.Invoke(img, null);
        }

        public void SetColor(Node fobject, MPImage img)
        {
            FGraphic graphic = fobject.Data.Graphic;

            FcuLogger.Debug($"SetUnityImageColor | {fobject.Data.Hierarchy} | {fobject.Data.FcuImageType} | hasFills: {graphic.HasFill} | hasStroke: {graphic.HasStroke}", FcuDebugSettingsFlags.LogComponentDrawer);

            img.GradientEffect = new GradientEffect
            {
                Enabled = false,
                GradientType = MPUIKIT.GradientType.Linear,
                Gradient = null
            };

            if (fobject.IsDrawableType())
            {
                monoBeh.CanvasDrawer.ImageDrawer.SetProceduralColor(fobject, img,
                setStrokeOnlyWidth: () =>
                {
                    img.StrokeWidth = fobject.StrokeWeight;
                    fobject.SetReason(ReasonKey.Stroke_BorderWidth);
                },
                setStroke: () =>
                {
                    switch (graphic.Stroke.Align)
                    {
                        case StrokeAlign.INSIDE:
                            {
                                img.OutlineColor = graphic.Stroke.SingleColor;
                                img.OutlineWidth = fobject.StrokeWeight;
                                monoBeh.CanvasDrawer.ImageDrawer.RemoveDAOutline(fobject.Data.GameObject);
                                fobject.SetReason(ReasonKey.Stroke_NativeOutline);
                            }
                            break;
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

        private void SetCorners(Node fobject, MPImage img)
        {
            if (fobject.Type == NodeType.ELLIPSE)
            {
                img.DrawShape = DrawShape.Circle;
                img.Circle = new Circle
                {
                    FitToRect = true
                };
            }
            else
            {
                img.DrawShape = DrawShape.Rectangle;

                img.Rectangle = new Rectangle
                {
                    CornerRadius = monoBeh.GraphicHelpers.GetCornerRadius(fobject)
                };
            }
        }
    }
}
#endif
#endif