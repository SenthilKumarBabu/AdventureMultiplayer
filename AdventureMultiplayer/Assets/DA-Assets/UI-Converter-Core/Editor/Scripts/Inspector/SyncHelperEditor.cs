using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    [CustomEditor(typeof(SyncHelper)), CanEditMultipleObjects]
    internal class SyncHelperEditor : Editor
    {
        [SerializeField] DAInspectorUITK _uitk;
        private ConverterBase fcu;
        private SyncHelper syncHelper;
        private static bool _sortActiveFirst = false;

        private void OnEnable()
        {
            syncHelper = (SyncHelper)target;

            if (syncHelper.Data != null)
            {
                fcu = syncHelper.Data.ConverterBase;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = _uitk.CreateRoot(_uitk.ColorScheme.BG);

            var debugToggle = _uitk.Toggle(FcuLocKey.common_label_debug.Localize());
            debugToggle.value = syncHelper.Debug;

            var defaultInspectorContainer = BuildDefaultInspectorContainer();

            RegisterDebugToggle(debugToggle, defaultInspectorContainer);

            if (fcu == null)
            {
                root.Add(_uitk.HelpBox(new HelpBoxData
                {
                    Message = FcuLocKey.label_fcu_is_null.Localize(
                        nameof(ConverterBase),
                        FcuConfig.CreatePrefabs,
                        FcuConfig.SetFcuToSyncHelpers),
                    MessageType = MessageType.Warning
                }));
            }

            if (syncHelper.Data != null)
            {
                var outerHeader = BuildInfoCard(debugToggle, defaultInspectorContainer, out var outerArrow);

                var outerBody = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column,
                        paddingTop = DAI_UitkConstants.MarginPadding
                    }
                };

                BuildReasonsSection(outerBody);

                var outerFoldout = new AnimatedFoldout(
                    "SyncHelper_Main",
                    outerHeader,
                    outerBody,
                    startExpanded: true,
                    _uitk.FoldoutCurve,
                    _uitk.FoldoutDuration,
                    _uitk.ColorScheme.BG);

                outerFoldout.Toggled += expanded =>
                {
                    outerArrow.text = expanded ? "▼" : "▶";
                };

                root.Add(outerFoldout);
            }

            root.Add(_uitk.Space10());


            root.Add(_uitk.HelpBox(new HelpBoxData
            {
                Message = FcuLocKey.label_dont_remove_fcu_meta.Localize(),
                MessageType = MessageType.Info,
                FontSize = (int)DAI_UitkConstants.FontSizeTiny
            }));

            root.Add(_uitk.Space5());

            root.Add(defaultInspectorContainer);

            return root;
        }

        private VisualElement BuildInfoCard(Toggle debugToggle, VisualElement defaultInspectorContainer, out Label arrowLabel)
        {
            var card = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = _uitk.ColorScheme.GROUP
                }
            };

            UIHelpers.SetDefaultPadding(card);
            UIHelpers.SetRadius(card, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(card, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(card, _uitk.ColorScheme.OUTLINE);


            arrowLabel = new Label("▼")
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    color = _uitk.ColorScheme.TEXT_SECOND,
                    marginRight = 6,
                    flexShrink = 0
                }
            };

            if (!syncHelper.Data.NameHierarchy.IsEmpty())
            {
                var headerContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column
                    }
                };

                var hierarchyRow = new VisualElement
                {
                    name = "SyncHelperHierarchyRow",
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                hierarchyRow.Add(arrowLabel);

                var hierarchyLabel = new Label(syncHelper.Data.NameHierarchy)
                {
                    name = "SyncHelperHierarchyLabel",
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal,
                        fontSize = DAI_UitkConstants.FontSizeNormal,
                        color = _uitk.ColorScheme.TEXT,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        flexGrow = 1,
                        flexShrink = 1
                    }
                };


                if (!syncHelper.Data.ProjectId.IsEmpty() && !syncHelper.Data.Id.IsEmpty())
                    RegisterHierarchyLabelContextMenu(hierarchyLabel);

                hierarchyRow.Add(hierarchyLabel);
                headerContainer.Add(hierarchyRow);


                if (syncHelper.Data.Tags != null && syncHelper.Data.Tags.Count > 0)
                {
                    var tagsRow = new VisualElement
                    {
                        name = "SyncHelperTagsRow",
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            flexWrap = Wrap.Wrap,
                            marginTop = DAI_UitkConstants.SpacingXXS
                        }
                    };

                    foreach (FcuTag tag in syncHelper.Data.Tags)
                    {
                        var badge = new Label(tag.ToString())
                        {
                            style =
                            {
                                fontSize = DAI_UitkConstants.FontSizeTiny,
                                backgroundColor = _uitk.ColorScheme.BUTTON,
                                color = _uitk.ColorScheme.TEXT_SECOND,
                                paddingLeft = 6,
                                paddingRight = 6,
                                paddingTop = 2,
                                paddingBottom = 2,
                                marginRight = 4,
                                marginBottom = 4,
                                flexShrink = 0,
                                unityTextAlign = TextAnchor.MiddleCenter
                            }
                        };

                        UIHelpers.SetRadius(badge, 4f);
                        UIHelpers.SetBorderWidth(badge, DAI_UitkConstants.BorderWidth);
                        UIHelpers.SetBorderColor(badge, _uitk.ColorScheme.OUTLINE);
                        tagsRow.Add(badge);
                    }

                    headerContainer.Add(tagsRow);
                }

                card.Add(headerContainer);
            }

            debugToggle.style.marginTop = DAI_UitkConstants.MarginPadding;
            card.Add(debugToggle);
            card.Add(defaultInspectorContainer);

            return card;
        }

        private void BuildReasonsSection(VisualElement root)
        {
            var reasonCategories = CollectReasonCategories();

            if (reasonCategories.Count == 0)
                return;

            int totalReasons = reasonCategories.Sum(c => c.items.Length);
            var header = BuildReasonsHeader(totalReasons, out var arrowLabel, out var sortButton, out var sortIcon);


            var body = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingTop = DAI_UitkConstants.MarginPadding
                }
            };


            RebuildReasonsBody(body, reasonCategories);


            sortButton.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopImmediatePropagation();
                _sortActiveFirst = !_sortActiveFirst;
                sortIcon.style.color = _sortActiveFirst
                    ? _uitk.ColorScheme.ACCENT_SECOND
                    : _uitk.ColorScheme.TEXT_SECOND;
                RebuildReasonsBody(body, reasonCategories);
            }, TrickleDown.TrickleDown);


            var foldout = new AnimatedFoldout(
                "SyncHelper_Reasons",
                header,
                body,
                startExpanded: true,
                _uitk.FoldoutCurve,
                _uitk.FoldoutDuration,
                _uitk.ColorScheme.BG);


            foldout.Toggled += expanded =>
            {
                arrowLabel.text = expanded ? "▼" : "▶";
            };

            root.Add(foldout);
        }


        private List<(string title, (string key, string desc, bool isActive)[] items)> CollectReasonCategories()
        {
            var result = new List<(string title, (string key, string desc, bool isActive)[] items)>();

            if (syncHelper.Data.Reasons == null || syncHelper.Data.Reasons.Count == 0)
                return result;


            var tagReasons = syncHelper.Data.Reasons
                .Where(r => r.key != ReasonKey.None && r.relatedTag != FcuTag.None)
                .GroupBy(r => r.relatedTag.ToString())
                .OrderBy(g => g.Key);

            foreach (var group in tagReasons)
            {
                var items = group
                    .Select(r => (
                        key: r.key.ToString(),
                        desc: r.key.GetDescription(syncHelper.Data),
                        isActive: syncHelper.Data.Tags.Contains(r.relatedTag)))
                    .ToArray();
                result.Add((group.Key, items));
            }


            var pipelineReasons = syncHelper.Data.Reasons
                .Where(r => r.key != ReasonKey.None && r.relatedTag == FcuTag.None)
                .GroupBy(r => r.key.GetReasonGroupFromPrefix())
                .OrderBy(g => g.Key);

            foreach (var group in pipelineReasons)
            {
                var items = group
                    .Select(r => (
                        key: r.key.ToString(),
                        desc: r.key.GetDescription(syncHelper.Data),
                        isActive: true))
                    .ToArray();
                result.Add((group.Key, items));
            }

            return result;
        }


        private VisualElement BuildReasonsHeader(int totalReasons, out Label arrowLabel, out VisualElement sortButton, out Label sortIcon)
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = _uitk.ColorScheme.GROUP,
                    height = DAI_UitkConstants.ButtonHeight
                }
            };
            UIHelpers.SetDefaultPadding(header);
            UIHelpers.SetRadius(header, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(header, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(header, _uitk.ColorScheme.OUTLINE);

            arrowLabel = new Label("▼")
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    color = _uitk.ColorScheme.TEXT_SECOND,
                    marginRight = 6,
                    alignSelf = Align.Center,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            header.Add(arrowLabel);

            header.Add(new Label("Reasons")
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = _uitk.ColorScheme.TEXT,
                    alignSelf = Align.Center,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            });

            var countBadge = new Label(totalReasons.ToString())
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    backgroundColor = _uitk.ColorScheme.ACCENT_SECOND,
                    color = Color.white,
                    paddingLeft = 6, paddingRight = 6,
                    paddingTop = 3, paddingBottom = 3,
                    marginLeft = 8, minWidth = 18, minHeight = 18,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    alignSelf = Align.Center
                }
            };
            UIHelpers.SetRadius(countBadge, 9f);
            header.Add(countBadge);

            header.Add(new VisualElement { style = { flexGrow = 1 } });

            sortButton = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    paddingLeft = 16, paddingRight = 8,
                    paddingTop = 4, paddingBottom = 4,
                    alignSelf = Align.Stretch
                }
            };

            sortIcon = new Label("↕")
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    color = _sortActiveFirst ? _uitk.ColorScheme.ACCENT_SECOND : _uitk.ColorScheme.TEXT_SECOND,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            sortButton.Add(sortIcon);
            header.Add(sortButton);

            return header;
        }


        private void RebuildReasonsBody(
            VisualElement body,
            List<(string title, (string key, string desc, bool isActive)[] items)> categories)
        {
            body.Clear();
            var sorted = _sortActiveFirst
                ? categories.OrderByDescending(c => c.items.Any(i => i.isActive)).ToList()
                : categories;
            foreach (var (title, items) in sorted)
                body.Add(BuildCategoryContainer(title, items));
        }


        private VisualElement BuildCategoryContainer(string title, (string key, string desc, bool isActive)[] items)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = _uitk.ColorScheme.GROUP,
                    marginBottom = DAI_UitkConstants.SpacingXXS * 3
                }
            };
            UIHelpers.SetPadding(container, DAI_UitkConstants.MarginPadding);
            UIHelpers.SetRadius(container, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(container, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(container, _uitk.ColorScheme.OUTLINE);

            var catTitle = new Label(title)
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = _uitk.ColorScheme.TEXT,
                    marginBottom = DAI_UitkConstants.SpacingXXS,
                    marginLeft = 2
                }
            };
            container.Add(catTitle);

            foreach (var (key, desc, isActive) in items)
                container.Add(BuildReasonItemPanel(key, desc, isActive));

            return container;
        }


        private VisualElement BuildReasonItemPanel(string key, string desc, bool isActive)
        {
            var panel = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart,
                    backgroundColor = _uitk.ColorScheme.BUTTON,
                    marginBottom = DAI_UitkConstants.SpacingXXS,
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            UIHelpers.SetRadius(panel, 4f);
            UIHelpers.SetBorderWidth(panel, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(panel, _uitk.ColorScheme.OUTLINE);

            panel.Add(new Label(desc)
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    color = _uitk.ColorScheme.TEXT_SECOND,
                    whiteSpace = WhiteSpace.Normal,
                    flexShrink = 1
                }
            });

            panel.Add(new VisualElement { style = { flexGrow = 1 } });

            var keyBadge = new Label(key)
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTiny,
                    backgroundColor = isActive
                        ? new Color(_uitk.ColorScheme.ACCENT_SECOND.r, _uitk.ColorScheme.ACCENT_SECOND.g, _uitk.ColorScheme.ACCENT_SECOND.b, 0.25f)
                        : _uitk.ColorScheme.GROUP,
                    color = isActive ? Color.white : _uitk.ColorScheme.TEXT_SECOND,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 2,
                    paddingBottom = 2,
                    marginLeft = 8,
                    flexShrink = 0,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            UIHelpers.SetRadius(keyBadge, 4f);
            UIHelpers.SetBorderWidth(keyBadge, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(keyBadge, _uitk.ColorScheme.OUTLINE);
            panel.Add(keyBadge);

            return panel;
        }


        private VisualElement BuildDefaultInspectorContainer()
        {
            var container = new VisualElement();
            InspectorElement.FillDefaultInspector(container, serializedObject, this);
            container.style.display = syncHelper.Debug ? DisplayStyle.Flex : DisplayStyle.None;
            return container;
        }


        private void RegisterDebugToggle(Toggle debugToggle, VisualElement container)
        {
            debugToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(syncHelper, "Toggle Debug");
                syncHelper.Debug = evt.newValue;
                container.style.display = syncHelper.Debug ? DisplayStyle.Flex : DisplayStyle.None;
                EditorUtility.SetDirty(syncHelper);
            });
        }


        private void RegisterHierarchyLabelContextMenu(Label label)
        {
            label.tooltip = FcuLocKey.sync_helper_link_view_in_figma.Localize();
            label.RegisterCallback<ContextClickEvent>(evt =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy Name Hierarchy"), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = syncHelper.Data.NameHierarchy;
                    Debug.Log($"[ConverterBase] Copied to clipboard: {syncHelper.Data.NameHierarchy}");
                });
                menu.AddItem(new GUIContent("Open Figma URL"), false, () =>
                {
                    string figmaUrl = $"https://www.figma.com/design/{syncHelper.Data.ProjectId}?node-id={syncHelper.Data.Id.Replace(":", "-")}";
                    Application.OpenURL(figmaUrl);
                });
                menu.ShowAsContext();
            });
        }
    }
}
