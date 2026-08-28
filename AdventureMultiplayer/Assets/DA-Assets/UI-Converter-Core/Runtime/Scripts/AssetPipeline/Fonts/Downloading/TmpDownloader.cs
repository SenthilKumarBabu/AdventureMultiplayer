#if UNITY_EDITOR
using DA_Assets.UCC.Extensions;
using DA_Assets.DAI;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public class TmpDownloader : FcuBase
    {


        [SerializeField] DA_Assets.Tools.SerializedDictionary<FontSubset, FontAtlasSettings> atlasConfig
            = new DA_Assets.Tools.SerializedDictionary<FontSubset, FontAtlasSettings>
            {
#if TextMeshPro
                { FontSubset.Latin,             new FontAtlasSettings(1024, 1024, AtlasPopulationMode.Dynamic) },
                { FontSubset.Cyrillic,          new FontAtlasSettings(1024, 1024, AtlasPopulationMode.Dynamic) },
                { FontSubset.Devanagari,        new FontAtlasSettings(1024, 1024, AtlasPopulationMode.Dynamic) },
                { FontSubset.Korean,            new FontAtlasSettings(2048, 2048, AtlasPopulationMode.Dynamic) },
                { FontSubset.Japanese,          new FontAtlasSettings(4096, 4096, AtlasPopulationMode.Dynamic) },
                { FontSubset.ChineseSimplified, new FontAtlasSettings(4096, 4096, AtlasPopulationMode.Dynamic) },
#endif
            };
        [SerializeField] bool enableMultiAtlasSupport = true;

        [Space]

        [SerializeField] UnityFonts unityTmpFonts;

        public async Task CreateFonts(List<FontMetadata> figmaFonts, CancellationToken token)
        {
            if (!monoBeh.UsingTextMesh())
            {
                await Task.Yield();
                return;
            }

#if TextMeshPro
            unityTmpFonts = monoBeh.FontDownloader.FindUnityFonts(figmaFonts, monoBeh.FontLoader.TmpFonts);
#endif
            if (unityTmpFonts.Missing.Count == 0)
            {
                await SetFallbackFontAssets(figmaFonts);
                return;
            }

            List<FontStruct> generated = new List<FontStruct>();
            List<FontError> notGenerated = new List<FontError>();

            foreach (FontStruct missingFont in unityTmpFonts.Missing)
            {
                _ = GenerateFont(missingFont, token, @return =>
                {
                    generated.Add(@return.Object);

                    if (@return.Success == false)
                    {
                        notGenerated.Add(new FontError
                        {
                            FontStruct = missingFont,
                            Error = @return.Error
                        });
                    }
                });
            }

            int tempCount = -1;
            while (FcuLogger.WriteLogBeforeEqual(generated, unityTmpFonts.Missing, FcuLocKey.log_generating_tmp_fonts, ref tempCount))
            {
                await Task.Delay(1000, token);
            }

            if (notGenerated.Count > 0)
            {
                monoBeh.FontDownloader.PrintFontNames(FcuLocKey.cant_generate_fonts, notGenerated);
            }

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            await monoBeh.FontLoader.AddToTmpMeshFontsList(token);

            await SetFallbackFontAssets(figmaFonts);
        }

        private async Task SetFallbackFontAssets(List<FontMetadata> figmaFonts)
        {
#if TextMeshPro
            unityTmpFonts = monoBeh.FontDownloader.FindUnityFonts(figmaFonts, monoBeh.FontLoader.TmpFonts);

            if (unityTmpFonts.Existing.Count == 0)
                return;

            var byFamily = unityTmpFonts.Existing
                .Where(x => x.Font != null)
                .GroupBy(x => x.FontMetadata.Family.ToLower())
                .ToList();

            foreach (var familyGroup in byFamily)
            {

                FontStruct primary = familyGroup.FirstOrDefault(x => x.FontSubset == FontSubset.Latin);

                if (primary.IsDefault())
                    primary = familyGroup.First();

                TMP_FontAsset primaryAsset = primary.Font as TMP_FontAsset;

                if (primaryAsset == null)
                    continue;

                if (primaryAsset.fallbackFontAssetTable == null)
                    primaryAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();

                foreach (FontStruct other in familyGroup)
                {
                    if (other.FontSubset == primary.FontSubset)
                        continue;

                    TMP_FontAsset otherAsset = other.Font as TMP_FontAsset;

                    if (otherAsset == null)
                        continue;

                    if (!primaryAsset.fallbackFontAssetTable.Contains(otherAsset))
                        primaryAsset.fallbackFontAssetTable.Add(otherAsset);
                }

                primaryAsset.fallbackFontAssetTable = primaryAsset.fallbackFontAssetTable
                    .Where(x => x != null)
                    .Distinct()
                    .ToList();
            }
#endif
            await Task.Yield();
        }

        private async Task GenerateFont(FontStruct fs, CancellationToken token, Return<FontStruct> @return)
        {
#if UNITY_EDITOR
            string baseFontName = monoBeh.FontDownloader.GetBaseFileName(fs);

            string tmpPath = Path.Combine(monoBeh.FontLoader.TmpFontsPath, $"{baseFontName}.asset");

            Font ttfFont = monoBeh.FontDownloader.FindClosestTtfFont(fs, baseFontName);

            if (ttfFont == null)
            {
                @return.Invoke(new DAResult<FontStruct>
                {
                    Error = new WebError(0, $"Can't find ttf font"),
                    Success = false
                });

                return;
            }

            if (!FontSubsetUnicodeRanges.TryGet(fs.FontSubset, out uint[] unicodes))
            {
                @return.Invoke(new DAResult<FontStruct>
                {
                    Error = new WebError(0, $"No unicode range for subset {fs.FontSubset}"),
                    Success = false
                });

                return;
            }

#if TextMeshPro
            try
            {
                LineHeightMode lineHeightMode = monoBeh.Settings.TextFontsSettings.LineHeightMode;
                FontAtlasSettings settings = GetAtlasSettings(fs.FontSubset);

                TMP_FontAsset tmpFontAsset = TMP_FontAsset.CreateFontAsset(
                    ttfFont,
                    settings.SamplingPointSize,
                    settings.AtlasPadding,
                    settings.RenderMode,
                    settings.Resolution.x,
                    settings.Resolution.y,
                    settings.PopulationMode
#if UNITY_2020_1_OR_NEWER
                    , enableMultiAtlasSupport);
#else
                    );
#endif

                UnityEditor.AssetDatabase.CreateAsset(tmpFontAsset, tmpPath.ToRelativePath());

                tmpFontAsset.material.name = $"{baseFontName} Atlas Material";
                tmpFontAsset.atlasTexture.name = $"{baseFontName} Atlas";

                UnityEditor.AssetDatabase.AddObjectToAsset(tmpFontAsset.material, tmpFontAsset);
                UnityEditor.AssetDatabase.AddObjectToAsset(tmpFontAsset.atlasTexture, tmpFontAsset);

                tmpFontAsset.SetDirtyExt();

                UnityEditor.AssetDatabase.SaveAssets();




                if (settings.PopulationMode != AtlasPopulationMode.Dynamic)
                {
                    tmpFontAsset.TryAddCharacters(unicodes, out uint[] _);
                }

                LineHeightAdjuster.AdjustTmpLineHeight(tmpFontAsset, lineHeightMode);

                tmpFontAsset.SetDirtyExt();
                UnityEditor.AssetDatabase.SaveAssets();

                @return.Invoke(new DAResult<FontStruct>
                {
                    Object = fs,
                    Success = true
                });

            }
            catch (Exception)
            {
                @return.Invoke(new DAResult<FontStruct>
                {
                    Error = new WebError(0, "Can't create font asset."),
                    Success = false
                });

                return;
            }
#endif
#endif
            await Task.Yield();
        }

        public async Task CreateFromTtfFolder(CancellationToken token)
        {
            if (!monoBeh.UsingTextMesh())
            {
                await Task.Yield();
                return;
            }

#if TextMeshPro && UNITY_EDITOR
            List<Font> ttfFonts = monoBeh.FontLoader.TtfFonts;

            if (ttfFonts.IsEmpty())
            {
                await monoBeh.FontLoader.AddToTtfFontsList(token);
                ttfFonts = monoBeh.FontLoader.TtfFonts;
            }

            if (ttfFonts.IsEmpty())
            {
                Debug.LogWarning(FcuLocKey.log_no_ttf_fonts_to_convert.Localize());
                return;
            }

            int created = 0;

            string tmpFontsPath = monoBeh.FontLoader.TmpFontsPath;
            tmpFontsPath.GetFullAssetPath().CreateFolderIfNotExists();

            foreach (Font ttfFont in ttfFonts)
            {
                if (ttfFont == null)
                    continue;

                string baseName = ttfFont.name;
                string tmpPath = Path.Combine(tmpFontsPath, $"{baseName} SDF.asset").ToUnityPath();

                if (System.IO.File.Exists(tmpPath))
                    continue;

                try
                {
                    LineHeightMode lineHeightMode = monoBeh.Settings.TextFontsSettings.LineHeightMode;
                    FontSubset subset = ParseSubsetFromName(baseName);
                    FontAtlasSettings settings2 = GetAtlasSettings(subset);

                    TMP_FontAsset tmpFontAsset = TMP_FontAsset.CreateFontAsset(
                        ttfFont,
                        settings2.SamplingPointSize,
                        settings2.AtlasPadding,
                        settings2.RenderMode,
                        settings2.Resolution.x,
                        settings2.Resolution.y,
                        settings2.PopulationMode
#if UNITY_2020_1_OR_NEWER
                        , enableMultiAtlasSupport);
#else
                        );
#endif

                    UnityEditor.AssetDatabase.CreateAsset(tmpFontAsset, tmpPath.ToRelativePath());
                    UnityEditor.AssetDatabase.AddObjectToAsset(tmpFontAsset.material, tmpFontAsset);
                    UnityEditor.AssetDatabase.AddObjectToAsset(tmpFontAsset.atlasTexture, tmpFontAsset);

                    tmpFontAsset.SetDirtyExt();


                    if (settings2.PopulationMode != AtlasPopulationMode.Dynamic)
                    {
                        if (FontSubsetUnicodeRanges.TryGet(subset, out uint[] subsetUnicodes))
                        {
                            tmpFontAsset.TryAddCharacters(subsetUnicodes, out uint[] _);
                        }
                    }

                    LineHeightAdjuster.AdjustTmpLineHeight(tmpFontAsset, lineHeightMode);

                    created++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create TMP from {baseName}: {ex.Message}");
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"TMP: created {created} font(s).");

            await monoBeh.FontLoader.AddToTmpMeshFontsList(token);
#endif
            await Task.Yield();
        }

        public DA_Assets.Tools.SerializedDictionary<FontSubset, FontAtlasSettings> AtlasConfig => atlasConfig;
        public bool EnableMultiAtlasSupport { get => enableMultiAtlasSupport; set => enableMultiAtlasSupport = value; }

        public FontAtlasSettings GetAtlasSettings(FontSubset subset)
        {
            if (atlasConfig.TryGetValue(subset, out FontAtlasSettings s))
                return s;
#if TextMeshPro
            return new FontAtlasSettings(1024, 1024, AtlasPopulationMode.Dynamic);
#else
            return new FontAtlasSettings(1024, 1024);
#endif
        }

        private FontSubset ParseSubsetFromName(string fontName)
        {

            foreach (FontSubset subset in Enum.GetValues(typeof(FontSubset)))
            {
                if (subset == FontSubset.Latin)
                    continue;

                if (fontName.IndexOf(subset.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    return subset;
            }

            return FontSubset.Latin;
        }
    }
}
#endif