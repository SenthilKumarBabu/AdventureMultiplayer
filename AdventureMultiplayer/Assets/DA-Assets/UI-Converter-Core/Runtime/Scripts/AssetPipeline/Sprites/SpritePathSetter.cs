#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS1998

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpritePathSetter : FcuBase
    {
        public async Task SetSpritePaths(List<Node> fobjects, SpriteIdentityCache cache, CancellationToken token)
        {
#if UNITY_EDITOR
            await Task.Yield();

            string[] assetSpritePaths;

            if (monoBeh.IsPlaying())
            {
                string root = Path.Combine(
                   Application.persistentDataPath,
                   monoBeh.Settings.ImageSpritesSettings.SpritesPath);

                assetSpritePaths = Directory.Exists(root)
                    ? Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                               .ToArray()
                    : Array.Empty<string>();
            }
            else
            {
                string[] searchInFolder = new string[]
                {
                    monoBeh.Settings.ImageSpritesSettings.SpritesPath
                };

                assetSpritePaths = FindSpriteAssetPaths(searchInFolder);
            }



            cache.BuildExistingSpritePathLookup(assetSpritePaths);

            IReadOnlyList<Node> uniqueRepresentatives = cache.UniqueRepresentatives;
            HashSet<string> reservedSpritePaths = new HashSet<string>(assetSpritePaths, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < uniqueRepresentatives.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                Node item = uniqueRepresentatives[i];
                int renderKey = cache.GetRenderKey(item);

                bool imageFileExists;
                string spritePath;

                if (cache.TryGetExistingPath(renderKey, out string existingPath) &&
                    IsTargetExtension(item, existingPath))
                {


                    imageFileExists = IsExistingSpriteLargeEnough(item, existingPath);
                    spritePath = existingPath;
                }
                else
                {

                    imageFileExists = false;
                    spritePath = GetSpritePath(item, reservedSpritePaths);
                }

                reservedSpritePaths.Add(spritePath);
                SetNeedDownloadFileFlag(item, imageFileExists);
                SetNeedGenerateFlag(item, imageFileExists);


                IReadOnlyList<Node> group = cache.GetGroup(renderKey);
                foreach (Node fo in group)
                {
                    fo.Data.SpritePath = spritePath;
                }


                for (int j = 1; j < group.Count; j++)
                {
                    SetNeedDownloadFileFlag(group[j], imageFileExists);
                    SetNeedGenerateFlag(group[j], imageFileExists);
                }

                if (i % 500 == 0)
                {
                    await Task.Yield();
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static string[] FindSpriteAssetPaths(string[] searchInFolder)
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", searchInFolder))
            {
                paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{typeof(Sprite).Name}", searchInFolder))
            {
                paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            }

            return paths.ToArray();
        }
#endif

        private string GetSpritePath(Node fobject, HashSet<string> reservedSpritePaths)
        {
            string spriteDir = fobject.Data.IsMutual
                ? "Mutual"
                : fobject.Data.RootFrame.Names.FolderName;

            string root = monoBeh.IsPlaying()
                ? Path.Combine(Application.persistentDataPath, monoBeh.Settings.ImageSpritesSettings.SpritesPath)
                : monoBeh.Settings.ImageSpritesSettings.SpritesPath.GetFullAssetPath();

            string absoluteFramePath = Path.Combine(root, spriteDir);
            absoluteFramePath.CreateFolderIfNotExists();

            string fileName = fobject.Data.Names.FileName;

            string spritePath = monoBeh.IsPlaying()
                ? Path.Combine(absoluteFramePath, fileName)
                : Path.Combine(monoBeh.Settings.ImageSpritesSettings.SpritesPath, spriteDir, fileName).ToUnityPath();

            return GetAvailableSpritePath(spritePath, reservedSpritePaths);
        }

        private string GetAvailableSpritePath(string spritePath, HashSet<string> reservedSpritePaths)
        {
            if (reservedSpritePaths.Add(spritePath))
                return spritePath;

            string directory = Path.GetDirectoryName(spritePath);
            string fileName = Path.GetFileNameWithoutExtension(spritePath);
            string extension = Path.GetExtension(spritePath);

            int index = 1;
            while (true)
            {
                string candidate = Path.Combine(directory, $"{fileName}-{index}{extension}");
                if (!monoBeh.IsPlaying())
                    candidate = candidate.ToUnityPath();

                if (reservedSpritePaths.Add(candidate))
                    return candidate;

                index++;
            }
        }

        private bool IsTargetExtension(Node fobject, string spritePath)
        {
            string spriteExt = Path.GetExtension(spritePath);

            if (spriteExt.StartsWith(".") && spriteExt.Length > 1)
                spriteExt = spriteExt.Remove(0, 1);

            ImageFormat? targetExt = null;

            if (monoBeh.UsingSvgImage())
            {
                if (fobject.CanUseUnityImage(monoBeh))
                {
                    targetExt = ImageFormat.PNG;
                }
            }

            if (targetExt == null)
            {
                targetExt = monoBeh.Settings.ImageSpritesSettings.ImageFormat;
            }

            return spriteExt.ToLower() == targetExt.ToLower();
        }

#if UNITY_EDITOR
        private bool IsExistingSpriteLargeEnough(Node fobject, string spritePath)
        {
            UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(spritePath) as UnityEditor.TextureImporter;
            if (importer == null)
                return false;

            importer.GetTextureSize(out int width, out int height);
            Vector2Int expectedSize = GetExpectedSpriteSize(fobject);

            return IsExistingSpriteSizeValid(
                fobject.Data.FcuImageType,
                new Vector2Int(width, height),
                expectedSize);
        }
#endif

        public static bool IsExistingSpriteSizeValid(FcuImageType imageType, Vector2Int actualSize, Vector2Int expectedSize)
        {
            if (imageType == FcuImageType.Generative)
                return actualSize == expectedSize;

            return actualSize.x >= expectedSize.x && actualSize.y >= expectedSize.y;
        }

        private Vector2Int GetExpectedSpriteSize(Node fobject)
        {
            Vector2 sourceSize = GetSpriteSourceSize(fobject);
            float scale = GetMaxAllowedScale(sourceSize);

            int width = Mathf.CeilToInt(sourceSize.x * scale);
            int height = Mathf.CeilToInt(sourceSize.y * scale);

            return new Vector2Int(width, height);
        }

        private Vector2 GetSpriteSourceSize(Node fobject)
        {
            return SpriteRenderKeyUtility.GetSpriteSourceSize(fobject);
        }

        private float GetMaxAllowedScale(Vector2 imageSize)
        {
            int maxSpriteSizeSettings = monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize;
            float scaleX = imageSize.x > 0 ? maxSpriteSizeSettings / imageSize.x : float.MaxValue;
            float scaleY = imageSize.y > 0 ? maxSpriteSizeSettings / imageSize.y : float.MaxValue;

            float maxScaleBySpriteSize = Mathf.Min(scaleX, scaleY);
            maxScaleBySpriteSize = Mathf.Max(monoBeh.Config.IMAGE_SCALE_MIN, maxScaleBySpriteSize);

            float clampedScale = Mathf.Clamp(maxScaleBySpriteSize, monoBeh.Config.IMAGE_SCALE_MIN, monoBeh.Config.IMAGE_SCALE_MAX);
            float roundedScale = (float)Math.Round(clampedScale, monoBeh.Config.Rounding.MaxAllowedScale);

            return Mathf.Min(roundedScale, monoBeh.Settings.ImageSpritesSettings.ImageScale);
        }



        public bool GetSpritePath(Node fobject, string[] spritePathes, out string path)
        {
            int renderKey = SpriteRenderKeyUtility.GetSpriteRenderKey(fobject);

            foreach (string spritePath in spritePathes)
            {
                if (!IsTargetExtension(fobject, spritePath))
                {
                    continue;
                }

                if (!GuidMetaUtility.TryExtractData(
                     spritePath + ".meta",
                     out int hash))
                {
                    continue;
                }

                if (SpriteRenderKeyUtility.MatchesPackedGuid(renderKey, hash))
                {
                    path = spritePath;
                    return true;
                }
            }

            path = null;
            return false;
        }

        private void SetNeedDownloadFileFlag(Node fobject, bool imageFileExists)
        {
            if (fobject.IsDownloadableType())
            {
                if (monoBeh.Settings.ImageSpritesSettings.RedownloadSprites)
                {
                    fobject.Data.NeedDownload = true;
                }
                else if (imageFileExists)
                {
                    fobject.Data.NeedDownload = false;
                }
                else
                {
                    fobject.Data.NeedDownload = true;
                }
            }
            else
            {
                fobject.Data.NeedDownload = false;
            }
        }

        private void SetNeedGenerateFlag(Node fobject, bool imageFileExists)
        {
            if (fobject.IsGenerativeType())
            {
                if (monoBeh.Settings.ImageSpritesSettings.RedownloadSprites)
                {
                    fobject.Data.NeedGenerate = true;
                }
                else if (imageFileExists)
                {
                    fobject.Data.NeedGenerate = false;
                }
                else
                {
                    fobject.Data.NeedGenerate = true;
                }
            }
            else
            {
                fobject.Data.NeedGenerate = false;
            }
        }
    }
}
#endif