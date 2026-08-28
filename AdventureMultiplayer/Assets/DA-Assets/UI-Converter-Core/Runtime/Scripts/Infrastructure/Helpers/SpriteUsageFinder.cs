#if UNITY_EDITOR
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
#endif

namespace DA_Assets
{
    public static class SpriteUsageFinder
    {
        public sealed class UsedSprite : IEquatable<UsedSprite>
        {
            public string Guid;

            public string Path;

            public string Name;

            public bool IsSpriteSubAsset;

            public Vector2Int Size;

            public bool IsAddressable;

            public List<UsageRef> Usages = new();

            public bool Selected { get; set; }


            public bool Equals(UsedSprite other)
            {
                if (other is null) return false;

                if (!string.IsNullOrEmpty(Guid) && !string.IsNullOrEmpty(other.Guid))
                    return string.Equals(Guid, other.Guid, StringComparison.OrdinalIgnoreCase);

                return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj) => Equals(obj as UsedSprite);

            public override int GetHashCode()
            {
                if (!string.IsNullOrEmpty(Guid))
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(Guid);

                return StringComparer.OrdinalIgnoreCase.GetHashCode(Path ?? string.Empty);
            }
        }

        public enum UsageKind
        {
            Scene,
            Prefab,
            Material,
            AnimatorController,
            AnimationClip,
            TimelinePlayable,
            SpriteAtlas,
            Tile,
            ScriptableObject,
            AddressableEntry,
            AddressableDep
        }

        public enum RefStrength
        {
            Direct,
            Indirect
        }

        public sealed class UsageRef
        {
            public UsageKind Kind;

            public string AssetGuid;

            public string AssetPath;

            public string AssetType;


            public string HierarchyPath;
            public string ComponentType;
            public string PropertyPath;
            public string ShaderProperty;
            public string AnimationBinding;
            public string TimelineTrack;
            public string AtlasSpriteName;
            public string Notes;

            public RefStrength Strength = RefStrength.Indirect;


            public string AddressableGroup;
            public string AddressKey;
            public string[] AddressLabels;
        }





        public static HashSet<UsedSprite> GetUsedSprites_AllAssets(
            string[] allSpriteGuids,
            bool includeScenes = true,
            bool includePrefabs = true,
            bool includeMaterials = true,
            bool includeAnimation = true,
            bool includeAtlases = true,
            bool includeTiles = true,
            bool includeScriptableObjects = false,
            bool includeAddressables = true
        )
        {
#if !UNITY_EDITOR

            return new HashSet<UsedSprite>();
#else

            var spriteIndex = BuildSpriteIndex(allSpriteGuids);


            if (spriteIndex.Count == 0)
                return new HashSet<UsedSprite>();

            var spritePaths = new HashSet<string>(
                spriteIndex.Values.Select(us => us.Path),
                StringComparer.OrdinalIgnoreCase);


            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (includeScenes) AddAssetsByFilter("t:Scene", roots);
            if (includePrefabs) AddAssetsByFilter("t:Prefab", roots);
            if (includeMaterials) AddAssetsByFilter("t:Material", roots);

            if (includeAnimation)
            {
                AddAssetsByFilter("t:AnimatorController", roots);
                AddAssetsByFilter("t:AnimationClip", roots);
                AddAssetsByFilter("t:PlayableAsset", roots);
            }

            if (includeAtlases) AddAssetsByFilter("t:SpriteAtlas", roots);
            if (includeTiles) AddAssetsByFilter("t:TileBase", roots);

            if (includeScriptableObjects) AddAssetsByFilter("t:ScriptableObject", roots);


            CollectUsagesViaDependencies(roots, spritePaths, spriteIndex);


#if ADDRESSABLES
            if (includeAddressables)
            {
                try
                {
                    CollectAddressablesUsages(spritePaths, spriteIndex);
                }
                catch
                {

                }
            }
#endif


            return new HashSet<UsedSprite>(spriteIndex.Values, spriteIndex.Comparer);
#endif
        }




#if UNITY_EDITOR

        private static UsedSpriteDictionary BuildSpriteIndex(string[] allSpriteGuids)
        {
            var dict = new UsedSpriteDictionary();

            foreach (var guid in allSpriteGuids ?? Array.Empty<string>())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).ToUnityPath();
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;


                var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                bool isSprite = mainType == typeof(Sprite);
                bool isTex2D = mainType == typeof(Texture2D);

                if (!isSprite && !isTex2D)
                    continue;

                var used = new UsedSprite
                {
                    Guid = guid,
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    IsSpriteSubAsset = isSprite,
                    Size = TryGetSize(path, isSprite),
                    IsAddressable = IsPathAddressable(path)
                };

                dict.AddOrGet(used);
            }

            return dict;
        }

        private static void AddAssetsByFilter(string filter, HashSet<string> roots)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid).ToUnityPath();
                if (!string.IsNullOrEmpty(p) && p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    roots.Add(p);
            }
        }

        private static void CollectUsagesViaDependencies(
            HashSet<string> roots,
            HashSet<string> spritePaths,
            UsedSpriteDictionary spriteIndex)
        {
            if (roots == null || roots.Count == 0) return;

            const int Chunk = 512;
            var rootArray = roots.ToArray();

            for (int i = 0; i < rootArray.Length; i += Chunk)
            {
                var slice = rootArray.Skip(i).Take(Chunk).ToArray();



                var added = new HashSet<(string referrer, string sprite)>(new PairOrdinalIgnoreCaseComparer());

                foreach (var referrerPath in slice)
                {
                    var referrerGuid = AssetDatabase.AssetPathToGUID(referrerPath);
                    var referrerType = AssetDatabase.GetMainAssetTypeAtPath(referrerPath);
                    var kind = GuessUsageKindFromType(referrerType);


                    string[] depsForReferrer;
                    try
                    {
                        depsForReferrer = AssetDatabase.GetDependencies(referrerPath, true);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var dep in depsForReferrer)
                    {
                        var np = dep.ToUnityPath();
                        if (!spritePaths.Contains(np))
                            continue;

                        if (!added.Add((referrerPath, np)))
                            continue;


                        if (!spriteIndex.TryGetByPath(np, out var usedSprite))
                            continue;


                        var depType = AssetDatabase.GetMainAssetTypeAtPath(np);
                        var isSpriteDep = (depType == typeof(Sprite));

                        usedSprite.Usages.Add(new UsageRef
                        {
                            Kind = kind,
                            AssetGuid = referrerGuid,
                            AssetPath = referrerPath,
                            AssetType = referrerType?.FullName ?? "Unknown",
                            Strength = RefStrength.Indirect,
                            Notes = isSpriteDep
                                ? "Dependency includes Sprite asset."
                                : "Dependency includes Texture2D with Sprite sub-assets."
                        });
                    }
                }
            }
        }

#if ADDRESSABLES
        private static void CollectAddressablesUsages(
            HashSet<string> spritePaths,
            UsedSpriteDictionary spriteIndex)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            foreach (var group in settings.groups.Where(g => g != null))
            {
                foreach (var entry in group.entries)
                {
                    var entryPath = entry.AssetPath.ToUnityPath();
                    if (string.IsNullOrEmpty(entryPath))
                        continue;

                    var entryGuid = entry.guid;
                    var entryType = AssetDatabase.GetMainAssetTypeAtPath(entryPath);
                    var labels = entry.labels?.ToArray() ?? Array.Empty<string>();


                    if (spritePaths.Contains(entryPath) && spriteIndex.TryGetByPath(entryPath, out var directSprite))
                    {
                        directSprite.IsAddressable = true;
                        directSprite.Usages.Add(new UsageRef
                        {
                            Kind = UsageKind.AddressableEntry,
                            AssetGuid = entryGuid,
                            AssetPath = entryPath,
                            AssetType = entryType?.FullName ?? "Unknown",
                            Strength = RefStrength.Direct,
                            AddressableGroup = group.Name,
                            AddressKey = entry.address,
                            AddressLabels = labels,
                            Notes = "Addressable entry points directly to the sprite/texture."
                        });
                    }


                    string[] addrDeps = null;
                    try { addrDeps = AssetDatabase.GetDependencies(entryPath, true); }
                    catch { addrDeps = null; }

                    if (addrDeps == null) continue;

                    foreach (var dep in addrDeps)
                    {
                        var np = dep.ToUnityPath();
                        if (!spritePaths.Contains(np))
                            continue;

                        if (!spriteIndex.TryGetByPath(np, out var usedSprite))
                            continue;

                        usedSprite.IsAddressable = true;

                        usedSprite.Usages.Add(new UsageRef
                        {
                            Kind = UsageKind.AddressableDep,
                            AssetGuid = entryGuid,
                            AssetPath = entryPath,
                            AssetType = entryType?.FullName ?? "Unknown",
                            Strength = RefStrength.Indirect,
                            AddressableGroup = group.Name,
                            AddressKey = entry.address,
                            AddressLabels = labels,
                            Notes = "Sprite is a dependency of an Addressable entry."
                        });
                    }
                }
            }
        }
#endif

        private static Vector2Int TryGetSize(string path, bool isSprite)
        {
            try
            {
                if (isSprite)
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null)
                        return new Vector2Int(Mathf.RoundToInt(s.rect.width), Mathf.RoundToInt(s.rect.height));
                }
                else
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (t != null)
                        return new Vector2Int(t.width, t.height);
                }
            }
            catch {  }

            return new Vector2Int(0, 0);
        }

        private static bool IsPathAddressable(string path)
        {
#if ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return false;

            var entry = settings.FindAssetEntry(guid);
            return entry != null;
#else
            return false;
#endif
        }

        private static UsageKind GuessUsageKindFromType(Type t)
        {
            if (t == typeof(SceneAsset)) return UsageKind.Scene;
            if (t == typeof(GameObject)) return UsageKind.Prefab;
            if (t == typeof(Material)) return UsageKind.Material;
            if (t != null && t.FullName == "UnityEditor.Animations.AnimatorController")
                return UsageKind.AnimatorController;
            if (t == typeof(AnimationClip)) return UsageKind.AnimationClip;


            if (t != null && typeof(UnityEngine.Playables.PlayableAsset).IsAssignableFrom(t))
                return UsageKind.TimelinePlayable;


            if (t != null && t.FullName == "UnityEngine.U2D.SpriteAtlas")
                return UsageKind.SpriteAtlas;


            if (t != null && IsSubclassOfFullName(t, "UnityEngine.Tilemaps.TileBase"))
                return UsageKind.Tile;


            if (t != null && typeof(ScriptableObject).IsAssignableFrom(t))
                return UsageKind.ScriptableObject;

            return UsageKind.ScriptableObject;
        }

        private static bool IsSubclassOfFullName(Type type, string baseFullName)
        {
            while (type != null && type != typeof(object))
            {
                if (string.Equals(type.FullName, baseFullName, StringComparison.Ordinal))
                    return true;

                type = type.BaseType;
            }
            return false;
        }





        private sealed class UsedSpriteDictionary
        {
            private readonly Dictionary<string, UsedSprite> byGuid =
                new(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, UsedSprite> byPath =
                new(StringComparer.OrdinalIgnoreCase);

            public IEqualityComparer<UsedSprite> Comparer => new UsedSpriteEqualityComparer();

            public int Count => byPath.Count;

            public IEnumerable<UsedSprite> Values => byPath.Values;

            public void AddOrGet(UsedSprite us)
            {
                if (!string.IsNullOrEmpty(us.Guid))
                    byGuid[us.Guid] = us;

                if (!string.IsNullOrEmpty(us.Path))
                    byPath[us.Path] = us;
            }

            public bool TryGetByPath(string path, out UsedSprite us)
            {
                return byPath.TryGetValue(path, out us);
            }
        }

        private sealed class UsedSpriteEqualityComparer : IEqualityComparer<UsedSprite>
        {
            public bool Equals(UsedSprite x, UsedSprite y) => x?.Equals(y) ?? (y is null);
            public int GetHashCode(UsedSprite obj) => obj?.GetHashCode() ?? 0;
        }

        private sealed class PairOrdinalIgnoreCaseComparer : IEqualityComparer<(string a, string b)>
        {
            public bool Equals((string a, string b) x, (string a, string b) y) =>
                StringComparer.OrdinalIgnoreCase.Equals(x.a, y.a) &&
                StringComparer.OrdinalIgnoreCase.Equals(x.b, y.b);

            public int GetHashCode((string a, string b) obj)
            {
                unchecked
                {
                    int h1 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.a ?? string.Empty);
                    int h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.b ?? string.Empty);
                    return (h1 * 397) ^ h2;
                }
            }
        }

#endif
    }
}
#endif