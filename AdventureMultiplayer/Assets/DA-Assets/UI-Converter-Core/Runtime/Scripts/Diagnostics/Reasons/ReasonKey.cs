#if UNITY_EDITOR
namespace DA_Assets.UCC.Model
{
    public enum ReasonKey
    {
        None = 0,


        Tag_IsEmpty = 100,
        Tag_ForceImage = 101,
        Tag_IsRootSprite = 102,
        Tag_SpriteSwapForAll = 103,
        Tag_ContainsIcon = 104,
        Tag_ContainsCustomButtonTags = 105,
        Tag_SingleImage = 106,
        Tag_BooleanOperation = 107,
        Tag_ChildrenNotEmpty = 108,
        Tag_CanBeInsideDefault = 109,
        Tag_ForceContainerTrue = 110,
        Tag_ForceImageTrue = 111,
        Tag_TagCannotBeInsideSingleImage = 112,
        Tag_AllTagsAllowSingleImage = 113,
        Tag_DrawableSolidOverlay = 114,


        Dl_IsEmpty = 200,
        Dl_ForceImage = 201,
        Dl_Vector = 202,
        Dl_IsMask = 203,
        Dl_NoImageTag = 204,
        Dl_HasUndownloadableTag = 205,
        Dl_IsArcDataFilled = 206,
        Dl_ContainsImageEmojiVideo = 207,
        Dl_FillTransparencyPlusStroke = 208,
        Dl_SmallSizeOutsideStroke = 209,
        Dl_MultipleFills = 210,
        Dl_MultipleStrokes = 211,
        Dl_FillAndStroke = 212,
        Dl_ProceduralFillAndStrokeSupported = 213,
        Dl_NoFillsOrStrokes = 214,
        Dl_HasGradients = 215,
        Dl_GradientType = 216,
        Dl_NotRectangle = 217,
        Dl_ContainsRoundedCorners = 218,
        Dl_HasStrokes = 219,
        Dl_GradientFillPlusStroke = 220,
        Dl_ContainsShadows = 221,
        Dl_ContainsBlur = 222,
        Dl_NoConditionMatched = 223,


        Gen_UsingSVG = 300,
        Gen_UsingProceduralImage = 301,
        Gen_IsUITK = 302,
        Gen_IsNova = 303,
        Gen_IsDownloadable = 304,
        Gen_IsOverlappedByStroke = 305,
        Gen_RenderSizeTooBig = 306,
        Gen_NotRectangle = 307,
        Gen_HasFillAndStroke = 308,
        Gen_CanGenerateStrokeOnly = 309,
        Gen_ContainsRoundedCorners = 310,
        Gen_GenerationFailed = 311,
        Gen_CanGenerateShaderSelfOnly = 312,
        Gen_NoBlockedDescendants = 313,
        Gen_DrawableSolidFillAndStroke = 314,


        Mask_ObjectMaskNova = 400,
        Mask_ObjectMaskCanvas = 401,
        Mask_ObjectMaskSpriteRenderer = 402,
        Mask_FrameClipMaskNova = 403,
        Mask_FrameClipMaskLinearOrRotated = 404,
        Mask_FrameClipMaskRectMask2D = 405,
        Mask_FrameClipMaskSpriteRenderer = 406,


        Fill_SolidColor = 500,
        Fill_GradientNative = 501,
        Fill_GradientComponent = 502,
        Fill_Transparent = 503,
        Fill_BakedInSprite = 504,
        Fill_SingleColorTint = 505,


        Stroke_NativeOutline = 600,
        Stroke_UnityOutline = 601,
        Stroke_SpriteOutline = 602,
        Stroke_BorderWidth = 603,
        Stroke_BakedInSprite = 604,
        Stroke_Ignored = 605,
        Stroke_None = 606,
        Stroke_DAOutline = 607,


        Slice9_Applied = 700,
        Slice9_NoChildren = 701,
        Slice9_Not9Children = 702,
        Slice9_WrongAnchors = 703,


        Auto9_Applied = 800,
        Auto9_NoRoundedCorners = 801,
        Auto9_NotRectangle = 802,
        Auto9_NotSingleColor = 803,
        Auto9_HasIncompatibleEffects = 804,
        Auto9_HasChildren = 805,




        PerTag_IsVector = 1000,
        PerTag_HasFills = 1001,
        PerTag_HasStrokes = 1002,
        PerTag_IsBackground = 1003,
        PerTag_IsBooleanOperation = 1004,
        PerTag_IsSingleImage = 1005,
        PerTag_ForceImage = 1006,
        PerTag_IsRootSprite = 1007,
        PerTag_ButtonTagsSingleImage = 1008,
        PerTag_SpriteSwapForAll = 1009,
        PerTag_DrawableSolidOverlay = 1010,


        PerTag_HasVisibleChildren = 1020,
        PerTag_ContainsIcon = 1021,
        PerTag_ForceContainer = 1022,


        PerTag_ParentIsPage = 1030,


        PerTag_NodeTypeIsText = 1040,


        PerTag_HasLayoutMode = 1050,


        PerTag_AutoResizeText = 1060,


        PerTag_PreserveRatio = 1070,


        PerTag_IsAnyMask = 1080,


        PerTag_NameIsButton = 1090,


        PerTag_HasShadowEffects = 1100,


        PerTag_HasBackgroundBlur = 1110,


        PerTag_HasLayoutGrids = 1120,


        PerTag_OpacityNotOne = 1130,


        PerTag_Is9SliceStructure = 1140,


        PerTag_PassedAutoSlice9Checks = 1150,


        PerTag_ManualFigmaTag = 1160,




        PerTag_Skip_ParentNotPage = 1300,


        PerTag_Skip_NoLayoutMode = 1310,
        PerTag_Skip_ALG_NoVisibleChildren = 1311,


        PerTag_Skip_PreserveRatioOff = 1320,


        PerTag_Skip_NotAMask = 1330,


        PerTag_Skip_NameNotButton = 1340,


        PerTag_Skip_NoAutoResize = 1350,
        PerTag_Skip_ParentIsAutoLayout = 1351,


        PerTag_Skip_NoFillsOrStrokes = 1360,


        PerTag_Skip_NoShadowEffects = 1370,
        PerTag_Skip_FrameworkNoShadow = 1371,


        PerTag_Skip_NoBlurEffects = 1380,
        PerTag_Skip_FrameworkNoBlur = 1381,


        PerTag_Skip_DrawLayoutGridsOff = 1390,
        PerTag_Skip_NoLayoutGrids = 1391,


        PerTag_Skip_OpacityIsOne = 1400,


        PerTag_Skip_NoChildren = 1410,
    }
}
#endif