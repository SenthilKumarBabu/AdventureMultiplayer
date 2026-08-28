using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    /// <summary>
    /// A drag-and-drop zone element for UI Toolkit with dashed border and plus icon.
    /// Dashed border is built from child VisualElements for guaranteed compatibility with Unity 2021.3+.
    /// </summary>
    public class DropZoneElement : VisualElement
    {
        public event Action<string[]> OnFilesDropped;

        private readonly Label _iconLabel;
        private readonly Label _textLabel;
        private Color _normalBorderColor;
        private readonly Color _hoverBorderColor;
        private Color _normalTextColor;
        private readonly Color _hoverTextColor;
        private string _acceptedExtension;

        private DAInspectorUITK _uitk;

        // Dash border elements (4 edge containers)
        private readonly List<VisualElement> _dashEdges = new List<VisualElement>();
        private const float DASH_LENGTH = 4f;
        private const float GAP_LENGTH = 3f;
        private const float BORDER_THICKNESS = 1f;

        public DropZoneElement(DAInspectorUITK uitk, string placeholderText = "Drop file here", string acceptedExtension = ".zip")
        {
            _uitk = uitk;
            _normalBorderColor = uitk.ColorScheme.OUTLINE;
            _hoverBorderColor = uitk.ColorScheme.GREEN;

            Color baseTextColor = uitk.ColorScheme.TEXT;
            _normalTextColor = new Color(baseTextColor.r, baseTextColor.g, baseTextColor.b, 0.75f);

            _hoverTextColor = uitk.ColorScheme.GREEN;
            _acceptedExtension = acceptedExtension;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.height = 80;
            style.marginTop = 5;
            style.marginBottom = 5;
            style.position = Position.Relative;

            // No standard borders
            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;
            style.borderTopLeftRadius = 0;
            style.borderTopRightRadius = 0;
            style.borderBottomLeftRadius = 0;
            style.borderBottomRightRadius = 0;

            // Background
            style.backgroundColor = uitk.ColorScheme.FCU_BG;

            // Build dashed border from child elements
            BuildDashedBorder();

            // Plus icon - inline with text
            _iconLabel = new Label("+")
            {
                style =
                {
                    fontSize = 16,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = _normalTextColor,
                    marginRight = 5,
                    marginBottom = 0
                }
            };
            Add(_iconLabel);

            // Placeholder text - inline with icon
            _textLabel = new Label(placeholderText)
            {
                style =
                {
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    color = _normalTextColor
                }
            };
            Add(_textLabel);

            // Register drag events
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragLeaveEvent>(OnDragLeave);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(e => OnDragPerform(e, acceptedExtension));

            // Register click event for file picker
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            // Rebuild dashes when size changes
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.newRect.width > 1f && evt.newRect.height > 1f)
                RebuildDashes(evt.newRect.width, evt.newRect.height);
        }

        // Creates 4 edge containers positioned absolutely along each edge.
        private void BuildDashedBorder()
        {
            // Top edge
            var top = CreateEdgeContainer();
            top.style.top = 0;
            top.style.left = 0;
            top.style.right = 0;
            top.style.height = BORDER_THICKNESS;
            top.style.flexDirection = FlexDirection.Row;
            _dashEdges.Add(top);
            Add(top);

            // Bottom edge
            var bottom = CreateEdgeContainer();
            bottom.style.bottom = 0;
            bottom.style.left = 0;
            bottom.style.right = 0;
            bottom.style.height = BORDER_THICKNESS;
            bottom.style.flexDirection = FlexDirection.Row;
            _dashEdges.Add(bottom);
            Add(bottom);

            // Left edge
            var left = CreateEdgeContainer();
            left.style.top = BORDER_THICKNESS;
            left.style.bottom = BORDER_THICKNESS;
            left.style.left = 0;
            left.style.width = BORDER_THICKNESS;
            left.style.flexDirection = FlexDirection.Column;
            _dashEdges.Add(left);
            Add(left);

            // Right edge
            var right = CreateEdgeContainer();
            right.style.top = BORDER_THICKNESS;
            right.style.bottom = BORDER_THICKNESS;
            right.style.right = 0;
            right.style.width = BORDER_THICKNESS;
            right.style.flexDirection = FlexDirection.Column;
            _dashEdges.Add(right);
            Add(right);
        }

        private VisualElement CreateEdgeContainer()
        {
            return new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    overflow = Overflow.Hidden
                }
            };
        }

        // Populates each edge container with dash segments
        private void RebuildDashes(float totalWidth, float totalHeight)
        {
            // Horizontal edges (top, bottom)
            float hLen = totalWidth;
            for (int i = 0; i < 2; i++)
            {
                FillEdge(_dashEdges[i], hLen, true);
            }

            // Vertical edges (left, right) - subtract corners
            float vLen = totalHeight - BORDER_THICKNESS * 2;
            for (int i = 2; i < 4; i++)
            {
                FillEdge(_dashEdges[i], vLen, false);
            }
        }

        private void FillEdge(VisualElement edge, float length, bool horizontal)
        {
            edge.Clear();
            float pos = 0;
            bool isDash = true;

            while (pos < length)
            {
                float segLen = isDash ? DASH_LENGTH : GAP_LENGTH;
                segLen = Mathf.Min(segLen, length - pos);

                if (isDash)
                {
                    var dash = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore,
                        style =
                        {
                            backgroundColor = _normalBorderColor
                        }
                    };

                    if (horizontal)
                    {
                        dash.style.width = segLen;
                        dash.style.height = BORDER_THICKNESS;
                    }
                    else
                    {
                        dash.style.width = BORDER_THICKNESS;
                        dash.style.height = segLen;
                    }

                    edge.Add(dash);
                }
                else
                {
                    // Gap - transparent spacer
                    var gap = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore
                    };

                    if (horizontal)
                    {
                        gap.style.width = segLen;
                        gap.style.height = BORDER_THICKNESS;
                    }
                    else
                    {
                        gap.style.width = BORDER_THICKNESS;
                        gap.style.height = segLen;
                    }

                    edge.Add(gap);
                }

                pos += segLen;
                isDash = !isDash;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0) // Only left click
                return;

            string extensionFilter = string.IsNullOrEmpty(_acceptedExtension)
                ? ""
                : _acceptedExtension.TrimStart('.');

            string filterDisplay = string.IsNullOrEmpty(extensionFilter)
                ? "All files"
                : $"{extensionFilter.ToUpper()} files";

            string filter = string.IsNullOrEmpty(extensionFilter)
                ? ""
                : $"{filterDisplay},*.{extensionFilter}";

            string path = EditorUtility.OpenFilePanel("Select file", "", extensionFilter);

            if (!string.IsNullOrEmpty(path))
            {
                OnFilesDropped?.Invoke(new string[] { path });
            }
        }

        /// <summary>
        /// Sets the border color for the dashed border.
        /// </summary>
        public void SetBorderColor(Color color)
        {
            _normalBorderColor = color;
            UpdateDashColors(color);
        }

        private void UpdateDashColors(Color color)
        {
            foreach (var edge in _dashEdges)
            {
                foreach (var child in edge.Children())
                {
                    // Only dashes have backgroundColor set, gaps are transparent
                    if (child.resolvedStyle.backgroundColor.a > 0)
                        child.style.backgroundColor = color;
                }
            }
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            SetHighlight(true);
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            SetHighlight(false);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }

        private void OnDragPerform(DragPerformEvent evt, string acceptedExtension)
        {
            SetHighlight(false);

            string[] paths = DragAndDrop.paths;
            if (paths == null || paths.Length == 0)
                return;

            // Filter by extension if specified
            if (!string.IsNullOrEmpty(acceptedExtension))
            {
                var filteredPaths = new System.Collections.Generic.List<string>();
                foreach (string path in paths)
                {
                    if (path.EndsWith(acceptedExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        filteredPaths.Add(path);
                    }
                }
                paths = filteredPaths.ToArray();
            }

            if (paths.Length > 0)
            {
                DragAndDrop.AcceptDrag();
                OnFilesDropped?.Invoke(paths);
            }
        }

        private void SetHighlight(bool highlight)
        {
            Color borderColor = highlight ? _hoverBorderColor : _normalBorderColor;
            Color textColor = highlight ? _hoverTextColor : _normalTextColor;

            UpdateDashColors(borderColor);
            _iconLabel.style.color = textColor;
            _textLabel.style.color = textColor;

            style.backgroundColor = highlight
                ? new Color(_hoverBorderColor.r, _hoverBorderColor.g, _hoverBorderColor.b, 0.2f)
                : _uitk.ColorScheme.FCU_BG;
        }

        public void SetDroppedFile(string fileName)
        {
            _textLabel.text = fileName;
            _iconLabel.text = "✓";
            _iconLabel.style.color = _uitk.ColorScheme.GREEN;
        }

        public void Reset(string placeholderText = "Drop file here")
        {
            _textLabel.text = placeholderText;
            _iconLabel.text = "+";
            _iconLabel.style.color = _normalTextColor;
        }
    }
}
