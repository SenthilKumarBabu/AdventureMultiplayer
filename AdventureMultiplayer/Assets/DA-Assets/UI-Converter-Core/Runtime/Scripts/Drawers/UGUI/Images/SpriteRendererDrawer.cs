#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
#if DA_UI_COMPONENTS_EXISTS
using DA_Assets.UI;
#endif
using System;
using UnityEngine;

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    [Serializable]
    public class SpriteRendererDrawer : FcuBase
    {
        public void Draw(Node fobject, Sprite sprite, GameObject target)
        {
            target.TryAddComponent(out SpriteRenderer sr);
            sr.sprite = sprite;
            sr.sortingOrder = target.transform.GetSiblingIndex();

            if (sprite == null)
            {
                sr.sprite = monoBeh.Config.WhiteSprite32px;
                sr.drawMode = SpriteDrawMode.Tiled;
                Vector2 size = target.GetComponent<RectTransform>().rect.size;
                sr.size = size;
                SetColor(fobject, sr);
            }
            else
            {
                sr.drawMode = SpriteDrawMode.Simple;

                if (fobject.Data.FcuImageType == FcuImageType.Generative || fobject.Data.Graphic.HasSingleColor)
                {
                    SetColor(fobject, sr);
                }
                else
                {
                    sr.color = Color.white;
                }
            }

            sr.flipX = monoBeh.Settings.SpriteRendererSettings.FlipX;
            sr.flipY = monoBeh.Settings.SpriteRendererSettings.FlipY;
            sr.maskInteraction = monoBeh.Settings.SpriteRendererSettings.MaskInteraction;
            sr.spriteSortPoint = monoBeh.Settings.SpriteRendererSettings.SortPoint;
            sr.sortingLayerName = monoBeh.Settings.SpriteRendererSettings.SortingLayer;
        }

        public void SetColor(Node fobject, SpriteRenderer img)
        {
            FGraphic graphic = fobject.Data.Graphic;

            FcuLogger.Debug($"SetUnityImageColor | {fobject.Data.NameHierarchy} | {fobject.Data.FcuImageType} | graphic.HasFills: {graphic.HasFill} | graphic.HasStrokes: {graphic.HasStroke}", FcuDebugSettingsFlags.LogComponentDrawer);

#if DA_UI_COMPONENTS_EXISTS
            void AddOutline(float strokeWeight, float cornerRadius, Color color)
            {
                img.gameObject.TryAddComponent(out SpriteOutline uiOutline);
                uiOutline.OutlineWidth = strokeWeight;
                uiOutline.CornerSegments = 10;
                uiOutline.CornerRadius = cornerRadius;
                uiOutline.FillCenter = false;
                uiOutline.color = color;
            }
#endif

            if (fobject.IsDrawableType())
            {
                Vector4 radius = monoBeh.GraphicHelpers.GetCornerRadius(fobject);

                bool strokeOnly = graphic.HasStroke && !graphic.HasFill;

                if (strokeOnly)
                {
                    Color tr = Color.white;
                    tr.a = 0;
                    img.color = tr;
                    fobject.SetReason(ReasonKey.Fill_Transparent);

#if DA_UI_COMPONENTS_EXISTS
                    AddOutline(fobject.StrokeWeight, radius.x, graphic.Stroke.SingleColor);
                    fobject.SetReason(ReasonKey.Stroke_SpriteOutline);
#endif
                }
                else
                {
                    img.color = graphic.Fill.SingleColor;
                    fobject.SetReason(ReasonKey.Fill_SolidColor);

                    if (graphic.HasStroke)
                    {
#if DA_UI_COMPONENTS_EXISTS
                        AddOutline(fobject.StrokeWeight, radius.x, graphic.Stroke.SingleColor);
                        fobject.SetReason(ReasonKey.Stroke_SpriteOutline);
#endif
                    }
                }

                if (!graphic.HasStroke)
                {
#if DA_UI_COMPONENTS_EXISTS
                    fobject.Data.GameObject.TryDestroyComponent<SpriteOutline>();
#endif
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
        }
    }
}
#endif