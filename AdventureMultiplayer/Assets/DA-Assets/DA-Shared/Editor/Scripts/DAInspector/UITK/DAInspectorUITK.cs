using DA_Assets.Shared.Extensions;
using DA_Assets.Singleton;
using DA_Assets.Constants;
using DA_Assets.UpdateChecker;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if !UNITY_2020_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace DA_Assets.DAI
{
    [CreateAssetMenu(fileName = "DAInspectorUITK", menuName = DAConstants.Publisher + "/DAInspectorUITK")]
    [Serializable]
    public class DAInspectorUITK : ScriptableObject
    {
        [SerializeField] AnimationCurve _foldoutCurve;
        [SerializeField] float _foldoutDuration = 0.25f;
        [Header("Schemes")]
        [SerializeField] UitkColorScheme _darkScheme;
        [SerializeField] UitkColorScheme _lightScheme;

        public AnimationCurve FoldoutCurve => _foldoutCurve;
        public float FoldoutDuration => _foldoutDuration;

        private UitkColorScheme _currentScheme => EditorGUIUtility.isProSkin ? _darkScheme : _lightScheme;
        public UitkColorScheme ColorScheme => _currentScheme;

        public VisualElement CreateRoot(Color bg, bool extrude = true)
        {
            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = bg,
                    paddingLeft = DAI_UitkConstants.SpacingXL,
                    paddingRight = DAI_UitkConstants.SpacingXL,
                    paddingTop = DAI_UitkConstants.SpacingXL,
                    paddingBottom = DAI_UitkConstants.SpacingXL
                }
            };

            // Normalize inspector container.
            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                root.schedule.Execute(() =>
                {
                    var parent = root.parent;
                    var pp = parent?.parent;

                    // Window-hosted UITK trees do not have the same wrapper hierarchy as inspector roots.
                    // In those cases there is nothing to normalize, so skip the inspector-specific styling.
                    if (pp == null)
                    {
                        return;
                    }

                    VisualElement sc = pp.Children().ElementAtOrDefault(1);
                    VisualElement tc = pp.Children().ElementAtOrDefault(2);

                    if (sc == null)
                    {
                        return;
                    }

                    UIHelpers.SetZeroMarginPadding(sc);

                    sc.style.marginBottom = -3; // Layout fix

                    if (extrude)
                    {
                        sc.style.backgroundColor = bg;
                        if (sc.parent != null)
                        {
                            sc.parent.style.backgroundColor = bg;
                        }
                    }

                    if (tc != null)
                    {
                        tc.style.display = DisplayStyle.None;
                    }
                });
            });

            return root;
        }

        public CustomHelpBox HelpBox(HelpBoxData data)
        {
            return new CustomHelpBox(this, data);
        }

        public DA_Dropdown<T> Dropdown<T>(
            string labelText = null,
            DropdownWidthMode labelWidthMode = DropdownWidthMode.Fixed,
            float labelFixedWidthPx = 0f,
            DropdownWidthMode dropdownWidthMode = DropdownWidthMode.Fixed,
            float dropdownFixedWidthPx = 0f)
        {
            return new DA_Dropdown<T>(
                this,
                labelText,
                labelWidthMode,
                labelFixedWidthPx,
                dropdownWidthMode,
                dropdownFixedWidthPx);
        }

        public Button HelpButton(Action onClick)
        {
            var btn = RoundButton(onClick);
            btn.tooltip = "Show/Hide Instruction";

            var helpTex = EditorGUIUtility.IconContent("_Help")?.image as Texture2D;

            if (helpTex != null)
                btn.style.backgroundImage = new StyleBackground(helpTex);
            else
                btn.text = "?";

            return btn;
        }

        public VisualElement BuildHelpPanel(DALanguage lang, string[] steps = default)
        {
            var panelWrap = new VisualElement
            {
                style =
                {
                    width = DAI_UitkConstants.HelpPanelWidth,
                    minWidth = DAI_UitkConstants.HelpPanelWidth,
                    maxWidth = DAI_UitkConstants.HelpPanelWidth,
                    flexDirection = FlexDirection.Column,
                    marginRight = DAI_UitkConstants.MarginPadding,
                }
            };

            SetVisible(panelWrap, false);

            var card = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = _currentScheme.GROUP
                }
            };

            UIHelpers.SetRadius(card, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(card, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(card, _currentScheme.OUTLINE);
            panelWrap.Add(card);

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,

                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding / 2,
                    paddingTop = DAI_UitkConstants.MarginPadding / 2,
                    paddingBottom = DAI_UitkConstants.MarginPadding / 2,

                    backgroundColor = _currentScheme.BUTTON
                }
            };

            UIHelpers.SetRadius(header, DAI_UitkConstants.CornerRadius);
            card.Add(header);

            var t = new Label(SharedLocKey.label_help.Localize())
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeTitle
                }
            };
            header.Add(t);

            var closeBtn = new Button(() => ToggleHelpPanel(panelWrap))
            {
                text = "×",
                tooltip = SharedLocKey.text_close.Localize(),
                style =
                {
                    width = DAI_UitkConstants.ButtonHeight,
                    height = DAI_UitkConstants.ButtonHeight,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    backgroundColor = _currentScheme.BUTTON,
                }
            };

            UIHelpers.SetRadius(closeBtn, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(closeBtn, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(closeBtn, _currentScheme.OUTLINE);
            header.Add(closeBtn);

            var scroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    paddingBottom = DAI_UitkConstants.MarginPadding
                }
            };
            card.Add(scroll);

            foreach (var s in steps)
            {
                var lbl = new Label(s);
                lbl.style.whiteSpace = WhiteSpace.Normal;
                lbl.style.unityTextAlign = TextAnchor.UpperLeft;
                lbl.style.marginBottom = DAI_UitkConstants.MarginPadding;
                lbl.style.fontSize = DAI_UitkConstants.FontSizeNormal;
                scroll.Add(lbl);
            }

            return panelWrap;
        }

        public void ToggleHelpPanel(VisualElement panel) => SetVisible(panel, !IsVisible(panel));

        public void SetVisible(VisualElement ve, bool visible)
        {
            if (ve == null)
            {
                return;
            }

            if (visible)
            {
                ve.style.display = DisplayStyle.Flex;
            }
            else
            {
                ve.style.display = DisplayStyle.None;
            }
        }

        public bool IsVisible(VisualElement ve) => ve != null && ve.style.display == DisplayStyle.Flex;

        public Button StopButton(Action onClick)
        {
            var btn = RoundButton(onClick);
            btn.text = "\u23F9";
            btn.tooltip = "Stop";
            btn.SetEnabled(false);
            return btn;
        }

        public Label SectionTitle(string text)
        {
            return new Label(text)
            {
                name = "sectionTitle",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                }
            };
        }

        public VisualElement VerticalDivider(int thickness = 1, int margin = 10)
        {
            return new VisualElement
            {
                name = "verticalDivider",
                style =
                {
                    width = thickness,
                    minWidth = thickness,
                    backgroundColor = _currentScheme.OUTLINE,
                    marginLeft = margin,
                    marginRight = margin,
                    alignSelf = Align.Stretch,
                    flexShrink = 0
                }
            };
        }


        public ScrollView ScrollView()
        {
            ScrollView scrollView = new ScrollView
            {
                name = "scrollView",
#if UNITY_2020_1_OR_NEWER
                horizontalScrollerVisibility = ScrollerVisibility.Auto,
#else
                showHorizontal = true,
#endif
                style =
                {
                    flexGrow = 1,
                    backgroundColor = _currentScheme.BG
                }
            };

            UIHelpers.SetRadius(scrollView, DAI_UitkConstants.CornerRadius);

            return scrollView;
        }

        public (VisualElement container, SliderInt slider) ColoredSlider(
            string label,
            int min = 0,
            int max = 0,
            int initial = 0,
            int step = 0,
            int midTreshold = 100, // DAI_UitkConstants.SliderMidThreshold
            int highTreshold = 150) // DAI_UitkConstants.SliderHighThreshold
        {
            float SegmentLineHeight = DAI_UitkConstants.SegmentLineHeight;
            float SegmentVerticalOffset = DAI_UitkConstants.SegmentVerticalOffset;

            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var slider = new SliderInt(label, min, max)
            {
                name = "coloredSlider",
                value = initial
            };
            slider.style.flexGrow = 1;

            var intField = new IntegerField
            {
                name = "intField",
                value = slider.value
            };
            intField.style.width = DAI_UitkConstants.IntFieldWidth;

            container.Add(slider);
            container.Add(intField);

            void Apply(int v)
            {
                int stepped = Mathf.Clamp(
                    Mathf.RoundToInt(v / (float)step) * step,
                    min,
                    max);
                if (slider.value != stepped) slider.value = stepped;
                if (intField.value != stepped) intField.value = stepped;
                slider.MarkDirtyRepaint();
            }

            slider.RegisterValueChangedCallback(e => Apply(e.newValue));
            intField.RegisterValueChangedCallback(e => Apply(e.newValue));

            slider.generateVisualContent += ctx =>
            {
                Rect r = slider.contentRect;
                float labelW = slider.labelElement != null ? slider.labelElement.layout.width : 0f;
                float left = r.xMin + labelW;
                float width = Mathf.Max(0f, r.width - labelW);
                if (width <= 0f) return;
                float lineH = SegmentLineHeight;
                float y = r.center.y - SegmentVerticalOffset;
                float min2 = slider.lowValue;
                float max2 = slider.highValue;
                float tMid = Mathf.InverseLerp(min2, max2, midTreshold);
                float tHigh = Mathf.InverseLerp(min2, max2, highTreshold);
                float xMid = left + width * tMid;
                float xHigh = left + width * tHigh;
                float xMax = left + width;

                if (xMid > left)
                    FillRect(
                        ctx,
                        new Rect(left + DAI_UitkConstants.SegmentInset, y, xMid - left, lineH),
                        _currentScheme.GREEN);

                if (xHigh > xMid)
                    FillRect(
                        ctx,
                        new Rect(xMid, y, xHigh - xMid, lineH),
                        _currentScheme.ORANGE);

                if (xMax > xHigh)
                    FillRect(ctx, new Rect(xHigh, y, xMax - xHigh, lineH),
                        _currentScheme.RED);
            };

            slider.RegisterCallback<GeometryChangedEvent>(_ => slider.MarkDirtyRepaint());
            if (slider.labelElement != null)
                slider.labelElement.RegisterCallback<GeometryChangedEvent>(_ => slider.MarkDirtyRepaint());

            Apply(slider.value);
            return (container, slider);

            void FillRect(MeshGenerationContext l_ctx, Rect l_rect, Color l_color)
            {
                var mesh = l_ctx.Allocate(4, 6);

                var v0 = new Vertex() { position = new Vector3(l_rect.xMin, l_rect.yMin, 0), tint = l_color, uv = Vector2.zero };
                var v1 = new Vertex() { position = new Vector3(l_rect.xMax, l_rect.yMin, 0), tint = l_color, uv = Vector2.right };
                var v2 = new Vertex() { position = new Vector3(l_rect.xMax, l_rect.yMax, 0), tint = l_color, uv = Vector2.one };
                var v3 = new Vertex() { position = new Vector3(l_rect.xMin, l_rect.yMax, 0), tint = l_color, uv = Vector2.up };

                mesh.SetNextVertex(v0);
                mesh.SetNextVertex(v1);
                mesh.SetNextVertex(v2);
                mesh.SetNextVertex(v3);

                mesh.SetNextIndex(0);
                mesh.SetNextIndex(1);
                mesh.SetNextIndex(2);
                mesh.SetNextIndex(2);
                mesh.SetNextIndex(3);
                mesh.SetNextIndex(0);
            }
        }

        public TabItem TabItem(string text, Action clicked)
        {
            return new TabItem(text, clicked, this);
        }

        public ExpandableTabItem ExpandableTabItem(string text, string stateId)
        {
            return new ExpandableTabItem(text, stateId, this);
        }

        public CustomButton Button(string text, Action clicked)
        {
            return new CustomButton(text, clicked, this);
        }

        public RoundButton RoundButton(Action clicked)
        {
            return new RoundButton(clicked, this);
        }

        public EnumField EnumField(string label, Enum defaultValue = null, int? maxWidth = 150) // FieldMaxWidthMedium
        {
            EnumField field = new EnumField(label, defaultValue);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public DropdownField DropdownField(string label, int? maxWidth = 150) // FieldMaxWidthMedium
        {
            DropdownField field = new DropdownField(label);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public LayerField LayerField(string label, int defaultValue = 0, int? maxWidth = 150) // FieldMaxWidthMedium
        {
            LayerField field = new LayerField(label, defaultValue);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public TextField TextField(string label, int? maxWidth = 250) // FieldMaxWidthLarge
        {
            TextField field = new TextField(label);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public ColorField ColorField(string label, int? maxWidth = 150) // FieldMaxWidthMedium
        {
            ColorField field = new ColorField(label);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public IntegerField IntegerField(string label, int? maxWidth = 150) // FieldMaxWidthMedium
        {
            IntegerField field = new IntegerField(label);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public FloatField FloatField(string label, int? maxWidth = 150) // FieldMaxWidthMedium
        {
            FloatField field = new FloatField(label);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, null);
        }

        public Vector2IntField Vector2IntField(string label, int? maxWidth = 250) // FieldMaxWidthLarge
        {
            Vector2IntField field = new Vector2IntField(label);
            field.style.marginRight = DAI_UitkConstants.V2IntFieldMargin;
            return StyleFieldInput(field, maxWidth, Color.clear, null);
        }

        public Vector4Field Vector4Field(string label, int? maxWidth = 250) // FieldMaxWidthLarge
        {
            Vector4Field field = new Vector4Field(label);
            return StyleFieldInput(field, maxWidth, Color.clear, null);
        }

        public Toggle Toggle(string label)
        {
            Toggle field = new Toggle(label);
            return StyleFieldInput(field, null, null, FlexDirection.RowReverse);
        }

        public EnumFlagsField EnumFlagsField(string label, Enum defaultValue = null, int? maxWidth = 250) // FieldMaxWidthLarge
        {
            EnumFlagsField field = new EnumFlagsField(label, defaultValue);
            return StyleFieldInput(field, maxWidth, _currentScheme.BUTTON, FlexDirection.RowReverse);
        }

        private T StyleFieldInput<T>(
            T field,
            int? maxWidth,
            Color? color,
            FlexDirection? flexDirection) where T : VisualElement
        {
            if (field.childCount > 1)
            {
                VisualElement input = field[1];

                if (maxWidth.HasValue)
                {
                    input.style.maxWidth = maxWidth.Value;
                }

                if (color.HasValue)
                {
                    input.style.backgroundColor = color.Value;
                }

                if (flexDirection.HasValue)
                {
                    input.style.flexDirection = flexDirection.Value;
                }

                input.style.marginLeft = new StyleLength(StyleKeyword.Auto);
            }

            return field;
        }

        public SliderInt SliderInt(string label, int start, int end)
        {
            return new SliderInt(label, start, end);
        }

        public VisualElement CenteredLogo(
            Texture2D logoDarkTheme,
            Texture2D logoLightTheme,
            int minWidthPx = 96, // LogoMinWidth
            int maxWidthPx = 512, // LogoMaxWidth
            int marginLeft = 90, // LogoMargin
            int marginRight = 90) // LogoMargin
        {
            var wrap = new VisualElement()
            {
                style =
                {
                    alignItems = new StyleEnum<Align>(Align.Center),
                    justifyContent = new StyleEnum<Justify>(Justify.Center),
                    width = new StyleLength(Length.Percent(100f)),
                    marginLeft = new StyleLength(marginLeft),
                    marginRight = new StyleLength(marginRight),
                }
            };

            Texture2D logo = EditorGUIUtility.isProSkin ? logoDarkTheme : logoLightTheme;

            var img = new Image()
            {
                image = logo,
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    minWidth = new StyleLength(minWidthPx),
                    maxWidth = new StyleLength(maxWidthPx),
                }
            };

            if (logo != null)
            {
                float aspect = (float)logo.height / logo.width;

                void Resize()
                {
                    float avail = wrap.resolvedStyle.width;

                    if (float.IsNaN(avail) || avail <= 0f)
                        return;

                    float w = Mathf.Clamp(avail, minWidthPx, maxWidthPx);
                    img.style.width = new StyleLength(w);
                    img.style.height = new StyleLength(w * aspect);
                }

                wrap.RegisterCallback<AttachToPanelEvent>(_ => Resize());
                wrap.RegisterCallback<GeometryChangedEvent>(_ => Resize());
            }

            wrap.Add(img);
            return wrap;
        }

        public VisualElement Footer()
        {
            return Footer(default);
        }

        public VisualElement Footer(DALanguage lang)
        {
            var root = new VisualElement
            {
                style =
                {
                    justifyContent = Justify.Center,
                    alignItems = Align.Center,

                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(StyleKeyword.Auto),
                }
            };

            Color baseColor = _currentScheme.TEXT_SECOND;

            Color hoverColor = EditorGUIUtility.isProSkin
                ? UIHelpers.Darken(baseColor, DAI_UitkConstants.FooterHoverPct)
                : UIHelpers.Lighten(baseColor, DAI_UitkConstants.FooterHoverPct);
                
            var line1 = new Label(SharedLocKey.label_made_by.Localize(lang));
            line1.style.unityTextAlign = TextAnchor.MiddleCenter;
            line1.style.fontSize = 11;
            line1.style.unityFontStyleAndWeight = FontStyle.Normal;
            line1.style.color = new StyleColor(baseColor);

            UIHelpers.SetZeroMarginPadding(line1);

            var line2 = new Label("D.A. Assets");
            line2.style.unityTextAlign = TextAnchor.MiddleCenter;
            line2.style.fontSize = DAI_UitkConstants.FontSizeNormal;
            line2.style.unityFontStyleAndWeight = FontStyle.Bold;
            line2.style.color = new StyleColor(baseColor);

            UIHelpers.SetZeroMarginPadding(line2);

            bool isEastAsianLanguage = lang == DALanguage.ja ||
                                       lang == DALanguage.ko ||
                                       lang == DALanguage.zh;

            if (isEastAsianLanguage)
            {
                root.style.flexDirection = FlexDirection.Row;
                root.Add(line2);
                root.Add(line1);
            }
            else
            {
                root.style.flexDirection = FlexDirection.Column;
                root.Add(line1);
                root.Add(line2);
            }

#if UNITY_2020_1_OR_NEWER
            root.RegisterCallback<PointerEnterEvent>(_ =>
#else
            root.RegisterCallback<MouseEnterEvent>(_ =>
#endif
            {
                line1.style.color = new StyleColor(hoverColor);
                line2.style.color = new StyleColor(hoverColor);
            });

#if UNITY_2020_1_OR_NEWER
            root.RegisterCallback<PointerLeaveEvent>(_ =>
#else
            root.RegisterCallback<MouseLeaveEvent>(_ =>
#endif
            {
                line1.style.color = new StyleColor(baseColor);
                line2.style.color = new StyleColor(baseColor);
            });

            root.AddManipulator(new Clickable(() =>
            {
                Application.OpenURL(DAConstants.SiteLink);
            }));

            return root;
        }


        public SquareIconButton SquareIconButton(Action onClick, Texture2D icon = null, string tooltip = null)
        {
            return new SquareIconButton(this, icon, tooltip, onClick);
        }

        public DropZoneElement DropZoneElement(string placeholderText = "Drop file here", string acceptedExtension = ".zip")
        {
            return new DropZoneElement(this, placeholderText, acceptedExtension);
        }

        public VisualElement Space(int h)
        {
            return new VisualElement()
            {
                style =
                {
                    height = new StyleLength(h),
                    width = new StyleLength(h)
                }
            };
        }

        public VisualElement Space5() => Space(5); // SpacingXS (close enough)
        public VisualElement Space10() => Space(10); // MarginPadding
        public VisualElement Space30() => Space(30);

        public struct FooterAssetInfo
        {
            public DA_Assets.UpdateChecker.Models.AssetType AssetType;
            public string ProductVersion;
        }

        public VisualElement CreateFooterWithVersionInfo(DALanguage lang, params FooterAssetInfo[] assetInfos)
        {
            var root = new VisualElement
            {
                name = "FooterRoot",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexWrap = Wrap.Wrap,
                    height = new StyleLength(StyleKeyword.Auto),
                    minHeight = DAI_UitkConstants.FooterMinHeight,
                    marginTop = DAI_UitkConstants.MarginPadding,
                    borderTopWidth = DAI_UitkConstants.BorderWidth,
                    borderTopColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.2f)),
                    paddingTop = DAI_UitkConstants.SpacingXL,
                    marginLeft = DAI_UitkConstants.NegativeRootPadding,
                    marginRight = DAI_UitkConstants.NegativeRootPadding,
                    paddingLeft = DAI_UitkConstants.FooterInnerPadding,
                    paddingRight = DAI_UitkConstants.FooterInnerPadding,
                }
            };

            var versionContainer = new VisualElement
            {
                name = "VersionContainer",
                style =
                {
                    justifyContent = Justify.FlexStart,
                    paddingLeft = DAI_UitkConstants.SpacingXS,
                    flexShrink = 0
                }
            };

            if (assetInfos != null)
            {
                foreach (var assetInfo in assetInfos)
                {
                    var versionDisplay = new DA_Assets.UpdateChecker.VersionDisplayElement(
                        assetInfo.AssetType,
                        assetInfo.ProductVersion,
                        lang,
                        this);

                    versionDisplay.style.marginTop = 0;
                    versionDisplay.style.marginBottom = DAI_UitkConstants.SpacingXXS;
                    versionContainer.Add(versionDisplay);
                }
            }

            var separator = new VisualElement
            {
                name = "Separator",
                style =
                {
                    width = DAI_UitkConstants.SplitterWidth,
                    alignSelf = Align.Stretch,
                    backgroundColor = new StyleColor(_currentScheme.FOOTER_SEPARATOR),
                    marginLeft = DAI_UitkConstants.MarginPadding,
                    marginRight = DAI_UitkConstants.MarginPadding,
                    marginTop = DAI_UitkConstants.NegativeRootPadding,
                    marginBottom = DAI_UitkConstants.NegativeRootPadding,
                }
            };

            var brandingContainer = Footer(lang);
            brandingContainer.style.width = new StyleLength(StyleKeyword.Auto);
            brandingContainer.style.minWidth = 100; // Layout specific
            brandingContainer.style.marginLeft = new StyleLength(StyleKeyword.Auto);
            brandingContainer.style.marginRight = new StyleLength(StyleKeyword.Auto);

            var rightWrapper = new VisualElement
            {
                name = "RightWrapper",
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    minWidth = brandingContainer.style.minWidth
                }
            };

            rightWrapper.Add(brandingContainer);

            root.Add(versionContainer);
            root.Add(separator);
            root.Add(rightWrapper);

            root.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (float.IsNaN(versionContainer.layout.width) || float.IsNaN(brandingContainer.layout.width) || float.IsNaN(root.layout.width) || root.layout.width == 0)
                {
                    return;
                }

                const float separatorTotalWidth = 21f;

                float requiredWidth = versionContainer.layout.width
                                      + brandingContainer.layout.width
                                      + separatorTotalWidth;

                float availableWidth = root.layout.width - root.resolvedStyle.paddingLeft - root.resolvedStyle.paddingRight;

                bool shouldWrap = requiredWidth > availableWidth;

                var newDisplay = shouldWrap ? DisplayStyle.None : DisplayStyle.Flex;
                var newJustify = shouldWrap ? Justify.Center : Justify.FlexEnd;

                if (separator.style.display != newDisplay || rightWrapper.style.justifyContent != newJustify)
                {
                    rightWrapper.schedule.Execute(() =>
                    {
                        separator.style.display = newDisplay;
                        rightWrapper.style.justifyContent = newJustify;
                    });
                }
            });

            return root;
        }

        public VisualElement BuildRateMe(string packageLink, Func<string> descriptionProvider, string prefsKey, Func<string> tooltipProvider = null)
        {
            var frame = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = Align.FlexStart,
                    marginTop = DAI_UitkConstants.SpacingXS,
                    marginBottom = DAI_UitkConstants.SpacingXS,
                    paddingLeft = DAI_UitkConstants.SpacingS,
                    paddingRight = DAI_UitkConstants.SpacingS,
                    paddingTop = DAI_UitkConstants.SpacingS,
                    paddingBottom = DAI_UitkConstants.SpacingS
                }
            };

            ApplyThinBorder(frame, _currentScheme.THIN_BORDER, 1);

            var desc = new Label(descriptionProvider())
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = DAI_UitkConstants.RateMeMinWidth,
                    whiteSpace = WhiteSpace.Normal,
                    marginRight = DAI_UitkConstants.SpacingM,
                    marginBottom = DAI_UitkConstants.SpacingM
                }
            };
            desc.tooltip = tooltipProvider?.Invoke() ?? string.Empty;

            void UpdateTextOnConfigLoad()
            {
                desc.text = descriptionProvider();
                if (tooltipProvider != null)
                {
                    desc.tooltip = tooltipProvider();
                }
                UpdateService.OnConfigLoaded -= UpdateTextOnConfigLoad;
            }

            UpdateService.OnConfigLoaded += UpdateTextOnConfigLoad;

            var rightCol = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexShrink = 0,
                    width = StyleKeyword.Auto,
                    minWidth = DAI_UitkConstants.RateMeRightColWidth
                }
            };

            var starsRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center
                }
            };

            for (int i = 0; i < 5; i++)
            {
                var star = new Label
                {
                    text = "⭐️",
                    style =
                    {
                        width = DAI_UitkConstants.IconSizeSmall,
                        height = DAI_UitkConstants.IconSizeSmall,
                        marginLeft = i > 0 ? DAI_UitkConstants.SpacingXS : 0
                    }
                };
                starsRow.Add(star);
            }

            var dontShowBtn = Button("Don't show", SetInt);

            var openBtn = Button("Open Asset Store", () =>
            {
                Application.OpenURL(packageLink);
                SetInt();
            });

            rightCol.Add(starsRow);
            rightCol.Add(Space10());
            rightCol.Add(dontShowBtn);
            rightCol.Add(Space5());
            rightCol.Add(openBtn);

            frame.Add(desc);
            frame.Add(rightCol);

            void SetInt()
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetInt(prefsKey, 1);
                frame.style.display = DisplayStyle.None;
#endif
            }

            return frame;
        }

        public void ApplyThinBorder(VisualElement ve, Color color, int width = 1)
        {
            ve.style.borderTopWidth = width;
            ve.style.borderRightWidth = width;
            ve.style.borderBottomWidth = width;
            ve.style.borderLeftWidth = width;
            ve.style.borderTopColor = color;
            ve.style.borderRightColor = color;
            ve.style.borderBottomColor = color;
            ve.style.borderLeftColor = color;
        }

        public IMGUIContainer DragZone(IntegerField intField, Texture2D iconTex)
        {
            var iconContainer = new IMGUIContainer(() =>
            {
                Rect controlRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(25), GUILayout.Height(25));

                if (iconTex != null)
                {
                    GUI.Label(controlRect, new GUIContent(iconTex));
                }

                int controlID = GUIUtility.GetControlID(FocusType.Passive, controlRect);
                Event currentEvent = Event.current;

                switch (currentEvent.GetTypeForControl(controlID))
                {
                    case EventType.Repaint:
                        EditorGUIUtility.AddCursorRect(controlRect, MouseCursor.ResizeHorizontal);
                        break;
                    case EventType.MouseDown:
                        if (controlRect.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                        {
                            GUIUtility.hotControl = controlID;
                            currentEvent.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                        {
                            intField.value += (int)currentEvent.delta.x;
                            currentEvent.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                }
            });

            return iconContainer;
        }

        public void Colorize(Action action)
        {
            if (EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = _currentScheme.IMGUI;
                action.Invoke();
                GUI.backgroundColor = Color.white;
            }
            else
            {
                action.Invoke();
            }
        }

        public VisualElement ItemSeparator()
        {
            VisualElement separator = new VisualElement();

            separator.style.height = 1;
            separator.style.backgroundColor = _currentScheme.SEPARATOR;

            separator.style.marginTop = 5;
            separator.style.marginBottom = 5;

            return separator;
        }

        public Label CreateTitle(string text, string tooltip = null)
        {
            var title = new Label(text)
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                title.tooltip = tooltip;
            }

            return title;
        }

        public VisualElement CreateSectionPanel(bool withBorder = false)
        {
            var panel = new VisualElement
            {
                style =
                {
                    backgroundColor = _currentScheme.GROUP
                }
            };

            UIHelpers.SetDefaultRadius(panel);
            UIHelpers.SetDefaultPadding(panel);

            if (withBorder)
            {
                panel.style.borderTopWidth = 1;
                panel.style.borderBottomWidth = 1;
                panel.style.borderLeftWidth = 1;
                panel.style.borderRightWidth = 1;
            }

            return panel;
        }

        /// <summary>Transparent nested panel with an outline border for grouping related fields inside a section.</summary>
        public VisualElement CreateSubPanel()
        {
            var panel = new VisualElement();

            UIHelpers.SetDefaultRadius(panel);
            UIHelpers.SetDefaultPadding(panel);

            panel.style.borderTopWidth    = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth   = 1;
            panel.style.borderRightWidth  = 1;

            var borderColor = _currentScheme.OUTLINE;
            panel.style.borderTopColor    = borderColor;
            panel.style.borderBottomColor = borderColor;
            panel.style.borderLeftColor   = borderColor;
            panel.style.borderRightColor  = borderColor;

            return panel;
        }



        public VisualElement CreateFolderInput(
            string label,
            string tooltip,
            string initialValue,
            System.Action<string> onPathChanged,
            System.Func<string> onButtonClick,
            string buttonTooltip = null)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var textField = TextField(label);
            textField.tooltip = tooltip;
            string normalizedInitialValue = NormalizeFolderPath(initialValue);
            textField.value = normalizedInitialValue;
            if (normalizedInitialValue != initialValue)
            {
                onPathChanged(normalizedInitialValue);
            }

            textField.style.flexGrow = 1;
            textField.RegisterValueChangedCallback(evt =>
            {
                string normalizedPath = NormalizeFolderPath(evt.newValue);
                if (normalizedPath != evt.newValue)
                {
                    textField.SetValueWithoutNotify(normalizedPath);
                }

                onPathChanged(normalizedPath);
            });

            var button = Button("...", () =>
            {
                string path = onButtonClick();
                if (!string.IsNullOrEmpty(path))
                {
                    textField.value = NormalizeFolderPath(path);
                }
            });
            button.tooltip = buttonTooltip ?? string.Empty;

            button.style.maxWidth = 50;
            button.style.maxHeight = 18;
            button.style.marginTop = 1;
            UIHelpers.SetRadius(button, 3);

            container.Add(textField);
            container.Add(button);

            return container;
        }

        private static string NormalizeFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            string normalizedPath = path.Trim().Replace('\\', '/');
            string assetsPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');

            if (string.Equals(normalizedPath, assetsPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            string assetsPathPrefix = $"{assetsPath}/";
            if (normalizedPath.StartsWith(assetsPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return $"Assets/{normalizedPath.Substring(assetsPathPrefix.Length)}";
            }

            return normalizedPath;
        }

    }

    public static class UIHelpers
    {
        public static Color32 Lighten(Color32 c, float percent)
        {
            float p = Mathf.Clamp01(percent / 100f);

            byte L(byte v)
            {
                int vi = Mathf.RoundToInt(v + (255 - v) * p);
                return (byte)Mathf.Clamp(vi, 0, 255);
            }

            return new Color32(L(c.r), L(c.g), L(c.b), c.a);
        }

        public static Color32 Darken(Color32 c, float percent)
        {
            float p = Mathf.Clamp01(percent / 100f);

            byte D(byte v)
            {
                int vi = Mathf.RoundToInt(v * (1f - p));
                return (byte)Mathf.Clamp(vi, 0, 255);
            }

            return new Color32(D(c.r), D(c.g), D(c.b), c.a);
        }

        public static void SetZeroMarginPadding(VisualElement element)
        {
            SetMargin(element, 0);
            SetPadding(element, 0);
        }

        public static void SetMargin(VisualElement element, float margin)
        {
            element.style.marginTop = margin;
            element.style.marginBottom = margin;
            element.style.marginLeft = margin;
            element.style.marginRight = margin;
        }

        public static void SetDefaultMargin(VisualElement element)
        {
            SetMargin(element, DAI_UitkConstants.MarginPadding);
        }

        public static void SetPadding(VisualElement element, float padding)
        {
            element.style.paddingTop = padding;
            element.style.paddingBottom = padding;
            element.style.paddingLeft = padding;
            element.style.paddingRight = padding;
        }

        public static void SetDefaultPadding(VisualElement element)
        {
            SetPadding(element, DAI_UitkConstants.MarginPadding);
        }

        public static void SetBorderColor(VisualElement element, Color color)
        {
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        /*public static void SetDefaultBorderColor(VisualElement element)
        {
            SetBorderColor(element, DAI_UitkConstants.BorderColor);
        }*/

        public static void SetBorderWidth(VisualElement element, float width)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
        }

        public static void SetDefaultBorderWidth(VisualElement element)
        {
            SetBorderWidth(element, DAI_UitkConstants.BorderWidth);
        }

        public static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        public static void SetDefaultRadius(VisualElement element)
        {
            SetRadius(element, DAI_UitkConstants.CornerRadius);
        }
    }

    public static class DAI_UitkConstants
    {
        public static Vector2 DefaultWindowSize => new Vector2(800, 600);
        public static Vector2 PopupSize => new Vector2(300, 300);
        public static Vector2 SpriteRemoverSize => new Vector2(500, 130);
        public static Vector2 McpManagerMinSize => new Vector2(400, 300);
        public static float TwoPaneSplitViewDefaultWidth => 500f;

        // > 9
        public static float MarginPadding => 10f;
        public static float CornerRadius => 8f;
        public static float CornerRadiusSmall => 3f;
        public static float CornerRadiusRound => 999f;
        public static float SegmentInset => 2f;
        public static float BorderWidth => 1f;
        public static float SplitterWidth => 1f;

        public static float SpacingXXS => 2f;
        public static float SpacingXS => 4f;
        public static float SpacingS => 6f;
        public static float SpacingM => 8f;
        public static float SpacingL => 12f;
        public static float SpacingXL => 15f;
        public static float IndentStep => 20f;

        public static float ButtonHeight => 28f;
        public static float SmallButtonSize => 30f;
        public static float BrowseButtonWidth => 50f;
        public static float CloseButtonSize => 16f;
        public static float CloseButtonInset => 4f;
        public static float CancelButtonMaxWidth => 100f;

        public static float IconSizeSmall => 16f;
        public static float IconSizeMedium => 20f;
        public static float IconSizeLarge => 36f;
        public static float StatusIconSize => 14f;
        public static float ThumbSize => 64f;

        public static float FontSizeTitle => 14f;
        public static float FontSizeNormal => 12f;
        public static float FontSizeTiny => 10f;
        public static float FontSizeStatus => 16f;

        public static float FieldMaxWidthSmall => 100f;
        public static float FieldMaxWidthMedium => 150f;
        public static float FieldMaxWidthLarge => 250f;
        public static float IntFieldWidth => 60f;
        public static float V2IntFieldMargin => -82f;

        public static float ListViewItemHeight => 20f;
        public static float ItemBaseHeight => 100f;

        public static float HelpPanelWidth => 300f;
        public static float ItemLabelWidth => 80f;
        public static float LineNumWidth => 35f;
        public static float SidebarWidth => 200f;
        public static float RateMeMinWidth => 150f;
        public static float RateMeRightColWidth => 160f;

        public static float LogoMinWidth => 96f;
        public static float LogoMaxWidth => 512f;
        public static float LogoMargin => 90f;

        public static float ApplyButtonMinWidth => 120f;
        public static float FooterHeight => 44f;
        public static float FooterMinHeight => 40f;
        public static float NegativeRootPadding => -15f;
        public static float FooterInnerPadding => 13f;
        public static float DragThresholdSqrMagnitude => 16f;

        public static float PbarHeight => 14f;
        public static float MaxHeight => 240f;
        public static float GapYAbove => 18f;
        public static float GapYBelow => 6f;
        public static float RowMarginBottom => 2f;
        public static float HoverOffset => 1f;

        public static float TransitionDuration => 0.15f;
        public static int AnimFrameMs => 10;
        public static float BtnHoverScale => 1.025f;
        public static float BtnPressScale => 1.05f;
        public static float TabPressScale => 0.98f;
        public static float TabHoverScale => 1.03f;
        public static float RoundBtnHoverScale => 1.05f;
        public static float RoundBtnPressScale => 1.1f;
        public static float IconBtnPressScale => 1.25f;
        public static float IconBtnSize => 32f;
        public static float IconBtnPadding => 5f;

        public static float HoverLightenPct => 10f;
        public static float ActiveLightenPct => 20f;
        public static float HoverDarkenPct => 10f;
        public static float ActiveDarkenPct => 20f;
        public static float FooterHoverPct => 25f;
        public static float HelpBoxHoverPct => 0.05f;
        public static float HelpBoxActivePct => 0.1f;

        public static int SliderMidThreshold => 100;
        public static int SliderHighThreshold => 150;
        public static float SegmentLineHeight => 3f;
        public static float SegmentVerticalOffset => 0.5f;

        public static float SearchBarBtnWidth => 22f;
        public static int SearchAppearCount => 12;
        public static int VirtualScrollExtra => 3;
        public static int MenuItemPriority => 90;
    }

    [Serializable]
    public class UitkColorScheme
    {
        public Color32 ARROW_GRAY = new Color32(153, 161, 179, 255);
        public Color32 HOVER_WHITE = new Color32(255, 255, 255, 20);

        public Color CLEAR = new Color32(0, 0, 0, 0);

        public Color FCU_BG = new Color32(0, 0, 0, 255);
        public Color BG = new Color32(16, 16, 16, 255);
        public Color GROUP = new Color32(28, 28, 28, 255);
        public Color IMGUI = new Color32(110, 110, 110, 255);
        public Color OUTLINE = new Color32(38, 38, 38, 255);
        public Color BUTTON = new Color32(48, 48, 48, 255);
        public Color TEXT = new Color32(255, 255, 255, 255);
        public Color TEXT_SECOND = new Color32(210, 210, 210, 255);

        //https://developer.apple.com/design/human-interface-guidelines/color
        public Color RED = new Color32(233, 21, 45, 255);
        public Color ORANGE = new Color32(255, 141, 40, 255);
        public Color GREEN = new Color32(52, 199, 89, 255);
        public Color BLUE = new Color32(30, 110, 244, 255);

        public Color ACCENT_SECOND = new Color32(0, 139, 239, 255);
        public Color SEPARATOR = new Color32(255, 255, 255, 10);

        public Color DIFF_REMOVED = new Color(0.30f, 0.12f, 0.12f, 1f);
        public Color DIFF_ADDED = new Color(0.11f, 0.24f, 0.11f, 1f);
        public Color DIFF_MISSING = new Color(0.30f, 0.20f, 0.10f, 1f);
        public Color DIFF_EXTRA = new Color(0.10f, 0.20f, 0.30f, 1f);
        
        public Color THIN_BORDER = new Color(0f, 0f, 0f, 0.25f);
        public Color FOOTER_SEPARATOR = new Color(0.5f, 0.5f, 0.5f, 0.2f);
    }

    public struct DragData
    {
        public int SourceGroup;
        public string Path;
    }
}
