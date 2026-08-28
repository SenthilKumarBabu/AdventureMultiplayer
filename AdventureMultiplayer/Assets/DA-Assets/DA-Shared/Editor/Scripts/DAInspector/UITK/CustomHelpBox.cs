using System;
using System.Collections.Generic;
using System.Text;
using DA_Assets.Shared.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2022_2_OR_NEWER
using UnityEngine.UIElements.Experimental;
#endif

namespace DA_Assets.DAI
{
    public struct HelpBoxData
    {
        public string Message;
        public MessageType MessageType;
        public Action OnClick;
        public int FontSize;
        public Dictionary<string, string> Links;
    }

    public class CustomHelpBox : VisualElement
    {
        public CustomHelpBox(DAInspectorUITK uitk, HelpBoxData data)
        {
            var colorScheme = uitk.ColorScheme;
            var backgroundColor = new StyleColor(Color.clear);

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.backgroundColor = backgroundColor;
            style.position = Position.Relative;

            UIHelpers.SetDefaultRadius(this);
            UIHelpers.SetDefaultPadding(this);
            UIHelpers.SetBorderWidth(this, 1f);
            UIHelpers.SetBorderColor(this, colorScheme.OUTLINE);

            style.transitionProperty = new List<StylePropertyName> { "background-color" };
            style.transitionDuration = new List<TimeValue> { new TimeValue(DAI_UitkConstants.TransitionDuration, TimeUnit.Second) };
            style.transitionTimingFunction = new List<EasingFunction> { EasingMode.Ease };

            RegisterCallback<PointerEnterEvent>(evt => style.backgroundColor = backgroundColor);
            RegisterCallback<PointerLeaveEvent>(evt => style.backgroundColor = backgroundColor);
            RegisterCallback<PointerDownEvent>(evt => style.backgroundColor = backgroundColor);
            RegisterCallback<PointerUpEvent>(evt => style.backgroundColor = backgroundColor);
            this.AddManipulator(new ContextualMenuManipulator(evt =>
                evt.menu.AppendAction("Copy", _ => CopyRawContent(data))));

            var icon = new Image
            {
                image = GetIconForMessageType(data.MessageType),
                scaleMode = ScaleMode.ScaleToFit
            };

            icon.style.width = DAI_UitkConstants.IconSizeLarge;
            icon.style.height = DAI_UitkConstants.IconSizeLarge;
            icon.style.marginRight = DAI_UitkConstants.SpacingM;
            icon.style.flexShrink = 0;

            var label = new Label(data.Message);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = data.FontSize > 0 ? data.FontSize : DAI_UitkConstants.FontSizeNormal;
            label.style.color = colorScheme.TEXT;
            label.style.flexShrink = 1;
            label.enableRichText = true;
#if UNITY_2022_2_OR_NEWER
            label.selection.isSelectable = true;
#elif UNITY_2022_1_OR_NEWER
            label.isSelectable = true;
#endif
            RegisterLinkCallbacks(label, data);

            const float closeButtonSize = 16f; // DAI_UitkConstants.CloseButtonSize
            const float closeButtonInset = 4f; // DAI_UitkConstants.CloseButtonInset
            style.paddingRight = DAI_UitkConstants.MarginPadding + closeButtonSize + closeButtonInset;

            Add(icon);
            Add(label);

            var closeButton = new Button(() => style.display = DisplayStyle.None)
            {
                text = "x",
                tooltip = SharedLocKey.text_close.Localize(),
                style =
                {
                    position = Position.Absolute,
                    right = closeButtonInset,
                    top = closeButtonInset,
                    width = closeButtonSize,
                    height = closeButtonSize,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    backgroundColor = new StyleColor(uitk.ColorScheme.BUTTON),
                    color = new StyleColor(uitk.ColorScheme.TEXT_SECOND)
                }
            };

            UIHelpers.SetRadius(closeButton, DAI_UitkConstants.CornerRadiusSmall);
            UIHelpers.SetBorderWidth(closeButton, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(closeButton, uitk.ColorScheme.OUTLINE);
            closeButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            closeButton.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
            closeButton.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            Add(closeButton);

            if (data.OnClick != null)
            {
                this.AddManipulator(new Clickable(data.OnClick));
            }
        }

        private void CopyRawContent(HelpBoxData data)
        {
            EditorGUIUtility.systemCopyBuffer = BuildRawCopyText(data);
        }

        private string BuildRawCopyText(HelpBoxData data)
        {
            var text = new StringBuilder(data.Message ?? string.Empty);

            if (data.Links == null || data.Links.Count == 0)
            {
                return text.ToString();
            }

            if (text.Length > 0)
            {
                text.AppendLine();
                text.AppendLine();
            }

            text.AppendLine("Links:");

            foreach (var link in data.Links)
            {
                text.AppendLine($"{link.Key}: {link.Value}");
            }

            return text.ToString();
        }

        private Texture2D GetIconForMessageType(MessageType type)
        {
            string iconName;
            switch (type)
            {
                case MessageType.Info:
                    iconName = "console.infoicon";
                    break;
                case MessageType.Warning:
                    iconName = "console.warnicon";
                    break;
                case MessageType.Error:
                    iconName = "console.erroricon";
                    break;
                default:
                    iconName = "console.infoicon";
                    break;
            }

            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }

        private void RegisterLinkCallbacks(Label label, HelpBoxData data)
        {
            if (data.Links == null || data.Links.Count == 0)
            {
                return;
            }

#if UNITY_2022_2_OR_NEWER
            label.RegisterCallback<PointerDownLinkTagEvent>(evt => evt.StopPropagation());
            label.RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                if (data.Links.TryGetValue(evt.linkID, out string url))
                {
                    Application.OpenURL(url);
                }

                evt.StopPropagation();
            });
#else
            label.RegisterCallback<ClickEvent>(evt =>
            {
                OpenLink(data.Links);
                evt.StopPropagation();
            });
#endif
        }

        private void OpenLink(Dictionary<string, string> links)
        {
            if (links.Count == 1)
            {
                foreach (var link in links)
                {
                    if (!string.IsNullOrEmpty(link.Value))
                    {
                        Application.OpenURL(link.Value);
                    }
                }

                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (var link in links)
            {
                if (string.IsNullOrEmpty(link.Value))
                {
                    continue;
                }

                string url = link.Value;
                menu.AddItem(new GUIContent(string.IsNullOrEmpty(link.Key) ? url : link.Key), false, () => Application.OpenURL(url));
            }

            menu.ShowAsContext();
        }
    }
}
