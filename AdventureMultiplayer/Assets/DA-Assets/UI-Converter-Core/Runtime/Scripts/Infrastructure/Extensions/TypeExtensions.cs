#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Extensions;
using System.Linq;

namespace DA_Assets.UCC.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsAnyEffectInChildren(this Node fobject)
        {
            if (fobject.Effects != null && fobject.Effects.Any(effect => effect.IsVisible()))
            {
                return true;
            }

            if (fobject.Children != null)
            {
                foreach (var child in fobject.Children)
                {
                    if (child.IsAnyEffectInChildren())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsAnyImageOrVideoOrEmojiTypeInChildren(this Node fobject)
        {
            if (fobject.IsAnyImageOrVideoOrEmojiType())
            {
                return true;
            }

            if (fobject.Children != null)
            {
                foreach (var child in fobject.Children)
                {
                    if (child.IsAnyImageOrVideoOrEmojiTypeInChildren())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsShadowType(this Effect effect) => effect.Type.ToString().Contains("SHADOW");
        public static bool IsBlurType(this Effect effect) => effect.Type.ToString().Contains("BLUR");

        public static bool IsGradientType(this Paint paint) => paint.Type.ToString().Contains("GRADIENT");

        public static bool IsAnyImageOrVideoOrEmojiType(this Node fobject)
        {
            if (fobject.Fills.IsEmpty())
                return false;

            return fobject.Fills.Any(fill =>
                fill.IsVisible() &&
                (fill.Type == PaintType.IMAGE ||
                 fill.Type == PaintType.VIDEO ||
                 fill.Type == PaintType.EMOJI));
        }

        public static bool IsSingleImageOrVideoOrEmojiType(this Node fobject)
        {
            bool hasImageOrVideo = fobject.IsAnyImageOrVideoOrEmojiType();
            return hasImageOrVideo || fobject.Data.ForceImage;
        }

        public static bool IsAnyMask(this Node fobject) => fobject.IsObjectMask() || fobject.IsClipMask() || fobject.IsFrameMask();
        public static bool IsFrameMask(this Node fobject) => fobject.ContainsTag(FcuTag.Frame);
        public static bool IsClipMask(this Node fobject) => fobject.ClipsContent.ToBoolNullFalse();
        public static bool IsObjectMask(this Node fobject) => fobject.IsMask.ToBoolNullFalse();
        public static bool IsGenerativeType(this Node fobject) => fobject.Data.FcuImageType == FcuImageType.Generative;
        public static bool IsDrawableType(this Node fobject) => fobject.Data.FcuImageType == FcuImageType.Drawable;
        public static bool IsDownloadableType(this Node fobject) => fobject.Data.FcuImageType == FcuImageType.Downloadable;
        public static bool IsMaskType(this Node fobject) => fobject.Data.FcuImageType == FcuImageType.Mask;
    }
}
#endif