#if UNITY_EDITOR
#if NOVA_UI_EXISTS
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using Nova;
using System;
using UnityEngine;

namespace DA_Assets.UCC.Drawers
{
    [Serializable]
    public class NovaImageDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            target.TryGetComponentSafe(out UIBlock2D uIBlock2D);

            uIBlock2D.SetImage(sprite);

            if (fobject.ContainsTag(FcuTag.Slice9) || fobject.ContainsTag(FcuTag.AutoSlice9))
            {
                uIBlock2D.ImageAdjustment = new ImageAdjustment
                {
                    ScaleMode = ImageScaleMode.Sliced
                };
            }
            else
            {
                uIBlock2D.ImageAdjustment = new ImageAdjustment
                {
                    ScaleMode = ImageScaleMode.Fit
                };
            }

            if (fobject.IsDrawableType())
            {
                SetCorners(fobject, uIBlock2D);
            }

            SetColor(fobject, uIBlock2D);
        }

        private BorderDirection ConvertStrokeType(StrokeAlign strokeAlign)
        {
            switch (strokeAlign)
            {
                case StrokeAlign.INSIDE:
                    return BorderDirection.In;
                case StrokeAlign.OUTSIDE:
                    return BorderDirection.Out;
                case StrokeAlign.CENTER:
                    return BorderDirection.Center;
                default:
                    return BorderDirection.Out;
            }
        }

        public void SetColor(Node fobject, UIBlock2D sr)
        {
            FGraphic graphic = fobject.Data.Graphic;

            FcuLogger.Debug($"SetUnityImageColor | {fobject.Data.NameHierarchy} | {fobject.Data.FcuImageType} | graphic.HasFills: {graphic.HasFill} | graphic.HasStrokes: {graphic.HasStroke}", FcuDebugSettingsFlags.LogComponentDrawer);

            if (fobject.IsDrawableType() || fobject.IsGenerativeType())
            {
                if (graphic.HasFill)
                {
                    sr.Color = graphic.Fill.SingleColor;
                }

                if (sr.Sprite != null && sr.Color == default)
                {
                    sr.Color = Color.white;
                }

                if (graphic.HasStroke && !fobject.IsGenerativeType())
                {
                    Color strokeColor = graphic.Stroke.SingleColor;

                    sr.Border = new Border
                    {
                        Enabled = true,
                        Color = strokeColor,
                        Width = fobject.StrokeWeight,
                        Direction = ConvertStrokeType(fobject.StrokeAlign)
                    };
                }
                else
                {
                    sr.Border = new Border
                    {
                        Enabled = false
                    };
                }
            }
            else if (fobject.IsDownloadableType())
            {
                if (fobject.Data.Graphic.HasSingleColor)
                {
                    sr.Color = fobject.Data.Graphic.SpriteSingleColor;
                }
                else
                {
                    sr.Color = Color.white;
                }
            }
        }

        private void SetCorners(Node fobject, UIBlock2D img)
        {
            Vector4 cr = monoBeh.GraphicHelpers.GetCornerRadius(fobject);

            img.CornerRadius = new Length
            {
                Type = LengthType.Value,
                Value = cr.x
            };
        }
    }
}
#endif
#endif