#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class GoogleFontsApi : FcuBase
    {
        [SerializeField] public FontSubset FontSubsets = FontSubset.Latin;

        public List<FontSubset> SelectedFontAssets
        {
            get
            {
                List<FontSubset> selectedSubsets = Enum.GetValues(FontSubsets.GetType())
                    .Cast<FontSubset>()
                    .Where(x => FontSubsets.HasFlag(x))
                    .ToList();

                return selectedSubsets;
            }
        }

        private Dictionary<FontSubset, List<FontItem>> googleFontsBySubset = new Dictionary<FontSubset, List<FontItem>>();

        public async Task GetGoogleFontsBySubset(CancellationToken token)
        {
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingGoogleFonts, 0, true);

            try
            {
                List<FontSubset> missingSubsets = new List<FontSubset>();

                foreach (FontSubset subset in Enum.GetValues(FontSubsets.GetType()))
                {
                    if (FontSubsets.HasFlag(subset) == false)
                        continue;

                    if (googleFontsBySubset.TryGetValue(subset, out var _) == false)
                    {
                        missingSubsets.Add(subset);
                    }
                }

                if (missingSubsets.Count == 0)
                {
                    return;
                }

                foreach (FontSubset missingSubset in missingSubsets)
                {
                    string missingSubsetName = missingSubset.ToApiName();

                    Debug.Log(FcuLocKey.loading_google_fonts.Localize(missingSubset.ToString()));

                    string gfontsUrl = "https://www.googleapis.com/webfonts/v1/webfonts?subset={0}&key={1}";
                    string url = string.Format(gfontsUrl, missingSubsetName, monoBeh.Config.GoogleFontsApiKey);

                    DARequest request = new DARequest
                    {
                        RequestType = RequestType.Get,
                        Query = url
                    };

                    DAResult<FontRoot> return0 = await monoBeh.RequestSender.SendRequest<FontRoot>(request, token);

                    if (@return0.Success)
                    {
                        googleFontsBySubset.Add(missingSubset, @return0.Object.Items);
                    }
                    else
                    {
                        Debug.LogError(FcuLocKey.log_google_fonts_subset_failed.Localize(missingSubset.ToString()));
                    }
                }
            }
            finally
            {
                monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingGoogleFonts);
            }
        }

        public string GetUrlByWeight(FontItem fontItem, int weight, FontStyle fontStyle)
        {
            if (fontItem.Files == null)
                return null;

            string key;

            if (fontStyle == FontStyle.Italic)
                key = weight == 400 ? "italic" : $"{weight}italic";
            else
                key = weight == 400 ? "regular" : weight.ToString();

            if (fontItem.Files.TryGetValue(key, out string url))
                return url;


            if (fontItem.Files.Count > 0)
            {
                Debug.LogWarning(FcuLocKey.log_google_fonts_weight_not_found.Localize(key, fontItem.Family));
                return fontItem.Files.Values.FirstOrDefault();
            }

            return null;
        }

        public FontItem GetFontItem(FontMetadata fontMetadata, FontSubset fontSubset)
        {
            try
            {
                if (!googleFontsBySubset.TryGetValue(fontSubset, out var googleFonts) || googleFonts == null)
                    return default;

                string subsetApiName = fontSubset.ToApiName();


                FontItem exact = SearchInList(googleFonts, fontMetadata.Family, subsetApiName, exactMatch: true);
                if (!exact.IsDefault()) return exact;



                FontItem prefix = SearchInList(googleFonts, fontMetadata.Family, subsetApiName, exactMatch: false);
                if (!prefix.IsDefault())
                {
                    Debug.Log($"[FCU] CJK prefix match: '{fontMetadata.Family}' → '{prefix.Family}' ({fontSubset})");
                    return prefix;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FCU] GetFontItem error for '{fontMetadata.Family}': {ex.Message}");
            }

            return default;
        }

        private FontItem SearchInList(List<FontItem> googleFonts, string family, string subsetApiName, bool exactMatch)
        {
            string normalizedFamily = family.ToLower();

            foreach (FontItem item in googleFonts)
            {
                string itemFamily = item.Family.ToLower();

                bool nameMatch = exactMatch
                    ? itemFamily == normalizedFamily
                    : itemFamily.StartsWith(normalizedFamily);

                if (!nameMatch)
                    continue;


                if (item.Subsets != null && item.Subsets.Count > 0)
                {
                    if (!item.Subsets.Contains(subsetApiName))
                        continue;
                }

                return item;
            }

            return default;
        }
    }
}
#endif