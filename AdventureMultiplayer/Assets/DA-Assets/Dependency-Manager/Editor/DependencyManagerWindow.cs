using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DM
{
    public class DependencyManagerWindow : EditorWindow
    {
        private ScrollView scrollView;
        private Label summaryLabel;

        [MenuItem("Tools/D.A. Assets/Dependency Manager", priority = 1000)]
        public static void Open()
        {
            var window = GetWindow<DependencyManagerWindow>();
            window.titleContent = new GUIContent(DependencyManagerLocKey.label_dependency_manager_window.Localize());
            window.minSize = new Vector2(420f, 320f);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 6;
            root.style.paddingRight = 6;
            root.style.paddingTop = 6;
            root.style.paddingBottom = 6;

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween
                }
            };

            summaryLabel = new Label("Dependencies: -");
            summaryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(summaryLabel);

            var headerButtons = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };

            var refreshButton = new Button(RefreshList)
            {
                text = DependencyManagerLocKey.label_refresh.Localize()
            };
            refreshButton.style.marginRight = 4;

            var recheckButton = new Button(() =>
            {
                DependencyManager.CheckAllDependencies();
                RefreshList();
            })
            {
                text = DependencyManagerLocKey.label_auto_detect.Localize()
            };

            headerButtons.Add(refreshButton);
            headerButtons.Add(recheckButton);
            header.Add(headerButtons);
            root.Add(header);

            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1f;
            root.Add(scrollView);

            RefreshList();
        }

        private void RefreshList()
        {
            if (scrollView == null)
            {
                return;
            }

            scrollView.Clear();
            IReadOnlyList<DependencyItem> dependencies = DependencyManager
                .GetDependencyItems()
                .OrderBy(d => d.name)
                .ToList();

            summaryLabel.text = DependencyManagerLocKey.label_dependencies_count.Localize(dependencies.Count);

            foreach (DependencyItem dependency in dependencies)
            {
                AddDependencyRow(dependency);
            }
        }

        private void AddDependencyRow(DependencyItem dependency)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    marginBottom = 4,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0f, 0f, 0f, 0.25f)
                }
            };

            var topRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var toggle = new Toggle
            {
                text = string.IsNullOrWhiteSpace(dependency.name) ? dependency.ScriptingDefineSymbol : dependency.name,
                value = dependency.IsEnabled
            };
            toggle.style.flexGrow = 1f;
            toggle.style.marginRight = 6;

            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == dependency.IsEnabled)
                {
                    return;
                }

                if (evt.newValue)
                {
                    DependencyManager.ForceEnableDependency(dependency);
                }
                else
                {
                    DependencyManager.ForceDisableDependency(dependency);
                }

                RefreshList();
            });

            var manualState = new Label(dependency.DisabledManually
                ? DependencyManagerLocKey.label_disabled_manually.Localize()
                : DependencyManagerLocKey.label_auto_managed.Localize());
            manualState.style.fontSize = 11;
            manualState.style.unityFontStyleAndWeight = dependency.DisabledManually ? FontStyle.Italic : FontStyle.Normal;
            manualState.style.color = dependency.DisabledManually
                ? new Color(0.8f, 0.3f, 0.3f)
                : new Color(0.6f, 0.6f, 0.6f);

            topRow.Add(toggle);
            topRow.Add(manualState);
            container.Add(topRow);

            var symbolRow = new Label($"Define: {dependency.ScriptingDefineSymbol}");
            symbolRow.style.fontSize = 11;
            container.Add(symbolRow);

            var strategyRow = new Label($"Strategy: {dependency.Strategy}");
            strategyRow.style.fontSize = 11;
            container.Add(strategyRow);

            string foundPath = string.IsNullOrEmpty(dependency.LastFoundPath) ? DependencyManager.PathNotFound : dependency.LastFoundPath;
            var pathRow = new Label($"Path: {foundPath}");
            pathRow.style.fontSize = 11;
            container.Add(pathRow);

            scrollView.Add(container);
        }
    }
}
