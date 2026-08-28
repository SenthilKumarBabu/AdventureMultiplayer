#if UNITY_EDITOR

using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextCoreFontAsset = UnityEngine.TextCore.Text.FontAsset;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.UCC
{
    public static class LineHeightAdjusterWindowHelper
    {
        public static bool ShouldShowWindow(
            ConverterBase monoBeh,
            List<Node> fobjects)
        {
            if (!TryCreateWindowData(monoBeh, fobjects, out LineHeightAdjusterWindowData data))
            {
                return false;
            }

            return LineHeightAdjusterWindowHelperBase.CanShowWindow(
                monoBeh,
                data,
                monoBeh.EditorDelegateHolder.ShowLineHeightAdjusterWindow);
        }

        public static async Task<LineHeightAdjusterWindowResult> ShowWindow(
            ConverterBase monoBeh,
            List<Node> fobjects,
            CancellationToken token)
        {
            if (!TryCreateWindowData(monoBeh, fobjects, out LineHeightAdjusterWindowData data))
            {
                return new LineHeightAdjusterWindowResult { Action = LineHeightAdjusterAction.ContinueImport };
            }

            LineHeightAdjusterWindowResult result = await LineHeightAdjusterWindowHelperBase.ShowWindow(
                monoBeh,
                data,
                monoBeh.EditorDelegateHolder.ShowLineHeightAdjusterWindow,
                token);

            if (result.Action == LineHeightAdjusterAction.ContinueImport)
            {
                await RefreshFontList(monoBeh, token);
            }

            return result;
        }

        private static bool TryCreateWindowData(
            ConverterBase monoBeh,
            List<Node> fobjects,
            out LineHeightAdjusterWindowData data)
        {
#if TextMeshPro
            if (monoBeh.UsingTextMesh())
            {
                data = CreateTmpWindowData(monoBeh, fobjects);
                return true;
            }
#endif

            if (monoBeh.UsingUI_Toolkit_Text())
            {
                data = CreateUitkWindowData(monoBeh, fobjects);
                return true;
            }

            data = new LineHeightAdjusterWindowData();
            return false;
        }

#if TextMeshPro
        private static LineHeightAdjusterWindowData CreateTmpWindowData(
            ConverterBase monoBeh,
            List<Node> fobjects)
        {
            LineHeightMode mode = monoBeh.Settings.TextFontsSettings.LineHeightMode;

            return CreateWindowData(
                "TMP",
                CollectIssues(
                    monoBeh.FontDownloader.GetProjectTmpFonts(fobjects),
                    mode,
                    CreateTmpIssue));
        }
#endif

        private static LineHeightAdjusterWindowData CreateUitkWindowData(
            ConverterBase monoBeh,
            List<Node> fobjects)
        {
            LineHeightMode mode = monoBeh.Settings.TextFontsSettings.LineHeightMode;

            return CreateWindowData(
                "UITK",
                CollectIssues(
                    monoBeh.FontDownloader.GetProjectUitkFonts(fobjects),
                    mode,
                    CreateUitkIssue));
        }

        private static LineHeightAdjusterWindowData CreateWindowData(
            string backendLabel,
            List<SelectableObject<LineHeightAdjustmentIssue>> issues)
        {
            return new LineHeightAdjusterWindowData
            {
                WindowTitle = $"{backendLabel} LineHeightAdjuster",
                SummaryNoun = $"{backendLabel} font asset(s)",
                HelpSteps = new[]
                {
                    $"FCU found {backendLabel} font assets whose current Face Info line-height values do not match the selected line height mode.",
                    "Apply changes to rewrite exactly these fields: capLine, ascentLine, descentLine, and lineHeight.",
                    "FCU sets capLine and ascentLine to the resolved cap height, sets descentLine to baseline, and recalculates lineHeight.",
                    "baseline and all other Face Info fields stay unchanged.",
                    "Continue without changes if you want to keep the current font assets untouched."
                },
                Fonts = issues
            };
        }

        private static List<SelectableObject<LineHeightAdjustmentIssue>> CollectIssues<TFont>(
            IEnumerable<TFont> fonts,
            LineHeightMode mode,
            System.Func<TFont, LineHeightMode, SelectableObject<LineHeightAdjustmentIssue>> createIssue)
            where TFont : class
        {
            return fonts
                .Where(font => font != null)
                .Select(font => createIssue(font, mode))
                .Where(issue => issue != null)
                .ToList();
        }

#if TextMeshPro
        private static SelectableObject<LineHeightAdjustmentIssue> CreateTmpIssue(TMP_FontAsset fontAsset, LineHeightMode mode)
        {
            if (LineHeightAdjuster.IsTmpLineHeightAdjusted(fontAsset, mode, out string details))
            {
                return null;
            }

            return new SelectableObject<LineHeightAdjustmentIssue>
            {
                Selected = true,
                Object = new TmpLineHeightAdjustmentIssue
                {
                    FontAsset = fontAsset,
                    AssetPath = UnityEditor.AssetDatabase.GetAssetPath(fontAsset),
                    Details = details
                }
            };
        }
#endif

        private static SelectableObject<LineHeightAdjustmentIssue> CreateUitkIssue(TextCoreFontAsset fontAsset, LineHeightMode mode)
        {
            if (LineHeightAdjuster.IsUitkLineHeightAdjusted(fontAsset, mode, out string details))
            {
                return null;
            }

            return new SelectableObject<LineHeightAdjustmentIssue>
            {
                Selected = true,
                Object = new UitkLineHeightAdjustmentIssue
                {
                    FontAsset = fontAsset,
                    AssetPath = UnityEditor.AssetDatabase.GetAssetPath(fontAsset),
                    Details = details
                }
            };
        }

        private static async Task RefreshFontList(ConverterBase monoBeh, CancellationToken token)
        {
#if TextMeshPro
            if (monoBeh.UsingTextMesh())
            {
                await monoBeh.FontLoader.AddToTmpMeshFontsList(token);
                return;
            }
#endif

            if (monoBeh.UsingUI_Toolkit_Text())
            {
                await monoBeh.FontLoader.AddToUitkFontAssetsList(token);
            }
        }
    }
}
#endif