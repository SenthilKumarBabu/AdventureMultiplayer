#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

#pragma warning disable CS1998

namespace DA_Assets.UCC
{
    [Serializable]
    public class SpriteDuplicateRemover : FcuBase
    {
        internal async Task<List<List<SpriteUsageFinder.UsedSprite>>> GetDuplicateGroups(CancellationToken token)
        {
            string spritesPath = monoBeh.Settings.ImageSpritesSettings.SpritesPath;
            string[] allSpriteGuids = new string[] { };

#if UNITY_EDITOR
            allSpriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesPath });
#endif

            HashSet<SpriteUsageFinder.UsedSprite> allSpritesWithUsage = SpriteUsageFinder.GetUsedSprites_AllAssets(
                allSpriteGuids,
                includeScenes: true,
                includePrefabs: true,
                includeMaterials: true,
                includeAnimation: true,
                includeAtlases: true,
                includeTiles: true,
                includeScriptableObjects: true,
                includeAddressables: true
            );

            if (allSpritesWithUsage.Count == 0)
            {
                Debug.Log(FcuLocKey.log_sprite_duplicate_remover_no_sprites.Localize(spritesPath));
                return null;
            }

            var spriteLookup = allSpritesWithUsage.ToDictionary(
                s => s.Path,
                s => s,
                StringComparer.OrdinalIgnoreCase
            );

            string[] allSpritePaths = allSpritesWithUsage.Select(s => s.Path).ToArray();

            var sdf = new SpriteDuplicateFinder();
            List<List<SpriteUsageFinder.UsedSprite>> thinGroups = sdf.GetGroups(allSpritePaths, false);

            List<List<SpriteUsageFinder.UsedSprite>> groups = new List<List<SpriteUsageFinder.UsedSprite>>();
            foreach (var thinGroup in thinGroups)
            {
                var fatGroup = new List<SpriteUsageFinder.UsedSprite>();
                foreach (var thinSprite in thinGroup)
                {
                    if (spriteLookup.TryGetValue(thinSprite.Path, out var fatSprite))
                    {
                        fatGroup.Add(fatSprite);
                    }
                    else
                    {
                        fatGroup.Add(thinSprite);
                    }
                }
                groups.Add(fatGroup);
            }

            foreach (var g in groups)
            {
                if (g == null || g.Count == 0) continue;

                SpriteUsageFinder.UsedSprite bestToKeep = null;

                var usedInGroup = g.Where(s => s.Usages.Count > 0).ToList();

                if (usedInGroup.Any())
                {
                    bestToKeep = usedInGroup.OrderByDescending(s => (s.Size.x) * (s.Size.y)).FirstOrDefault();
                }

                if (bestToKeep == null)
                {
                    bestToKeep = g.OrderByDescending(s => (s.Size.x) * (s.Size.y)).FirstOrDefault();
                }

                if (bestToKeep == null)
                    continue;

                foreach (var sprite in g)
                {
                    sprite.Selected = (sprite != bestToKeep);
                }
            }

            return groups;
        }

        internal async Task RemoveDuplicates(List<Node> fobjects, SpriteDuplicateFinderRequest request, CancellationToken token)
        {
            List<List<SpriteUsageFinder.UsedSprite>> processedGroups = null;

            while (!request.IsResolved)
            {
                if (token.IsCancellationRequested)
                    return;
                await Task.Delay(100, token);
            }

            processedGroups = await request.Completion;

            await ApplyDuplicateSelection(fobjects, processedGroups, token);
        }

        internal async Task ApplyDuplicateSelection(List<Node> fobjects, List<List<SpriteUsageFinder.UsedSprite>> processedGroups, CancellationToken token)
        {
            var replaceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var toDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in processedGroups)
            {
                if (group == null || group.Count == 0)
                    continue;

                var keepCandidates = group.Where(x => !x.Selected).ToList();

                if (keepCandidates.Count > 0)
                {
                    var best = keepCandidates.OrderByDescending(s => (s.Size.x) * (s.Size.y)).First();
                    string bestPath = best.Path.ToUnityPath();

                    foreach (var dub in group)
                    {
                        string path = dub.Path.ToUnityPath();
                        if (dub.Selected && !path.Equals(bestPath, StringComparison.OrdinalIgnoreCase))
                        {
                            replaceMap[path] = bestPath;
                            toDelete.Add(path);
                        }
                    }
                }
                else
                {
                    foreach (var dub in group)
                    {
                        string path = dub.Path.ToUnityPath();
                        replaceMap[path] = null;
                        toDelete.Add(path);
                    }
                }
            }

            if (replaceMap.Count == 0 && toDelete.Count == 0)
                return;

            foreach (var fobject in fobjects)
            {
                string spritePath = fobject.Data?.SpritePath;
                if (spritePath.IsEmpty())
                    continue;

                string normalizedPath = spritePath.ToUnityPath();
                if (replaceMap.TryGetValue(normalizedPath, out string newPath))
                {
                    fobject.Data.SpritePath = newPath;
                    RedrawReplacedSprite(fobject, newPath);
                }
            }
#if UNITY_EDITOR
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var path in toDelete)
                {
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
        }

        private void RedrawReplacedSprite(Node fobject, string newPath)
        {
            if (newPath.IsEmpty())
                return;

            if (fobject.Data?.GameObject == null)
                return;

            monoBeh.CanvasDrawer.ImageDrawer.Draw(fobject);
        }
    }
}
#endif