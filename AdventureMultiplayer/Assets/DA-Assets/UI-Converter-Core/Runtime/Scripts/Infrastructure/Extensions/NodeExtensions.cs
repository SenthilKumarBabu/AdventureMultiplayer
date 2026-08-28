#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;

namespace DA_Assets.UCC.Extensions
{
    public static class NodeExtensions
    {
        public static void SetData(this Node fobject, SyncHelper syncHelper, ConverterBase fcu)
        {
            fobject.Data.ConverterBase = fcu;
            fobject.Data.GameObject = syncHelper.gameObject;
            syncHelper.Data = fobject.Data;
        }

        public static bool CanUseUnityImage(this Node fobject, ConverterBase fcu)
        {
            bool? result = null;

            if (fcu.IsUGUI())
            {
                if (fcu.UsingAnyProceduralImage())
                {
                    var conditions = fcu.Settings.ImageSpritesSettings.ProceduralCondition;

                    if (result == null && conditions.HasFlag(ProceduralCondition.Sprite) && fobject.IsDownloadableType())
                    {
                        result = true;
                    }

                    if (result == null && conditions.HasFlag(ProceduralCondition.RectangleNoRoundedCorners))
                    {
                        bool isRectOrFrame = fobject.Type == NodeType.RECTANGLE || fobject.Type == NodeType.FRAME;
                        bool hasNoCorners = !fobject.ContainsRoundedCorners();
                        bool downloadableByFills = fcu.GraphicHelpers.IsDownloadableByFills(fobject, out ReasonKey _reason).ToBoolNullFalse();
                        bool hasNoStroke = !fobject.Data.Graphic.HasStroke;

                        FcuLogger.Debug($"{nameof(CanUseUnityImage)} | {fobject.Data.NameHierarchy} | {isRectOrFrame} | {hasNoCorners} | {downloadableByFills} | {hasNoStroke} | {_reason}", FcuDebugSettingsFlags.LogComponentDrawer);

                        result = isRectOrFrame && hasNoCorners && downloadableByFills && hasNoStroke;
                    }
                }
                else if (fobject.IsSvgExtension())
                {
                    result = false;
                }
                else if (fcu.UsingSvgImage())
                {
                    if (result == null && fcu.Settings.ImageSpritesSettings.SvgCondition.HasFlag(SvgCondition.ImageOrVideo))
                    {
                        if (fobject.IsAnyImageOrVideoOrEmojiTypeInChildren())
                        {
                            result = true;
                        }
                    }

                    if (result == null && fcu.Settings.ImageSpritesSettings.SvgCondition.HasFlag(SvgCondition.AnyEffect))
                    {
                        if (fobject.IsAnyEffectInChildren())
                        {
                            result = true;
                        }
                    }
                }
            }

            FcuLogger.Debug($"{nameof(CanUseUnityImage)} | {fobject.Data.NameHierarchy} | {result}", FcuDebugSettingsFlags.LogComponentDrawer);

            return result.ToBoolNullFalse();
        }
    }
}
#endif