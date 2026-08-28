using DA_Assets.DAI;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    [Serializable]
    internal class FramesSection : MonoBehaviourLinkerEditor<ConverterEditorBase, ConverterBase>
    {
        protected int _visibleItemCount = 10;
        protected float _itemHeight = 35;

        private readonly Dictionary<string, InfinityScrollRectWindow<SelectableNode>> _scrolls =
            new Dictionary<string, InfinityScrollRectWindow<SelectableNode>>();

        private readonly Dictionary<string, Label> _pageHeaderLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Toggle> _pageToggles = new Dictionary<string, Toggle>();

        private Label _docHeaderLabel;
        private Toggle _docToggle;

        private Action _onStateChanged;
        private const string DocFoldoutKey = "fcu.frames.document";

        public void UpdateScrollContent()
        {
            var activePageIds = new HashSet<string>();

            foreach (var page in monoBeh.InspectorDrawer.SelectableDocument.Childs)
            {
                activePageIds.Add(page.Id);

                if (!_scrolls.TryGetValue(page.Id, out var scroll))
                {
                    scroll = new InfinityScrollRectWindow<SelectableNode>(_visibleItemCount, _itemHeight, scriptableObject.gui, uitk);
                    _scrolls[page.Id] = scroll;
                }

                scroll.SetData(page.Childs, DrawFrameIMGUI);
                scroll.Refresh();
            }

            var staleIds = _scrolls.Keys.Where(x => !activePageIds.Contains(x)).ToArray();
            foreach (var staleId in staleIds)
            {
                _scrolls.Remove(staleId);
            }
        }

        public VisualElement BuildFramesSectionUI()
        {
            _onStateChanged = RefreshUIState;

            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };

            var doc = monoBeh.InspectorDrawer.SelectableDocument;

            var docHeaderContent = GetDocHeaderContent();
            _docHeaderLabel = new Label(docHeaderContent.title);
            _docHeaderLabel.tooltip = docHeaderContent.tooltip;
            _docToggle = new Toggle();

            _docToggle.RegisterValueChangedCallback(e =>
            {
                SetAllChildrenSelected(doc, e.newValue);
                _onStateChanged?.Invoke();
            });

            StopToggleBubbling(_docToggle);

            var body = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };

            foreach (var page in doc.Childs)
            {
                body.Add(BuildPageExpander(page));
            }

            var header = MakeHeader(_docHeaderLabel, _docToggle, out var arrowDoc);
            var fold = new AnimatedFoldout(DocFoldoutKey, header, body, false, uitk.FoldoutCurve, uitk.FoldoutDuration, uitk.ColorScheme.CLEAR);

            fold.Toggled += exp =>
            {
                arrowDoc.text = exp ? "▾" : "▸";
            };
            arrowDoc.text = fold.Expanded ? "▾" : "▸";

            var container = NarrowContainer(0);
            container.Add(fold);

            root.Add(container);
            RefreshUIState();
            return root;
        }

        private VisualElement BuildPageExpander(SelectableNode page)
        {
            var pageBody = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };

            var lbl = new Label(PageTitle(page));
            _pageHeaderLabels[page.Id] = lbl;

            var tgl = new Toggle();
            _pageToggles[page.Id] = tgl;

            tgl.RegisterValueChangedCallback(e =>
            {
                SetAllChildrenSelected(page, e.newValue);
                _onStateChanged?.Invoke();
            });

            StopToggleBubbling(tgl);

            var header = MakeHeader(lbl, tgl, out var arrow);
            arrow.text = "▸";

            var list = BuildFramesIMGUIForPage(page);
            pageBody.Add(list);

            var fold = new AnimatedFoldout(page.Id, header, pageBody, false, uitk.FoldoutCurve, uitk.FoldoutDuration, uitk.ColorScheme.CLEAR);
            fold.Toggled += exp =>
            {
                arrow.text = exp ? "▾" : "▸";
            };
            arrow.text = fold.Expanded ? "▾" : "▸";

            var container = NarrowContainer(1);
            container.Add(fold);
            return container;
        }

        private VisualElement BuildFramesIMGUIForPage(SelectableNode page)
        {
            if (!_scrolls.TryGetValue(page.Id, out var scroll))
            {
                scroll = new InfinityScrollRectWindow<SelectableNode>(_visibleItemCount, _itemHeight, scriptableObject.gui, uitk);
                _scrolls[page.Id] = scroll;
            }

            scroll.SetData(page.Childs, DrawFrameIMGUI);
            scroll.OnGUIChanged = _onStateChanged;
            scroll.style.width = Length.Percent(90);
            scroll.style.alignSelf = Align.Center;
            scroll.style.backgroundColor = uitk.ColorScheme.CLEAR;

            return scroll;
        }

        private void DrawFrameIMGUI(SelectableNode item)
        {
            item.Selected = scriptableObject.gui.CheckBox
            (
                new GUIContent(item.Name),
                item.Selected,
                rightSide: false,
                onValueChange: () =>
                {
                    monoBeh.InspectorDrawer.FillSelectableFramesArray(monoBeh.CurrentProject.FigmaProject.Document);
                    _onStateChanged?.Invoke();
                }
            );
        }

        private void RefreshUIState()
        {
            if (_docHeaderLabel != null)
            {
                var docHeaderContent = GetDocHeaderContent();
                _docHeaderLabel.text = docHeaderContent.title;
                _docHeaderLabel.tooltip = docHeaderContent.tooltip;
            }

            if (_docToggle != null)
            {
                GetCounts(monoBeh.InspectorDrawer.SelectableDocument, out var sel, out var all);
                SetTriState(_docToggle, sel, all);
            }

            var doc = monoBeh.InspectorDrawer.SelectableDocument;

            foreach (var page in doc.Childs)
            {
                if (_pageHeaderLabels.TryGetValue(page.Id, out var lbl))
                {
                    lbl.text = PageTitle(page);
                }

                if (_pageToggles.TryGetValue(page.Id, out var tgl))
                {
                    GetCounts(page, out var sel, out var all);
                    SetTriState(tgl, sel, all);
                }
            }
        }

        private void GetCounts(SelectableNode node, out int selected, out int all)
        {
            selected = 0;
            all = 0;    

            if (node == null)
            {
                return;
            }

            var leafs = node.Childs
                .Where(x => x != null)
                .SelectRecursive(x => x.Childs)
                .Where(x => x.Childs.IsEmpty());

            all = leafs.Count();
            selected = leafs.Count(x => x.Selected);
        }

        private void SetTriState(Toggle t, int selected, int all)
        {
            bool allOn = all > 0 && selected == all;
            bool noneOn = selected == 0;

#if UNITY_2020_1_OR_NEWER
            t.showMixedValue = !allOn && !noneOn;
            t.SetValueWithoutNotify(allOn);
#else
            t.SetValueWithoutNotify(allOn);
#endif
        }

        private void SetAllChildrenSelected(SelectableNode item, bool selected)
        {
            item.Selected = selected;

            foreach (var c in item.Childs)
            {
                SetAllChildrenSelected(c, selected);
            }
        }

        private (string title, string tooltip) GetDocHeaderContent()
        {
            GetCounts(monoBeh.InspectorDrawer.SelectableDocument, out var sel, out var all);
            return (
                FcuLocKey.label_frames_to_import.Localize(sel, all),
                FcuLocKey.tooltip_frames_to_import.Localize(sel, all));
        }

        private string PageTitle(SelectableNode page)
        {
            GetCounts(page, out var sel, out var all);
            return $"{page.Name} ({sel}/{all})";
        }

        private void StopToggleBubbling(Toggle t)
        {
#if UNITY_2020_1_OR_NEWER
            t.RegisterCallback<ClickEvent>(e => { e.StopImmediatePropagation(); });
#else
            t.RegisterCallback<MouseUpEvent>(e => { e.StopImmediatePropagation(); });
#endif

            t.RegisterCallback<MouseDownEvent>(e => { e.StopImmediatePropagation(); });
        }

        private VisualElement MakeHeader(Label titleLabel, Toggle rightToggle, out Label arrow)
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    marginBottom = DAI_UitkConstants.RowMarginBottom,
                    height = 32,
                    paddingLeft = 8,
                    paddingRight = 8,
                    width = Length.Percent(100)
                }
            };

            UIHelpers.SetBorderColor(header, scriptableObject.uitk.ColorScheme.OUTLINE);
            UIHelpers.SetBorderWidth(header, DAI_UitkConstants.BorderWidth);

            arrow = new Label("▾")
            {
                style =
                {
                    fontSize = 14,
                    width = 14
                }
            };

            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var spacer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };

            header.Add(arrow);
            header.Add(scriptableObject.uitk.Space5());
            header.Add(titleLabel);
            header.Add(spacer);

            if (rightToggle != null)
            {
                header.Add(rightToggle);
            }

            return header;
        }

        private VisualElement NarrowContainer(int depth)
        {
            float p = WidthPercentForDepth(depth);
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignSelf = Align.Center,
                    width = new Length(p, LengthUnit.Percent),
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };
        }

        private float WidthPercentForDepth(int depth)
        {
            float v = 100f - depth * 4f;
            if (v < 70f)
            {
                v = 70f;
            }

            if (v > 100f)
            {
                v = 100f;
            }

            return v;
        }
    }
}