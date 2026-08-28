#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public class SpriteDownloadUrl : ISpriteDownloadStrategy
    {
        private readonly ConverterBase _monoBeh;
        private readonly SpriteIdentityCache _cache;

        private int _maxConcurrentDownloads = 100;
        private int _maxDownloadAttempts = 3;
        private float _maxChunkSize = 32_000_000;
        private int _maxSpritesCount = 100;
        private int _errorLogSplitLimit = 50;

        public ImportMode Mode => ImportMode.Url;

        public SpriteDownloadUrl(ConverterBase monoBeh, SpriteIdentityCache cache)
        {
            _monoBeh = monoBeh;
            _cache = cache;
        }

        public async Task DownloadSprites(List<Node> fobjects, CancellationToken token)
        {

            List<Node> uniqueNodesToDownload;

            if (_cache != null)
            {
                IReadOnlyList<Node> reps = _cache.UniqueRepresentatives;
                uniqueNodesToDownload = new List<Node>(reps.Count);
                foreach (Node rep in reps)
                {
                    if (rep.Data.NeedDownload)
                        uniqueNodesToDownload.Add(rep);
                }
            }
            else
            {

                uniqueNodesToDownload = fobjects
                    .Where(x => x.Data.NeedDownload)
                    .GroupBy(SpriteRenderKeyUtility.GetSpriteRenderKey)
                    .Select(g => g.First())
                    .ToList();
            }

            if (uniqueNodesToDownload.IsEmpty())
            {
                Debug.Log(FcuLocKey.log_sprite_downloader_no_sprites.Localize());
                return;
            }

            await SpriteDataCalculator.CalculateAndSetSpriteData(uniqueNodesToDownload, _monoBeh, token);

            ConcurrentBag<SpriteData> failedSprites = await DownloadWithScaleFallback(uniqueNodesToDownload, token);
            LogFailedDownloads(new ConcurrentBag<Node>(failedSprites.Select(x => x.Node)));
        }

        private async Task<ConcurrentBag<SpriteData>> DownloadWithScaleFallback(List<Node> sprites, CancellationToken token)
        {
            var finalFailedSprites = new ConcurrentBag<SpriteData>();
            List<Node> pendingSprites = sprites;

            while (!pendingSprites.IsEmpty())
            {
                token.ThrowIfCancellationRequested();

                Dictionary<ImageFormatScaleKey, List<List<SpriteData>>> chunks = SpriteChunker.CreateChunks(
                    pendingSprites,
                    _maxChunkSize,
                    _maxSpritesCount);

                List<SpriteData> spritesWithLinks = await FigmaLinkFetcher.FetchLinksAsync(
                    chunks,
                    _monoBeh,
                    token);

                ConcurrentBag<SpriteData> failedSprites = await ConcurrentSpriteDownloader.DownloadAllAsync(
                    spritesWithLinks,
                    _maxConcurrentDownloads,
                    _maxDownloadAttempts,
                    _monoBeh,
                    token);

                if (failedSprites.IsEmpty())
                    break;

                var retrySprites = new List<Node>();

                foreach (SpriteData failedSprite in failedSprites)
                {
                    if (failedSprite.CanRetryWithLowerScale &&
                        SpriteDataCalculator.TryReduceScale(failedSprite.Node, _monoBeh))
                    {
                        retrySprites.Add(failedSprite.Node);
                    }
                    else
                    {
                        finalFailedSprites.Add(failedSprite);
                    }
                }

                pendingSprites = retrySprites.Distinct().ToList();
            }

            return finalFailedSprites;
        }

        private void LogFailedDownloads(ConcurrentBag<Node> failedObjects)
        {
            if (failedObjects.IsEmpty())
            {
                return;
            }

            List<List<string>> components = failedObjects.Select(x => x.Data.NameHierarchy).Split(_errorLogSplitLimit);

            foreach (List<string> component in components)
            {
                string hierarchies = string.Join("\n", component);
                Debug.LogError(FcuLocKey.log_malformed_url.Localize(component.Count, hierarchies));
            }
        }
    }
}
#endif