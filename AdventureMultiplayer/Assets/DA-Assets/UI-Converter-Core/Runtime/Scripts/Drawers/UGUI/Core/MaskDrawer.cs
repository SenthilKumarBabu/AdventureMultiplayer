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
    public class MaskDrawer : FcuBase
    {
        public void Draw(Node fobject)
        {
            GameObject targetGo;

            if (fobject.IsObjectMask() && !fobject.ContainsTag(FcuTag.Frame))
            {
                monoBeh.CurrentProject.TryGetByIndex(fobject.Data.ParentIndex, out Node target);
                targetGo = target.Data.GameObject;
            }
            else
            {
                targetGo = fobject.Data.GameObject;
            }

            ReasonKey reason = ReasonKey.None;

            if (fobject.IsObjectMask())
            {
                if (monoBeh.IsNova())
                {
#if NOVA_UI_EXISTS
                    reason = ReasonKey.Mask_ObjectMaskNova;
                    targetGo.TryAddComponent(out Nova.ClipMask unityMask);
                    Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);
                    unityMask.Mask = sprite.texture;
#endif
                }
                else if (!monoBeh.UsingSpriteRenderer())
                {
                    reason = ReasonKey.Mask_ObjectMaskCanvas;
                    monoBeh.CanvasDrawer.ImageDrawer.Draw(fobject, targetGo);
                    targetGo.TryAddComponent(out Mask unityMask);
                    unityMask.showMaskGraphic = false;
                }
                else
                {
                    reason = ReasonKey.Mask_ObjectMaskSpriteRenderer;
                }

                fobject.Data.GameObject.Destroy();
            }
            else if (fobject.IsFrameMask() || fobject.IsClipMask())
            {
                if (monoBeh.IsNova())
                {
#if NOVA_UI_EXISTS
                    reason = ReasonKey.Mask_FrameClipMaskNova;
                    targetGo.TryAddComponent(out Nova.ClipMask unityMask);
#endif
                }
                else if (monoBeh.UseImageLinearMaterial() || fobject.Data.FRect.absoluteAngle != 0)
                {
                    reason = ReasonKey.Mask_FrameClipMaskLinearOrRotated;
                    targetGo.TryAddComponent(out Mask unityMask);
                }
                else if (!monoBeh.UsingSpriteRenderer())
                {
                    reason = ReasonKey.Mask_FrameClipMaskRectMask2D;
                    targetGo.TryAddComponent(out RectMask2D unityMask);
                }
                else
                {
                    reason = ReasonKey.Mask_FrameClipMaskSpriteRenderer;
                }
            }

            fobject.SetReason(reason);

            FcuLogger.Debug($"Draw | {fobject.Data.NameHierarchy} | {reason}", FcuDebugSettingsFlags.LogMask);
        }
    }
}
#endif