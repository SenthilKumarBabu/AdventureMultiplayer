using DA_Assets.DAI;
using UnityEditor;
using DA_Assets.FCU;

namespace DA_Assets.UCC
{
    public partial class ConverterEditorBase
    {
        protected virtual void OnDisable()
        {
            monoBeh.Config.Localizator.OnLanguageChanged -= RebuildUI;
            ConverterBase.OnResetPerformed -= OnFcuReset;
            monoBeh.InspectorDrawer.OnFramesChanged -= RefreshFrames;
            monoBeh.InspectorDrawer.OnScrollContentUpdated -= this.FrameList.UpdateScrollContent;
        }

        protected virtual void OnEnable()
        {
            monoBeh = (ConverterBase)target;

            monoBeh.Config.Localizator.OnLanguageChanged += RebuildUI;
            ConverterBase.OnResetPerformed += OnFcuReset;
            monoBeh.InspectorDrawer.OnFramesChanged += RefreshFrames;
            monoBeh.InspectorDrawer.OnScrollContentUpdated += this.FrameList.UpdateScrollContent;

            monoBeh.EditorDelegateHolder.SetSpriteRects = SpriteEditorUtility.SetSpriteRects;
            monoBeh.EditorDelegateHolder.ShowDifferenceChecker = ShowDifferenceChecker;
            monoBeh.EditorDelegateHolder.ShowRateLimitWindow = ShowRateLimitDialog;
            monoBeh.EditorDelegateHolder.ShowLineHeightAdjusterWindow = ShowLineHeightAdjusterWindow;
            monoBeh.EditorDelegateHolder.ShowSpriteDuplicateFinder = ShowSpriteDuplicateFinder;
            monoBeh.EditorDelegateHolder.SetGameViewSize = GameViewUtils.SetGameViewSize;
            monoBeh.EditorDelegateHolder.StartProgress = (target, category, totalItems, indeterminate) =>
                EditorProgressBarManager.StartProgress(target, category, totalItems, indeterminate);
            monoBeh.EditorDelegateHolder.UpdateProgress = (target, category, itemsDone) =>
                EditorProgressBarManager.UpdateProgress(target, category, itemsDone);
            monoBeh.EditorDelegateHolder.CompleteProgress = (target, category) =>
                EditorProgressBarManager.CompleteProgress(target, category);
            monoBeh.EditorDelegateHolder.StopAllProgress = target =>
                EditorProgressBarManager.StopAllProgress(target);

            if (monoBeh.AuthProvider != null)
                _ = monoBeh.AuthProvider.TryRestoreSession();
        }

        private void OnFcuReset(ConverterBase resetInstance)
        {
            if (resetInstance == monoBeh)
            {
                ForceRebuild();
            }
        }
    }
}