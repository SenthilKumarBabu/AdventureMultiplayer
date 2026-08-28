#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public static class ConcurrentSpriteDownloader
    {
        public static async Task<ConcurrentBag<SpriteData>> DownloadAllAsync(
            List<SpriteData> spritesWithLinks,
            int maxConcurrentDownloads,
            int maxDownloadAttempts,
            ConverterBase monoBeh,
            CancellationToken token)
        {
            var failedSprites = new ConcurrentBag<SpriteData>();
            int totalCount = spritesWithLinks.Count;
            int downloadedCount = 0;

            Debug.Log(FcuLocKey.log_start_download_images.Localize());
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingSprites, totalCount, false);

            using (SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrentDownloads))
            {
                List<Task> downloadTasks = new List<Task>();

                foreach (var spriteData in spritesWithLinks)
                {
                    await semaphore.WaitAsync(token);

                    downloadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            bool success = await DownloadSingleSpriteAsync(spriteData, maxDownloadAttempts, monoBeh, token);
                            if (!success)
                            {
                                failedSprites.Add(spriteData);
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogException(ex);
                            failedSprites.Add(spriteData);
                        }
                        finally
                        {
                            int currentCount = Interlocked.Increment(ref downloadedCount);
                            monoBeh.EditorDelegateHolder.UpdateProgress.Invoke(monoBeh, ProgressBarCategory.DownloadingSprites, currentCount);
                            semaphore.Release();
                        }
                    }, token));
                }

                await Task.WhenAll(downloadTasks);
            }

            monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingSprites);
            Debug.Log(FcuLocKey.log_downloading_images.Localize(totalCount - failedSprites.Count, totalCount));

            return failedSprites;
        }

        private static async Task<bool> DownloadSingleSpriteAsync(SpriteData spriteData, int maxAttempts, ConverterBase monoBeh, CancellationToken token)
        {
            if (string.IsNullOrEmpty(spriteData.Link))
            {
                return false;
            }

            DARequest request = new DARequest
            {
                RequestType = RequestType.GetFile,
                Query = spriteData.Link
            };

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(FcuConfig.SpriteDownloadTimeoutSeconds)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token))
                {
                    DAResult<byte[]> result = await monoBeh.RequestSender.SendRequest<byte[]>(request, linkedCts.Token);

                    if (result.Success && result.Object != null)
                    {
                        SpriteBatchWriter.Add(spriteData.Node, result.Object);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
#endif