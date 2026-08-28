namespace DA_Assets.UCC
{
    internal class LineHeightAdjusterWindow : LineHeightAdjusterWindowBase<LineHeightAdjusterWindow>
    {
        protected override void ApplyAdjustment(LineHeightAdjustmentIssue issue)
        {
#if TextMeshPro
            if (issue is TmpLineHeightAdjustmentIssue tmpIssue && tmpIssue.FontAsset != null)
            {
                LineHeightAdjuster.ApplyAndSaveTmpLineHeight(
                    tmpIssue.FontAsset,
                    monoBeh.Settings.TextFontsSettings.LineHeightMode);
                return;
            }
#endif

            if (issue is UitkLineHeightAdjustmentIssue uitkIssue && uitkIssue.FontAsset != null)
            {
                LineHeightAdjuster.ApplyAndSaveUitkLineHeight(
                    uitkIssue.FontAsset,
                    monoBeh.Settings.TextFontsSettings.LineHeightMode);
            }
        }
    }
}