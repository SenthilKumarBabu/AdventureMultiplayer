using DA_Assets.DAI;
using DA_Assets.UCC.Extensions;
using DA_Assets.UpdateChecker.Models;
using UnityEditor;
using UnityEngine.UIElements;
using static DA_Assets.DAI.DAInspectorUITK;

namespace DA_Assets.UCC
{
    public partial class ConverterEditorBase
    {
        private void BuildContent()
        {
            AddDecorativeBackground(_root, includeTopBanner: true);

            var headerCard = Card();
            headerCard.style.backgroundColor = uitk.ColorScheme.CLEAR;
            headerCard.Add(Header.BuildHeaderUI());
            _root.Add(headerCard);
            _root.Add(uitk.Space10());

            var actionsCard = Card();
            actionsCard.Add(BuildUrlAndActionsRow());
            _root.Add(actionsCard);

            bool hasContent = !monoBeh.InspectorDrawer.SelectableDocument.IsProjectEmpty();
            var framesCard = BuildFramesCard(hasContent);
            _root.Add(uitk.Space5());
            _root.Add(framesCard);
            _root.Add(BuildBottomExtraInfo());
            _root.Add(uitk.Space5());
            _root.Add(BuildFooter());
        }

        private VisualElement BuildFooter()
        {
            var fcuInfo = new FooterAssetInfo
            {
                AssetType = assetType,
                ProductVersion = monoBeh.Config.ProductVersion
            };

            string fuitkVersion = FuitkConfigReflectionHelper.GetProductVersion();
            if (fuitkVersion != null)
            {
                return uitk.CreateFooterWithVersionInfo(
                    monoBeh.Config.Localizator.Language,
                    fcuInfo,
                    new FooterAssetInfo
                    {
                        AssetType = AssetType.UITK_CONV,
                        ProductVersion = fuitkVersion
                    });
            }

            return uitk.CreateFooterWithVersionInfo(monoBeh.Config.Localizator.Language, fcuInfo);
        }

        private void ToggleWindowModeAndRebuild()
        {
            monoBeh.Settings.MainSettings.WindowMode = !monoBeh.Settings.MainSettings.WindowMode;

            if (monoBeh.Settings.MainSettings.WindowMode)
                SettingsWindow.Show();
            else
                SettingsWindow.CreateTabs();

            ForceRebuild();
        }

        private void SetDisplay(VisualElement ve, bool visible)
        {
            ve.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement Card()
        {
            var ve = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };

            return ve;
        }

        public VisualElement DrawWindowedGUI()
        {
            var root = uitk.CreateRoot(uitk.ColorScheme.BG);
            AddDecorativeBackground(root, includeTopBanner: false);

            var progressBarsContainer = BuildProgressBarsContainer();
            root.Add(progressBarsContainer);

            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };

            var btnOpen = uitk.SquareIconButton(
                SettingsWindow.Show,
                gui.Resources.IconOpen,
                Localize(FcuLocKey.tooltip_open_fcu_window));

            var btnToggle = uitk.SquareIconButton(
                ToggleWindowModeAndRebuild,
                gui.Resources.IconExpandWindow,
                Localize(FcuLocKey.tooltip_change_window_mode));

            toolbar.Add(btnOpen);
            toolbar.Add(uitk.Space5());
            toolbar.Add(btnToggle);
            root.Add(toolbar);

            return root;
        }

        private VisualElement BuildProgressBarsContainer()
        {
            var container = new VisualElement { name = "ProgressBarsContainer" };
            container.style.marginTop = -15;
            container.style.marginLeft = -15;
            container.style.marginRight = -15;
            EditorProgressBarManager.RegisterContainer(monoBeh, container, uitk);
            return container;
        }
    }
}