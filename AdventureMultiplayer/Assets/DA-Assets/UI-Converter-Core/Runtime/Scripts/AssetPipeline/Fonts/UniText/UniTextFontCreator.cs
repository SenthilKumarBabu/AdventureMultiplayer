#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITEXT
using LightSide;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    public class UniTextFontCreator : FcuBase
    {
        public async Task CreateFonts(List<FontMetadata> figmaFonts, CancellationToken token)
        {
#if UNITEXT && UNITY_EDITOR
            UnityFonts unityUniTextFonts = monoBeh.FontDownloader.FindUnityFonts(figmaFonts, monoBeh.FontLoader.UniTextFontStacks);

            if (unityUniTextFonts.Missing.Count == 0)
                return;

            List<FontStruct> generated = new List<FontStruct>();
            List<FontError> notGenerated = new List<FontError>();

            foreach (FontStruct missingFont in unityUniTextFonts.Missing)
            {
                GenerateFont(missingFont, @return =>
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
            while (FcuLogger.WriteLogBeforeEqual(generated, unityUniTextFonts.Missing, FcuLocKey.log_generating_tmp_fonts, ref tempCount))
            {
                await Task.Delay(1000, token);
            }

            if (notGenerated.Count > 0)
            {
                monoBeh.FontDownloader.PrintFontNames(FcuLocKey.cant_generate_fonts, notGenerated);
            }

            UnityEditor.AssetDatabase.Refresh();
            RebuildFontStacksFromTtfFonts(monoBeh.FontLoader.TtfFonts);
            await monoBeh.FontLoader.AddToUniTextFontsList(token);
#endif
            await Task.Yield();
        }

        public async Task CreateFromTtfFolder(CancellationToken token)
        {
#if UNITEXT && UNITY_EDITOR
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

            string uniTextFontsPath = monoBeh.FontLoader.UniTextFontsPath;
            uniTextFontsPath.GetFullAssetPath().CreateFolderIfNotExists();

            int created = 0;
            int skipped = 0;

            foreach (Font ttfFont in ttfFonts)
            {
                if (ttfFont == null)
                    continue;

                bool createdNow = CreateOrGetUniTextFontAsset(ttfFont, ttfFont.name, out _);
                if (createdNow)
                    created++;
                else
                    skipped++;
            }

            RebuildFontStacksFromTtfFonts(ttfFonts);

            Debug.Log($"UniText: created {created} font(s), skipped {skipped} existing.");

            UnityEditor.AssetDatabase.Refresh();
            await monoBeh.FontLoader.AddToUniTextFontsList(token);
#endif
            await Task.Yield();
        }

#if UNITEXT && UNITY_EDITOR
        private void GenerateFont(FontStruct fs, Return<FontStruct> @return)
        {
            string baseFontName = monoBeh.FontDownloader.GetBaseFileName(fs);
            Font ttfFont = monoBeh.FontDownloader.FindClosestTtfFont(fs, baseFontName);

            if (ttfFont == null)
            {
                @return.Invoke(new DAResult<FontStruct>
                {
                    Error = new WebError(0, $"Can't find ttf font for UniText conversion"),
                    Success = false
                });

                return;
            }

            bool success = CreateOrGetUniTextFontAsset(ttfFont, baseFontName, out UniTextFont uniTextFont)
                || uniTextFont != null;

            @return.Invoke(new DAResult<FontStruct>
            {
                Object = fs,
                Success = success,
                Error = success ? default : new WebError(0, "Failed to create UniText font asset.")
            });
        }

        private bool CreateOrGetUniTextFontAsset(Font ttfFont, string baseName, out UniTextFont uniTextFont)
        {
            uniTextFont = null;

            try
            {
                string fontAssetPath = Path.Combine(monoBeh.FontLoader.UniTextFontsPath, $"{baseName}.asset").ToUnityPath();
                uniTextFont = UnityEditor.AssetDatabase.LoadAssetAtPath<UniTextFont>(fontAssetPath);

                if (uniTextFont != null)
                {
                    uniTextFont.sourceFont = ttfFont;
                    uniTextFont.SetDirtyExt();
                    return false;
                }

                string ttfAssetPath = UnityEditor.AssetDatabase.GetAssetPath(ttfFont);

                if (ttfAssetPath.IsEmpty())
                    return false;

                string fullPath = Path.GetFullPath(ttfAssetPath);

                if (!File.Exists(fullPath))
                    return false;

                byte[] fontBytes = File.ReadAllBytes(fullPath);

                uniTextFont = UniTextFont.CreateFontAsset(fontBytes);

                if (uniTextFont == null)
                {
                    Debug.LogError($"Failed to create UniTextFont from {baseName}");
                    return false;
                }

                uniTextFont.sourceFont = ttfFont;
                UnityEditor.AssetDatabase.CreateAsset(uniTextFont, fontAssetPath);

                uniTextFont.SetDirtyExt();
                UnityEditor.AssetDatabase.SaveAssets();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        private void RebuildFontStacksFromTtfFonts(List<Font> ttfFonts)
        {
            if (ttfFonts.IsEmpty())
                return;

            string uniTextFontsPath = monoBeh.FontLoader.UniTextFontsPath;
            uniTextFontsPath.GetFullAssetPath().CreateFolderIfNotExists();

            var families = new Dictionary<string, List<UniTextFont>>(StringComparer.OrdinalIgnoreCase);

            foreach (Font ttfFont in ttfFonts)
            {
                if (ttfFont == null)
                    continue;

                if (!CreateOrGetUniTextFontAsset(ttfFont, ttfFont.name, out UniTextFont uniTextFont) || uniTextFont == null)
                {
                    uniTextFont = LoadUniTextFontAsset(ttfFont.name);
                }

                if (uniTextFont == null)
                    continue;

                string familyName = GetFamilyName(uniTextFont, ttfFont.name);
                if (!families.TryGetValue(familyName, out List<UniTextFont> familyFonts))
                {
                    familyFonts = new List<UniTextFont>();
                    families[familyName] = familyFonts;
                }

                if (familyFonts.Any(x => x == uniTextFont) == false
                    && familyFonts.Any(x => x != null && x.FontDataHash == uniTextFont.FontDataHash) == false)
                {
                    familyFonts.Add(uniTextFont);
                }
            }

            CreateOrUpdateGeneratedFontStackAsset(families);
            CleanupLegacyGeneratedFontStacks(families.Keys);
        }

        private UniTextFont LoadUniTextFontAsset(string baseName)
        {
            string fontAssetPath = Path.Combine(monoBeh.FontLoader.UniTextFontsPath, $"{baseName}.asset").ToUnityPath();
            return UnityEditor.AssetDatabase.LoadAssetAtPath<UniTextFont>(fontAssetPath);
        }

        private void CreateOrUpdateGeneratedFontStackAsset(Dictionary<string, List<UniTextFont>> families)
        {
            if (families.IsEmpty())
                return;

            string folder = Path.Combine(monoBeh.FontLoader.UniTextFontsPath, "FCU Generated").ToUnityPath();
            folder.GetFullAssetPath().CreateFolderIfNotExists();
            string fontStackPath = Path.Combine(folder, "FCU_UniText_Generated.asset").ToUnityPath();
            UniTextFontStack fontStack = UnityEditor.AssetDatabase.LoadAssetAtPath<UniTextFontStack>(fontStackPath);

            if (fontStack == null)
            {
                fontStack = ScriptableObject.CreateInstance<UniTextFontStack>();
                UnityEditor.AssetDatabase.CreateAsset(fontStack, fontStackPath);
            }

            fontStack.name = "FCU_UniText_Generated";
            fontStack.families = families
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => CreateFontFamily(x.Key, x.Value))
                .Where(x => x.primary != null)
                .ToArray();
            fontStack.fallbackStack = null;
            fontStack.SetDirtyExt();
            UnityEditor.AssetDatabase.SaveAssetIfDirty(fontStack);
        }

        private static FontFamily CreateFontFamily(string familyName, List<UniTextFont> familyFonts)
        {
            UniTextFont primary = SelectPrimaryFont(familyFonts);
            UniTextFont[] faces = familyFonts
                .Where(x => x != null && x != primary)
                .OrderBy(x => x.FaceInfo.isItalic)
                .ThenBy(x => Mathf.Abs(x.FaceInfo.weightClass - 400))
                .ThenBy(x => x.name, StringComparer.Ordinal)
                .ToArray();

            return new FontFamily
            {
                name = familyName,
                primary = primary,
                faces = faces
            };
        }

        private void CleanupLegacyGeneratedFontStacks(IEnumerable<string> familyNames)
        {
            string rootFolder = monoBeh.FontLoader.UniTextFontsPath.ToUnityPath();
            string generatedAssetPath = Path.Combine(rootFolder, "FCU Generated", "FCU_UniText_Generated.asset").ToUnityPath();
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(UniTextFontStack)}", new[] { rootFolder.ToRelativePath() });
            var knownFamilies = new HashSet<string>(familyNames, StringComparer.OrdinalIgnoreCase);

            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid).ToUnityPath();
                if (assetPath == generatedAssetPath)
                    continue;

                if (!IsLegacyGeneratedFontStackAsset(assetPath, rootFolder, knownFamilies, out _))
                    continue;

                UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static bool IsLegacyGeneratedFontStackAsset(
            string assetPath,
            string rootFolder,
            HashSet<string> knownFamilies,
            out UniTextFontStack fontStack)
        {
            fontStack = null;

            string assetDirectory = Path.GetDirectoryName(assetPath)?.ToUnityPath();
            if (!string.Equals(assetDirectory, rootFolder, StringComparison.OrdinalIgnoreCase))
                return false;

            fontStack = UnityEditor.AssetDatabase.LoadAssetAtPath<UniTextFontStack>(assetPath);
            if (fontStack == null || fontStack.fallbackStack != null || fontStack.families == null || fontStack.families.Length != 1)
                return false;

            FontFamily family = fontStack.families[0];
            string familyName = family.name;

            if (familyName.IsEmpty() && family.primary != null)
                familyName = family.primary.FaceInfo.familyName;

            if (familyName.IsEmpty() || !knownFamilies.Contains(familyName))
                return false;

            string expectedFileName = $"{familyName} FontStack";
            string actualFileName = Path.GetFileNameWithoutExtension(assetPath);
            return string.Equals(actualFileName, expectedFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static UniTextFont SelectPrimaryFont(List<UniTextFont> familyFonts)
        {
            return familyFonts
                .Where(x => x != null)
                .OrderBy(x => x.FaceInfo.isItalic ? 1 : 0)
                .ThenBy(x => Mathf.Abs(x.FaceInfo.weightClass - 400))
                .ThenBy(x => x.name, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static string GetFamilyName(UniTextFont uniTextFont, string fallbackName)
        {
            string familyName = uniTextFont.FaceInfo.familyName;
            if (familyName.IsEmpty() == false)
                return familyName;

            return fallbackName;
        }
#endif
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