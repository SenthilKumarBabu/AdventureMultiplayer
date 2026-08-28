// Generic Unity Editor window that displays a scrollable list with search/filter support.
// Optimized for large datasets using virtualized rendering.
// Allows filtering items by a nested field/property path like "Object.Name".

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    public delegate void DrawItem<T>(T item);

    public class InfinityScrollRectWindow<T> : VisualElement
    {
        // Custom inspector styling/utilities
        [SerializeField] DAInspector gui;
        [SerializeField] DAInspectorUITK uitk;

        // Cached item array to display
        private T[] _items;

        // Current scroll offset
        private Vector2 _scrollPosition;

        // Number of items visible at once
        protected int _visibleItemCount;
        private readonly int _maxVisibleItemCount;

        // Height of each item
        protected float _itemHeight;

        // Total scrollable height based on item count
        private float _totalScrollHeight;

        // Height of the scroll viewport
        private float _visibleAreaHeight;

        // Callback to render individual items
        private DrawItem<T> _drawItem;

        // Dot-delimited field/property path for filtering
        private readonly string _filterPath;

        // Getter function for extracting the filter value
        private Func<T, object> _valueGetter;

        // Current search input
        private string _searchText = string.Empty;

        // Min items before search UI is shown
        private int _searchAppearItemsCount = DAI_UitkConstants.SearchAppearCount;

        private readonly VisualElement _searchRow;
        private readonly TextField _searchField;
        private readonly Button _clearButton;
        private readonly IMGUIContainer _imguiContainer;

        public Action OnGUIChanged { get; set; }

        // Constructor sets layout parameters and resolves filter accessor
        public InfinityScrollRectWindow(
            int visibleItemCount, 
            float itemHeight, 
            DAInspector gui, 
            DAInspectorUITK uitk = null,
            string filterFieldPath = "name")
        {
            this.gui = gui;
            this.uitk = uitk;
            _maxVisibleItemCount = Mathf.Max(1, visibleItemCount);
            _visibleItemCount = _maxVisibleItemCount;
            _itemHeight = itemHeight;
            _filterPath = filterFieldPath;

            style.flexDirection = FlexDirection.Column;
            style.backgroundColor = Color.clear;

            _searchRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };

            var searchLabel = new Label("Search")
            {
                style =
                {
                    marginRight = DAI_UitkConstants.SpacingXS
                }
            };

            if (this.uitk != null)
            {
                searchLabel.style.color = this.uitk.ColorScheme.TEXT_SECOND;
            }

            _searchRow.Add(searchLabel);

            _searchField = CreateSearchField();
            _searchRow.Add(_searchField);

            _clearButton = CreateClearButton();
            _searchRow.Add(_clearButton);
            Add(_searchRow);

            _imguiContainer = new IMGUIContainer(DrawListGUI)
            {
                style =
                {
                    backgroundColor = Color.clear
                }
            };
            Add(_imguiContainer);

            // Tries to create a value getter for the target path — disables search if path is invalid
            if (MemberPathCache.TryGetOrCreate(typeof(T), _filterPath, out var accessor))
            {
                _valueGetter = (T item) => accessor.GetValue(item);
            }
            else
            {
                _valueGetter = null;
            }

            UpdateSearchVisibility();
        }

        // Assigns item data and drawing logic — recalculates layout
        public void SetData(IEnumerable<T> items, DrawItem<T> drawItem)
        {
            _drawItem = drawItem;
            _items = items?.ToArray() ?? Array.Empty<T>();

            _visibleItemCount = Mathf.Min(_maxVisibleItemCount, _items.Length);

            _visibleAreaHeight = _visibleItemCount * _itemHeight;
            _totalScrollHeight = _items.Length * _itemHeight;
            _imguiContainer.style.height = Mathf.Max(_visibleAreaHeight, _itemHeight);
            UpdateSearchVisibility();
            _imguiContainer.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            _imguiContainer.MarkDirtyRepaint();
        }

        private TextField CreateSearchField()
        {
            var field = new TextField
            {
                multiline = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 50,
                    flexBasis = 0,
                    height = 26,
                    backgroundColor = Color.clear,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap
                }
            };

            field.ClearClassList();

            var input = field.Q<VisualElement>(null, "unity-text-field__input") ?? field.Q<VisualElement>(null, "unity-text-input");
            input?.ClearClassList();

            if (input != null)
            {
                input.style.backgroundColor = Color.clear;
                input.style.height = Length.Percent(100);
                input.style.unityTextAlign = TextAnchor.MiddleLeft;
                input.style.paddingLeft = 6;
                input.style.paddingRight = 6;
                input.style.marginLeft = 0;
                input.style.marginRight = 0;
                input.style.overflow = Overflow.Hidden;
            }

            UIHelpers.SetRadius(field, 0);
            UIHelpers.SetBorderWidth(field, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(field, GetOutlineColor());
            UIHelpers.SetZeroMarginPadding(field);

            field.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue ?? string.Empty;
                _scrollPosition = Vector2.zero;
                _imguiContainer.MarkDirtyRepaint();
            });

            return field;
        }

        private Button CreateClearButton()
        {
            var button = new Button(ClearSearch)
            {
                text = "✕",
                style =
                {
                    width = 26,
                    minWidth = 26,
                    height = 26,
                    marginLeft = DAI_UitkConstants.SpacingXXS,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    backgroundColor = Color.clear
                }
            };

            if (uitk != null)
            {
                button.style.color = uitk.ColorScheme.TEXT_SECOND;
            }

            UIHelpers.SetRadius(button, 0);
            UIHelpers.SetBorderWidth(button, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(button, GetOutlineColor());

            return button;
        }

        private Color GetOutlineColor()
        {
            if (uitk != null)
            {
                return uitk.ColorScheme.OUTLINE;
            }

            return EditorGUIUtility.isProSkin
                ? new Color32(64, 64, 64, 255)
                : new Color32(168, 168, 168, 255);
        }

        private void ClearSearch()
        {
            _searchText = string.Empty;
            _searchField.SetValueWithoutNotify(string.Empty);
            _searchField.Blur();
            _scrollPosition = Vector2.zero;
            _imguiContainer.MarkDirtyRepaint();
        }

        private void UpdateSearchVisibility()
        {
            bool canSearch = _valueGetter != null;
            bool showSearch = canSearch && _items != null && _items.Length >= _searchAppearItemsCount;
            _searchRow.style.display = showSearch ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Renders the scroll virtualization body inside the internal IMGUI container.
        private void DrawListGUI()
        {
            bool wasChanged = GUI.changed;
            GUI.changed = false;

            if (_items == null || _items.Length < 1)
            {
                GUILayout.Label("No data.");
                FinishGuiFrame(wasChanged);
                return;
            }

            if (_drawItem == null)
            {
                GUILayout.Label("DrawItem is missing.");
                FinishGuiFrame(wasChanged);
                return;
            }

            T[] targetItems = _items;

            // Apply filtering if search is active and accessor is valid
            bool canSearch = _valueGetter != null;
            if (canSearch && !string.IsNullOrWhiteSpace(_searchText))
            {
                string q = _searchText.Trim();

                targetItems = _items.Where(item =>
                {
                    var v = _valueGetter(item);

                    if (v == null) 
                        return false;

                    string s = v.ToString();

                    if (string.IsNullOrEmpty(s))
                        return false;

                    return s.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) >= 0;
                }).ToArray();
            }

            if (targetItems.Length == 0)
            {
                GUILayout.Label("Nothing matches.");
                FinishGuiFrame(wasChanged);
                return;
            }

            _totalScrollHeight = targetItems.Length * _itemHeight;

            gui.Colorize(() =>
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(_visibleAreaHeight));
            });

            // Calculate which items to draw based on scroll position
            float currentScrollPos = _scrollPosition.y;
            int startIndex = Mathf.Max(0, (int)(currentScrollPos / _itemHeight));
            int endIndex = Mathf.Min(targetItems.Length, startIndex + _visibleItemCount + DAI_UitkConstants.VirtualScrollExtra); // extra padding

            GUILayout.BeginVertical();
            GUILayout.Space(startIndex * _itemHeight); // offset before first visible item

            for (int i = startIndex; i < endIndex; i++)
                _drawItem(targetItems[i]);

            GUILayout.Space(_totalScrollHeight - endIndex * _itemHeight); // bottom padding
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            FinishGuiFrame(wasChanged);
        }

        private void FinishGuiFrame(bool previousChangedState)
        {
            if (GUI.changed)
            {
                OnGUIChanged?.Invoke();
            }

            GUI.changed |= previousChangedState;
        }
    }

    // Utility that resolves a nested property/field access chain from a dot-path like "Object.Name".
    // Supports any depth and mix of fields/properties. Uses reflection.
    internal sealed class MemberPathAccessor
    {
        private readonly MemberInfo[] _members; // Sequence of field/property infos to walk

        private MemberPathAccessor(MemberInfo[] members)
        {
            _members = members;
        }

        // Attempts to parse a dot-delimited path (e.g. "Object.Name") from a given root type
        public static bool TryCreate(Type rootType, string path, out MemberPathAccessor accessor)
        {
            accessor = null;
            if (string.IsNullOrWhiteSpace(path)) return false;

            var parts = path.Split('.');
            var members = new List<MemberInfo>(parts.Length);
            var type = rootType;

            const BindingFlags flags = 
                  BindingFlags.Instance | 
                  BindingFlags.Public | 
                  BindingFlags.NonPublic |
                  BindingFlags.FlattenHierarchy | 
                  BindingFlags.IgnoreCase;

            // Walk through each path segment and resolve field/property info
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) return false;

                var prop = type.GetProperty(part, flags);
                if (prop != null)
                {
                    members.Add(prop);
                    type = prop.PropertyType;
                    continue;
                }

                var field = type.GetField(part, flags);
                if (field != null)
                {
                    members.Add(field);
                    type = field.FieldType;
                    continue;
                }

                return false; // Segment not found
            }

            accessor = new MemberPathAccessor(members.ToArray());
            return true;
        }

        // Retrieves the final value from the root object by traversing all members
        public object GetValue(object root)
        {
            var current = root;

            for (int i = 0; i < _members.Length; i++)
            {
                if (current == null)
                    return null;

                switch (_members[i])
                {
                    case PropertyInfo p:
                        current = p.GetValue(current, null);
                        break;
                    case FieldInfo f:
                        current = f.GetValue(current);
                        break;
                    default:
                        return null;
                }
            }
            return current;
        }
    }

    // Simple static cache for storing resolved MemberPathAccessor instances.
    // Reduces overhead by avoiding redundant reflection lookups for the same path/type.
    internal static class MemberPathCache
    {
        private static readonly Dictionary<(Type, string), MemberPathAccessor> Cache =
            new Dictionary<(Type, string), MemberPathAccessor>();

        // Retrieves existing accessor or builds one if not cached
        public static bool TryGetOrCreate(Type rootType, string path, out MemberPathAccessor accessor)
        {
            var key = (rootType, path);
            if (Cache.TryGetValue(key, out accessor))
                return true;

            if (MemberPathAccessor.TryCreate(rootType, path, out accessor))
            {
                Cache[key] = accessor;
                return true;
            }

            return false; // Could not resolve path
        }
    }
}
