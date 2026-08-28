#if UNITY_EDITOR
using UnityEngine;
using Object = UnityEngine.Object;

namespace DA_Assets.UCC
{
    internal static class SpriteAlphaNormalizer
    {
        public static byte[] NormalizeOpaqueAlpha(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return bytes;

            Texture2D texture = null;

            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, markNonReadable: false))
                    return bytes;

                Color32[] pixels = texture.GetPixels32();
                bool hasExportRoundingAlpha = false;

                for (int i = 0; i < pixels.Length; i++)
                {
                    byte alpha = pixels[i].a;

                    if (alpha < byte.MaxValue - 1)
                        return bytes;

                    if (alpha == byte.MaxValue - 1)
                        hasExportRoundingAlpha = true;
                }

                if (!hasExportRoundingAlpha)
                    return bytes;

                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].a = byte.MaxValue;
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return texture.EncodeToPNG();
            }
            finally
            {
                DestroyTexture(texture);
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }
#else
            Object.Destroy(texture);
#endif
        }
    }
}
#endif