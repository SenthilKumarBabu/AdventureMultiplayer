#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    [Serializable]
    public class ImageTypeSetter : FcuBase
    {
        [SerializeField] ConcurrentBag<string> downloadableIds = new ConcurrentBag<string>();
        [SerializeField] ConcurrentBag<string> generativeIds = new ConcurrentBag<string>();
        [SerializeField] ConcurrentBag<string> drawableIds = new ConcurrentBag<string>();
        [SerializeField] ConcurrentBag<string> noneIds = new ConcurrentBag<string>();

        public ConcurrentBag<string> DownloadableIds => downloadableIds;
        public ConcurrentBag<string> GenerativeIds => generativeIds;
        public ConcurrentBag<string> DrawableIds => drawableIds;
        public ConcurrentBag<string> NoneIds => noneIds;

        public void ClearAllIds()
        {
            downloadableIds = new ConcurrentBag<string>();
            generativeIds = new ConcurrentBag<string>();
            drawableIds = new ConcurrentBag<string>();
            noneIds = new ConcurrentBag<string>();
        }

        private void SetImageFormat(Node fobject)
        {
            ImageFormat imageFormat;
            string reason;

            if (monoBeh.UsingSvgImage())
            {
                var svgCondition = monoBeh.Settings.ImageSpritesSettings.SvgCondition;
                bool hasImageOrVideo = svgCondition.HasFlag(SvgCondition.ImageOrVideo) && fobject.IsAnyImageOrVideoOrEmojiTypeInChildren();
                bool hasAnyEffect = svgCondition.HasFlag(SvgCondition.AnyEffect) && fobject.IsAnyEffectInChildren();

                if (hasImageOrVideo)
                {
                    imageFormat = ImageFormat.PNG;
                    reason = "svgCondition_imageOrVideo";
                }
                else if (hasAnyEffect)
                {
                    imageFormat = ImageFormat.PNG;
                    reason = "svgCondition_anyEffect";
                }
                else
                {
                    imageFormat = monoBeh.Settings.ImageSpritesSettings.ImageFormat;
                    reason = "svgCondition_passed";
                }
            }
            else
            {
                imageFormat = monoBeh.Settings.ImageSpritesSettings.ImageFormat;
                reason = "notUsingSvgImageComponent";
            }

            FcuLogger.Debug($"SetImageFormat | {fobject.Data.NameHierarchy} | {imageFormat} | {reason}", FcuDebugSettingsFlags.LogIsDownloadable);

            fobject.Data.ImageFormat = imageFormat;
        }

        public async Task SetInsideDownloadableFlags(List<Node> fobjects, CancellationToken token)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(fobjects, fobject =>
                {
                    if (fobject.ContainsTag(FcuTag.Image) == false)
                        return;
                    SetInsideDownloadableFlag(fobject);
                });
            }, token);
        }

        private void SetInsideDownloadableFlag(Node fobject)
        {
            Node parent;
            Node current = fobject;

            while (monoBeh.CurrentProject.TryGetParent(current, out parent))
            {
                if (monoBeh.ImageTypeSetter.DownloadableIds.Contains(parent.Id))
                {
                    fobject.Data.InsideDownloadable = true;
                    return;
                }
                current = parent;
            }

            fobject.Data.InsideDownloadable = false;
        }

        public async Task SetImageTypes(List<Node> fobjects, CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_set_image_types.Localize());

            downloadableIds = new ConcurrentBag<string>();
            generativeIds = new ConcurrentBag<string>();
            drawableIds = new ConcurrentBag<string>();
            noneIds = new ConcurrentBag<string>();

            await Task.Run(() =>
            {
                Parallel.ForEach(fobjects, fobject =>
                {
                    if (fobject.ContainsTag(FcuTag.Image) == false)
                        return;

                    SetImageFormat(fobject);

                    bool isDownloadable = IsDownloadable(fobject);
                    bool isGenerative = !fobject.Data.ForceImage && IsGenerative(fobject, isDownloadable);
                    bool isDrawable = IsDrawable(fobject);

                    if (fobject.Data.ForceImage)
                    {
                        fobject.Data.FcuImageType = FcuImageType.Downloadable;
                        downloadableIds.Add(fobject.Id);
                    }
                    else if (isGenerative)
                    {
                        fobject.Data.FcuImageType = FcuImageType.Generative;
                        generativeIds.Add(fobject.Id);
                    }
                    else if (isDownloadable)
                    {
                        fobject.Data.FcuImageType = FcuImageType.Downloadable;
                        downloadableIds.Add(fobject.Id);
                    }
                    else if (isDrawable)
                    {
                        fobject.Data.FcuImageType = FcuImageType.Drawable;
                        drawableIds.Add(fobject.Id);
                    }
                    else
                    {
                        fobject.Data.FcuImageType = FcuImageType.None;
                        noneIds.Add(fobject.Id);
                    }

                    FcuLogger.Debug($"SetImageType | {fobject.Data.NameHierarchy} | {fobject.Data.FcuImageType}", FcuDebugSettingsFlags.LogIsDownloadable);
                });

                FcuLogger.Debug($"SetImageType | {downloadableIds.Count} | {generativeIds.Count} | {drawableIds.Count} | {noneIds.Count}", FcuDebugSettingsFlags.LogIsDownloadable);
            }, token);
        }

        private bool IsDownloadable(Node fobject)
        {
            bool? result = null;
            ReasonKey reason = ReasonKey.None;

            if (fobject.Data.IsEmpty)
            {
                reason = ReasonKey.Dl_IsEmpty;
                result = false;
            }
            else if (fobject.Data.ForceImage)
            {
                reason = ReasonKey.Dl_ForceImage;
                result = true;
            }
            else if (fobject.Type == NodeType.VECTOR)
            {
                reason = ReasonKey.Dl_Vector;
                result = true;
            }
            else if (fobject.IsMask.ToBoolNullFalse())
            {
                reason = ReasonKey.Dl_IsMask;
                result = true;
            }
            else if (fobject.HaveUndownloadableTags(out ReasonKey _reason1))
            {
                reason = _reason1;
                result = false;
            }
            else if (fobject.IsArcDataFilled())
            {
                reason = ReasonKey.Dl_IsArcDataFilled;
                result = true;
            }

            if (result == null)
            {
                bool? res = monoBeh.GraphicHelpers.IsDownloadableByFills(fobject, out ReasonKey _reason2);

                if (res != null)
                {
                    reason = _reason2;
                    result = res;
                }
            }

            if (result == null)
            {
                if (!fobject.ContainsTag(FcuTag.Shadow))
                {
                    if (fobject.Effects.IsEmpty() == false)
                    {
                        int shadowCount = fobject.Effects.Count(x => x.IsVisible() && x.IsShadowType());

                        if (shadowCount > 0)
                        {
                            reason = ReasonKey.Dl_ContainsShadows;
                            result = true;
                        }
                    }
                }
            }

            if (result == null)
            {
                if (!fobject.ContainsTag(FcuTag.Blur))
                {
                    if (fobject.Effects.IsEmpty() == false)
                    {
                        int blurCount = fobject.Effects.Count(x => x.IsVisible() && x.IsBlurType());

                        if (blurCount > 0)
                        {
                            reason = ReasonKey.Dl_ContainsBlur;
                            result = true;
                        }
                    }
                }
            }

            if (result == null)
            {
                reason = ReasonKey.Dl_NoConditionMatched;
            }

            fobject.SetReason(reason);

            FcuLogger.Debug($"{nameof(IsDownloadable)} | {result} | {fobject.Data.NameHierarchy} | {reason}", FcuDebugSettingsFlags.LogIsDownloadable);
            return result.ToBoolNullFalse();
        }

        private bool IsGenerative(Node fobject, bool isDownloadable)
        {
            bool? result = null;
            ReasonKey reason = ReasonKey.None;

            FGraphic graphic = fobject.Data.Graphic;

            if (monoBeh.UsingSVG())
            {
                reason = ReasonKey.Gen_UsingSVG;
                result = false;
            }
            else if (monoBeh.UsingAnyProceduralImage())
            {
                reason = ReasonKey.Gen_UsingProceduralImage;
                result = false;
            }
            else if (monoBeh.IsUITK())
            {
                reason = ReasonKey.Gen_IsUITK;
                result = false;
            }
            else if (monoBeh.IsNova())
            {
                reason = ReasonKey.Gen_IsNova;
                result = false;
            }
            else if (isDownloadable ||
                fobject.Data.IsEmpty ||
                fobject.Data.ForceImage ||
                fobject.Type == NodeType.VECTOR ||
                fobject.IsMask.ToBoolNullFalse() ||
                fobject.IsArcDataFilled() ||
                fobject.ContainsImageEmojiVideo())
            {
                reason = ReasonKey.Gen_IsDownloadable;
                result = false;
            }
            else if (fobject.Data.IsOverlappedByStroke)
            {
                reason = ReasonKey.Gen_IsOverlappedByStroke;
                result = false;
            }
            else if (!fobject.Size.IsSupportedRenderSize(monoBeh.Settings.ImageSpritesSettings.ImageScale, out Vector2Int spriteSize, out Vector2Int _renderSize))
            {
                reason = ReasonKey.Gen_RenderSizeTooBig;
                result = false;
            }
            else if (monoBeh.GraphicHelpers.CanDrawWithUnityImage(fobject, graphic))
            {
                reason = ReasonKey.Gen_DrawableSolidFillAndStroke;
                result = false;
            }
            else if (CanGenerateSelfOnlyShape(fobject, graphic))
            {
                reason = fobject.ContainsRoundedCorners()
                    ? ReasonKey.Gen_ContainsRoundedCorners
                    : ReasonKey.Gen_CanGenerateShaderSelfOnly;
                result = true;
            }
            else if (!HasBlockedDescendantForSingleImage(fobject))
            {
                reason = ReasonKey.Gen_NoBlockedDescendants;
                result = false;
            }
            else if (!fobject.IsRectangle())
            {
                reason = ReasonKey.Gen_NotRectangle;
                result = false;
            }
            else if (CanDrawSolidFillAndStroke(fobject, graphic) ||
                     CanDrawSolidFillContainer(fobject, graphic))
            {
                reason = ReasonKey.Gen_DrawableSolidFillAndStroke;
                result = false;
            }
            else if (CanGenerateShaderSelfOnly(fobject, graphic))
            {
                reason = ReasonKey.Gen_CanGenerateShaderSelfOnly;
                result = true;
            }
            else if (graphic.HasStroke)
            {
                reason = ReasonKey.Gen_CanGenerateStrokeOnly;
                result = true;
            }
            else if (fobject.ContainsRoundedCorners())
            {
                reason = ReasonKey.Gen_ContainsRoundedCorners;
                result = true;
            }
            else if (CanGeneratePaintOrEffect(graphic, fobject))
            {
                reason = ReasonKey.Gen_CanGenerateShaderSelfOnly;
                result = true;
            }

            fobject.SetReason(reason);

            FcuLogger.Debug($"{nameof(IsGenerative)} | {result} | {fobject.Data.NameHierarchy} | {reason}", FcuDebugSettingsFlags.LogIsDownloadable);

            return result.ToBoolNullFalse();
        }

        private bool CanDrawSolidFillAndStroke(Node fobject, FGraphic graphic)
        {
            if (!monoBeh.IsUGUI() || !monoBeh.UsingUnityImage())
                return false;

            if (!fobject.ContainsTag(FcuTag.Container))
                return false;

            if (!graphic.HasFill || !graphic.HasStroke)
                return false;

            if (!graphic.Fill.HasSolid || graphic.Fill.HasGradient)
                return false;

            if (!graphic.Stroke.HasSolid || graphic.Stroke.HasGradient)
                return false;

            if (!graphic.FillAlpha1)
                return false;

            if (HasVisibleEffects(fobject) || fobject.ContainsImageEmojiVideo())
                return false;

            int visibleStrokes = fobject.Strokes.Count(x => x.IsVisible());

            return fobject.Fills.HasOnlyVisibleSolidPaints() &&
                   visibleStrokes == 1 &&
                   fobject.IndividualStrokeWeights.IsDefault();
        }

        private bool CanDrawSolidFillContainer(Node fobject, FGraphic graphic)
        {
            if (!monoBeh.IsUGUI() || !monoBeh.UsingUnityImage())
                return false;

            if (!fobject.ContainsTag(FcuTag.Container))
                return false;

            if (!fobject.IsRectangle() || fobject.ContainsRoundedCorners())
                return false;

            if (!graphic.HasFill || graphic.HasStroke)
                return false;

            if (!graphic.Fill.HasSolid || graphic.Fill.HasGradient)
                return false;

            if (HasVisibleEffects(fobject) || fobject.ContainsImageEmojiVideo())
                return false;

            return fobject.Fills.HasOnlyVisibleSolidPaints();
        }

        private bool CanGenerateShaderSelfOnly(Node fobject, FGraphic graphic)
        {
            if (!fobject.ContainsTag(FcuTag.Container))
                return false;

            bool hasVisibleEffects = HasVisibleEffects(fobject);

            return graphic.HasFill || graphic.HasStroke || hasVisibleEffects;
        }

        private static bool CanGenerateSelfOnlyShape(Node fobject, FGraphic graphic)
        {
            if (!fobject.Children.IsEmpty())
                return false;

            if (fobject.Type != NodeType.ELLIPSE && !fobject.ContainsRoundedCorners())
                return false;

            return graphic.HasFill || graphic.HasStroke || HasVisibleEffects(fobject);
        }

        private static bool HasBlockedDescendantForSingleImage(Node fobject)
        {
            if (fobject.Children.IsEmpty())
                return false;

            foreach (Node child in fobject.Children)
            {
                if (!TagSetter.EvaluateCanBeInsideSingleImage(child, applyReasons: false, out _))
                    return true;

                if (HasBlockedDescendantForSingleImage(child))
                    return true;
            }

            return false;
        }

        private bool CanGeneratePaintOrEffect(FGraphic graphic, Node fobject)
        {
            if (graphic.Fill.HasGradient || graphic.Stroke.HasGradient)
                return true;

            return HasVisibleEffects(fobject) && (graphic.HasFill || graphic.HasStroke);
        }

        private static bool HasVisibleEffects(Node fobject)
        {
            return !fobject.Effects.IsEmpty() && fobject.Effects.Any(x => x.IsVisible());
        }

        private bool IsDrawable(Node fobject)
        {
            bool result = true;
            string reason = "drawable";

            FcuLogger.Debug($"{nameof(IsDrawable)} | {result} | {fobject.Data.NameHierarchy} | {reason}", FcuDebugSettingsFlags.LogIsDownloadable);

            return result;
        }
    }
}
#endif