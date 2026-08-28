#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using Object = UnityEngine.Object;
using System.IO;

#if VECTOR_GRAPHICS_EXISTS && UNITY_EDITOR
using Unity.VectorGraphics.Editor;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpriteProcessor : FcuBase
    {
        private int _errorLogSplitLimit = 50;
        private ConcurrentBag<Node> failedObjects = new ConcurrentBag<Node>();

        public Sprite GetSprite(Node fobject)
        {
            if (fobject.Data.SpritePath.IsEmpty())
                return null;

            if (!monoBeh.IsPlaying())
            {
#if UNITY_EDITOR
                return AssetDatabase.LoadAssetAtPath<Sprite>(fobject.Data.SpritePath);
#else
                return null;
#endif
            }

            string path = fobject.Data.SpritePath;
            if (!File.Exists(path))
                return null;

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
                return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, markNonReadable: true))
            {
                Object.Destroy(tex);
                return null;
            }

            return Sprite.Create(tex,
                                 new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f),
                                 100,
                                 0,
                                 SpriteMeshType.Tight);
        }

        public void SaveSprite(Texture2D texture, Node fobject)
        {
#if UNITY_EDITOR
            if (texture == null)
            {
                Debug.LogError(FcuLocKey.log_sprite_processor_null_texture.Localize(fobject.Name));
                return;
            }

            try
            {
                string path = fobject.Data.SpritePath;

                byte[] bytes = SpriteAlphaNormalizer.NormalizeOpaqueAlpha(texture.EncodeToPNG());

                System.IO.File.WriteAllBytes(path, bytes);

                AssetDatabase.Refresh();

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                DestroyRuntimeTexture(texture);
            }
#endif
        }


        public void UpdateSpriteSize(Node fobject)
        {
            if (fobject.Data.SpritePath.IsEmpty())
            {
                Debug.LogError(FcuLocKey.log_sprite_processor_path_empty.Localize(fobject.Name));
                return;
            }

            string path = fobject.Data.SpritePath;
            if (!File.Exists(path))
            {
                Debug.LogError(FcuLocKey.log_sprite_processor_file_not_found.Localize(fobject.Name, path));
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogError(FcuLocKey.log_sprite_processor_file_unreadable.Localize(fobject.Name, path));
                return;
            }

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes, markNonReadable: true))
            {
                Debug.LogError(FcuLocKey.log_sprite_processor_load_failed.Localize(fobject.Name, path));
                Object.Destroy(tex);
                return;
            }

            int width = tex.width;
            int height = tex.height;
            fobject.Data.SpriteSize = new Vector2Int(width, height);
            Debug.LogError(FcuLocKey.log_sprite_processor_size_updated.Localize(fobject.Name, width, height));
            Object.Destroy(tex);
        }

        private static void DestroyRuntimeTexture(Texture2D texture)
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

        public async Task MarkAsSprites(List<Node> fobjects, CancellationToken token)
        {
#if UNITY_EDITOR
            failedObjects = new ConcurrentBag<Node>();

            AssetDatabase.Refresh();

            List<Node> nodesWithSprite = fobjects
                .Where(x => x.Data.SpritePath != null && !x.Data.SpritePath.IsEmpty())
                .ToList();

            int allCount = nodesWithSprite.Count;
            int count = 0;









            var groupsByPath = nodesWithSprite
                .GroupBy(x => x.Data.SpritePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var group in groupsByPath)
            {
                if (token.IsCancellationRequested)
                    return;

                Node representative = group.First();
                int membersInGroup = group.Count();

                _ = SetImgTypeSprite(representative, () =>
                {
                    count += membersInGroup;
                }, token);
            }

            int tempCount = -1;
            while (FcuLogger.WriteLogBeforeEqual(
                ref count,
                ref allCount,
                FcuLocKey.log_mark_as_sprite.Localize(count, allCount),
                ref tempCount))
            {
                if (token.IsCancellationRequested)
                    return;

                await Task.Delay(1000, token);
            }

            LogFailedSprites(failedObjects);
#endif
        }

        private void LogFailedSprites(ConcurrentBag<Node> failedObjects)
        {
            if (failedObjects.Count() > 0)
            {
                List<List<string>> comps = failedObjects.Select(x => x.Data.NameHierarchy).Split(_errorLogSplitLimit);

                foreach (List<string> comp in comps)
                {
                    string hierarchies = string.Join("\n", comp);

                    Debug.LogError(
                        FcuLocKey.cant_load_sprites.Localize(comp.Count, hierarchies));
                }
            }
        }
#if UNITY_EDITOR
        private async Task SetImgTypeSprite(Node fobject, Action callback, CancellationToken token)
        {
            int maxRetries = monoBeh.Config.SetImgTypeSpriteMaxRetries;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (token.IsCancellationRequested)
                    return;

                bool success;

                if (fobject.IsSvgExtension())
                {
                    success = SetVectorTextureSettings(fobject);
                }
                else
                {
                    success = SetRasterTextureSettings(fobject);
                }

                if (success)
                {
                    callback.Invoke();
                    return;
                }

                await Task.Delay(100, token);
            }

            Debug.LogError(FcuLocKey.log_sprite_processor_retries_exhausted.Localize(
                fobject.Data.NameHierarchy,
                maxRetries,
                fobject.Data.SpritePath));

            failedObjects.Add(fobject);
            callback.Invoke();
        }

        private bool SetVectorTextureSettings(Node fobject)
        {
            try
            {
#if VECTOR_GRAPHICS_EXISTS
                SVGImporter importer = AssetImporter.GetAtPath(fobject.Data.SpritePath) as SVGImporter;
                UpdateVectorTextureSettings(importer, fobject.Data.SpritePath);
                if (IsVectorTextureSettingsCorrect(importer))
                {
                    return true;
                }
                else
                {
                    UpdateVectorTextureSettings(importer, fobject.Data.SpritePath);
                    return false;
                }
#else
                return true;
#endif
            }
            catch (Exception ex)
            {
                FcuLogger.Debug(ex, FcuDebugSettingsFlags.LogError);
                failedObjects.Add(fobject);
                return true;
            }
        }
#if VECTOR_GRAPHICS_EXISTS
        private bool IsVectorTextureSettingsCorrect(SVGImporter importer)
        {
            bool settingsCorrect = importer.SvgType == monoBeh.Settings.SVGImporterSettings.SvgType &&
                                   importer.SvgPixelsPerUnit == monoBeh.Settings.ImageSpritesSettings.ImageScale &&
                                   importer.GradientResolution == monoBeh.Settings.SVGImporterSettings.GradientResolution &&
                                   importer.CustomPivot == monoBeh.Settings.SVGImporterSettings.CustomPivot &&
                                   importer.GeneratePhysicsShape == monoBeh.Settings.SVGImporterSettings.GeneratePhysicsShape &&
                                   importer.ViewportOptions == monoBeh.Settings.SVGImporterSettings.ViewportOptions &&
                                   importer.StepDistance == monoBeh.Settings.SVGImporterSettings.StepDistance &&
                                   importer.SamplingStepDistance == monoBeh.Settings.SVGImporterSettings.SamplingSteps &&
                                   importer.AdvancedMode == monoBeh.Settings.SVGImporterSettings.AdvancedMode &&
                                   importer.MaxCordDeviationEnabled == monoBeh.Settings.SVGImporterSettings.MaxCordDeviationEnabled &&
                                   importer.MaxTangentAngleEnabled == monoBeh.Settings.SVGImporterSettings.MaxTangentAngleEnabled;

            return settingsCorrect;
        }

        private void UpdateVectorTextureSettings(SVGImporter importer, string spritePath)
        {
            var svgImporterSettings = monoBeh.Settings.SvgImageSettings;

            importer.SvgType = monoBeh.Settings.SVGImporterSettings.SvgType;
            importer.SvgPixelsPerUnit = monoBeh.Settings.ImageSpritesSettings.ImageScale;
            importer.GradientResolution = monoBeh.Settings.SVGImporterSettings.GradientResolution <= ushort.MaxValue ? (ushort)monoBeh.Settings.SVGImporterSettings.GradientResolution : ushort.MaxValue;
            importer.CustomPivot = monoBeh.Settings.SVGImporterSettings.CustomPivot;
            importer.GeneratePhysicsShape = monoBeh.Settings.SVGImporterSettings.GeneratePhysicsShape;
            importer.ViewportOptions = monoBeh.Settings.SVGImporterSettings.ViewportOptions;
            importer.StepDistance = monoBeh.Settings.SVGImporterSettings.StepDistance;
            importer.SamplingStepDistance = monoBeh.Settings.SVGImporterSettings.SamplingSteps;
            importer.AdvancedMode = monoBeh.Settings.SVGImporterSettings.AdvancedMode;

            importer.MaxCordDeviationEnabled = monoBeh.Settings.SVGImporterSettings.MaxCordDeviationEnabled;
            importer.MaxCordDeviation = monoBeh.Settings.SVGImporterSettings.MaxCordDeviation;

            importer.MaxTangentAngleEnabled = monoBeh.Settings.SVGImporterSettings.MaxTangentAngleEnabled;
            importer.MaxTangentAngle = monoBeh.Settings.SVGImporterSettings.MaxTangentAngle;

            SaveAsset(importer);
        }
#endif
        private void SaveAsset(AssetImporter importer)
        {
            importer.SetDirtyExt();
            importer.SaveAndReimport();
        }


        private bool SetRasterTextureSettings(Node fobject)
        {
            try
            {
                TextureImporter importer = AssetImporter.GetAtPath(fobject.Data.SpritePath) as TextureImporter;

                SetRasterTextureSize(fobject, importer);

                if (IsRasterTextureSettingsCorrect(fobject, importer))
                {
                    return true;
                }
                else
                {
                    UpdateRasterTextureSettings(fobject, importer);
                    return false;
                }
            }
            catch (Exception ex)
            {
                FcuLogger.Debug(ex, FcuDebugSettingsFlags.LogError);
                failedObjects.Add(fobject);
                return true;
            }
        }

        private void SetRasterTextureSize(Node fobject, TextureImporter importer)
        {
            importer.GetTextureSize(out int width, out int height);
            importer.SetMaxTextureSize(width, height);
            fobject.Data.SpriteSize = new Vector2Int(width, height);
            fobject.Data.UseImageLinearMaterial = HasTransparentPixels(fobject.Data.SpritePath);
        }

        private bool HasTransparentPixels(string path)
        {
            if (path.IsEmpty() || !File.Exists(path))
                return false;

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
                return false;

            Texture2D texture = null;

            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes, markNonReadable: false))
                    return false;

                Color32[] pixels = texture.GetPixels32();
                return pixels.Any(x => x.a < byte.MaxValue - 1);
            }
            catch (Exception ex)
            {
                FcuLogger.Debug(ex, FcuDebugSettingsFlags.LogError);
                return false;
            }
            finally
            {
                DestroyRuntimeTexture(texture);
            }
        }

        private bool IsRasterTextureSettingsCorrect(Node fobject, TextureImporter importer)
        {
            bool part1 = importer.isReadable == monoBeh.Settings.TextureImporterSettings.IsReadable &&
                   importer.textureType == monoBeh.Settings.TextureImporterSettings.TextureType &&
                   importer.crunchedCompression == monoBeh.Settings.TextureImporterSettings.CrunchedCompression &&
                   importer.textureCompression == monoBeh.Settings.TextureImporterSettings.TextureCompression &&
                   importer.mipmapEnabled == monoBeh.Settings.TextureImporterSettings.MipmapEnabled &&
                   importer.spriteImportMode == monoBeh.Settings.TextureImporterSettings.SpriteImportMode;

            bool perUnit;

            if (monoBeh.UsingSpriteRenderer())
            {
                perUnit = importer.spritePixelsPerUnit == monoBeh.Settings.ImageSpritesSettings.ImageScale;
            }
            else if (fobject.ContainsTag(FcuTag.Slice9) || fobject.ContainsTag(FcuTag.AutoSlice9))
            {
                perUnit = importer.spritePixelsPerUnit == (int)(fobject.Data.Scale * 100f);
            }
            else
            {
                perUnit = importer.spritePixelsPerUnit == monoBeh.Settings.ImageSpritesSettings.PixelsPerUnit;
            }

            return part1 && perUnit;
        }

        private void UpdateRasterTextureSettings(Node fobject, TextureImporter importer)
        {
            importer.isReadable = monoBeh.Settings.TextureImporterSettings.IsReadable;
            importer.textureType = monoBeh.Settings.TextureImporterSettings.TextureType;
            importer.crunchedCompression = monoBeh.Settings.TextureImporterSettings.CrunchedCompression;
            importer.textureCompression = monoBeh.Settings.TextureImporterSettings.TextureCompression;
            importer.mipmapEnabled = monoBeh.Settings.TextureImporterSettings.MipmapEnabled;
            importer.spriteImportMode = monoBeh.Settings.TextureImporterSettings.SpriteImportMode;

            if (monoBeh.UsingSpriteRenderer())
            {
                importer.spritePixelsPerUnit = monoBeh.Settings.ImageSpritesSettings.ImageScale;
            }
            else if (fobject.ContainsTag(FcuTag.Slice9) || fobject.ContainsTag(FcuTag.AutoSlice9))
            {
                importer.spritePixelsPerUnit = (int)(fobject.Data.Scale * 100f);
            }
            else
            {
                importer.spritePixelsPerUnit = monoBeh.Settings.ImageSpritesSettings.PixelsPerUnit;
            }

            if (importer.crunchedCompression)
            {
                importer.compressionQuality = monoBeh.Settings.TextureImporterSettings.CompressionQuality;
            }

            SaveAsset(importer);
        }
#endif
    }
}
#endif