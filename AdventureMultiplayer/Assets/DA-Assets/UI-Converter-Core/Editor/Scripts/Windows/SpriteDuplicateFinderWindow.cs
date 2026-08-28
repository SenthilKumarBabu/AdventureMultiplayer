using DA_Assets.DAI;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal class SpriteDuplicateFinderWindow : LinkedEditorWindow<
        SpriteDuplicateFinderWindow,
        ConverterEditorBase,
        ConverterBase>
    {
        private ScrollView _results;
        private CustomButton _applyBtn;
        private CustomButton _cancelBtn;
        private CustomButton _deselectAllBtn;
        private Button _helpBtn;
        private VisualElement _helpPanel;
        private Label _statsLabel;

        private bool _inited;
        private int _dropHighlightIndex = -1;

        private const string DND_KEY = "SDF_DUB";

        internal int GroupCount => _groups.Count;

        private readonly List<List<SpriteUsageFinder.UsedSprite>> _groups = new List<List<SpriteUsageFinder.UsedSprite>>();

        private readonly Dictionary<int, VisualElement> _groupCards = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, VisualElement> _groupRows = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, Label> _groupHeaders = new Dictionary<int, Label>();

        private SpriteDuplicateFinderRequest _request;

        private readonly FcuLocKey[] _helpSteps =
        {
            FcuLocKey.sprite_duplicate_help_step_1,
            FcuLocKey.sprite_duplicate_help_step_2,
            FcuLocKey.sprite_duplicate_help_step_3,
            FcuLocKey.sprite_duplicate_help_step_4,
            FcuLocKey.sprite_duplicate_help_step_5,
            FcuLocKey.sprite_duplicate_help_step_6,
            FcuLocKey.sprite_duplicate_help_step_7,
            FcuLocKey.sprite_duplicate_help_step_8,
            FcuLocKey.sprite_duplicate_help_step_9,
        };

        protected override void OnTargetBound()
        {
            localizator = monoBeh.Config.Localizator;
        }

        private void Update()
        {
            if (_groups.IsEmpty())
            {
                Close();
            }
        }

        internal void SetData(SpriteDuplicateFinderRequest request)
        {
            if (_request != null)
            {
                _request.Resolved -= OnRequestResolved;
            }

            _request = request;
            _request.Resolved += OnRequestResolved;

            _groups.Clear();
            _groups.AddRange(request.Groups);

            _inited = true;

            if (_results != null)
                BuildAllGroupsUI();
        }

        protected override void OnTargetUnbound()
        {
            if (_request != null)
            {
                _request.Resolved -= OnRequestResolved;
                _request = null;
            }
        }

        private void OnRequestResolved()
        {
            Close();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;

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
                localizator.Language,
                steps: _helpSteps.Select(s =>
                s == FcuLocKey.sprite_duplicate_help_step_7
                    ? Localize(s, Localize(FcuLocKey.common_button_apply))
                    : Localize(s)
            ).ToArray());

            topArea.Add(_helpPanel);

            var contentCol = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            topArea.Add(contentCol);

            _results = uitk.ScrollView();
            _results.contentContainer.style.marginRight = DAI_UitkConstants.MarginPadding;
            _results.style.flexGrow = 1;
            _results.style.flexShrink = 1;
            _results.style.minHeight = 0;
            _results.style.marginBottom = 0;
            contentCol.Add(_results);

            var bottomBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    flexShrink = 0
                }
            };
            root.Add(bottomBar);

            _helpBtn = uitk.HelpButton(() => uitk.ToggleHelpPanel(_helpPanel));


            bottomBar.Add(_helpBtn);
            bottomBar.Add(uitk.Space5());
            var statsPanel = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    backgroundColor = uitk.ColorScheme.BUTTON,
                    height = DAI_UitkConstants.ButtonHeight,
                },
            };

            UIHelpers.SetPadding(statsPanel, DAI_UitkConstants.MarginPadding / 2);
            UIHelpers.SetRadius(statsPanel, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(statsPanel, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(statsPanel, uitk.ColorScheme.OUTLINE);
            bottomBar.Add(statsPanel);
            root.Add(uitk.Space((int)DAI_UitkConstants.SpacingM));

            _statsLabel = new Label
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            statsPanel.Add(_statsLabel);
            UpdateStatsLabel();

            bottomBar.Add(uitk.Space5());

            _deselectAllBtn = uitk.Button(
                "Deselect All Groups",
                DeselectAllGroups);
            bottomBar.Add(_deselectAllBtn);

            bottomBar.Add(uitk.Space5());

            _applyBtn = uitk.Button(
                Localize(FcuLocKey.common_button_apply),
                OnApply);
            _applyBtn.color = EditorGUIUtility.isProSkin ? uitk.ColorScheme.ACCENT_SECOND : uitk.ColorScheme.BUTTON;
            _applyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            bottomBar.Add(_applyBtn);

            bottomBar.Add(uitk.Space5());

            _cancelBtn = uitk.Button(
                Localize(FcuLocKey.label_stop_import),
                () =>
                {
                    monoBeh.AssetTools.StopAsset(ImportStatus.Stopped);
                    Close();
                });
            _cancelBtn.color = uitk.ColorScheme.RED;
            _cancelBtn.style.maxWidth = DAI_UitkConstants.CancelButtonMaxWidth;
            _cancelBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            bottomBar.Add(_cancelBtn);

            if (_inited)
                BuildAllGroupsUI();
        }

        private void BuildAllGroupsUI()
        {
            if (_results == null)
            {
                return;
            }

            _results.Clear();
            _groupCards.Clear();
            _groupRows.Clear();
            _groupHeaders.Clear();
            _dropHighlightIndex = -1;

            for (int gi = 0; gi < _groups.Count; ++gi)
            {
                var card = CreateGroupCard(gi);
                _results.Add(card);
            }

            UpdateStatsLabel();
        }

        private VisualElement CreateGroupCard(int gi)
        {
            var card = new VisualElement
            {
                name = "card",
                style =
                {
                    flexDirection  = FlexDirection.Column,
                    marginBottom   = DAI_UitkConstants.MarginPadding,
                    backgroundColor= uitk.ColorScheme.GROUP
                },
                userData = gi
            };

            UIHelpers.SetDefaultPadding(card);

            UIHelpers.SetRadius(card, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(card, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(card, uitk.ColorScheme.OUTLINE);
            _groupCards[gi] = card;

            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = DAI_UitkConstants.MarginPadding
                }
            };

            var header = new Label
            {
                style =
                {
                    flexGrow = 1
                }
            };
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            _groupHeaders[gi] = header;

            UpdateHeader(gi);

            headerRow.Add(header);

            var deselectGroupBtn = uitk.Button("Deselect Group", () => DeselectGroup(gi));
            deselectGroupBtn.style.marginLeft = DAI_UitkConstants.MarginPadding;
            headerRow.Add(deselectGroupBtn);

            card.Add(headerRow);

            RegisterDropEvents(card);

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap      = Wrap.Wrap
                }
            };

            row.userData = gi;
            RegisterDropEvents(row);

            _groupRows[gi] = row;
            card.Add(row);

            RenderGroupItems(gi);
            return card;
        }

        private void RegisterDropEvents(VisualElement ve)
        {
            ve.RegisterCallback<DragEnterEvent>(e =>
            {
                if (!TryGetDragData(out var payload))
                    return;

                int gi = GetGroupIndexFrom(ve);
                SetDropHighlight(gi, true);
            });

            ve.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (TryGetDragData(out _))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    e.StopPropagation();
                }
            });

            ve.RegisterCallback<DragLeaveEvent>(e =>
            {
                if (!TryGetDragData(out _))
                    return;

                int gi = GetGroupIndexFrom(ve);
                if (_dropHighlightIndex == gi)
                    SetDropHighlight(gi, false);
            });

            ve.RegisterCallback<DragExitedEvent>(e =>
            {
                if (_dropHighlightIndex >= 0)
                    SetDropHighlight(_dropHighlightIndex, false);
            });

            ve.RegisterCallback<DragPerformEvent>(e =>
            {
                if (TryGetDragData(out var payload))
                {
                    DragAndDrop.AcceptDrag();
                    int toIndex = GetGroupIndexFrom(ve);
                    if (_dropHighlightIndex >= 0)
                        SetDropHighlight(_dropHighlightIndex, false);
                    MoveSpriteByPath(payload.SourceGroup, toIndex, payload.Path);
                    e.StopPropagation();
                }
            });

        }

        private int GetGroupIndexFrom(VisualElement ve)
        {
            var card = ve;

            while (card != null && !_groupCards.Values.Contains(card))
                card = card.parent;

            if (card == null)
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_group_card_missing_for_element, ve));
                return -1;
            }

            if (card.userData is int gi)
            {
                return gi;
            }
            else
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_group_card_userdata_invalid, card.userData));
                return -1;
            }
        }

        private void SetDropHighlight(int gi, bool on)
        {
            if (!_groupCards.TryGetValue(gi, out var card))
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_group_card_missing_for_index, gi));
                return;
            }

            if (on)
            {
                _dropHighlightIndex = gi;
                UIHelpers.SetBorderColor(card, uitk.ColorScheme.OUTLINE);
                card.style.backgroundColor = uitk.ColorScheme.BUTTON;
            }
            else
            {
                if (_dropHighlightIndex == gi) _dropHighlightIndex = -1;
                UIHelpers.SetBorderColor(card, uitk.ColorScheme.OUTLINE);
                card.style.backgroundColor = uitk.ColorScheme.GROUP;
            }
        }

        private void RenderGroupItems(int gi)
        {
            if (!_groupRows.TryGetValue(gi, out var row))
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_group_row_missing_for_index, gi));
                return;
            }

            row.Clear();

            var group = _groups[gi];

            var ordered = group
                .OrderByDescending(d => Mathf.Max(1, (int)d.Size.x) * Mathf.Max(1, (int)d.Size.y))
                .ToList();

            foreach (SpriteUsageFinder.UsedSprite ds in ordered)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ds.Path);

                if (tex == null)
                    continue;

                var thumb = MakeThumbPreview(
                    tex.GetPixels32(),
                    tex.width,
                    tex.height,
                    (int)DAI_UitkConstants.ThumbSize,
                    (int)DAI_UitkConstants.ThumbSize);

                var col = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column,
                        alignItems    = Align.Center,
                        marginRight   = DAI_UitkConstants.MarginPadding,
                        marginBottom  = DAI_UitkConstants.MarginPadding
                    }
                };

                var imgContainer = new VisualElement
                {
                    style =
                    {
                        width = DAI_UitkConstants.ThumbSize,
                        height = DAI_UitkConstants.ThumbSize,
                    }
                };

                var img = new Image
                {
                    image = thumb,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        width  = DAI_UitkConstants.ThumbSize,
                        height = DAI_UitkConstants.ThumbSize
                    }
                };

                UIHelpers.SetRadius(img, DAI_UitkConstants.CornerRadius);
                UIHelpers.SetBorderWidth(img, DAI_UitkConstants.BorderWidth);
                UIHelpers.SetBorderColor(img, uitk.ColorScheme.OUTLINE);
                imgContainer.Add(img);

                if (ds.Usages != null && ds.Usages.Count > 0)
                {
                    var usageBadge = new Label(ds.Usages.Count.ToString())
                    {
                        style =
                        {
                            position = Position.Absolute,
                            top = 0,
                            maxWidth = DAI_UitkConstants.FieldMaxWidthSmall,
                            backgroundColor = uitk.ColorScheme.GREEN,
                            color = Color.white,
                            fontSize = DAI_UitkConstants.FontSizeTiny,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            paddingLeft = DAI_UitkConstants.SpacingXS,
                            paddingRight = DAI_UitkConstants.SpacingXS,
                            borderTopRightRadius = DAI_UitkConstants.CornerRadius,
                            borderBottomLeftRadius = DAI_UitkConstants.CornerRadius
                        }
                    };
                    imgContainer.Add(usageBadge);
                }

                col.Add(imgContainer);

                var tog = new Toggle { value = ds.Selected };
                tog.RegisterValueChangedCallback(e =>
                {
                    ds.Selected = e.newValue;
                    int giNow = GetGroupIndexFrom(col);

                    RenderGroupItems(giNow);
                    UpdateStatsLabel();
                });

                tog.style.marginTop = DAI_UitkConstants.SpacingXXS;
                col.Add(tog);
                col.Add(MakeWrappedNameLabel(System.IO.Path.GetFileName(ds.Path), DAI_UitkConstants.ItemLabelWidth));

                col.Add(new Label($"{(int)ds.Size.x}x{(int)ds.Size.y}")
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        fontSize       = DAI_UitkConstants.FontSizeTiny,
                        width          = DAI_UitkConstants.ItemLabelWidth,
                    }
                });

                Vector2 downPos = Vector2.zero;
                bool dragging = false;

                col.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 1)
                    {
                        int srcGiNow = GetGroupIndexFrom(col);
                        ShowContextMenuForSprite(srcGiNow, ds.Path);
                        return;
                    }

                    if (e.button != 0)
                        return;

                    downPos = e.mousePosition;
                    dragging = true;

                    int srcIndexNow = GetGroupIndexFrom(col);
                    var payload = new DragData
                    {
                        SourceGroup = srcIndexNow,
                        Path = ds.Path
                    };

                    Texture2D texRef = AssetDatabase.LoadAssetAtPath<Texture2D>(ds.Path);
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData(DND_KEY, payload);

                    DragAndDrop.objectReferences = texRef != null ? new UnityEngine.Object[]
                    {
                        texRef
                    } : Array.Empty<UnityEngine.Object>();

                    DragAndDrop.paths = new[] { ds.Path };
                });

                col.RegisterCallback<MouseMoveEvent>(e =>
                {
                    if (!dragging)
                        return;

                    if ((e.mousePosition - downPos).sqrMagnitude > DAI_UitkConstants.DragThresholdSqrMagnitude)
                    {
                        DragAndDrop.StartDrag(System.IO.Path.GetFileName(ds.Path));
                        dragging = false;
                    }
                });

                col.RegisterCallback<MouseUpEvent>(e =>
                {
                    if (e.button == 0 && dragging)
                    {
                        PingAssetByPath(ds.Path, focusProjectWindow: true);
                        e.StopPropagation();
                    }
                    dragging = false;
                });

                col.RegisterCallback<MouseUpEvent>(e => dragging = false);
                row.Add(col);
            }

            UpdateHeader(gi);
            UpdateStatsLabel();
        }

        private static Texture2D MakeThumbPreview(Color32[] pix, int w0, int h0, int outW, int outH)
        {
            var tex = new Texture2D(w0, h0, TextureFormat.RGBA32, false);
            tex.SetPixels32(pix);
            tex.Apply();

            var rt = RenderTexture.GetTemporary(outW, outH);
            var previousRt = RenderTexture.active;
            RenderTexture.active = rt;

            float scale = Mathf.Min(outW / (float)w0, outH / (float)h0);
            int drawW = Mathf.Max(1, Mathf.RoundToInt(w0 * scale));
            int drawH = Mathf.Max(1, Mathf.RoundToInt(h0 * scale));
            float offsetX = (outW - drawW) * 0.5f;
            float offsetY = (outH - drawH) * 0.5f;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, outW, outH, 0);
            GL.Clear(true, true, Color.clear);
            Graphics.DrawTexture(new Rect(offsetX, offsetY, drawW, drawH), tex);
            GL.PopMatrix();

            var prev = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            prev.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
            prev.Apply();

            RenderTexture.active = previousRt;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            return prev;
        }

        private static Label MakeWrappedNameLabel(string text, float width = 80f)
        {
            var lbl = new Label(text);
            lbl.style.width = width;
            lbl.style.maxWidth = width;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            lbl.style.overflow = Overflow.Hidden;
            lbl.style.unityTextAlign = TextAnchor.UpperCenter;
            lbl.style.fontSize = DAI_UitkConstants.FontSizeTiny;
            lbl.style.marginTop = DAI_UitkConstants.SpacingXXS;
            return lbl;
        }

        private void ShowContextMenuForSprite(int sourceGroupIndex, string path)
        {
            var group = _groups[sourceGroupIndex];
            var ds = group.FirstOrDefault(s => s.Path == path);

            if (ds == null)
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_sprite_data_missing, path, sourceGroupIndex));
                return;
            }

            var menu = new GenericMenu();

            if (ds.Usages != null && ds.Usages.Count > 0)
            {
                menu.AddItem(new GUIContent($"Show {ds.Usages.Count} Usages..."), false, () =>
                {
                    SpriteUsagePopup.Show(ds.Usages, uitk);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Show Usages (None)"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent(Localize(FcuLocKey.common_button_create_group)), false, () =>
            {
                CreateGroupFromSprite(sourceGroupIndex, path);
            });

            menu.AddItem(new GUIContent(Localize(FcuLocKey.sprite_duplicate_context_move_to_group)), false, () =>
            {
                MoveToGroupPopup.Show(sourceGroupIndex, path, this);
            });

            menu.ShowAsContext();
        }

        private void CreateGroupFromSprite(int sourceGroup, string path)
        {
            if (sourceGroup < 0 || sourceGroup >= _groups.Count)
            {
                EditorUtility.DisplayDialog("Create group", $"Invalid source group index: {sourceGroup}", "OK");
                return;
            }

            var src = _groups[sourceGroup];

            int idx = src.FindIndex(x => x.Path == path);
            if (idx < 0)
            {
                EditorUtility.DisplayDialog("Create group", "Source group does not contain this sprite.", "OK");
                return;
            }

            var ds = src[idx];
            src.RemoveAt(idx);
            UpdateSelectionsForGroup(src);

            int insertIndex = Mathf.Min(sourceGroup + 1, _groups.Count);
            _groups.Insert(insertIndex, new List<DA_Assets.SpriteUsageFinder.UsedSprite> { ds });

            ShiftUiDictionariesOnInsert(insertIndex);
            var newCard = CreateGroupCard(insertIndex);
            _results.Insert(insertIndex, newCard);

            RenderGroupItems(sourceGroup);
            RenderGroupItems(insertIndex);

            for (int i = insertIndex; i < _groups.Count; i++)
                UpdateHeader(i);

            UpdateStatsLabel();
        }

        private void ShiftUiDictionariesOnInsert(int insertIndex)
        {
            for (int i = _groups.Count - 2; i >= insertIndex; i--)
            {
                if (_groupCards.TryGetValue(i, out var card))
                {
                    _groupCards.Remove(i);
                    _groupCards[i + 1] = card;
                    card.userData = i + 1;
                }

                if (_groupRows.TryGetValue(i, out var row))
                {
                    _groupRows.Remove(i);
                    _groupRows[i + 1] = row;
                }

                if (_groupHeaders.TryGetValue(i, out var header))
                {
                    _groupHeaders.Remove(i);
                    _groupHeaders[i + 1] = header;
                }
            }
        }

        internal void MoveSpriteByPath(int from, int to, string path)
        {
            if (from == to)
            {
                EditorUtility.DisplayDialog(
                    Localize(FcuLocKey.move_to_group_title),
                    "Source and target groups are the same.",
                    Localize(FcuLocKey.common_button_ok));
                return;
            }

            if (from < 0 || from >= _groups.Count)
            {
                EditorUtility.DisplayDialog(
                    Localize(FcuLocKey.move_to_group_title),
                    $"Invalid source index: {from}",
                    Localize(FcuLocKey.common_button_ok));
                return;
            }

            if (to < 0 || to >= _groups.Count)
            {
                EditorUtility.DisplayDialog(
                    Localize(FcuLocKey.move_to_group_title),
                    $"Target group {to} does not exist.",
                    Localize(FcuLocKey.common_button_ok));
                return;
            }

            if (_groupCards.TryGetValue(from, out var fromCard) && fromCard.userData is int fUI)
                from = fUI;

            if (_groupCards.TryGetValue(to, out var toCard) && toCard.userData is int tUI)
                to = tUI;

            if (from == to)
            {
                EditorUtility.DisplayDialog(
                    Localize(FcuLocKey.move_to_group_title),
                    "Already in this group.",
                    Localize(FcuLocKey.common_button_ok));
                return;
            }

            var src = _groups[from];
            var dst = _groups[to];

            int idx = src.FindIndex(x => x.Path == path);
            if (idx < 0)
            {
                EditorUtility.DisplayDialog(
                    Localize(FcuLocKey.move_to_group_title),
                    "Source group does not contain this sprite anymore.",
                    Localize(FcuLocKey.common_button_ok));
                return;
            }

            var ds = src[idx];
            src.RemoveAt(idx);
            dst.Add(ds);

            UpdateSelectionsForGroup(src);
            UpdateSelectionsForGroup(dst);

            RenderGroupItems(from);
            RenderGroupItems(to);
            UpdateHeader(from);
            UpdateHeader(to);
            UpdateStatsLabel();
        }

        private void UpdateSelectionsForGroup(List<SpriteUsageFinder.UsedSprite> g)
        {
            if (g == null || g.Count == 0)
                return;

            var keep = g.OrderByDescending(d => d.Size.x * d.Size.y).First();
            foreach (var d in g) d.Selected = d != keep;
        }

        private void DeselectGroup(int gi)
        {
            if (gi < 0 || gi >= _groups.Count)
                return;

            foreach (SpriteUsageFinder.UsedSprite sprite in _groups[gi])
                sprite.Selected = false;

            RenderGroupItems(gi);
            UpdateHeader(gi);
            UpdateStatsLabel();
        }

        private void DeselectAllGroups()
        {
            foreach (List<SpriteUsageFinder.UsedSprite> group in _groups)
            {
                foreach (SpriteUsageFinder.UsedSprite sprite in group)
                    sprite.Selected = false;
            }

            BuildAllGroupsUI();
        }

        private void UpdateHeader(int gi)
        {
            if (_groupHeaders.TryGetValue(gi, out var label))
            {
                int count = _groups[gi].Count;
                label.text = Localize(FcuLocKey.sprite_duplicate_label_group_count, gi, count);
            }
            else
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_group_header_missing, gi));
            }

            if (_groupCards.TryGetValue(gi, out var card))
            {
                card.userData = gi;
            }
            else
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_group_card_missing_for_index, gi));
            }
        }

        private void OnApply()
        {
            _request?.ResolveCurrent();
        }

        private void UpdateStatsLabel()
        {
            int total = _groups.Sum(g => g.Count);
            int selected = _groups.Sum(g => g.Count(d => d.Selected));
            _statsLabel.text = Localize(FcuLocKey.sprite_duplicate_label_selected_for_deletion, selected, total);
        }

        private bool TryGetDragData(out DragData data)
        {
            object obj = DragAndDrop.GetGenericData(DND_KEY);
            if (obj is DragData dd)
            {
                data = dd;
                return true;
            }
            else
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_drag_data_invalid,
                    obj?.GetType()?.ToString() ?? "null",
                    typeof(DragData).Name,
                    DND_KEY));
            }
            data = default;
            return false;
        }

        private void PingAssetByPath(string assetPath, bool focusProjectWindow = true)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj == null)
            {
                Debug.LogError(Localize(FcuLocKey.log_sdf_asset_not_found, assetPath));
                return;
            }

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);

            if (focusProjectWindow)
                EditorUtility.FocusProjectWindow();
        }
    }
}