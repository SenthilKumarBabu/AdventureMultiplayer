using DA_Assets.UCC.Extensions;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.UCC
{
    public partial class ConverterEditorBase
    {
        internal LayoutUpdaterWindow LayoutUpdaterWindow => LayoutUpdaterWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(900, 600),
            false,
            title: FcuLocKey.layout_updater_title.Localize());

        internal RateLimitWindow RateLimitWindow => RateLimitWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(700, 370),
            false,
            title: FcuLocKey.rate_limit_window_title.Localize());

        internal LineHeightAdjusterWindow LineHeightAdjusterWindow => LineHeightAdjusterWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(900, 600),
            false,
            title: "LineHeightAdjuster");

        internal SpriteDuplicateFinderWindow SpriteDuplicateFinderWindow => SpriteDuplicateFinderWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(900, 600),
            false,
            title: FcuLocKey.layout_updater_button_sprite_duplicate_finder.Localize());

        internal FcuSettingsWindow SettingsWindow => FcuSettingsWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(800, 600),
            false,
            title: FcuLocKey.common_button_settings.Localize());

        private void ShowWindowDelayed(Action showAction)
        {
            void DelayedShow()
            {
                EditorApplication.delayCall -= DelayedShow;
                showAction();
            }

            EditorApplication.delayCall += DelayedShow;
        }

        private void ShowDifferenceChecker(LayoutUpdaterInput data, Action<LayoutUpdaterOutput> callback)
            => ShowWindowDelayed(() => { this.LayoutUpdaterWindow.SetData(data, callback); this.LayoutUpdaterWindow.Show(); });

        private void ShowRateLimitDialog(RateLimitWindowData data, Action<RateLimitWindowResult> callback)
            => ShowWindowDelayed(() => { this.RateLimitWindow.SetData(data, callback); this.RateLimitWindow.Show(); });

        private void ShowLineHeightAdjusterWindow(LineHeightAdjusterWindowData data, Action<LineHeightAdjusterWindowResult> callback)
            => ShowWindowDelayed(() =>
            {
                this.LineHeightAdjusterWindow.SetData(data, callback);
                this.LineHeightAdjusterWindow.titleContent = new GUIContent(data.WindowTitle ?? "LineHeightAdjuster");
                this.LineHeightAdjusterWindow.Show();
            });

        private void ShowSpriteDuplicateFinder(SpriteDuplicateFinderRequest request)
            => ShowWindowDelayed(() =>
            {
                if (request.IsResolved)
                    return;

                this.SpriteDuplicateFinderWindow.SetData(request);

                if (!request.IsResolved)
                    this.SpriteDuplicateFinderWindow.Show();
            });
    }
}