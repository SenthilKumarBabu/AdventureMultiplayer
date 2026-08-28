#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC
{
    public enum ImportMode
    {
        Url,
        Zip
    }

    public enum RequestType
    {
        Get,
        Post,
        GetFile,
    }

    public enum RequestName
    {
        None,
        Project,
    }

    public enum FcuNameType
    {
        Object,
        Field,
        Method,
        File,
        UitkGuid,
        Class,
        UssClass,
        LocKey,

        HumanizedTextPrefabName,
        UxmlPath,

        Figma,
        UITK_SpritePath,
        UITK_FontPath,
        Folder
    }

    public enum FieldSerializationMode
    {
        SyncHelpers = 0,
        Attributes = 1,
        GameObjectNames = 2
    }

    public enum TextPrefabNameType
    {
        HumanizedColorString,
        HumanizedColorHEX,
        Figma,
    }

    [Flags]
    public enum SpriteDownloadOptions
    {
        None = 0,
        MultipleFills = 1 << 0,
        SupportedGradients = 1 << 1,
        UnsupportedGradients = 1 << 2
    }

    public enum ButtonTransitionType
    {
        Default,
        SpriteSwapForAll
    }

    [Flags]
    public enum ProceduralCondition
    {
        Sprite = 1 << 0,
        RectangleNoRoundedCorners = 1 << 1,
    }

    [Flags]
    public enum SvgCondition
    {
        ImageOrVideo = 1 << 0,
        AnyEffect = 1 << 1,
    }

    public enum PreserveRatioMode
    {
        None,
        WidthControlsHeight,
        HeightControlsWidth,
    }

    public enum FcuImageType
    {
        None,
        Downloadable,
        Drawable,
        Generative,
        Mask
    }

    public enum PositioningMode
    {
        Absolute = 0,
        GameView = 1
    }

    public enum UIFramework
    {
        UGUI = 0,
        UITK = 1,
        NOVA = 2
    }

    public enum ImageFormat
    {
        PNG = 0,
        JPG = 1,
        SVG = 2
    }

    public enum ImageComponent
    {
        UnityImage = 0,
        SubcShape = 1,
        MPImage = 2,
        ProceduralImage = 3,
        RawImage = 4,
        SpriteRenderer = 5,
        RoundedImage = 6,
        UIBlock2D = 7,
        SvgImage = 8,
        UI_Toolkit_Image = 9,
        FlexibleImage = 10
    }

    public enum TextComponent
    {
        UnityEngine_UI_Text = 0,
        TextMeshPro = 1,
        RTL_TextMeshPro = 2,
        UI_Toolkit_Text = 3,
        UniText = 4
    }

    public enum LineHeightMode
    {
        Standard = 0,
        CapHeightToBaseline = 1
    }

    public enum ShadowComponent
    {
        Baked = 0,
        TrueShadow = 1
    }

    public enum ButtonComponent
    {
        UnityButton = 0,
        DAButton = 1
    }

    public enum LocalizationComponent
    {
        None = 0,
        DALocalizator = 1,
        I2Localization = 2,
    }

    public enum LocalizationKeyCaseType
    {
        snake_case = 0,
        UPPER_SNAKE_CASE = 1,
        PascalCase = 2,
    }
}
#endif