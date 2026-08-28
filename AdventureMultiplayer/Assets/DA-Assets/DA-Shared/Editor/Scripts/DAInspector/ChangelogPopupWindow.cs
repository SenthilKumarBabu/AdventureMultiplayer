using DA_Assets.DAI;
using DA_Assets.UpdateChecker.Models;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_2022_2_OR_NEWER
using UnityEngine.UIElements.Experimental;
#endif

namespace DA_Assets.UpdateChecker
{
    internal sealed class ChangelogPopupWindow : EditorWindow
    {
        private const float PopupWidth = 380f;
        private const float MaxPopupHeight = 420f;
        private const float MinPopupHeight = 112f;

        private static ChangelogPopupWindow _current;

        private DAInspectorUITK _uitk;
        private AssetVersion _version;
        private string _statusText;
        private bool _hasPointerEnteredPopup;

        public static void Show(Rect screenRect, DAInspectorUITK uitk, AssetVersion version, string statusText)
        {
            if (_current != null)
            {
                _current.Close();
            }

            _current = CreateInstance<ChangelogPopupWindow>();
            _current._uitk = uitk;
            _current._version = version;
            _current._statusText = statusText;
            Vector2 size = new Vector2(PopupWidth, EstimateHeight(version));
            _current.position = GetPopupPosition(screenRect, size);
            _current.ShowPopup();
            _current.Focus();
            EditorApplication.update += _current.UpdateHoverClose;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateHoverClose;

            if (_current == this)
            {
                _current = null;
            }
        }

        private void OnLostFocus()
        {
            if (_hasPointerEnteredPopup && EditorWindow.mouseOverWindow != this)
            {
                Close();
            }
        }

        public void CreateGUI()
        {
            Build();
        }

        private void Build()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            StyleRoot(root);
            root.RegisterCallback<PointerEnterEvent>(_ => _hasPointerEnteredPopup = true);

            root.Add(CreateHeader());

            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto,
                style =
                {
                    flexGrow = 1,
                    marginTop = 8
                }
            };

            if (HasStructuredChangelog(_version.Changelog))
            {
                AddSection(scroll, "notes", "Notes", "i", _version.Changelog.Notes, new Color(0.26f, 0.28f, 0.32f, 1f), new Color(0.84f, 0.87f, 0.91f, 1f));
                AddSection(scroll, "added", "Added", "+", _version.Changelog.Added, new Color(0.08f, 0.42f, 0.22f, 1f), new Color(0.28f, 1f, 0.58f, 1f));
                AddSection(scroll, "changed", "Changed", "~", _version.Changelog.Changed, new Color(0.42f, 0.32f, 0.08f, 1f), new Color(1f, 0.86f, 0.24f, 1f));
                AddSection(scroll, "deprecated", "Deprecated", "!", _version.Changelog.Deprecated, new Color(0.42f, 0.24f, 0.08f, 1f), new Color(1f, 0.62f, 0.24f, 1f));
                AddSection(scroll, "removed", "Removed", "-", _version.Changelog.Removed, new Color(0.44f, 0.12f, 0.12f, 1f), new Color(1f, 0.46f, 0.46f, 1f));
                AddSection(scroll, "fixed", "Fixed", "*", _version.Changelog.Fixed, new Color(0.10f, 0.28f, 0.46f, 1f), new Color(0.44f, 0.74f, 1f, 1f));
                AddSection(scroll, "security", "Security", "#", _version.Changelog.Security, new Color(0.32f, 0.16f, 0.46f, 1f), new Color(0.78f, 0.55f, 1f, 1f));
                AddSection(scroll, "knownIssues", "Known Issues", "?", _version.Changelog.KnownIssues, new Color(0.50f, 0.24f, 0.08f, 1f), new Color(1f, 0.68f, 0.39f, 1f));
            }
            else
            {
                scroll.Add(CreateDescriptionFallback());
            }

            root.Add(scroll);
        }

        private VisualElement CreateHeader()
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var title = new Label(_statusText)
            {
                style =
                {
                    flexGrow = 1,
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new StyleColor(_uitk.ColorScheme.TEXT)
                }
            };
            header.Add(title);

            if (!string.IsNullOrEmpty(_version.ReleaseDate))
            {
                var date = new Label(_version.ReleaseDate)
                {
                    style =
                    {
                        marginRight = 8,
                        color = new StyleColor(_uitk.ColorScheme.TEXT_SECOND),
                        fontSize = 11
                    }
                };
                header.Add(date);
            }

            var close = new Button(Close) { text = "x" };
            StyleCloseButton(close);
            header.Add(close);

            return header;
        }

        private void AddSection(VisualElement root, string className, string title, string icon, List<ChangelogItem> items, Color bg, Color fg)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = CreateItemLabel(className, icon, title, fg, items[i]);
                item.style.marginTop = 10;
                root.Add(item);
            }
        }

        private Label CreateItemLabel(string className, string icon, string title, Color labelColor, ChangelogItem item)
        {
            string text = string.IsNullOrEmpty(item.RichText) ? item.Description : item.RichText;
            string prefixColor = ColorUtility.ToHtmlStringRGB(labelColor);
            string prefix = $"<color=#{prefixColor}><b>[{icon} {title}]</b></color> ";
            var label = new Label(prefix + PrepareRichText(text))
            {
                enableRichText = true,
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = 11,
                    color = new StyleColor(_uitk.ColorScheme.TEXT),
                    marginRight = 4,
                    minWidth = 0,
                    width = Length.Percent(100)
                }
            };

            label.AddToClassList("changelog-" + className);
            RegisterLinkCallbacks(label, item.Links);
            return label;
        }

        private Label CreateDescriptionFallback()
        {
            var label = new Label(string.IsNullOrEmpty(_version.Description) ? "No changelog details." : _version.Description)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    color = new StyleColor(_uitk.ColorScheme.TEXT),
                    fontSize = 11,
                    minWidth = 0,
                    width = Length.Percent(100)
                }
            };
            return label;
        }

        private static bool HasStructuredChangelog(ChangelogRelease changelog)
        {
            return HasItems(changelog.Added) ||
                   HasItems(changelog.Changed) ||
                   HasItems(changelog.Deprecated) ||
                   HasItems(changelog.Removed) ||
                   HasItems(changelog.Fixed) ||
                   HasItems(changelog.Security) ||
                   HasItems(changelog.Notes) ||
                   HasItems(changelog.KnownIssues);
        }

        private static bool HasItems(List<ChangelogItem> items)
        {
            return items != null && items.Count > 0;
        }

        private static string PrepareRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("<code>", "<color=#FF9D00><b>")
                .Replace("</code>", "</b></color>");
        }

        private static void RegisterLinkCallbacks(Label label, List<ChangelogLink> links)
        {
            if (links == null || links.Count == 0)
            {
                return;
            }

#if UNITY_2022_2_OR_NEWER
            label.RegisterCallback<PointerDownLinkTagEvent>(evt => evt.StopPropagation());
            label.RegisterCallback<PointerUpLinkTagEvent>(evt =>
            {
                string url = GetUrl(links, evt.linkID);
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }

                evt.StopPropagation();
            });
#else
            label.RegisterCallback<ClickEvent>(evt =>
            {
                OpenLink(links);
                evt.StopPropagation();
            });
#endif
        }

        private static void OpenLink(List<ChangelogLink> links)
        {
            if (links.Count == 1)
            {
                string url = links[0].Url;
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }

                return;
            }

            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < links.Count; i++)
            {
                string url = links[i].Url;
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                string id = links[i].Id;
                string linkUrl = url;
                menu.AddItem(new GUIContent(string.IsNullOrEmpty(id) ? linkUrl : id), false, () => Application.OpenURL(linkUrl));
            }

            menu.ShowAsContext();
        }

        private static string GetUrl(List<ChangelogLink> links, string id)
        {
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].Id == id)
                {
                    return links[i].Url;
                }
            }

            return null;
        }

        private static Rect GetPopupPosition(Rect anchor, Vector2 size)
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float x = anchor.xMax - size.x;
            float y = anchor.yMin - size.y - 6f;

          //  if (y < main.yMin + ScreenMargin)
          //  {
          //      y = anchor.yMax + 6f;
        //    }

          //  x = Mathf.Clamp(x, main.xMin + ScreenMargin, main.xMax - size.x - ScreenMargin);
       //     y = Mathf.Clamp(y, main.yMin + ScreenMargin, main.yMax - size.y - ScreenMargin);

            return new Rect(x, y, size.x, size.y);
        }

        private void UpdateHoverClose()
        {
            if (!_hasPointerEnteredPopup)
            {
                return;
            }

            if (EditorWindow.mouseOverWindow != this)
            {
                Close();
            }
        }

        private static float EstimateHeight(AssetVersion version)
        {
            float height = 58f;

            if (HasStructuredChangelog(version.Changelog))
            {
                height += EstimateSectionHeight(version.Changelog.Added);
                height += EstimateSectionHeight(version.Changelog.Changed);
                height += EstimateSectionHeight(version.Changelog.Deprecated);
                height += EstimateSectionHeight(version.Changelog.Removed);
                height += EstimateSectionHeight(version.Changelog.Fixed);
                height += EstimateSectionHeight(version.Changelog.Security);
                height += EstimateSectionHeight(version.Changelog.Notes);
                height += EstimateSectionHeight(version.Changelog.KnownIssues);
            }
            else
            {
                height += EstimateTextHeight(version.Description);
            }

            return Mathf.Clamp(height, MinPopupHeight, MaxPopupHeight);
        }

        private static float EstimateSectionHeight(List<ChangelogItem> items)
        {
            if (items == null || items.Count == 0)
            {
                return 0f;
            }

            float height = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                string text = string.IsNullOrEmpty(items[i].RichText) ? items[i].Description : items[i].RichText;
                height += EstimateTextHeight(text) + 5f;
            }

            return height;
        }

        private static float EstimateTextHeight(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 18f;
            }

            int lineBreaks = text.Split('\n').Length - 1;
            int softLines = Mathf.CeilToInt(text.Length / 58f);
            int lines = Mathf.Max(1, softLines + lineBreaks);
            return lines * 15f;
        }

        private void StyleRoot(VisualElement root)
        {
            root.style.backgroundColor = _uitk.ColorScheme.BG;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
        }

        private void StyleCloseButton(Button close)
        {
            close.style.width = 22;
            close.style.height = 20;
            close.style.fontSize = 10;
            close.style.paddingLeft = 0;
            close.style.paddingRight = 0;
            close.style.paddingTop = 0;
            close.style.paddingBottom = 0;
            close.style.unityTextAlign = TextAnchor.MiddleCenter;
            close.style.backgroundColor = new StyleColor(_uitk.ColorScheme.BUTTON);
            close.style.color = new StyleColor(_uitk.ColorScheme.TEXT_SECOND);
            UIHelpers.SetRadius(close, 4f);
            UIHelpers.SetBorderWidth(close, 1f);
            UIHelpers.SetBorderColor(close, _uitk.ColorScheme.OUTLINE);
        }
    }
}
