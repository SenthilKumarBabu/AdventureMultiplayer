#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.DAI;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpriteGenerator : FcuBase
    {
        public async Task GenerateSprites(List<Node> fobjects, CancellationToken token)
        {
            await GenerateSprites(fobjects, null, token);
        }

        internal async Task GenerateSprites(List<Node> fobjects, SpriteIdentityCache cache, CancellationToken token)
        {
            IEnumerable<Node> source = cache?.UniqueRepresentatives ?? fobjects;
            List<Node> generative = source.Where(x => x.Data.NeedGenerate).ToList();

            if (generative.IsEmpty())
                return;

            int generatedCount = 0;
            int loggedCount = -1;
            int totalCount = generative.Count;
            Node fobject;

            Debug.Log(FcuLocKey.log_generating_sprites.Localize(0, totalCount));
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.GeneratingSprites, totalCount, false);

            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    token.ThrowIfCancellationRequested();

                    fobject = generative[i];

                    _ = GenerateSprite(fobject, () =>
                    {
                        int currentCount = Interlocked.Increment(ref generatedCount);
                        monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.GeneratingSprites, currentCount);
                    });

                    await Task.Delay(250);
                }

                while (true)
                {
                    int currentCount = generatedCount;

                    if (loggedCount != currentCount)
                    {
                        loggedCount = currentCount;
                        Debug.Log(FcuLocKey.log_generating_sprites.Localize(currentCount, totalCount));
                    }

                    if (currentCount >= totalCount)
                        break;

                    await Task.Delay(1000, token);
                }
            }
            finally
            {
                monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.GeneratingSprites);
            }
        }

        private async Task GenerateSprite(Node fobject, Action increase)
        {
            FcuLogger.Debug($"GenerateSprites | {fobject.Data.NameHierarchy} | {fobject.Data.NeedGenerate}", FcuDebugSettingsFlags.LogSpriteGenerator);

            try
            {
                byte[] bytes = FcuFigmageSpriteBaker.BakePngBytes(fobject, ResolveRenderScale(fobject));

                SpriteBatchWriter.Add(fobject, bytes);
            }
            catch (Exception ex)
            {
                FcuLogger.Debug($"Can't generate '{fobject.Data.NameHierarchy}'\n{ex}", FcuDebugSettingsFlags.LogError);
                fobject.Data.FcuImageType = FcuImageType.Drawable;
                fobject.SetReason(ReasonKey.Gen_GenerationFailed);
            }

            increase.Invoke();
            await Task.Yield();
        }

        private float ResolveRenderScale(Node fobject)
        {
            var settings = monoBeh.Settings.ImageSpritesSettings;

            float scale = Mathf.Max(monoBeh.Config.IMAGE_SCALE_MIN, settings.ImageScale);
            scale = Mathf.Round(scale * 100f) / 100f;

            fobject.GetBoundingSize(out Vector2 bSize);
            fobject.GetRenderSize(out Vector2 rSize);

            float maxX = Mathf.Max(bSize.x, rSize.x, fobject.Size.x);
            float maxY = Mathf.Max(bSize.y, rSize.y, fobject.Size.y);
            float maxEdge = Mathf.Max(maxX, maxY);

            if (maxEdge <= 0f)
                return scale;

            float capScale = (float)settings.MaxSpriteSize / maxEdge;
            float effective = Mathf.Min(scale, capScale);
            effective = Mathf.Max(monoBeh.Config.IMAGE_SCALE_MIN, effective);

            if (effective < scale)
            {
                FcuLogger.Debug(
                    $"Figmage scale clamped {scale:0.##} -> {effective:0.##} for '{fobject.Data.NameHierarchy}' (max edge {maxEdge:0} px, cap {settings.MaxSpriteSize} px).",
                    FcuDebugSettingsFlags.LogSpriteGenerator);
            }

            return effective;
        }
    }
}
#endif