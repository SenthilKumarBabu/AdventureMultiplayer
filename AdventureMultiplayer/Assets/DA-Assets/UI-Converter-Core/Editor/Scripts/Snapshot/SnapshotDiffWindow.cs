using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DA_Assets.DAI;

namespace DA_Assets.UCC.Snapshot
{
    public class SnapshotDiffWindow : EditorWindow
    {
        [SerializeField] DAInspectorUITK uitk;

        private ComparisonReport _report;


        private HashSet<string> _expandedGOs = new HashSet<string>();
        private HashSet<string> _expandedComponents = new HashSet<string>();

        public static void ShowWindow()
        {
            GetWindow<SnapshotDiffWindow>("Snapshot Diff");
        }

        public static void ShowWithReport(ComparisonReport report)
        {
            var window = GetWindow<SnapshotDiffWindow>("Snapshot Diff");
            window._report = report;
            window._expandedGOs.Clear();
            window._expandedComponents.Clear();
            window.RebuildUI();
        }

        private void CreateGUI()
        {
            RebuildUI();
        }

        private void RebuildUI()
        {
            rootVisualElement.Clear();

            if (uitk == null)
            {
                rootVisualElement.Add(new Label("DAInspectorUITK is not assigned. Please assign it in the Inspector."));
                return;
            }

            if (_report.RootEntries == null || _report.RootEntries.Count == 0)
            {
                rootVisualElement.Add(uitk.HelpBox(new HelpBoxData
                {
                    Message = "No comparison data. Run a snapshot test first.",
                    MessageType = UnityEditor.MessageType.Info
                }));
                return;
            }

            var scheme = uitk.ColorScheme;


            var root = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = scheme.BG
                }
            };
            rootVisualElement.Add(root);


            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = DAI_UitkConstants.SpacingM,
                    paddingRight = DAI_UitkConstants.SpacingM,
                    paddingTop = DAI_UitkConstants.SpacingXS,
                    paddingBottom = DAI_UitkConstants.SpacingXS,
                    backgroundColor = scheme.GROUP,
                    borderBottomWidth = DAI_UitkConstants.BorderWidth,
                    borderBottomColor = scheme.OUTLINE,
                    flexShrink = 0
                }
            };

            var summaryLabel = new Label(
                $"Total components: {_report.TotalComponents}  |  Deviations: {_report.TotalDeviations}")
            {
                style =
                {
                    flexGrow = 1,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = scheme.TEXT,
                    fontSize = DAI_UitkConstants.FontSizeNormal
                }
            };
            toolbar.Add(summaryLabel);

            var expandBtn = uitk.Button("Expand All", () =>
            {
                ExpandAll(_report.RootEntries);
                RebuildUI();
            });
            toolbar.Add(expandBtn);

            toolbar.Add(uitk.Space5());

            var collapseBtn = uitk.Button("Collapse All", () =>
            {
                _expandedGOs.Clear();
                _expandedComponents.Clear();
                RebuildUI();
            });
            toolbar.Add(collapseBtn);

            root.Add(toolbar);


            var scrollView = uitk.ScrollView();
            scrollView.style.flexGrow = 1;

            foreach (var rootEntry in _report.RootEntries)
            {
                BuildGameObjectEntry(scrollView, rootEntry, 0);
            }

            root.Add(scrollView);
        }

        private void BuildGameObjectEntry(VisualElement parent, GameObjectEntry entry, int indent)
        {
            var scheme = uitk.ColorScheme;
            bool isExpanded = _expandedGOs.Contains(entry.RelativePath);
            bool hasChildren = (entry.Children != null && entry.Children.Count > 0) ||
                               (entry.Components != null && entry.Components.Count > 0);


            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = indent * DAI_UitkConstants.IndentStep,
                    paddingTop = DAI_UitkConstants.SpacingXXS,
                    paddingBottom = DAI_UitkConstants.SpacingXXS,
                    paddingRight = DAI_UitkConstants.SpacingXS
                }
            };


            row.RegisterCallback<MouseEnterEvent>(_ =>
                row.style.backgroundColor = (Color)scheme.HOVER_WHITE);
            row.RegisterCallback<MouseLeaveEvent>(_ =>
                row.style.backgroundColor = StyleKeyword.Null);


            if (hasChildren)
            {
                var arrow = new Label(isExpanded ? "▼" : "▶")
                {
                    style =
                    {
                        width = DAI_UitkConstants.IconSizeSmall,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = (Color)scheme.ARROW_GRAY,
                        fontSize = DAI_UitkConstants.FontSizeNormal
                    }
                };
                string path = entry.RelativePath;
                arrow.RegisterCallback<MouseDownEvent>(_ =>
                {
                    if (_expandedGOs.Contains(path))
                        _expandedGOs.Remove(path);
                    else
                        _expandedGOs.Add(path);
                    RebuildUI();
                });
                row.Add(arrow);
            }
            else
            {
                row.Add(new VisualElement { style = { width = DAI_UitkConstants.IconSizeSmall } });
            }


            if (entry.DeviationCount > 0)
            {
                row.Add(CreateBadge(entry.DeviationCount));
            }
            else
            {
                row.Add(new VisualElement { style = { width = DAI_UitkConstants.IconSizeSmall + 2 } });
            }


            string icon = GetStatusIcon(entry.Status, true);
            row.Add(new Label(icon) { style = { width = DAI_UitkConstants.IconSizeMedium, unityTextAlign = TextAnchor.MiddleCenter } });


            var nameLabel = new Label(entry.Name)
            {
                style =
                {
                    color = scheme.TEXT,
                    fontSize = DAI_UitkConstants.FontSizeNormal
                }
            };
            row.Add(nameLabel);

            parent.Add(row);


            if (isExpanded)
            {
                if (entry.Components != null)
                {
                    foreach (var comp in entry.Components)
                    {
                        BuildComponentEntry(parent, comp, indent + 1, entry.RelativePath, entry.FigmaJson);
                    }
                }

                if (entry.Children != null)
                {
                    foreach (var child in entry.Children)
                    {
                        BuildGameObjectEntry(parent, child, indent + 1);
                    }
                }
            }
        }

        private void BuildComponentEntry(VisualElement parent, ComponentEntry comp, int indent, string parentPath, string figmaJson = null)
        {
            var scheme = uitk.ColorScheme;
            string compKey = $"{parentPath}/{comp.FileName}";
            bool hasDiff = comp.Status != EntryStatus.Match;
            bool isExpanded = _expandedComponents.Contains(compKey);

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = indent * DAI_UitkConstants.IndentStep,
                    paddingTop = DAI_UitkConstants.BorderWidth,
                    paddingBottom = DAI_UitkConstants.BorderWidth,
                    paddingRight = DAI_UitkConstants.SpacingXS
                }
            };

            row.RegisterCallback<MouseEnterEvent>(_ =>
                row.style.backgroundColor = (Color)scheme.HOVER_WHITE);
            row.RegisterCallback<MouseLeaveEvent>(_ =>
                row.style.backgroundColor = StyleKeyword.Null);


            if (hasDiff)
            {
                var arrow = new Label(isExpanded ? "▼" : "▶")
                {
                    style =
                    {
                        width = DAI_UitkConstants.IconSizeSmall,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = (Color)scheme.ARROW_GRAY,
                        fontSize = DAI_UitkConstants.FontSizeNormal
                    }
                };
                arrow.RegisterCallback<MouseDownEvent>(_ =>
                {
                    if (_expandedComponents.Contains(compKey))
                        _expandedComponents.Remove(compKey);
                    else
                        _expandedComponents.Add(compKey);
                    RebuildUI();
                });
                row.Add(arrow);
            }
            else
            {
                row.Add(new VisualElement { style = { width = DAI_UitkConstants.IconSizeSmall } });
            }


            if (hasDiff)
            {
                string badgeText = comp.Status == EntryStatus.Diff ? comp.DiffLineCount.ToString() : "!";
                row.Add(CreateSmallBadge(badgeText, scheme.RED));
            }
            else
            {
                row.Add(new VisualElement { style = { width = DAI_UitkConstants.IconSizeSmall + 2 } });
            }


            string icon = GetStatusIcon(comp.Status, false);
            row.Add(new Label(icon) { style = { width = DAI_UitkConstants.IconSizeMedium, unityTextAlign = TextAnchor.MiddleCenter } });


            string statusSuffix = comp.Status == EntryStatus.Missing ? " [MISSING]"
                : comp.Status == EntryStatus.Extra ? " [EXTRA]"
                : "";
            var label = new Label($"{comp.FileName}{statusSuffix}")
            {
                style =
                {
                    color = scheme.TEXT_SECOND,
                    fontSize = DAI_UitkConstants.FontSizeNormal
                }
            };
            row.Add(label);

            parent.Add(row);


            if (isExpanded && hasDiff)
            {
                BuildDiffView(parent, comp, indent + 1, figmaJson);
            }
        }

        private void BuildDiffView(VisualElement parent, ComponentEntry comp, int indent, string figmaJson = null)
        {
            var scheme = uitk.ColorScheme;
            float indentPx = indent * DAI_UitkConstants.IndentStep;

            string[] baselineLines = SplitLines(comp.BaselineJson);
            string[] sceneLines = SplitLines(comp.SceneJson);
            string[] figmaLines = !string.IsNullOrEmpty(figmaJson) ? SplitLines(figmaJson) : null;
            bool hasFigma = figmaLines != null && figmaLines.Length > 0;

            int maxLines = Math.Max(baselineLines.Length, sceneLines.Length);
            if (hasFigma)
                maxLines = Math.Max(maxLines, figmaLines.Length);

            if (maxLines == 0) return;

            var diffContainer = new VisualElement
            {
                style =
                {
                    marginLeft = indentPx,
                    marginTop = DAI_UitkConstants.SpacingXXS,
                    marginBottom = DAI_UitkConstants.SpacingXS,
                    borderTopWidth = DAI_UitkConstants.BorderWidth,
                    borderBottomWidth = DAI_UitkConstants.BorderWidth,
                    borderLeftWidth = DAI_UitkConstants.BorderWidth,
                    borderRightWidth = DAI_UitkConstants.BorderWidth,
                    borderTopColor = scheme.OUTLINE,
                    borderBottomColor = scheme.OUTLINE,
                    borderLeftColor = scheme.OUTLINE,
                    borderRightColor = scheme.OUTLINE
                }
            };

            UIHelpers.SetDefaultRadius(diffContainer);


            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = scheme.GROUP,
                    borderBottomWidth = DAI_UitkConstants.BorderWidth,
                    borderBottomColor = scheme.OUTLINE
                }
            };


            if (hasFigma)
            {
                headerRow.Add(new Label("") { style = { width = DAI_UitkConstants.LineNumWidth, borderRightWidth = DAI_UitkConstants.BorderWidth, borderRightColor = scheme.OUTLINE } });
                headerRow.Add(new Label("FIGMA")
                {
                    style =
                    {
                        flexGrow = 1,
                        flexBasis = 0,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = scheme.TEXT,
                        fontSize = DAI_UitkConstants.FontSizeNormal,
                        paddingLeft = DAI_UitkConstants.SpacingS,
                        paddingTop = DAI_UitkConstants.CornerRadiusSmall,
                        paddingBottom = DAI_UitkConstants.CornerRadiusSmall,
                        borderRightWidth = DAI_UitkConstants.BorderWidth,
                        borderRightColor = scheme.OUTLINE
                    }
                });
            }


            headerRow.Add(new Label("") { style = { width = DAI_UitkConstants.LineNumWidth, borderRightWidth = DAI_UitkConstants.BorderWidth, borderRightColor = scheme.OUTLINE } });
            headerRow.Add(new Label("BASELINE")
            {
                style =
                {
                    flexGrow = 1,
                    flexBasis = 0,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = scheme.TEXT,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    paddingLeft = DAI_UitkConstants.SpacingS,
                    paddingTop = DAI_UitkConstants.CornerRadiusSmall,
                    paddingBottom = DAI_UitkConstants.CornerRadiusSmall,
                    borderRightWidth = DAI_UitkConstants.BorderWidth,
                    borderRightColor = scheme.OUTLINE
                }
            });


            headerRow.Add(new Label("") { style = { width = DAI_UitkConstants.LineNumWidth, borderRightWidth = DAI_UitkConstants.BorderWidth, borderRightColor = scheme.OUTLINE } });
            headerRow.Add(new Label("SCENE")
            {
                style =
                {
                    flexGrow = 1,
                    flexBasis = 0,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = scheme.TEXT,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    paddingLeft = DAI_UitkConstants.SpacingS,
                    paddingTop = DAI_UitkConstants.CornerRadiusSmall,
                    paddingBottom = DAI_UitkConstants.CornerRadiusSmall
                }
            });

            diffContainer.Add(headerRow);


            Color diffRemovedBg = uitk.ColorScheme.DIFF_REMOVED;
            Color diffAddedBg = uitk.ColorScheme.DIFF_ADDED;
            Color missingBg = uitk.ColorScheme.DIFF_MISSING;
            Color extraBg = uitk.ColorScheme.DIFF_EXTRA;

            for (int i = 0; i < maxLines; i++)
            {
                string lineA = i < baselineLines.Length ? baselineLines[i] : "";
                string lineB = i < sceneLines.Length ? sceneLines[i] : "";
                bool isDiff = !string.Equals(lineA, lineB, StringComparison.Ordinal);

                Color leftBg, rightBg;

                if (comp.Status == EntryStatus.Missing)
                {
                    leftBg = isDiff ? missingBg : scheme.BG;
                    rightBg = scheme.BG;
                }
                else if (comp.Status == EntryStatus.Extra)
                {
                    leftBg = scheme.BG;
                    rightBg = isDiff ? extraBg : scheme.BG;
                }
                else
                {
                    leftBg = isDiff ? diffRemovedBg : scheme.BG;
                    rightBg = isDiff ? diffAddedBg : scheme.BG;
                }

                var lineRow = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        minHeight = DAI_UitkConstants.ListViewItemHeight
                    }
                };


                if (hasFigma)
                {
                    string figmaLine = i < figmaLines.Length ? figmaLines[i] : "";

                    var figmaCell = new Label(figmaLine)
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexBasis = 0,
                            backgroundColor = scheme.BG,
                            color = scheme.TEXT_SECOND,
                            fontSize = DAI_UitkConstants.FontSizeNormal,
                            paddingLeft = DAI_UitkConstants.SpacingS,
                            paddingRight = DAI_UitkConstants.SpacingXS,
                            unityFontStyleAndWeight = FontStyle.Normal,
#if UNITY_6000_0_OR_NEWER
                            whiteSpace = WhiteSpace.Pre,
#else
                            whiteSpace = WhiteSpace.NoWrap,
#endif
                            overflow = Overflow.Hidden,
                            borderRightWidth = DAI_UitkConstants.BorderWidth,
                            borderRightColor = scheme.OUTLINE
                        }
                    };

                    lineRow.Add(CreateLineNum(i + 1, scheme.BG, scheme));
                    lineRow.Add(figmaCell);
                }


                var leftCell = new Label(lineA)
                {
                    style =
                    {
                        flexGrow = 1,
                        flexBasis = 0,
                        backgroundColor = leftBg,
                        color = scheme.TEXT,
                        fontSize = DAI_UitkConstants.FontSizeNormal,
                        paddingLeft = DAI_UitkConstants.SpacingS,
                        paddingRight = DAI_UitkConstants.SpacingXS,
                        unityFontStyleAndWeight = FontStyle.Normal,
#if UNITY_6000_0_OR_NEWER
                        whiteSpace = WhiteSpace.Pre,
#else
                        whiteSpace = WhiteSpace.NoWrap,
#endif
                        overflow = Overflow.Hidden,
                        borderRightWidth = DAI_UitkConstants.BorderWidth,
                        borderRightColor = scheme.OUTLINE
                    }
                };


                var rightCell = new Label(lineB)
                {
                    style =
                    {
                        flexGrow = 1,
                        flexBasis = 0,
                        backgroundColor = rightBg,
                        color = scheme.TEXT,
                        fontSize = DAI_UitkConstants.FontSizeNormal,
                        paddingLeft = DAI_UitkConstants.SpacingS,
                        paddingRight = DAI_UitkConstants.SpacingXS,
                        unityFontStyleAndWeight = FontStyle.Normal,
#if UNITY_6000_0_OR_NEWER
                        whiteSpace = WhiteSpace.Pre,
#else
                        whiteSpace = WhiteSpace.NoWrap,
#endif
                        overflow = Overflow.Hidden
                    }
                };

                lineRow.Add(CreateLineNum(i + 1, leftBg, scheme));
                lineRow.Add(leftCell);
                lineRow.Add(CreateLineNum(i + 1, rightBg, scheme));
                lineRow.Add(rightCell);
                diffContainer.Add(lineRow);
            }

            parent.Add(diffContainer);
        }

        private Label CreateLineNum(int num, Color bg, UitkColorScheme scheme)
        {
            return new Label(num.ToString())
            {
                style =
                {
                    width = DAI_UitkConstants.LineNumWidth,
                    backgroundColor = bg,
                    color = scheme.TEXT_SECOND,
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    unityTextAlign = TextAnchor.MiddleRight,
                    paddingRight = DAI_UitkConstants.SpacingXS,
                    flexShrink = 0,
                    borderRightWidth = DAI_UitkConstants.BorderWidth,
                    borderRightColor = scheme.OUTLINE
                }
            };
        }

        private VisualElement CreateBadge(int count)
        {
            var scheme = uitk.ColorScheme;
            Color color = count > 0 ? scheme.RED : scheme.GREEN;
            string text = count > 0 ? count.ToString() : "✓";

            var badge = new Label(text)
            {
                style =
                {
                    backgroundColor = color,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    width = DAI_UitkConstants.IconSizeSmall,
                    height = DAI_UitkConstants.IconSizeSmall,
                    flexShrink = 0
                }
            };

            UIHelpers.SetRadius(badge, 9);
            return badge;
        }

        private VisualElement CreateSmallBadge(string text, Color color)
        {
            var badge = new Label(text)
            {
                style =
                {
                    backgroundColor = color,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    width = DAI_UitkConstants.IconSizeSmall,
                    height = DAI_UitkConstants.IconSizeSmall,
                    flexShrink = 0
                }
            };

            UIHelpers.SetRadius(badge, DAI_UitkConstants.CornerRadius);
            return badge;
        }

        private string GetStatusIcon(EntryStatus status, bool isGameObject)
        {
            switch (status)
            {
                case EntryStatus.Match: return isGameObject ? "🎮" : "📦";
                case EntryStatus.Diff: return isGameObject ? "🎮" : "📦";
                case EntryStatus.Missing: return "⚠";
                case EntryStatus.Extra: return "➕";
                default: return "?";
            }
        }

        private string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        }

        private void ExpandAll(List<GameObjectEntry> entries)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                _expandedGOs.Add(entry.RelativePath);

                if (entry.Components != null)
                {
                    foreach (var comp in entry.Components)
                    {
                        if (comp.Status != EntryStatus.Match)
                            _expandedComponents.Add($"{entry.RelativePath}/{comp.FileName}");
                    }
                }

                ExpandAll(entry.Children);
            }
        }
    }
}