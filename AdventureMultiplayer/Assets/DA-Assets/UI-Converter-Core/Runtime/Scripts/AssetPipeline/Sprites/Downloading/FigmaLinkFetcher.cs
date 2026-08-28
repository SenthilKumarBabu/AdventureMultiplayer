#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public static class FigmaLinkFetcher
    {
        private struct LinkRequestChunk
        {
            public ImageFormatScaleKey Key { get; set; }
            public List<SpriteData> SpriteChunk { get; set; }
        }

        public static async Task<List<SpriteData>> FetchLinksAsync(
            Dictionary<ImageFormatScaleKey, List<List<SpriteData>>> idFormatChunks,
            ConverterBase monoBeh,
            CancellationToken token)
        {
            List<LinkRequestChunk> requestChunks = idFormatChunks
                .SelectMany(pair => pair.Value.Select(chunk => new LinkRequestChunk
                {
                    Key = pair.Key,
                    SpriteChunk = chunk
                }))
                .ToList();

            if (requestChunks.IsEmpty())
                return new List<SpriteData>();

            var spritesWithLinks = new List<SpriteData>();
            int totalSpriteCount = requestChunks.Sum(c => c.SpriteChunk.Count);

            Debug.Log(FcuLocKey.log_getting_links.Localize(0, totalSpriteCount));
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks, totalSpriteCount, false);

            Queue<LinkRequestChunk> queue = new Queue<LinkRequestChunk>(requestChunks);
            int processedSpriteCount = 0;

            while (queue.Count > 0)
            {
                token.ThrowIfCancellationRequested();

                LinkRequestChunk request = queue.Dequeue();
                List<string> ids = request.SpriteChunk.Select(x => x.Node.Id).ToList();
                DARequest daRequest = RequestCreator.CreateImageLinksRequest(
                    monoBeh.Settings.MainSettings.ProjectId,
                    request.Key.ImageFormat.ToLower(),
                    request.Key.Scale,
                    ids,
                    monoBeh.RequestSender.GetRequestHeader(monoBeh.AuthProvider?.Token),
                    monoBeh.Settings.MainSettings.FigmaEnvironment);

                DAResult<FigmaImageRequest> result = await monoBeh.RequestSender.SendRequest<FigmaImageRequest>(
                    daRequest,
                    token);

                if (!result.Success)
                {
                    bool canSplit = CanSplitLinkFetchFailure(result);
                    bool canRetryWithLowerScale = CanRetryLinkFetchFailureWithLowerScale(result);

                    if (request.SpriteChunk.Count > 1 && canSplit)
                    {
                        EnqueueSplit(request, queue);
                        continue;
                    }

                    LogLinkFetchFailure(result);
                    AddChunkWithoutLinks(request.SpriteChunk, spritesWithLinks, canRetryWithLowerScale);
                    processedSpriteCount += request.SpriteChunk.Count;
                    monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks, processedSpriteCount);
                    continue;
                }

                var images = result.Object.images ?? new Dictionary<string, string>();

                if (images.IsEmpty() && ids.Any())
                {
                    Debug.LogError(FcuLocKey.log_figma_link_missing_ids.Localize(string.Join(", ", ids)));
                }

                PopulateLinks(request.SpriteChunk, images, monoBeh.Settings.MainSettings.Https);
                spritesWithLinks.AddRange(request.SpriteChunk);
                processedSpriteCount += request.SpriteChunk.Count;
                monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks, processedSpriteCount);
            }

            monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks);
            Debug.Log(FcuLocKey.log_getting_links.Localize(spritesWithLinks.Count, totalSpriteCount));

            return spritesWithLinks;
        }

        private static bool CanSplitLinkFetchFailure(DAResult<FigmaImageRequest> result)
        {
            int status = result.Error.status;
            return status == 400 || status == 413 || status == 414 || status == 422;
        }

        private static bool CanRetryLinkFetchFailureWithLowerScale(DAResult<FigmaImageRequest> result)
        {
            int status = result.Error.status;
            return status == 400 || status == 422;
        }

        private static void EnqueueSplit(LinkRequestChunk request, Queue<LinkRequestChunk> queue)
        {
            int mid = request.SpriteChunk.Count / 2;

            queue.Enqueue(new LinkRequestChunk
            {
                Key = request.Key,
                SpriteChunk = request.SpriteChunk.GetRange(0, mid)
            });

            queue.Enqueue(new LinkRequestChunk
            {
                Key = request.Key,
                SpriteChunk = request.SpriteChunk.GetRange(mid, request.SpriteChunk.Count - mid)
            });
        }

        private static void LogLinkFetchFailure(DAResult<FigmaImageRequest> result)
        {
            string errorDetails = result.Error.err.IsEmpty() ? "-" : result.Error.err;
            Debug.LogError(FcuLocKey.log_figma_link_fetch_failed.Localize(result.Error.status, errorDetails));
        }

        private static void AddChunkWithoutLinks(
            List<SpriteData> chunk,
            List<SpriteData> spritesWithLinks,
            bool canRetryWithLowerScale)
        {
            for (int i = 0; i < chunk.Count; i++)
            {
                SpriteData spriteData = chunk[i];
                spriteData.Link = string.Empty;
                spriteData.CanRetryWithLowerScale = canRetryWithLowerScale;
                spritesWithLinks.Add(spriteData);
            }
        }

        private static void PopulateLinks(List<SpriteData> chunk, Dictionary<string, string> images, bool useHttps)
        {
            for (int i = 0; i < chunk.Count; i++)
            {
                var spriteData = chunk[i];
                if (images.TryGetValue(spriteData.Node.Id, out string link))
                {
                    spriteData.Link = useHttps ? link : link?.Replace("https://", "http://");
                }
                else
                {
                    spriteData.Link = string.Empty;
                }
                chunk[i] = spriteData;
            }
        }
    }
}
#endif