#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITEXT
using LightSide;
#endif

namespace DA_Assets.UCC.Drawers.CanvasDrawers
{
    public partial class UniTextDrawer
    {
#if UNITEXT
        private void PopulateFontModifier(UniText text, Node fobject)
        {
            FontMetadata defaultFont = fobject.Style.GetFontMetadata();

            if (!monoBeh.FontLoader.TryResolveUniTextFamily(defaultFont, out _, out _))
                return;

            RangeRule rule = null;
            if (text.Font == null)
            {
                rule = RegisterRangeRule(text, new FontModifier());
                rule.data.Add(new RangeRule.Data
                {
                    range = string.Empty,
                    parameter = GetFamilyName(defaultFont)
                });
            }

            var data = BuildRunRanges(
                fobject,
                selector: style => style.GetFontMetadata(),
                toParameter: metadata => GetFamilyName(metadata),
                include: metadata => !string.Equals(
                        GetFamilyName(metadata),
                        GetFamilyName(defaultFont),
                        StringComparison.Ordinal)
                    && monoBeh.FontLoader.TryResolveUniTextFamily(metadata, out _, out _));

            if (rule != null)
                rule.data.AddRange(data);
            else
                RegisterIfHasData(text, new FontModifier(), data);
        }

        private UniTextFontStack ResolveFontStack(Node fobject)
        {
            List<FontMetadata> requestedFonts = fobject.GetAllFontMetadata();
            var resolved = new List<ResolvedFontAlias>();

            foreach (FontMetadata metadata in requestedFonts)
            {
                if (TryResolveFontAlias(metadata, out ResolvedFontAlias alias))
                    resolved.Add(alias);
            }

            if (resolved.Count == 0)
                return null;

            if (resolved.Count == 1 && !ContainsGeneratedAliasFamilies(resolved[0].SourceStack))
                return resolved[0].SourceStack;

            return CreateCombinedFontStack(resolved);
        }

        private UniTextFontStack CreateCombinedFontStack(List<ResolvedFontAlias> resolved)
        {
            var combinedFamilies = new List<FontFamily>();
            var addedFamilyKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (UniTextFontStack stack in resolved.Select(x => x.SourceStack).Distinct())
            {
                AppendStackFamilies(stack, combinedFamilies, addedFamilyKeys, new HashSet<UniTextFontStack>());
            }

            if (combinedFamilies.Count == 0)
                return resolved[0].SourceStack;

            return GetOrCreateCombinedFontStackAsset(combinedFamilies);
        }

        private bool TryResolveFontAlias(FontMetadata metadata, out ResolvedFontAlias alias)
        {
            alias = default;

            if (!monoBeh.FontLoader.TryResolveUniTextFamily(metadata, out UniTextFontStack sourceStack, out FontFamily sourceFamily))
                return false;

            UniTextFont primary = sourceFamily.primary;
            if (monoBeh.FontLoader.TryGetUniTextExactFace(metadata, out _, out _, out UniTextFont exactFace))
            {
                primary = exactFace;
            }

            if (primary == null)
                return false;

            alias = new ResolvedFontAlias
            {
                Metadata = metadata,
                SourceStack = sourceStack,
                SourceFamily = sourceFamily,
                Primary = primary,
                AliasName = GetFamilyName(metadata)
            };

            return true;
        }

        private UniTextFont ResolvePrimaryFont(FontMetadata metadata)
        {
            if (monoBeh.FontLoader.TryGetUniTextExactFace(metadata, out _, out _, out UniTextFont exactFace))
                return exactFace;

            if (monoBeh.FontLoader.TryResolveUniTextFamily(metadata, out _, out FontFamily family))
                return family.primary;

            return null;
        }

        private static FontFamily CreateAliasFamily(ResolvedFontAlias alias)
        {
            var faces = new List<UniTextFont>();

            if (alias.SourceFamily.primary != null && alias.SourceFamily.primary != alias.Primary)
                faces.Add(alias.SourceFamily.primary);

            if (!alias.SourceFamily.faces.IsEmpty())
            {
                for (int i = 0; i < alias.SourceFamily.faces.Length; i++)
                {
                    UniTextFont face = alias.SourceFamily.faces[i];
                    if (face != null && face != alias.Primary && !faces.Contains(face))
                        faces.Add(face);
                }
            }

            return new FontFamily
            {
                name = alias.AliasName,
                primary = alias.Primary,
                faces = faces.ToArray(),
                preferredLanguage = alias.SourceFamily.preferredLanguage
            };
        }

        private static void AppendStackFamilies(
            UniTextFontStack stack,
            List<FontFamily> target,
            HashSet<string> addedFamilyKeys,
            HashSet<UniTextFontStack> visitedStacks)
        {
            if (stack == null || !visitedStacks.Add(stack))
                return;

            if (!stack.families.IsEmpty())
            {
                for (int i = 0; i < stack.families.Length; i++)
                {
                    FontFamily family = stack.families[i];
                    if (family.primary == null)
                        continue;

                    string familyKey = GetFamilyKey(family);
                    if (familyKey.StartsWith("FCU__", StringComparison.Ordinal))
                        continue;

                    if (!addedFamilyKeys.Add(familyKey))
                        continue;

                    target.Add(family);
                }
            }

            AppendStackFamilies(stack.fallbackStack, target, addedFamilyKeys, visitedStacks);
        }

        private static string GetFamilyKey(FontFamily family)
        {
            if (family.name.IsEmpty() == false)
                return family.name;

            if (family.primary != null && family.primary.FaceInfo.familyName.IsEmpty() == false)
                return family.primary.FaceInfo.familyName;

            return family.primary != null ? family.primary.name : string.Empty;
        }

        private static bool ContainsGeneratedAliasFamilies(UniTextFontStack stack)
        {
            if (stack == null || stack.families.IsEmpty())
                return false;

            for (int i = 0; i < stack.families.Length; i++)
            {
                if (GetFamilyKey(stack.families[i]).StartsWith("FCU__", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string BuildCombinedFontStackKey(IEnumerable<FontFamily> families)
        {
            return string.Join("|", families.Select(BuildFamilySignature));
        }

        private static string BuildFamilySignature(FontFamily family)
        {
            string primaryId = family.primary != null ? family.primary.GetInstanceID().ToString() : "null";
            string language = family.preferredLanguage ?? string.Empty;
            string faces = string.Join(",",
                family.faces?
                    .Where(face => face != null)
                    .Select(face => face.GetInstanceID().ToString())
                ?? Enumerable.Empty<string>());

            return $"{family.name}:{primaryId}:{language}:{faces}";
        }

        private UniTextFontStack GetOrCreateCombinedFontStackAsset(List<FontFamily> families)
        {
            string key = BuildCombinedFontStackKey(families);
            string hash = Hash128.Compute(key).ToString();

#if UNITY_EDITOR
            string folder = Path.Combine(monoBeh.FontLoader.UniTextFontsPath, "FCU Generated").ToUnityPath();
            folder.GetFullAssetPath().CreateFolderIfNotExists();
            string assetPath = Path.Combine(folder, "FCU_UniText_Generated.asset").ToUnityPath();
            string stackName = $"FCU_UniText_{hash}";
            UniTextFontStack combined = null;

            if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is not UniTextFontStack existing)
                        continue;

                    if (BuildCombinedFontStackKey(existing.families ?? Array.Empty<FontFamily>()) == key)
                    {
                        combined = existing;
                        break;
                    }
                }
            }

            if (combined == null)
            {
                combined = ScriptableObject.CreateInstance<UniTextFontStack>();
                combined.name = stackName;

                if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                {
                    UnityEditor.AssetDatabase.CreateAsset(combined, assetPath);
                }
                else
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(combined, assetPath);
                }
            }

            combined.families = families.ToArray();
            combined.fallbackStack = null;
            combined.SetDirtyExt();
            UnityEditor.AssetDatabase.SaveAssetIfDirty(combined);
            return combined;
#else
            UniTextFontStack combined = ScriptableObject.CreateInstance<UniTextFontStack>();
            combined.name = $"FCU_UniText_{hash}";
            combined.hideFlags = HideFlags.HideAndDontSave;
            combined.families = families.ToArray();
            combined.fallbackStack = null;
            return combined;
#endif
        }

        private static bool StackContainsFamily(UniTextFontStack stack, string familyName)
        {
            var visitedStacks = new HashSet<UniTextFontStack>();

            while (stack != null && visitedStacks.Add(stack))
            {
                if (!stack.families.IsEmpty())
                {
                    for (int i = 0; i < stack.families.Length; i++)
                    {
                        if (stack.families[i].name == familyName)
                            return true;
                    }
                }

                stack = stack.fallbackStack;
            }

            return false;
        }

        private static string GetFamilyName(FontMetadata metadata)
        {
            return metadata.Family;
        }

        private static string GetFallbackFamilyName(UniTextFontStack stack, FontFamily family, string preferredName, int familyIndex)
        {
            if (!preferredName.IsEmpty() && familyIndex == 0 && stack.families != null && stack.families.Length == 1)
                return preferredName;

            string stackName = stack.name?.Replace(" FontStack", string.Empty);
            if (!stackName.IsEmpty())
                return stackName;

            if (family.primary != null && !family.primary.name.IsEmpty())
                return family.primary.name;

            return $"Family_{familyIndex}";
        }

        private struct ResolvedFontAlias
        {
            public FontMetadata Metadata;
            public UniTextFontStack SourceStack;
            public FontFamily SourceFamily;
            public UniTextFont Primary;
            public string AliasName;
        }
#endif
    }
}
#endif