namespace DA_Assets.Constants
{
    /// <summary>
    /// Allowed maximum sprite sizes (longest edge in pixels), mirroring the values
    /// shown by Unity's <c>TextureImporter</c> inspector (kMaxTextureSizeValues).
    /// Used for clamping rendered sprite resolution to keep GPU work below the
    /// Windows TDR threshold on low-end iGPUs.
    /// </summary>
    public static class SpriteSizeConstants
    {
        public static readonly int[] MaxSpriteSizeValues =
            { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };

        public const int DefaultMaxSpriteSize = 4096;
        public const int MinMaxSpriteSize = 32;
        public const int MaxMaxSpriteSize = 16384;
    }
}
