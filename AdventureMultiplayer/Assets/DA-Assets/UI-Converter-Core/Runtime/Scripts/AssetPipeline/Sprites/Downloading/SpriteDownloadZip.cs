#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public class SpriteDownloadZip : ISpriteDownloadStrategy
    {
        private readonly ConverterBase _monoBeh;
        private readonly SpriteIdentityCache _cache;

        public ImportMode Mode => ImportMode.Zip;

        public SpriteDownloadZip(ConverterBase monoBeh, SpriteIdentityCache cache)
        {
            _monoBeh = monoBeh;
            _cache = cache;
        }

        public async Task DownloadSprites(List<Node> fobjects, CancellationToken token)
        {

            List<Node> toDownload;

            if (_cache != null)
            {
                IReadOnlyList<Node> reps = _cache.UniqueRepresentatives;
                toDownload = new List<Node>(reps.Count);
                foreach (Node rep in reps)
                {
                    if (rep.Data.NeedDownload)
                        toDownload.Add(rep);
                }
            }
            else
            {

                toDownload = fobjects
                    .Where(x => x.Data.NeedDownload)
                    .GroupBy(SpriteRenderKeyUtility.GetSpriteRenderKey)
                    .Select(g => g.First())
                    .ToList();
            }

            if (toDownload.Count == 0)
            {
                Debug.Log(FcuLocKey.log_zip_no_sprites_to_load.Localize());
                return;
            }

            await SpriteDataCalculator.CalculateAndSetSpriteData(toDownload, _monoBeh, token);

            var zipData = _monoBeh.CurrentProject.ZipData;
            var format = _monoBeh.Settings.ImageSpritesSettings.ImageFormat;

            Debug.Log(FcuLocKey.log_zip_loading_sprites.Localize(toDownload.Count, zipData.ImagesFolder));
            _monoBeh.EditorDelegateHolder.StartProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingSprites, toDownload.Count, false);

            int loadedCount = 0;
            int failedCount = 0;

            foreach (var fobj in toDownload)
            {
                if (token.IsCancellationRequested)
                    break;

                string sanitizedId = ZipProjectData.SanitizeNodeId(fobj.Id);
                string filePath = GetPath(zipData.ImagesFolder, sanitizedId, format);

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    try
                    {
                        byte[] bytes = await ReadFileAsync(filePath, token);
                        Debug.Log(FcuLocKey.log_zip_image_added.Localize(filePath));
                        SpriteBatchWriter.Add(fobj, bytes);
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(FcuLocKey.log_zip_image_load_failed.Localize(fobj.Id, ex.Message));
                        failedCount++;
                    }
                }
                else
                {
                    Debug.LogError(FcuLocKey.log_zip_image_not_found.Localize(fobj.Id, $"{sanitizedId}.{format}"));
                    failedCount++;
                }

                _monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingSprites, loadedCount + failedCount);
            }

            _monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingSprites);
            Debug.Log(FcuLocKey.log_zip_sprites_loaded.Localize(loadedCount, toDownload.Count, failedCount));
        }

        private string GetPath(string imagesFolder, string sanitizedId, ImageFormat format)
        {
            string ext = format.ToString().ToLower();
            string path = Path.Combine(imagesFolder, $"{sanitizedId}.{ext}");

            if (File.Exists(path))
                return path;

            return null;
        }

        private async Task<byte[]> ReadFileAsync(string filePath, CancellationToken token)
        {
#if UNITY_2021_3_OR_NEWER
            return await File.ReadAllBytesAsync(filePath, token);
#else
            return await Task.Run(() => File.ReadAllBytes(filePath), token);
#endif
        }
    }
}
#endif