#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class UnityImageDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            MaskableGraphic graphic;

            if (monoBeh.UsingRawImage())
            {
                target.TryAddGraphic(out RawImage img);
                graphic = img;

                if (sprite != null)
                {
                    img.texture = sprite.texture;
                }
            }
            else
            {
                target.TryAddGraphic(out Image img);
                graphic = img;

                img.sprite = sprite;
                img.type = monoBeh.Settings.UnityImageSettings.Type;
                img.preserveAspect = monoBeh.Settings.UnityImageSettings.PreserveAspect;
            }

            graphic.raycastTarget = monoBeh.Settings.UnityImageSettings.RaycastTarget;
            graphic.maskable = monoBeh.Settings.UnityImageSettings.Maskable;
#if UNITY_2020_1_OR_NEWER
            graphic.raycastPadding = monoBeh.Settings.UnityImageSettings.RaycastPadding;
#endif

            if (fobject.Data.UseImageLinearMaterial && monoBeh.UseImageLinearMaterial())
            {
                graphic.material = monoBeh.Config.ImageLinearMaterial;
            }
            else
            {
                graphic.material = null;
            }

            SetColor(fobject, graphic);
            monoBeh.CanvasDrawer.ImageDrawer.TryAddCornerRounder(fobject, target);
#if IMAGE_OVERFLOW_EXISTS
            ImageOverflowUtility.ApplyToImage(fobject, monoBeh, target);
#endif
        }

        public void SetColor(Node fobject, MaskableGraphic img)
        {
            FGraphic graphic = fobject.Data.Graphic;

            FcuLogger.Debug($"SetUnityImageColor | {fobject.Data.NameHierarchy} | {fobject.Data.FcuImageType} | graphic.HasFills: {graphic.HasFill} | graphic.HasStrokes: {graphic.HasStroke}", FcuDebugSettingsFlags.LogComponentDrawer);

            if (fobject.ContainsTag(FcuTag.BtnDisabled))
            {



                img.color = Color.white;
            }
            else if (fobject.IsDrawableType())
            {
                bool strokeOnly = graphic.HasStroke && !graphic.HasFill;

                if (strokeOnly)
                {
                    img.color = default;
                    fobject.SetReason(ReasonKey.Fill_Transparent);
                }
                else if (graphic.Fill.HasSolid)
                {
                    img.color = graphic.Fill.SolidPaint.Color;
                    fobject.SetReason(ReasonKey.Fill_SolidColor);
                }
                else if (graphic.Fill.HasGradient)
                {
                    img.color = Color.white;
                    monoBeh.CanvasDrawer.ImageDrawer.AddGradient(fobject, graphic.Fill.GradientPaint);
                    fobject.SetReason(ReasonKey.Fill_GradientComponent);
                }

                if (graphic.HasStroke)
                {
                    monoBeh.CanvasDrawer.ImageDrawer.AddDAOutline(fobject);
                }
                else
                {
                    monoBeh.CanvasDrawer.ImageDrawer.RemoveDAOutline(fobject.Data.GameObject);
                    fobject.SetReason(ReasonKey.None);
                }
            }
            else if (fobject.IsGenerativeType())
            {
                monoBeh.CanvasDrawer.ImageDrawer.ClearBakedSpriteOverlays(fobject);

                if (fobject.Data.Graphic.HasSingleColor)
                {
                    img.color = fobject.Data.Graphic.SpriteSingleColor;
                    fobject.SetReason(ReasonKey.Fill_SingleColorTint);
                }
                else
                {
                    img.color = Color.white;
                    fobject.SetReason(ReasonKey.Fill_BakedInSprite);
                }

                fobject.SetReason(ReasonKey.Stroke_BakedInSprite);
            }
            else if (fobject.IsDownloadableType())
            {
                monoBeh.CanvasDrawer.ImageDrawer.RemoveDAOutline(img.gameObject);

                if (fobject.Data.Graphic.HasSingleColor)
                {
                    img.color = fobject.Data.Graphic.SpriteSingleColor;
                    fobject.SetReason(ReasonKey.Fill_SingleColorTint);
                }
                else if (fobject.Data.Graphic.HasSingleGradient && !monoBeh.Settings.ImageSpritesSettings.DownloadOptions.HasFlag(SpriteDownloadOptions.SupportedGradients))
                {
                    monoBeh.CanvasDrawer.ImageDrawer.AddGradient(fobject, fobject.Data.Graphic.SpriteSingleLinearGradient);
                    fobject.SetReason(ReasonKey.Fill_GradientComponent);
                }
                else
                {
                    img.color = Color.white;
                    fobject.SetReason(ReasonKey.Fill_BakedInSprite);
                }

                fobject.SetReason(ReasonKey.Stroke_BakedInSprite);
            }
        }
    }
}
#endif