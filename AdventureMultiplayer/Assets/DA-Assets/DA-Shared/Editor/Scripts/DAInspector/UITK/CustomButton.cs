using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    public abstract class AnimatedButton : Button
    {
        public new Action onClick;

        public Color color
        {
            set
            {
                UpColor = value;
                RecalculateHoverDownColors();
                InitializeStyles(UpColor);
            }
        }

        protected bool _hovered;
        protected bool _pressed;

        protected float PressScale { get; set; } = 1f;
        protected float HoverScale { get; set; } = 1f;
        protected float NormalScale { get; set; } = 1f;

        protected Color UpColor { get; set; }
        protected Color HoverColor { get; set; }
        protected Color DownColor { get; set; }

        protected float TransitionSec { get; set; } = DAI_UitkConstants.TransitionDuration;
        protected AnimationCurve Curve { get; set; } = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private IVisualElementScheduledItem _animItem;
        protected float _currentScale = 1f;
        protected Color _currentColor;

        public AnimatedButton()
        {
            clickable = null;
            focusable = true;

            RegisterCallback<MouseDownEvent>(e =>
            {
                _pressed = true;
                StartPressAnim();
            });

            RegisterCallback<MouseUpEvent>(e =>
            {
                if (_pressed)
                {
                    _pressed = false;
                    StartHoverOrUpAnim();
                    onClick?.Invoke();
                }
            });

            RegisterCallback<MouseEnterEvent>(e =>
            {
                _hovered = true;
                if (!_pressed)
                {
                    StartHoverAnim();
                }
            });

            RegisterCallback<MouseLeaveEvent>(e =>
            {
                _hovered = false;
                if (!_pressed)
                {
                    StartUpAnim();
                }
            });
        }

        protected abstract void RecalculateHoverDownColors();

        protected void InitializeStyles(Color initialColor)
        {
            _currentColor = initialColor;
            _currentScale = NormalScale;
            ApplyAnimation(_currentScale, _currentColor);
        }

        private void StartPressAnim()
        {
            AnimateTo(PressScale, DownColor);
        }

        private void StartHoverAnim()
        {
            AnimateTo(HoverScale, HoverColor);
        }

        private void StartUpAnim()
        {
            AnimateTo(NormalScale, UpColor);
        }

        private void StartHoverOrUpAnim()
        {
            AnimateTo(_hovered ? HoverScale : NormalScale, _hovered ? HoverColor : UpColor);
        }

        private void AnimateTo(float targetScale, Color targetColor)
        {
            _animItem?.Pause();

            float startScale = _currentScale;
            Color startColor = _currentColor;
            float startTime = Time.realtimeSinceStartup;

            _animItem = schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / TransitionSec);
                float e = Curve.Evaluate(t);

                float newScale = Mathf.LerpUnclamped(startScale, targetScale, e);
                Color newColor = Color.LerpUnclamped(startColor, targetColor, e);

                ApplyAnimation(newScale, newColor);

                if (t >= 1f)
                {
                    _animItem?.Pause();
                }
            }).Every(DAI_UitkConstants.AnimFrameMs);
        }

        protected virtual void ApplyAnimation(float scale, Color color)
        {
            _currentScale = scale;
            _currentColor = color;

            style.scale = new Scale(new Vector3(scale, scale, 1));
            style.backgroundColor = new StyleColor(color);
        }
    }

    public class CustomButton : AnimatedButton
    {
        private const float HOVER_LIGHTEN_PERCENT = 10f; // DAI_UitkConstants.HoverLightenPct
        private const float ACTIVE_LIGHTEN_PERCENT = 20f; // DAI_UitkConstants.ActiveLightenPct
        private const float HOVER_DARKEN_PERCENT = 10f; // DAI_UitkConstants.HoverDarkenPct
        private const float ACTIVE_DARKEN_PERCENT = 20f; // DAI_UitkConstants.ActiveDarkenPct

        public CustomButton(string text, Action onClick, DAInspectorUITK uitk)
        {
            this.text = text;
            this.onClick = onClick;

            style.height = DAI_UitkConstants.ButtonHeight;
            style.flexGrow = 1;
            style.unityFontStyleAndWeight = FontStyle.Normal;

            var textElement = this.Q<TextElement>();
            if (textElement != null)
            {
                textElement.style.flexGrow = 1;
                textElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            UIHelpers.SetZeroMarginPadding(this);
            style.paddingLeft = DAI_UitkConstants.MarginPadding;
            style.paddingRight = DAI_UitkConstants.MarginPadding;
            UIHelpers.SetRadius(this, DAI_UitkConstants.CornerRadius);

            base.UpColor = uitk.ColorScheme.BUTTON;
            RecalculateHoverDownColors();

            base.HoverScale = DAI_UitkConstants.BtnHoverScale;
            base.PressScale = DAI_UitkConstants.BtnPressScale;
            base.NormalScale = 1.0f;
            base.TransitionSec = DAI_UitkConstants.TransitionDuration;

            InitializeStyles(base.UpColor);
        }

        protected override void RecalculateHoverDownColors()
        {
            Color32 baseColor32 = (Color32)base.UpColor;

            if (EditorGUIUtility.isProSkin)
            {
                base.HoverColor = UIHelpers.Lighten(baseColor32, DAI_UitkConstants.HoverLightenPct);
                base.DownColor = UIHelpers.Lighten(baseColor32, DAI_UitkConstants.ActiveLightenPct);
            }
            else
            {
                base.HoverColor = UIHelpers.Darken(baseColor32, DAI_UitkConstants.HoverDarkenPct);
                base.DownColor = UIHelpers.Darken(baseColor32, DAI_UitkConstants.ActiveDarkenPct);
            }
        }
    }

    public class SquareIconButton : AnimatedButton
    {
        private VisualElement _icon;
        private int _baseIconPx;
        private readonly Color _hoverOverlayColor;
        private readonly Color _downOverlayColor;

        public SquareIconButton(
            DAInspectorUITK gui,
            Texture2D tex,
            string tip = null,
            Action onClick = null,
            int sizePx = 32,
            int paddingPx = 5,
            float lightenPercent = 20f,
            float hoverLightenPercent = 10f,
            float pressScale = 1.25f,
            float transitionSec = 0.14f,
            AnimationCurve curve = null)
        {
            this.text = "";

            if (onClick != null)
            {
                this.onClick += onClick;
            }

            _baseIconPx = Mathf.Max(0, sizePx - 2 * Mathf.Max(0, paddingPx));

            base.PressScale = Mathf.Max(0.05f, pressScale);
            base.NormalScale = 1f;
            base.HoverScale = 1f;
            base.TransitionSec = Mathf.Max(0.001f, transitionSec);

            if (curve != null)
            {
                base.Curve = curve;
            }

            base.UpColor = gui.ColorScheme.CLEAR;
            _hoverOverlayColor = EditorGUIUtility.isProSkin
                ? gui.ColorScheme.HOVER_WHITE
                : new Color(0f, 0f, 0f, hoverLightenPercent / 100f * 0.5f);
            _downOverlayColor = EditorGUIUtility.isProSkin
                ? new Color(gui.ColorScheme.HOVER_WHITE.r, gui.ColorScheme.HOVER_WHITE.g, gui.ColorScheme.HOVER_WHITE.b, 0.14f)
                : new Color(0f, 0f, 0f, lightenPercent / 100f * 0.5f);
            RecalculateHoverDownColors();

            tooltip = tip;

            style.width = sizePx;
            style.height = sizePx;
            style.paddingLeft = paddingPx;
            style.paddingRight = paddingPx;
            style.paddingTop = paddingPx;
            style.paddingBottom = paddingPx;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;

            UIHelpers.SetRadius(this, 0);
            UIHelpers.SetBorderWidth(this, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(this, gui.ColorScheme.OUTLINE);
            UIHelpers.SetMargin(this, 0);

            _icon = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    alignSelf = Align.Center,
                    flexGrow = 0,
                    flexShrink = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0
                }
            };

            _icon.style.backgroundImage = new StyleBackground(tex);
            hierarchy.Add(_icon);

            InitializeStyles(base.UpColor);
            ApplyIconScale(base.NormalScale);
        }

        protected override void RecalculateHoverDownColors()
        {
            base.HoverColor = _hoverOverlayColor;
            base.DownColor = _downOverlayColor;
        }

        protected override void ApplyAnimation(float scale, Color color)
        {
            _currentScale = scale;
            _currentColor = color;

            style.backgroundColor = new StyleColor(color);

            ApplyIconScale(scale);
        }

        private void ApplyIconScale(float s)
        {
            float px = Mathf.Max(1f, _baseIconPx * s);
            var len = new Length(px, LengthUnit.Pixel);

            _icon.style.width = len;
            _icon.style.height = len;
        }
    }

    public sealed class TabItem : AnimatedButton
    {
        private readonly VisualElement _indicator;
        private readonly Label _label;
        private bool _selected;

        private readonly Color _tabColorDefault;
        private readonly Color _tabColorHover;
        private readonly Color _tabColorSelected;

        public TabItem(string text, Action onClick, DAInspectorUITK uitk)
        {
            _tabColorDefault = uitk.ColorScheme.GROUP;
            _tabColorHover = uitk.ColorScheme.BUTTON;
            _tabColorSelected = _tabColorHover;

            this.text = "";
            this.onClick = onClick;

            base.PressScale = DAI_UitkConstants.TabPressScale;
            base.HoverScale = DAI_UitkConstants.TabHoverScale;
            base.NormalScale = 1.0f;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.height = DAI_UitkConstants.ButtonHeight;
            style.marginTop = DAI_UitkConstants.SpacingXXS * 2;
            style.marginLeft = DAI_UitkConstants.CornerRadius;
            style.marginRight = DAI_UitkConstants.CornerRadius;

            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;

            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;

            UIHelpers.SetRadius(this, DAI_UitkConstants.CornerRadius / 2);

            _indicator = new VisualElement();
            _indicator.style.width = DAI_UitkConstants.SpacingXXS * 2;
            _indicator.style.height = Length.Percent(100);
            _indicator.style.backgroundColor = uitk.ColorScheme.ACCENT_SECOND;
            _indicator.style.borderTopLeftRadius = DAI_UitkConstants.CornerRadius / 2;
            _indicator.style.borderBottomLeftRadius = DAI_UitkConstants.CornerRadius / 2;
            _indicator.style.display = DisplayStyle.None;
            Add(_indicator);

            var content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.alignItems = Align.Center;
            content.style.flexGrow = 1f;
            content.style.paddingLeft = DAI_UitkConstants.MarginPadding;
            content.style.paddingRight = DAI_UitkConstants.CornerRadius;
            Add(content);

            _label = new Label(text);
            _label.style.unityTextAlign = TextAnchor.MiddleLeft;
            _label.style.fontSize = DAI_UitkConstants.FontSizeNormal;
            _label.style.flexGrow = 1f;
            content.Add(_label);

            SetSelected(false);
        }

        protected override void RecalculateHoverDownColors()
        {
            if (!_selected)
            {
                base.HoverColor = _tabColorHover;
                base.DownColor = _tabColorHover;
            }
            else
            {
                base.HoverColor = _tabColorSelected;
                base.DownColor = _tabColorSelected;
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            _indicator.style.display = _selected ? DisplayStyle.Flex : DisplayStyle.None;
            _label.style.unityFontStyleAndWeight = _selected ? FontStyle.Bold : FontStyle.Normal;

            if (_selected)
            {
                UpColor = _tabColorSelected;
            }
            else
            {
                UpColor = _tabColorDefault;
            }

            RecalculateHoverDownColors();
            InitializeStyles(UpColor);
        }
    }



    public sealed class ExpandableTabItem : VisualElement
    {
        private readonly TabItem _header;
        private readonly Label _arrowLabel;
        private readonly AnimatedFoldout _foldout;
        private readonly DAInspectorUITK _uitk;
        private readonly List<TabItem> _subItems = new List<TabItem>();

        public ExpandableTabItem(string text, string stateId, DAInspectorUITK uitk)
        {
            _uitk = uitk;
            style.flexDirection = FlexDirection.Column;

            // --- Header row (the clickable TabItem) ---
            _header = new TabItem(text, null, uitk);

            // Arrow label placed at the right end of the header content area.
            _arrowLabel = new Label("▸")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    fontSize = 11,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginLeft = StyleKeyword.Auto,
                    paddingRight = DAI_UitkConstants.CornerRadius,
                    color = uitk.ColorScheme.TEXT_SECOND
                }
            };

            // Inject arrow into the header's content row (second child after the indicator).
            if (_header.childCount >= 2)
            {
                _header[1].Add(_arrowLabel);
            }

            // Disable AnimatedButton's own onClick so only AnimatedFoldout's
            // ClickEvent fires — prevents double Toggle() on a single click.
            _header.onClick = null;

            // --- Sub-menu body ---
            var subBody = new VisualElement();
            subBody.style.flexDirection = FlexDirection.Column;

            _foldout = new AnimatedFoldout(
                stateId,
                _header,
                subBody,
                false,
                uitk.FoldoutCurve,
                uitk.FoldoutDuration,
                uitk.ColorScheme.BG);

            // AnimatedFoldout forces header.style.width = 100% which makes
            // ExpandableTabItem wider than regular TabItems. Reset it.
            _header.style.width = StyleKeyword.Null;

            _foldout.Toggled += expanded =>
            {
                _arrowLabel.text = expanded ? "▾" : "▸";
            };

            _arrowLabel.text = _foldout.Expanded ? "▾" : "▸";

            Add(_foldout);
        }

        /// <summary>Adds a sub-tab button with extra left indent to the expandable menu.</summary>
        public TabItem AddSubItem(string title, Action onClick)
        {
            var subItem = new TabItem(title, onClick, _uitk);

            // Indent sub-items relative to the parent header.
            subItem.style.marginLeft = DAI_UitkConstants.MarginPadding * 2;

            _subItems.Add(subItem);

            // The body is the second child of the AnimatedFoldout.
            if (_foldout.childCount >= 2)
            {
                _foldout[1].Add(subItem);
            }

            return subItem;
        }

        /// <summary>
        /// Marks the header indicator as selected when any child is active.
        /// </summary>
        public void SetHeaderSelected(bool selected)
        {
            _header.SetSelected(selected);
        }

        /// <summary>Collapses the sub-menu without animation.</summary>
        public void Collapse()
        {
            _foldout.SetExpanded(false, false);
        }
    }

    public class RoundButton : AnimatedButton
    {
        private const float HOVER_LIGHTEN_PERCENT = 10f; // DAI_UitkConstants.HoverLightenPct
        private const float ACTIVE_LIGHTEN_PERCENT = 20f; // DAI_UitkConstants.ActiveLightenPct
        private const float HOVER_DARKEN_PERCENT = 10f; // DAI_UitkConstants.HoverDarkenPct
        private const float ACTIVE_DARKEN_PERCENT = 20f; // DAI_UitkConstants.ActiveDarkenPct

        public RoundButton(Action onClick, DAInspectorUITK uitk)
        {
            this.onClick = onClick;

            style.width = DAI_UitkConstants.ButtonHeight;
            style.height = DAI_UitkConstants.ButtonHeight;

            UIHelpers.SetRadius(this, DAI_UitkConstants.CornerRadiusRound);
            UIHelpers.SetBorderWidth(this, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(this, uitk.ColorScheme.OUTLINE);
            UIHelpers.SetZeroMarginPadding(this);

            base.UpColor = uitk.ColorScheme.BUTTON;
            RecalculateHoverDownColors();

            base.HoverScale = DAI_UitkConstants.RoundBtnHoverScale;
            base.PressScale = DAI_UitkConstants.RoundBtnPressScale;
            base.NormalScale = 1.0f;
            base.TransitionSec = DAI_UitkConstants.TransitionDuration;

            InitializeStyles(base.UpColor);
        }

        protected override void RecalculateHoverDownColors()
        {
            Color32 baseColor32 = (Color32)base.UpColor;

            if (EditorGUIUtility.isProSkin)
            {
                base.HoverColor = UIHelpers.Lighten(baseColor32, DAI_UitkConstants.HoverLightenPct);
                base.DownColor = UIHelpers.Lighten(baseColor32, DAI_UitkConstants.ActiveLightenPct);
            }
            else
            {
                base.HoverColor = UIHelpers.Darken(baseColor32, DAI_UitkConstants.HoverDarkenPct);
                base.DownColor = UIHelpers.Darken(baseColor32, DAI_UitkConstants.ActiveDarkenPct);
            }
        }
    }
}
