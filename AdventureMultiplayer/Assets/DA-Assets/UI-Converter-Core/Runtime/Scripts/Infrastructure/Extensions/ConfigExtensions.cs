#if UNITY_EDITOR
using UnityEngine;

namespace DA_Assets.UCC.Extensions
{
    public static class ConfigExtensions
    {
        public static bool IsPlaying(this ConverterBase fcu) => Application.isPlaying;

        public static bool IsNova(this ConverterBase fcu) => fcu.Settings.MainSettings.UIFramework == UIFramework.NOVA;
        public static bool IsUITK(this ConverterBase fcu) => fcu.Settings.MainSettings.UIFramework == UIFramework.UITK;
        public static bool IsUGUI(this ConverterBase fcu) => fcu.Settings.MainSettings.UIFramework == UIFramework.UGUI;
        public static bool IsDebug(this ConverterBase fcu) => FcuDebugSettings.Settings.HasFlag(FcuDebugSettingsFlags.DebugMode);

        public static bool UsingAnyProceduralImage(this ConverterBase fcu) =>
             fcu.UsingJoshPui() || fcu.UsingDttPui() || fcu.UsingShapes2D() || fcu.UsingMPUIKit() || fcu.UsingFlexibleImage();

        public static bool UsingUnityButton(this ConverterBase fcu) =>
            fcu.Settings.ButtonSettings.ButtonComponent == ButtonComponent.UnityButton;

        public static bool UsingDAButton(this ConverterBase fcu) =>
            fcu.Settings.ButtonSettings.ButtonComponent == ButtonComponent.DAButton;

        public static bool UsingTrueShadow(this ConverterBase fcu) =>
            fcu.Settings.ShadowSettings.ShadowComponent == ShadowComponent.TrueShadow;

        public static bool UsingUnityText(this ConverterBase fcu) =>
            fcu.Settings.TextFontsSettings.TextComponent == TextComponent.UnityEngine_UI_Text;

        public static bool UsingTextMesh(this ConverterBase fcu) =>
            fcu.Settings.TextFontsSettings.TextComponent == TextComponent.TextMeshPro || fcu.Settings.TextFontsSettings.TextComponent == TextComponent.RTL_TextMeshPro;

        public static bool UsingRTLTextMeshPro(this ConverterBase fcu) =>
            fcu.Settings.TextFontsSettings.TextComponent == TextComponent.RTL_TextMeshPro;

        public static bool UsingUI_Toolkit_Text(this ConverterBase fcu) =>
            fcu.Settings.TextFontsSettings.TextComponent == TextComponent.UI_Toolkit_Text;

        public static bool UsingUniText(this ConverterBase fcu) =>
            fcu.Settings.TextFontsSettings.TextComponent == TextComponent.UniText;

        public static bool UsingSpriteRenderer(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.SpriteRenderer;

        public static bool UsingSvgImage(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.SvgImage;

        public static bool UsingSVG(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageFormat == ImageFormat.SVG;

        public static bool UsingShapes2D(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.SubcShape;

        public static bool UsingUnityImage(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.UnityImage;

        public static bool UsingRawImage(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.RawImage;

        public static bool UsingJoshPui(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.ProceduralImage;

        public static bool UsingDttPui(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.RoundedImage;

        public static bool UsingUIBlock2D(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.UIBlock2D;

        public static bool UsingUI_Toolkit_Image(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.UI_Toolkit_Image;

        public static bool UsingMPUIKit(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.MPImage;

        public static bool UsingFlexibleImage(this ConverterBase fcu) =>
            fcu.Settings.ImageSpritesSettings.ImageComponent == ImageComponent.FlexibleImage;

        public static bool UseImageLinearMaterial(this ConverterBase fcu)
        {
#if UNITY_EDITOR
            if (UnityEditor.PlayerSettings.colorSpace == UnityEngine.ColorSpace.Linear)
            {
                if (fcu.Settings.ImageSpritesSettings.UseImageLinearMaterial)
                {
                    return true;
                }
            }
#endif
            return false;
        }
    }
}
#endif