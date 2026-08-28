#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpriteColorizer : FcuBase
    {
        public async Task ColorizeSprites(List<Node> fobjects, CancellationToken token)
        {
            if (monoBeh.UsingSVG())
                return;

            HashSet<string> colorizedSpritePaths = new HashSet<string>();

            foreach (Node fobject in fobjects)
            {
                token.ThrowIfCancellationRequested();

                if (fobject.Data.FcuImageType != FcuImageType.Downloadable &&
                    fobject.Data.FcuImageType != FcuImageType.Generative)
                    continue;

                if (monoBeh.UsingSpriteRenderer())
                {

                    if (!fobject.Data.Graphic.HasSingleColor)
                        continue;
                }
                else if (monoBeh.IsUITK() || monoBeh.IsNova())
                {

                    if (!fobject.Data.Graphic.HasSingleColor)
                        continue;

                    if (fobject.Data.Graphic.HasSingleGradient)
                        continue;
                }
                else
                {

                    if (!fobject.Data.Graphic.HasSingleColor && !fobject.Data.Graphic.HasSingleGradient)
                        continue;

                    if (fobject.IsGenerativeType() && !fobject.Data.Graphic.HasSingleColor)
                        continue;


                    if (fobject.Data.Graphic.HasSingleGradient
                        && monoBeh.Settings.ImageSpritesSettings.DownloadOptions.HasFlag(SpriteDownloadOptions.SupportedGradients))
                        continue;
                }

                if (File.Exists(fobject.Data.SpritePath.GetFullAssetPath()) == false)
                    continue;

                if (colorizedSpritePaths.Contains(fobject.Data.SpritePath))
                {
                    fobject.Data.ManualWhiteColor = true;
                    continue;
                }

                byte[] rawData = File.ReadAllBytes(fobject.Data.SpritePath.GetFullAssetPath());

                if (fobject.Data.SpriteSize.x < 1 || fobject.Data.SpriteSize.y < 1)
                {
                    return;
                }

                Texture2D tex = null;

                try
                {
                    tex = new Texture2D(fobject.Data.SpriteSize.x, fobject.Data.SpriteSize.y, TextureFormat.RGBA32, false);
                    tex.LoadImage(rawData);

                    tex.Colorize(Color.white);

                    byte[] bytes = Array.Empty<byte>();

                    switch (monoBeh.Settings.ImageSpritesSettings.ImageFormat)
                    {
                        case ImageFormat.PNG:
                            bytes = tex.EncodeToPNG();
                            break;
                        case ImageFormat.JPG:
                            bytes = tex.EncodeToJPG();
                            break;
                    }

                    File.WriteAllBytes(fobject.Data.SpritePath, bytes);

                    colorizedSpritePaths.Add(fobject.Data.SpritePath);
                    fobject.Data.ManualWhiteColor = true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    DestroyTexture(tex);
                }

                await Task.Yield();
            }

            ImportColorizedSprites(colorizedSpritePaths);
        }

        private static void ImportColorizedSprites(HashSet<string> spritePaths)
        {
#if UNITY_EDITOR
            foreach (string spritePath in spritePaths)
            {
                AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
            }
#endif
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture == null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
#else
            UnityEngine.Object.Destroy(texture);
#endif
        }
    }
}
#endif