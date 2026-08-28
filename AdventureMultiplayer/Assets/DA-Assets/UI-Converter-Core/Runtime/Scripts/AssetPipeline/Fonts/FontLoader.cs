#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using System.Threading;
using System.Threading.Tasks;
using DA_Assets.Logging;
using Resources = UnityEngine.Resources;
using TextCoreFontAsset = UnityEngine.TextCore.Text.FontAsset;
using System.IO;
using System.Reflection;

#if TextMeshPro
using TMPro;
#endif

#if UNITEXT
using LightSide;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public class FontLoader : FcuBase
    {
        [SerializeField] string ttfFontsPath = Path.Combine("Assets", "Fonts", "Ttf");
        public string TtfFontsPath { get => ttfFontsPath; set => ttfFontsPath = value; }

        [SerializeField] string tmpFontsPath = Path.Combine("Assets", "Fonts", "Sdf");
        public string TmpFontsPath { get => tmpFontsPath; set => tmpFontsPath = value; }

        [SerializeField] string uitkFontAssetsPath = Path.Combine("Assets", "Fonts", "UITK");
        public string UitkFontAssetsPath { get => uitkFontAssetsPath; set => uitkFontAssetsPath = value; }

        [SerializeField] public List<Font> TtfFonts = new List<Font>();
        [SerializeField] public List<TextCoreFontAsset> UitkFontAssets = new List<TextCoreFontAsset>();

#if TextMeshPro
        [SerializeField] public List<TMP_FontAsset> TmpFonts = new List<TMP_FontAsset>();
#endif

#if UNITEXT
        [SerializeField] public List<UniTextFontStack> UniTextFontStacks = new List<UniTextFontStack>();
#endif

        [SerializeField] string uniTextFontsPath = Path.Combine("Assets", "Fonts", "UniText");
        public string UniTextFontsPath { get => uniTextFontsPath; set => uniTextFontsPath = value; }

        public void RemoveDubAndMissingFonts()
        {
            monoBeh.FontLoader.TtfFonts = monoBeh.FontLoader.TtfFonts.Where(x => x != null).ToList();
            monoBeh.FontLoader.TtfFonts = monoBeh.FontLoader.TtfFonts.Distinct().ToList();
            monoBeh.FontLoader.UitkFontAssets = monoBeh.FontLoader.UitkFontAssets.Where(x => x != null).ToList();
            monoBeh.FontLoader.UitkFontAssets = monoBeh.FontLoader.UitkFontAssets.Distinct().ToList();
#if TextMeshPro
            monoBeh.FontLoader.TmpFonts = monoBeh.FontLoader.TmpFonts.Where(x => x != null).ToList();
            monoBeh.FontLoader.TmpFonts = monoBeh.FontLoader.TmpFonts.Distinct().ToList();
#endif
#if UNITEXT
            monoBeh.FontLoader.UniTextFontStacks = monoBeh.FontLoader.UniTextFontStacks.Where(x => x != null).ToList();
            monoBeh.FontLoader.UniTextFontStacks = monoBeh.FontLoader.UniTextFontStacks.Distinct().ToList();
#endif
        }

        public T GetFontFromArray<T>(Node fobject, List<T> fontArray) where T : UnityEngine.Object
        {
            fobject.Data.HasFontAsset = true;

            T fontItem = null;
            int reason = 0;


            foreach (T _fontItem in fontArray)
            {
                if (_fontItem == null)
                    continue;

                if (_fontItem.IsDefault())
                    continue;

                if (fobject.FontNameToString(true, true, null, true) == _fontItem.name.FormatFontName())
                {
                    reason = 1;
                    fontItem = _fontItem;
                    break;
                }
            }


            if (fontItem == null)
            {
                foreach (T _fontItem in fontArray)
                {
                    if (_fontItem == null)
                        continue;

                    if (_fontItem.IsDefault())
                        continue;

                    if (fobject.FontNameToString(true, false, null, true) == _fontItem.name.FormatFontName())
                    {
                        reason = 2;
                        fontItem = _fontItem;
                        break;
                    }
                }
            }


            if (fontItem == null)
            {
                fobject.Data.HasFontAsset = false;

                FontMetadata requested = fobject.GetFontMetadata();
                string requestedFamily = requested.Family.FormatFontName();

                T bestMatch = null;
                int bestScore = int.MinValue;

                foreach (T _fontItem in fontArray)
                {
                    if (_fontItem == null)
                        continue;

                    if (_fontItem.IsDefault())
                        continue;

                    FontMatchCandidate candidate = GetFontMatchCandidate(_fontItem);
                    if (!IsFamilyMatch(requestedFamily, candidate))
                        continue;

                    int score = ScoreFontMatch(candidate, requested);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = _fontItem;
                    }
                }

                if (bestMatch != null)
                {
                    reason = 6;
                    fontItem = bestMatch;
                }
            }


            if (fontItem == null)
            {
                fobject.Data.HasFontAsset = false;

                foreach (T _fontItem in fontArray)
                {
                    if (_fontItem == null)
                        continue;

                    if (_fontItem.IsDefault())
                        continue;

                    string fontFamily = fobject.FontNameToString(false, false, null, true);

                    bool contains = _fontItem.name.FormatFontName().Contains(fontFamily);

                    if (contains)
                    {
                        reason = 3;
                        fontItem = _fontItem;
                        break;
                    }
                }
            }


            if (fontItem == null)
            {
                fobject.Data.HasFontAsset = false;

                if (typeof(T) == typeof(Font))
                {
                    reason = 4;
#if UNITY_2022_1_OR_NEWER
                    fontItem = Resources.GetBuiltinResource<T>("LegacyRuntime.ttf");
#else
                    fontItem = Resources.GetBuiltinResource<T>("Arial.ttf");
#endif

                }
                else
                {
                    reason = 5;
                    fontItem = Resources.Load<T>("Fonts & Materials/LiberationSans SDF");
                }
            }

            FcuLogger.Debug($"FontLoader | {fobject.Data.NameHierarchy} | {fontItem} | reason: {reason}", FcuDebugSettingsFlags.LogFontLoader);

            return fontItem;
        }

        internal static bool IsFamilyMatch(string requestedFamily, FontMatchCandidate candidate)
        {
            if (requestedFamily.IsEmpty() || requestedFamily == "null")
                return false;

            return candidate.Family == requestedFamily ||
                   candidate.Family.Contains(requestedFamily) ||
                   requestedFamily.Contains(candidate.Family) ||
                   candidate.AssetName.Contains(requestedFamily);
        }

        internal static int ScoreFontMatch(FontMatchCandidate candidate, FontMetadata requested)
        {
            string reqFamily = requested.Family.FormatFontName();
            string reqWeightName = requested.Weight.FontWeightToString().FormatFontName();
            bool reqItalic = requested.FontStyle == FontStyle.Italic;

            int score = 0;

            if (candidate.Family == reqFamily)
                score += 1000;
            else if (candidate.Family.Contains(reqFamily) || reqFamily.Contains(candidate.Family))
                score += 700;
            else
                score += 400;

            if (candidate.IsItalic == reqItalic)
                score += 150;

            string weightSource = candidate.Style.IsEmpty() ? candidate.AssetName : candidate.Style;

            if (TryResolveWeightFromName(weightSource, out int candidateWeight))
            {
                score += 250 - Mathf.Min(250, Mathf.Abs(candidateWeight - requested.Weight));
            }
            else if (candidate.AssetName.Contains(reqWeightName))
            {
                score += 120;
            }
            else if (requested.Weight <= 499 && (candidate.Style.Contains("basic") || candidate.AssetName.Contains("basic")))
            {
                score += 80;
            }

            return score;
        }

        internal static bool TryResolveWeightFromName(string normalizedName, out int weight)
        {
            foreach (TextExtensions.FontWeightKeyword kw in TextExtensions.FontWeightKeywords)
            {
                if (normalizedName.Contains(kw.Keyword))
                {
                    weight = kw.Weight;
                    return true;
                }
            }

            if (normalizedName.IsEmpty() || normalizedName == "null")
            {
                weight = 400;
                return true;
            }

            weight = 0;
            return false;
        }

        internal static FontMatchCandidate GetFontMatchCandidate(UnityEngine.Object fontAsset)
        {
            string assetName = fontAsset.name.FormatFontName();
            string family = assetName;
            string style = string.Empty;

            PropertyInfo faceInfoProp = fontAsset.GetType().GetProperty("faceInfo", BindingFlags.Public | BindingFlags.Instance);
            if (faceInfoProp != null)
            {
                object faceInfo = faceInfoProp.GetValue(fontAsset);
                if (faceInfo != null)
                {
                    PropertyInfo familyProp = faceInfo.GetType().GetProperty("familyName", BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo styleProp = faceInfo.GetType().GetProperty("styleName", BindingFlags.Public | BindingFlags.Instance);

                    if (familyProp != null)
                    {
                        string value = familyProp.GetValue(faceInfo)?.ToString();
                        if (value.IsEmpty() == false)
                        {
                            family = value.FormatFontName();
                        }
                    }

                    if (styleProp != null)
                    {
                        string value = styleProp.GetValue(faceInfo)?.ToString();
                        if (value.IsEmpty() == false)
                        {
                            style = value.FormatFontName();
                        }
                    }
                }
            }

            bool isItalic = style.Contains("italic") || style.Contains("oblique") || assetName.Contains("italic");

            return new FontMatchCandidate
            {
                AssetName = assetName,
                Family = family,
                Style = style,
                IsItalic = isItalic
            };
        }

        internal struct FontMatchCandidate
        {
            public string AssetName;
            public string Family;
            public string Style;
            public bool IsItalic;
        }

#if UNITEXT
        public UniTextFontStack GetUniTextFontStack(FontMetadata requested)
        {
            UniTextFontStack bestFamilyMatch = null;
            int bestFamilyScore = int.MinValue;

            foreach (UniTextFontStack fontItem in monoBeh.FontLoader.UniTextFontStacks)
            {
                if (fontItem == null || fontItem.IsDefault())
                    continue;

                int familyScore = ScoreUniTextFontStack(fontItem, requested);
                if (familyScore > bestFamilyScore)
                {
                    bestFamilyScore = familyScore;
                    bestFamilyMatch = fontItem;
                }
            }

            if (bestFamilyMatch != null)
                return bestFamilyMatch;

            UniTextFontStack bestMatch = null;
            int bestScore = int.MinValue;
            string requestedFamily = requested.Family.FormatFontName();

            foreach (UniTextFontStack fontItem in monoBeh.FontLoader.UniTextFontStacks)
            {
                if (fontItem == null || fontItem.IsDefault())
                    continue;

                FontMatchCandidate candidate = GetFontMatchCandidate(fontItem);
                if (!IsFamilyMatch(requestedFamily, candidate))
                    continue;

                int score = ScoreFontMatch(candidate, requested);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = fontItem;
                }
            }

            if (bestMatch != null)
                return bestMatch;

            foreach (UniTextFontStack fontItem in monoBeh.FontLoader.UniTextFontStacks)
            {
                if (fontItem == null || fontItem.IsDefault())
                    continue;

                if (fontItem.name.FormatFontName().Contains(requestedFamily))
                    return fontItem;
            }

            return null;
        }

        public bool NeedsUniTextBoldModifier(FontMetadata requested)
        {
            if (!TryResolveUniTextFamily(requested, out _, out FontFamily family))
                return requested.Weight != 400;

            UniTextFont primary = family.primary;
            if (primary == null)
                return requested.Weight != 400;

            return requested.Weight != primary.FaceInfo.weightClass;
        }

        public bool NeedsUniTextItalicModifier(FontMetadata requested)
        {
            if (requested.FontStyle != FontStyle.Italic)
                return false;

            if (!TryResolveUniTextFamily(requested, out _, out FontFamily family))
                return true;

            UniTextFont primary = family.primary;
            if (primary == null)
                return true;

            return !primary.FaceInfo.isItalic;
        }

        public bool TryResolveUniTextFamily(FontMetadata requested, out UniTextFontStack stack, out FontFamily family)
        {
            stack = GetUniTextFontStack(requested);
            family = default;

            return stack != null
                && TryGetBestUniTextFamily(stack, requested, out family);
        }

        public bool TryGetUniTextExactFace(FontMetadata requested, out UniTextFontStack stack, out FontFamily family, out UniTextFont face)
        {
            face = null;

            if (!TryResolveUniTextFamily(requested, out stack, out family))
                return false;

            face = FindBestExactStaticFace(family, requested);
            return face != null;
        }

        private static UniTextFont FindBestExactStaticFace(FontFamily family, FontMetadata requested)
        {
            bool wantItalic = requested.FontStyle == FontStyle.Italic;
            UniTextFont bestFace = null;
            int bestScore = int.MinValue;

            foreach (UniTextFont face in EnumerateUniTextFamilyFonts(family))
            {
                if (face == null || face.IsVariable)
                    continue;

                if (face.FaceInfo.weightClass != requested.Weight)
                    continue;

                if (face.FaceInfo.isItalic != wantItalic)
                    continue;

                int score = ScoreExactStaticUniTextFace(face, family);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFace = face;
                }
            }

            return bestFace;
        }

        private static int ScoreExactStaticUniTextFace(UniTextFont face, FontFamily family)
        {
            int score = 0;
            if (face == family.primary)
                score += 10000;

            string name = face.name ?? string.Empty;
            score -= name.Count(c => c == '-' || c == '_' || c == ' ');
            score -= name.Length;
            return score;
        }

        private static IEnumerable<UniTextFont> EnumerateUniTextFamilyFonts(FontFamily family)
        {
            if (family.primary != null)
                yield return family.primary;

            if (family.faces.IsEmpty())
                yield break;

            for (int i = 0; i < family.faces.Length; i++)
            {
                UniTextFont face = family.faces[i];
                if (face != null && face != family.primary)
                    yield return face;
            }
        }

        private static int ScoreUniTextFontStack(UniTextFontStack stack, FontMetadata requested)
        {
            if (stack == null)
                return int.MinValue;

            int bestScore = int.MinValue;
            var visitedStacks = new HashSet<UniTextFontStack>();

            while (stack != null && visitedStacks.Add(stack))
            {
                if (!stack.families.IsEmpty())
                {
                    for (int i = 0; i < stack.families.Length; i++)
                    {
                        int familyScore = ScoreUniTextFamily(stack.families[i], requested);
                        if (familyScore > bestScore)
                            bestScore = familyScore;
                    }
                }

                stack = stack.fallbackStack;
            }

            return bestScore;
        }

        private static int ScoreUniTextFamily(FontFamily family, FontMetadata requested)
        {
            string requestedFamily = requested.Family.FormatFontName();
            string familyName = GetUniTextFamilyName(family).FormatFontName();

            if (requestedFamily.IsEmpty() || familyName.IsEmpty())
                return int.MinValue;

            if (familyName != requestedFamily &&
                !familyName.Contains(requestedFamily) &&
                !requestedFamily.Contains(familyName))
            {
                return int.MinValue;
            }

            int baseScore = familyName == requestedFamily ? 10000 : 7000;
            int bestFaceScore = ScoreUniTextFace(family.primary, requested);

            if (!family.faces.IsEmpty())
            {
                for (int i = 0; i < family.faces.Length; i++)
                {
                    int faceScore = ScoreUniTextFace(family.faces[i], requested);
                    if (faceScore > bestFaceScore)
                        bestFaceScore = faceScore;
                }
            }

            if (bestFaceScore == int.MinValue)
                return int.MinValue;

            return baseScore + bestFaceScore;
        }

        private static int ScoreUniTextFace(UniTextFont font, FontMetadata requested)
        {
            if (font == null)
                return int.MinValue;

            int score = 0;
            bool requestedItalic = requested.FontStyle == FontStyle.Italic;

            if (font.FaceInfo.isItalic == requestedItalic)
                score += 500;

            int weightDelta = Mathf.Abs(font.FaceInfo.weightClass - requested.Weight);
            score += 2000 - Mathf.Min(2000, weightDelta);

            if (font.FaceInfo.weightClass == requested.Weight)
                score += 1500;

            return score;
        }

        private static string GetUniTextFamilyName(FontFamily family)
        {
            if (family.name.IsEmpty() == false)
                return family.name;

            if (family.primary != null)
            {
                if (family.primary.FaceInfo.familyName.IsEmpty() == false)
                    return family.primary.FaceInfo.familyName;

                return family.primary.name;
            }

            return string.Empty;
        }

        private static bool TryGetBestUniTextFamily(UniTextFontStack stack, FontMetadata requested, out FontFamily bestFamily)
        {
            bestFamily = default;

            if (stack == null)
                return false;

            int bestScore = int.MinValue;
            var visitedStacks = new HashSet<UniTextFontStack>();

            while (stack != null && visitedStacks.Add(stack))
            {
                if (!stack.families.IsEmpty())
                {
                    for (int i = 0; i < stack.families.Length; i++)
                    {
                        FontFamily family = stack.families[i];
                        int score = ScoreUniTextFamily(family, requested);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestFamily = family;
                        }
                    }
                }

                stack = stack.fallbackStack;
            }

            return bestScore != int.MinValue;
        }

        private static UniTextFont GetRenderedUniTextFace(UniTextFont baseFont, UniTextFont selectedFace)
        {
            if (selectedFace == null || selectedFace == baseFont)
                return baseFont;

            return selectedFace;
        }

        private static UniTextFont FindUniTextFace(FontFamily family, int targetWeight, bool italic)
        {
            var matches = new List<UniTextFont>();

            if (family.primary != null && family.primary.FaceInfo.isItalic == italic)
                matches.Add(family.primary);

            if (!family.faces.IsEmpty())
            {
                for (int i = 0; i < family.faces.Length; i++)
                {
                    UniTextFont face = family.faces[i];
                    if (face != null && face.FaceInfo.isItalic == italic && !matches.Contains(face))
                        matches.Add(face);
                }
            }

            if (matches.Count == 0)
                return null;

            UniTextFont variableFont = matches.FirstOrDefault(x => x.IsVariable);
            if (variableFont != null)
                return variableFont;

            matches.Sort((a, b) => a.FaceInfo.weightClass.CompareTo(b.FaceInfo.weightClass));
            int[] weights = matches.Select(x => x.FaceInfo.weightClass).ToArray();

            int idx = Array.BinarySearch(weights, targetWeight);
            if (idx >= 0)
                return matches[idx];

            int ins = ~idx;

            if (targetWeight < 400)
                return ins > 0 ? matches[ins - 1] : matches[ins];

            if (targetWeight > 500)
                return ins < matches.Count ? matches[ins] : matches[ins - 1];

            if (targetWeight == 400)
            {
                int idx500 = Array.BinarySearch(weights, 500);
                if (idx500 >= 0)
                    return matches[idx500];

                return ins > 0 ? matches[ins - 1] : matches[ins];
            }

            if (targetWeight == 500)
            {
                int idx400 = Array.BinarySearch(weights, 400);
                if (idx400 >= 0)
                    return matches[idx400];

                return ins < matches.Count ? matches[ins] : matches[ins - 1];
            }

            if (ins == 0)
                return matches[0];

            if (ins >= matches.Count)
                return matches[matches.Count - 1];

            int distL = targetWeight - weights[ins - 1];
            int distH = weights[ins] - targetWeight;
            return distH <= distL ? matches[ins] : matches[ins - 1];
        }
#endif

        public async Task AddToUitkFontAssetsList(CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_start_adding_to_fonts_list.Localize());

            await AddToList(
                monoBeh.FontLoader.UitkFontAssetsPath,
                monoBeh.FontLoader.UitkFontAssets,
                addedCount =>
                {
                    Debug.Log(FcuLocKey.log_added_total.Localize(addedCount, monoBeh.FontLoader.UitkFontAssets.Count()));
                }, token);

            RemoveDubAndMissingFonts();
        }

        public async Task AddToTtfFontsList(CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_start_adding_to_fonts_list.Localize());

            await AddToList(
                monoBeh.FontLoader.TtfFontsPath,
                monoBeh.FontLoader.TtfFonts,
                addedCount =>
                {
                    Debug.Log(FcuLocKey.log_added_total.Localize(addedCount, monoBeh.FontLoader.TtfFonts.Count()));
                }, token);

            RemoveDubAndMissingFonts();
        }
        public async Task AddToTmpMeshFontsList(CancellationToken token)
        {
#if TextMeshPro
            Debug.Log(FcuLocKey.log_start_adding_to_fonts_list.Localize());

            await AddToList(
                monoBeh.FontLoader.TmpFontsPath,
                monoBeh.FontLoader.TmpFonts,
                addedCount =>
                {
                    Debug.Log(FcuLocKey.log_added_total.Localize(addedCount, monoBeh.FontLoader.TmpFonts.Count()));
                }, token);

            RemoveDubAndMissingFonts();
#endif
            await Task.Yield();
        }

        public async Task AddToUniTextFontsList(CancellationToken token)
        {
#if UNITEXT
            Debug.Log(FcuLocKey.log_start_adding_to_fonts_list.Localize());

            await AddToList(
                monoBeh.FontLoader.UniTextFontsPath,
                monoBeh.FontLoader.UniTextFontStacks,
                addedCount =>
                {
                    Debug.Log(FcuLocKey.log_added_total.Localize(addedCount, monoBeh.FontLoader.UniTextFontStacks.Count()));
                }, token);

            RemoveDubAndMissingFonts();
#endif
            await Task.Yield();
        }

        public async Task AddToList<T>(string fontsPath, List<T> list, Action<int> addedCount, CancellationToken token) where T : UnityEngine.Object
        {
            List<T> loadedAssets = new List<T>();
            await LoadAssetFromFolder<T>(fontsPath, x => loadedAssets = x, token);

            int count = 0;

            T asset;

            for (int i = 0; i < loadedAssets.Count; i++)
            {
                asset = loadedAssets[i];

                if (list.Contains(asset) == false)
                {
                    count++;
                    list.Add(asset);
                }

                if (i % 25 == 0)
                {
                    await Task.Yield();
                }
            }

            addedCount.Invoke(count);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(monoBeh);
#endif
        }

        public async Task LoadAssetFromFolder<T>(string fontsPath, Action<List<T>> assets, CancellationToken token) where T : UnityEngine.Object
        {
            List<string> pathes = new List<string>();
            List<T> loadedAssets = new List<T>();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new string[] { fontsPath.ToRelativePath() });
            pathes = guids.Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x)).ToList();

            string path;

            for (int i = 0; i < pathes.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                path = pathes[i];

                T sourceFontFile = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                loadedAssets.Add(sourceFontFile);

                if (i % 25 == 0)
                {
                    await Task.Yield();
                }
            }
#endif

            assets.Invoke(loadedAssets);
            await Task.Yield();
        }
    }
}
#endif