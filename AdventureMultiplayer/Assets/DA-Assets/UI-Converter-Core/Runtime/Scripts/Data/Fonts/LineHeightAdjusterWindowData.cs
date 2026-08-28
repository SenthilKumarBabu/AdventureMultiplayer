#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.UCC
{
    public enum LineHeightAdjusterAction
    {
        None = 0,
        ContinueImport = 1,
        StopImport = 2
    }

    public struct LineHeightAdjusterWindowData
    {
        public string WindowTitle { get; set; }
        public string SummaryNoun { get; set; }
        public string[] HelpSteps { get; set; }
        public List<SelectableObject<LineHeightAdjustmentIssue>> Fonts { get; set; }
    }

    public struct LineHeightAdjusterWindowResult
    {
        public LineHeightAdjusterAction Action { get; set; }
    }

    public class LineHeightAdjustmentIssue
    {
        public Object FontAsset { get; set; }
        public string AssetPath { get; set; }
        public string Details { get; set; }
    }

#if TextMeshPro
    public sealed class TmpLineHeightAdjustmentIssue : LineHeightAdjustmentIssue
    {
        public new TMP_FontAsset FontAsset
        {
            get => base.FontAsset as TMP_FontAsset;
            set => base.FontAsset = value;
        }
    }
#endif

    public sealed class UitkLineHeightAdjustmentIssue : LineHeightAdjustmentIssue
    {
        public new UnityEngine.TextCore.Text.FontAsset FontAsset
        {
            get => base.FontAsset as UnityEngine.TextCore.Text.FontAsset;
            set => base.FontAsset = value;
        }
    }
}
#endif