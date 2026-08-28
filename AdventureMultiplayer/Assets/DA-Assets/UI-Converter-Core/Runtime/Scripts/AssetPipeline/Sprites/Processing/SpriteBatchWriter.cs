#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DA_Assets.UCC.Model;
using Debug = UnityEngine.Debug;
using DA_Assets.DAI;
using System.Collections.Concurrent;
using DA_Assets.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DA_Assets.UCC
{
    public static class SpriteBatchWriter
    {
        private struct PendingSprite
        {
            public Node Node;
            public byte[] Data;
        }

        private static readonly ConcurrentQueue<PendingSprite> _pending = new ConcurrentQueue<PendingSprite>();



        private static SpriteIdentityCache _identityCache;

        public static void SetCache(SpriteIdentityCache cache) => _identityCache = cache;

        public static void Add(Node fobject, byte[] data)
        {
            _pending.Enqueue(new PendingSprite
            {
                Node = fobject,
                Data = data
            });
        }

        public static async Task Flush(UnityEngine.Object key, CancellationToken token)
        {
            if (_pending.IsEmpty)
                return;

            List<PendingSprite> pendingSnapshot = new List<PendingSprite>();
            while (_pending.TryDequeue(out PendingSprite pendingSprite))
            {
                pendingSnapshot.Add(pendingSprite);
            }

            if (pendingSnapshot.Count == 0)
                return;

            int totalCount = pendingSnapshot.Count;
            ConverterBase fcu = key as ConverterBase;

            if (fcu != null)
            {
                fcu.EditorDelegateHolder.StartProgress?.Invoke(key, ProgressBarCategory.WritingSprites, totalCount, false);
            }

            try
            {
                await WriteSpritesAsync(pendingSnapshot, fcu, key, token);
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
                await WaitForMetaFilesAsync(pendingSnapshot, token);
                await WriteGuidMetaAsync(pendingSnapshot, token);
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
            finally
            {
                if (fcu != null)
                {
                    fcu.EditorDelegateHolder.CompleteProgress?.Invoke(key, ProgressBarCategory.WritingSprites);
                }
            }
        }

        private static async Task WriteSpritesAsync(List<PendingSprite> sprites, ConverterBase fcu, UnityEngine.Object key, CancellationToken token)
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                PendingSprite sprite = sprites[i];
                string dir = Path.GetDirectoryName(sprite.Node.Data.SpritePath);

                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                try
                {
                    byte[] data = SpriteAlphaNormalizer.NormalizeOpaqueAlpha(sprite.Data);

                    using (var stream = new FileStream(sprite.Node.Data.SpritePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await stream.WriteAsync(data, 0, data.Length, token);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }


                fcu?.EditorDelegateHolder.UpdateProgress?.Invoke(key, ProgressBarCategory.WritingSprites, i + 1);
                await Task.Yield();
            }
        }

        private static async Task WaitForMetaFilesAsync(List<PendingSprite> sprites, CancellationToken token)
        {
            var remaining = new List<PendingSprite>(sprites);
            Stopwatch sw = Stopwatch.StartNew();
            const double timeoutSeconds = 60;

            while (remaining.Count > 0)
            {
                token.ThrowIfCancellationRequested();

                remaining.RemoveAll(ps => File.Exists(ps.Node.Data.SpritePath + ".meta"));

                if (remaining.Count == 0)
                    return;

                if (sw.Elapsed.TotalSeconds > timeoutSeconds)
                {
                    Debug.LogError(FcuLocKey.log_sprite_batch_writer_timeout.Localize());
                    return;
                }

                await Task.Delay(200, token);
            }
        }

        private static async Task WriteGuidMetaAsync(List<PendingSprite> sprites, CancellationToken token)
        {
            foreach (PendingSprite ps in sprites)
            {
                token.ThrowIfCancellationRequested();


                int renderKey = _identityCache != null
                    ? _identityCache.GetRenderKey(ps.Node)
                    : SpriteRenderKeyUtility.GetSpriteRenderKey(ps.Node);

                GuidMetaUtility.WriteGuid(
                    ps.Node.Data.SpritePath + ".meta",
                    renderKey);

                await Task.Yield();
            }
        }
    }
}
#endif