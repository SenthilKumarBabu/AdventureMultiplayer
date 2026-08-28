#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpriteSlicer : FcuBase
    {
        public async Task SliceSprites(List<Node> fobjects, CancellationToken token)
        {
            foreach (Node fobject in fobjects)
            {
                token.ThrowIfCancellationRequested();

                if (!fobject.IsSprite())
                    continue;

                if (fobject.Children.IsEmpty())
                    continue;

                if (fobject.Children.Count != 9)
                    continue;

                Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);

                if (sprite == null)
                    continue;

                Node child0 = fobject.Children[0];
                Node child1 = fobject.Children[1];
                Node child2 = fobject.Children[2];
                Node child3 = fobject.Children[3];
                Node child4 = fobject.Children[4];
                Node child5 = fobject.Children[5];
                Node child6 = fobject.Children[6];
                Node child7 = fobject.Children[7];
                Node child8 = fobject.Children[8];

                float imageScale = fobject.Data.Scale;

                int left = (int)(child0.Size.x * imageScale);
                int top = (int)(child0.Size.y * imageScale);
                int right = (int)(child2.Size.x * imageScale);
                int bottom = (int)(child6.Size.y * imageScale);

                Vector4 border = new Vector4(left, bottom, right, top);

                if (fobject.Data.ManualWhiteColor && CanCreateMinimal9Slice(sprite, border))
                {
                    Texture2D texture = null;

                    try
                    {
                        texture = CreateMinimal9Slice(sprite, border, token);
                        monoBeh.SpriteProcessor.SaveSprite(texture, fobject);
                        texture = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        return;
                    }
                    finally
                    {
                        DestroyTexture(texture);
                    }

                    sprite = monoBeh.SpriteProcessor.GetSprite(fobject);
                }

                monoBeh.EditorDelegateHolder.SetSpriteRects(sprite, border);
                await Task.Yield();
            }
        }

        public async Task AutoSliceSprites(List<Node> fobjects, CancellationToken token)
        {
            HashSet<string> slicedSpritePaths = new HashSet<string>();

            foreach (Node fobject in fobjects)
            {
                token.ThrowIfCancellationRequested();

                if (!fobject.ContainsTag(FcuTag.AutoSlice9))
                    continue;

                if (!slicedSpritePaths.Add(fobject.Data.SpritePath))
                    continue;

                Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);

                if (sprite == null)
                    continue;

                if (!fobject.TryGetAutoSlice9SourceBorder(out Vector4 sourceBorder))
                    continue;

                float imageScale = fobject.Data.Scale;
                int texW = sprite.texture.width;
                int texH = sprite.texture.height;


                int left = Mathf.Max(Mathf.CeilToInt(sourceBorder.x * imageScale), 1);
                int right = Mathf.Max(Mathf.CeilToInt(sourceBorder.z * imageScale), 1);
                int top = Mathf.Max(Mathf.CeilToInt(sourceBorder.w * imageScale), 1);
                int bottom = Mathf.Max(Mathf.CeilToInt(sourceBorder.y * imageScale), 1);

                left   = Mathf.Min(left,   texW / 2);
                right  = Mathf.Min(right,  texW / 2);
                top    = Mathf.Min(top,    texH / 2);
                bottom = Mathf.Min(bottom, texH / 2);


                if (left <= 0 || right <= 0 || top <= 0 || bottom <= 0 ||
                    left + right >= texW || top + bottom >= texH)
                    continue;

                Vector4 border = new Vector4(left, bottom, right, top);

                if (fobject.CanMinimizeAutoSlice9Sprite())
                {
                    Texture2D texture = null;

                    try
                    {
                        texture = CreateMinimal9Slice(sprite, border, token);
                        monoBeh.SpriteProcessor.SaveSprite(texture, fobject);
                        texture = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        return;
                    }
                    finally
                    {
                        DestroyTexture(texture);
                    }

                    sprite = monoBeh.SpriteProcessor.GetSprite(fobject);
                }

                monoBeh.EditorDelegateHolder.SetSpriteRects(sprite, border);
                await Task.Yield();
            }
        }

        public Texture2D CreateMinimal9Slice(Sprite sourceSprite, Vector4 borders, CancellationToken token)
        {
            int leftBorder = (int)borders.x;
            int bottomBorder = (int)borders.y;
            int rightBorder = (int)borders.z;
            int topBorder = (int)borders.w;

            int newWidth = leftBorder + rightBorder;
            int newHeight = topBorder + bottomBorder;

            Texture2D newTexture = new Texture2D(newWidth, newHeight, sourceSprite.texture.format, false);

            Color[] sourcePixels = sourceSprite.texture.GetPixels();
            int sourceWidth = sourceSprite.texture.width;
            int sourceHeight = sourceSprite.texture.height;

            Color[] newPixels = new Color[newWidth * newHeight];

            for (int y = 0; y < topBorder; y++)
            {
                token.ThrowIfCancellationRequested();

                for (int x = 0; x < leftBorder; x++)
                {
                    int sourceIndex = x + (sourceHeight - 1 - y) * sourceWidth;
                    int destIndex = x + (newHeight - 1 - y) * newWidth;
                    newPixels[destIndex] = sourcePixels[sourceIndex];
                }
            }

            for (int y = 0; y < topBorder; y++)
            {
                token.ThrowIfCancellationRequested();

                for (int x = 0; x < rightBorder; x++)
                {
                    int sourceX = sourceWidth - rightBorder + x;
                    int sourceIndex = sourceX + (sourceHeight - 1 - y) * sourceWidth;
                    int destIndex = (leftBorder + x) + (newHeight - 1 - y) * newWidth;
                    newPixels[destIndex] = sourcePixels[sourceIndex];
                }
            }

            for (int y = 0; y < bottomBorder; y++)
            {
                token.ThrowIfCancellationRequested();

                for (int x = 0; x < leftBorder; x++)
                {
                    int sourceIndex = x + y * sourceWidth;
                    int destIndex = x + y * newWidth;
                    newPixels[destIndex] = sourcePixels[sourceIndex];
                }
            }

            for (int y = 0; y < bottomBorder; y++)
            {
                token.ThrowIfCancellationRequested();

                for (int x = 0; x < rightBorder; x++)
                {
                    int sourceX = sourceWidth - rightBorder + x;
                    int sourceIndex = sourceX + y * sourceWidth;
                    int destIndex = (leftBorder + x) + y * newWidth;
                    newPixels[destIndex] = sourcePixels[sourceIndex];
                }
            }

            newTexture.SetPixels(newPixels);
            newTexture.Apply();

            return newTexture;
        }

        private static bool CanCreateMinimal9Slice(Sprite sourceSprite, Vector4 borders)
        {
            if (sourceSprite == null || sourceSprite.texture == null)
                return false;

            int leftBorder = (int)borders.x;
            int bottomBorder = (int)borders.y;
            int rightBorder = (int)borders.z;
            int topBorder = (int)borders.w;

            if (leftBorder <= 0 || rightBorder <= 0 || topBorder <= 0 || bottomBorder <= 0)
                return false;

            Texture2D sourceTexture = sourceSprite.texture;
            return leftBorder + rightBorder < sourceTexture.width &&
                   topBorder + bottomBorder < sourceTexture.height;
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