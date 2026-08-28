using System;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.UI
{
    [AddComponentMenu("UI/Effects/Figma Layout Grid")]
    public class FigmaLayoutGrid : BaseMeshEffect
    {
        [SerializeField] LayoutGridPattern pattern;
        public LayoutGridPattern Pattern { get => pattern; set => pattern = value; }

        [SerializeField] float sectionSize;
        public float SectionSize { get => sectionSize; set => sectionSize = value; }

        [SerializeField] bool visible = true;
        public bool Visible { get => visible; set => visible = value; }

        [SerializeField] Color32 color = new Color32(255, 0, 0, 25);
        public Color32 Color { get => color; set => color = value; }

        [SerializeField] LayoutGridAlignment alignment;
        public LayoutGridAlignment Alignment { get => alignment; set => alignment = value; }

        [SerializeField] float gutterSize;
        public float GutterSize { get => gutterSize; set => gutterSize = value; }

        [SerializeField] float offset;
        public float Offset { get => offset; set => offset = value; }

        [SerializeField] int count = -1;
        public int Count { get => count; set => count = value; }

        private const float LINE_THICKNESS = 1f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || graphic == null || !visible)
                return;

            Rect rect = graphic.rectTransform.rect;

            switch (pattern)
            {
                case LayoutGridPattern.GRID:
                    DrawGrid(vh, rect);
                    break;
                case LayoutGridPattern.ROWS:
                    DrawRows(vh, rect);
                    break;
                case LayoutGridPattern.COLUMNS:
                    DrawColumns(vh, rect);
                    break;
            }
        }

        private void DrawGrid(VertexHelper vh, Rect rect)
        {
            if (sectionSize <= 0)
                return;

            // Draw vertical lines
            for (float x = rect.xMin; x <= rect.xMax; x += sectionSize)
            {
                AddLine(vh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), LINE_THICKNESS, color);
            }

            // Draw horizontal lines (start from top like Figma)
            for (float y = rect.yMax; y >= rect.yMin; y -= sectionSize)
            {
                AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), LINE_THICKNESS, color);
            }
        }

        private void DrawRows(VertexHelper vh, Rect rect)
        {
            float[] positions = CalculateStripPositions(rect.height, rect.yMin, rect.yMax, true);
            
            foreach (float y in positions)
            {
                AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), LINE_THICKNESS, color);
            }
        }

        private void DrawColumns(VertexHelper vh, Rect rect)
        {
            float[] positions = CalculateStripPositions(rect.width, rect.xMin, rect.xMax, false);
            
            foreach (float x in positions)
            {
                AddLine(vh, new Vector2(x, rect.yMin), new Vector2(x, rect.yMax), LINE_THICKNESS, color);
            }
        }

        private float[] CalculateStripPositions(float totalSize, float min, float max, bool isVertical)
        {
            System.Collections.Generic.List<float> positions = new System.Collections.Generic.List<float>();
            
            int actualCount = count == -1 ? CalculateAutoCount(totalSize) : count;
            
            if (actualCount <= 0)
                return positions.ToArray();

            float actualSectionSize = sectionSize;

            switch (alignment)
            {
                case LayoutGridAlignment.MIN:
                    {
                        // MIN = top/left in Figma. For vertical (ROWS): yMax = top. For horizontal (COLUMNS): xMin = left
                        float pos = isVertical ? (max - offset) : (min + offset);
                        for (int i = 0; i < actualCount; i++)
                        {
                            positions.Add(pos);
                            if (actualSectionSize > 0)
                            {
                                positions.Add(pos - (isVertical ? actualSectionSize : -actualSectionSize));
                            }
                            pos -= isVertical ? (actualSectionSize + gutterSize) : -(actualSectionSize + gutterSize);
                        }
                    }
                    break;

                case LayoutGridAlignment.MAX:
                    {
                        // MAX = bottom/right in Figma. For vertical (ROWS): yMin = bottom. For horizontal (COLUMNS): xMax = right
                        float pos = isVertical ? (min + offset) : (max - offset);
                        for (int i = 0; i < actualCount; i++)
                        {
                            positions.Add(pos);
                            if (actualSectionSize > 0)
                            {
                                positions.Add(pos + (isVertical ? actualSectionSize : -actualSectionSize));
                            }
                            pos += isVertical ? (actualSectionSize + gutterSize) : -(actualSectionSize + gutterSize);
                        }
                    }
                    break;

                case LayoutGridAlignment.STRETCH:
                    {
                        actualSectionSize = (totalSize - gutterSize * (actualCount - 1)) / actualCount;
                        float pos = min;
                        for (int i = 0; i < actualCount; i++)
                        {
                            positions.Add(pos);
                            positions.Add(pos + actualSectionSize);
                            pos += actualSectionSize + gutterSize;
                        }
                    }
                    break;

                case LayoutGridAlignment.CENTER:
                    {
                        float totalContentSize = actualSectionSize * actualCount + gutterSize * (actualCount - 1);
                        float startPos = (totalSize - totalContentSize) / 2f + min;
                        float pos = startPos;
                        for (int i = 0; i < actualCount; i++)
                        {
                            positions.Add(pos);
                            positions.Add(pos + actualSectionSize);
                            pos += actualSectionSize + gutterSize;
                        }
                    }
                    break;
            }

            return positions.ToArray();
        }

        private int CalculateAutoCount(float totalSize)
        {
            if (sectionSize <= 0)
                return 0;

            float availableSize = totalSize - offset * 2;
            return Mathf.Max(1, Mathf.FloorToInt(availableSize / (sectionSize + gutterSize)));
        }

        private void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color32 color)
        {
            Vector2 dir = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

            int idx = vh.currentVertCount;
            vh.AddVert(a + perp, color, Vector4.zero);
            vh.AddVert(a - perp, color, Vector4.zero);
            vh.AddVert(b - perp, color, Vector4.zero);
            vh.AddVert(b + perp, color, Vector4.zero);
            
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }
    }
}
