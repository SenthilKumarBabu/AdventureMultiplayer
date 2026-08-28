using DA_Assets.DAI;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal abstract class LineHeightAdjusterWindowBase<TWindow>
        : LinkedEditorWindow<TWindow, ConverterEditorBase, ConverterBase>
        where TWindow : LineHeightAdjusterWindowBase<TWindow>
    {
        private LineHeightAdjusterWindowData _data;
        private Action<LineHeightAdjusterWindowResult> _callback;
        private bool _hasData;
        private bool _resultSent;

        private Label _summaryLabel;
        private Toggle _selectAllToggle;
        private VisualElement _helpPanel;

        protected abstract void ApplyAdjustment(LineHeightAdjustmentIssue issue);

        public void SetData(LineHeightAdjusterWindowData data, Action<LineHeightAdjusterWindowResult> callback)
        {
            _data = data;
            _callback = callback;
            _hasData = true;
            _resultSent = false;

            if (rootVisualElement.childCount > 0)
            {
                CreateGUI();
            }
        }

        public void CreateGUI()
        {
            if (_hasData == false)
            {
                return;
            }

            var root = rootVisualElement;
            root.Clear();

            UIHelpers.SetDefaultPadding(root);
            root.style.backgroundColor = new StyleColor(uitk.ColorScheme.BG);
            root.style.flexDirection = FlexDirection.Column;

            var topArea = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            root.Add(topArea);

            _helpPanel = uitk.BuildHelpPanel(
                FcuConfig.Instance.Localizator.Language,
                steps: _data.HelpSteps ?? Array.Empty<string>());
            topArea.Add(_helpPanel);

            var content = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            topArea.Add(content);

            content.Add(new Label(_data.WindowTitle ?? "LineHeightAdjuster")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = DAI_UitkConstants.MarginPadding
                }
            });

            var controlsRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = DAI_UitkConstants.MarginPadding
                }
            };

            _summaryLabel = new Label
            {
                style =
                {
                    flexGrow = 1,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            controlsRow.Add(_summaryLabel);

            _selectAllToggle = new Toggle("Select all")
            {
                value = AreAllItemsSelected()
            };
            _selectAllToggle.RegisterValueChangedCallback(evt =>
            {
                foreach (var item in _data.Fonts)
                {
                    item.Selected = evt.newValue;
                }

                CreateGUI();
            });
            controlsRow.Add(_selectAllToggle);

            content.Add(controlsRow);

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };

            foreach (var item in _data.Fonts)
            {
                scrollView.Add(BuildItem(item));
            }

            content.Add(scrollView);

            var footer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    flexShrink = 0
                }
            };

            footer.Add(uitk.HelpButton(() => uitk.ToggleHelpPanel(_helpPanel)));
            footer.Add(uitk.Space10());

            var spacer = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };
            footer.Add(spacer);

            var continueBtn = uitk.Button("Continue Without Changes", () => SendResult(LineHeightAdjusterAction.ContinueImport));
            continueBtn.color = uitk.ColorScheme.BUTTON;
            footer.Add(continueBtn);

            footer.Add(uitk.Space10());

            var applyBtn = uitk.Button("Apply Selected And Continue", ApplySelectedAndContinue);
            applyBtn.color = uitk.ColorScheme.ACCENT_SECOND;
            applyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            footer.Add(applyBtn);

            footer.Add(uitk.Space10());

            var stopBtn = uitk.Button("Stop Import", () => SendResult(LineHeightAdjusterAction.StopImport, stopImport: true));
            stopBtn.color = uitk.ColorScheme.RED;
            stopBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            footer.Add(stopBtn);

            root.Add(footer);

            UpdateSummary();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_resultSent || _callback == null)
            {
                return;
            }

            SendResult(LineHeightAdjusterAction.StopImport, stopImport: true);
        }

        private VisualElement BuildItem(SelectableObject<LineHeightAdjustmentIssue> item)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new StyleColor(uitk.ColorScheme.GROUP),
                    marginBottom = DAI_UitkConstants.MarginPadding,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    paddingBottom = DAI_UitkConstants.MarginPadding,
                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding
                }
            };
            UIHelpers.SetDefaultRadius(container);

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var toggle = new Toggle
            {
                value = item.Selected
            };
            toggle.RegisterValueChangedCallback(evt =>
            {
                item.Selected = evt.newValue;
                UpdateSummary();
            });
            header.Add(toggle);

            var title = new Label(item.Object.FontAsset != null ? item.Object.FontAsset.name : "Missing font")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexGrow = 1
                }
            };
            header.Add(title);

            container.Add(header);

            container.Add(new Label(item.Object.AssetPath ?? string.Empty)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(new Color(0.7f, 0.7f, 0.7f))
                }
            });

            container.Add(new Label(item.Object.Details ?? string.Empty)
            {
                style =
                {
                    marginTop = DAI_UitkConstants.SpacingXS,
                    whiteSpace = WhiteSpace.Normal
                }
            });

            return container;
        }

        private void ApplySelectedAndContinue()
        {
            int adjusted = 0;
            string summaryNoun = _data.SummaryNoun ?? "font asset(s)";

            foreach (var item in _data.Fonts.Where(x => x.Selected))
            {
                if (item.Object?.FontAsset == null)
                {
                    continue;
                }

                ApplyAdjustment(item.Object);
                adjusted++;
            }

            Debug.Log($"[FCU] Adjusted line-height fields for {adjusted} {summaryNoun}.");
            SendResult(LineHeightAdjusterAction.ContinueImport);
        }

        private bool AreAllItemsSelected()
        {
            return _data.Fonts != null && _data.Fonts.Count > 0 && _data.Fonts.All(x => x.Selected);
        }

        private void UpdateSummary()
        {
            if (_summaryLabel == null || _data.Fonts == null)
            {
                return;
            }

            int selected = _data.Fonts.Count(x => x.Selected);
            string summaryNoun = _data.SummaryNoun ?? "font asset(s)";
            _summaryLabel.text = $"Selected {selected} of {_data.Fonts.Count} {summaryNoun} for line-height adjustment.";

            if (_selectAllToggle != null)
            {
                _selectAllToggle.SetValueWithoutNotify(AreAllItemsSelected());
            }
        }

        private void SendResult(LineHeightAdjusterAction action, bool stopImport = false)
        {
            if (_resultSent)
            {
                return;
            }

            _resultSent = true;

            if (stopImport)
            {
                monoBeh.AssetTools.StopAsset(ImportStatus.Stopped);
            }

            _callback?.Invoke(new LineHeightAdjusterWindowResult
            {
                Action = action
            });

            Close();
        }
    }
}