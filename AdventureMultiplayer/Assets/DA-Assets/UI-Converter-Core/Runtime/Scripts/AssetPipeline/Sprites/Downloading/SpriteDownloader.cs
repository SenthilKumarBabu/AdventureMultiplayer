#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpriteDownloader : FcuBase
    {
        private ISpriteDownloadStrategy _strategy;
        private ImportMode _cachedMode;



        private SpriteIdentityCache _identityCache;
        internal SpriteIdentityCache IdentityCache
        {
            get => _identityCache;
            set
            {
                _identityCache = value;
                _strategy = null;
            }
        }

        private ISpriteDownloadStrategy GetStrategy(SpriteIdentityCache cache)
        {
            var currentMode = monoBeh.Settings.MainSettings.ImportMode;
            if (_strategy == null || _cachedMode != currentMode)
            {
                _cachedMode = currentMode;
                _strategy = currentMode switch
                {
                    ImportMode.Url => new SpriteDownloadUrl(monoBeh, cache),
                    ImportMode.Zip => new SpriteDownloadZip(monoBeh, cache),
                    _ => throw new NotSupportedException($"ImportMode {currentMode} is not supported")
                };
            }
            return _strategy;
        }

        public Task DownloadSprites(List<Node> fobjects, CancellationToken token)
        {
            return GetStrategy(IdentityCache).DownloadSprites(fobjects, token);
        }

        public static void LogFailedDownloads(ConcurrentBag<Node> failedObjects, int splitLimit)
        {
            if (failedObjects.IsEmpty())
            {
                return;
            }

            List<List<string>> components = failedObjects.Select(x => x.Data.NameHierarchy).Split(splitLimit);

            foreach (List<string> component in components)
            {
                string hierarchies = string.Join("\n", component);
                Debug.LogError(FcuLocKey.log_malformed_url.Localize(component.Count, hierarchies));
            }
        }
    }

    public struct FigmaImageRequest
    {
#if JSONNET_EXISTS
        [JsonProperty("err")]
#endif
        public string error;
#if JSONNET_EXISTS
        [JsonProperty("images")]
#endif

        public Dictionary<string, string> images;
    }

    public struct SpriteData
    {
        public Node Node { get; set; }
        public string Format { get; set; }
        public string Link { get; set; }
        public float Scale { get; set; }
        public bool CanRetryWithLowerScale { get; set; }
    }

    public struct ImageFormatScaleKey
    {
        public ImageFormat ImageFormat { get; set; }
        public float Scale { get; set; }
    }
}
#endif