#if UNITY_EDITOR
using DA_Assets.Constants;
using DA_Assets.DAI;
using DA_Assets.Logging;
using System;
using System.IO;
using UnityEngine;

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class ImageSpritesSettings : FcuBase
    {
        [SerializeField] public ProceduralCondition ProceduralCondition = ProceduralCondition.Sprite | ProceduralCondition.RectangleNoRoundedCorners;


        [Tooltip(@"The VectorGraphics 2.0.0 asset does not support displaying SVG images with effects.

I also noticed that it doesn’t render objects that use an image as a fill in Figma.

By default, the asset will download objects that meet these conditions as PNG sprites and use the UI.Image component to draw them.

You can disable these options, but these components may not display correctly in Unity.")]
        [SerializeField] public SvgCondition SvgCondition = SvgCondition.ImageOrVideo | SvgCondition.AnyEffect;

        [SerializeField] ImageComponent imageComponent = ImageComponent.UnityImage;
        public ImageComponent ImageComponent
        {
            get => imageComponent;
            set => imageComponent = value;
        }

        [SerializeField] public SpriteDownloadOptions DownloadOptions = SpriteDownloadOptions.UnsupportedGradients | SpriteDownloadOptions.MultipleFills;

        [SerializeField] bool redownloadSprites = false;
        public bool RedownloadSprites { get => redownloadSprites; set => redownloadSprites = value; }

        [SerializeField] string spritesPath = Path.Combine("Assets", "Sprites");
        public string SpritesPath { get => spritesPath; set => spritesPath = value; }

        [SerializeField] ImageFormat imageFormat = ImageFormat.PNG;
        public ImageFormat ImageFormat
        {
            get => imageFormat;
            set => imageFormat = value;
        }

        [SerializeField] float imageScale = FcuConfig.IMAGE_SCALE_MAX;
        public float ImageScale
        {
            get
            {
                if (imageFormat == ImageFormat.SVG && imageScale != 1f)
                {
                    Debug.Log(FcuLocKey.log_svg_scale_1.Localize());
                    imageScale = 1f;
                }

                return imageScale;
            }
            set => imageScale = value;
        }

        [SerializeField] int maxSpriteSize = SpriteSizeConstants.DefaultMaxSpriteSize;
        public int MaxSpriteSize
        {
            get => maxSpriteSize;
            set => maxSpriteSize = Mathf.Clamp(value, SpriteSizeConstants.MinMaxSpriteSize, SpriteSizeConstants.MaxMaxSpriteSize);
        }

        [SerializeField] PreserveRatioMode preserveRatioMode = PreserveRatioMode.None;
        public PreserveRatioMode PreserveRatioMode { get => preserveRatioMode; set => preserveRatioMode = value; }

        [SerializeField] float pixelsPerUnit = 100;
        public float PixelsPerUnit { get => pixelsPerUnit; set => pixelsPerUnit = value; }

        [SerializeField] bool useImageLinearMaterial = true;
        public bool UseImageLinearMaterial { get => useImageLinearMaterial; set => useImageLinearMaterial = value; }
    }
}
#endif