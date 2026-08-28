#if UNITY_2021_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace DA_Assets.ImgOverflow
{
    [AddComponentMenu("UI/Image Overflow")]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    public class ImageOverflow : BaseMeshEffect
    {
        private const float Epsilon = 0.0001f;

        public Image Image => GetComponent<Image>();

        [SerializeField] Borders _areaAnchors = new Borders(0.1f, 0.1f, 0.1f, 0.1f);
        public Borders AreaAnchors
        {
            get => _areaAnchors;
            set
            {
                bool changed = !_areaAnchors.Equals(value);
                _areaAnchors = value;

                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }

                if (_debugMode)
                {
                    DebugValues();
                }

                if (changed)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }
            }
        }

        public bool HasOverflow =>
            _areaAnchors.left > Epsilon ||
            _areaAnchors.right > Epsilon ||
            _areaAnchors.top > Epsilon ||
            _areaAnchors.bottom > Epsilon;

        [SerializeField] GizmosColor _gizmoColor;
        public GizmosColor GizmosColor => _gizmoColor;

        [Header("Converted Values in Percent")]
        [SerializeField] float _leftPercent;
        [SerializeField] float _rightPercent;
        [SerializeField] float _topPercent;
        [SerializeField] float _bottomPercent;

        [Header("Converted Values in Pixels")]
        [SerializeField] float _leftPixels;
        [SerializeField] float _rightPixels;
        [SerializeField] float _topPixels;
        [SerializeField] float _bottomPixels;

        [Space]
        [SerializeField] bool _passEqualCheck;

        [SerializeField] bool _debugMode;
        public bool DebugMode => _debugMode;

        private void DebugValues()
        {
            Image img = GetComponent<Image>();
            Rect rect = img.GetPixelAdjustedRect();
            float totalWidth = rect.width;
            float totalHeight = rect.height;

            _leftPixels = PercentToPixels(_areaAnchors.left, totalWidth);
            _topPixels = PercentToPixels(_areaAnchors.top, totalHeight);
            _rightPixels = PercentToPixels(_areaAnchors.right, totalWidth);
            _bottomPixels = PercentToPixels(_areaAnchors.bottom, totalHeight);

            _leftPercent = PixelsToPercent(_leftPixels, totalWidth);
            _topPercent = PixelsToPercent(_topPixels, totalHeight);
            _rightPercent = PixelsToPercent(_rightPixels, totalWidth);
            _bottomPercent = PixelsToPercent(_bottomPixels, totalHeight);

            if (_leftPercent == _areaAnchors.left &&
                _topPercent == _areaAnchors.top &&
                _rightPercent == _areaAnchors.right &&
                _bottomPercent == _areaAnchors.bottom)
            {
                _passEqualCheck = true;
            }
            else
            {
                _passEqualCheck = false;
            }
        }

        public float PixelsToPercent(float pixels, float total)
        {
            return pixels / total;
        }

        public float PercentToPixels(float percent, float total)
        {
            return percent * total;
        }

        public void ClearOverflow()
        {
            AreaAnchors = new Borders(0f, 0f, 0f, 0f);
        }

        public void SetOverflowPixels(float left, float right, float top, float bottom)
        {
            Rect rect = Image.GetPixelAdjustedRect();
            SetOverflowPixels(left, right, top, bottom, rect.size);
        }

        public void SetOverflowPixels(float left, float right, float top, float bottom, Vector2 rectSize)
        {
            float width = Mathf.Max(Epsilon, rectSize.x);
            float height = Mathf.Max(Epsilon, rectSize.y);

            AreaAnchors = new Borders(
                Mathf.Max(0f, left) / width,
                Mathf.Max(0f, right) / width,
                Mathf.Max(0f, top) / height,
                Mathf.Max(0f, bottom) / height);
        }

        public void SetOverflowPixels(Borders pixels)
        {
            SetOverflowPixels(pixels.left, pixels.right, pixels.top, pixels.bottom);
        }

        public void SetOverflowPixels(Borders pixels, Vector2 rectSize)
        {
            SetOverflowPixels(pixels.left, pixels.right, pixels.top, pixels.bottom, rectSize);
        }

        public void SetOverflowFromRects(Rect contentRect, Rect renderRect)
        {
            float left = contentRect.xMin - renderRect.xMin;
            float right = renderRect.xMax - contentRect.xMax;
            float top = contentRect.yMin - renderRect.yMin;
            float bottom = renderRect.yMax - contentRect.yMax;

            SetOverflowPixels(left, right, top, bottom, contentRect.size);
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (enabled == false)
                return;

            Image img = GetComponent<Image>();

            if (img.sprite == null)
                return;

            Rect r = img.GetPixelAdjustedRect();

            float width = r.width;
            float height = r.height;

            if (_areaAnchors.left > 100 || _areaAnchors.right > 100 || _areaAnchors.top > 100 || _areaAnchors.bottom > 100)
            {
                _areaAnchors = new Borders(0.1f, 0.1f, 0.1f, 0.1f);
                return;
            }

            float leftOffset = _areaAnchors.left * width;
            float rightOffset = _areaAnchors.right * width;
            float topOffset = _areaAnchors.top * height;
            float bottomOffset = _areaAnchors.bottom * height;

            r.xMin -= leftOffset;
            r.xMax += rightOffset;
            r.yMin -= bottomOffset;
            r.yMax += topOffset;

            // Debug.Log($"width: {width} | height: {height} | r.xMin: {r.xMin} | r.xMax: {r.xMax} | r.yMin: {r.yMin} | r.yMax: {r.yMax}\nareaAnchors: {areaAnchors}");

            vh.Clear();

            Vector4 inner = img.overrideSprite != null ? DataUtility.GetInnerUV(img.overrideSprite) : Vector4.zero;

            Vector4 v = new Vector4(
                r.x,
                r.y,
                r.x + r.width,
                r.y + r.height
            );

            Vector2 uvMin = new Vector2(inner.x, inner.y);
            Vector2 uvMax = new Vector2(inner.z, inner.w);

            Color color32 = img.color;

            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uvMin.x, uvMin.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uvMin.x, uvMax.y));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uvMax.x, uvMax.y));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uvMax.x, uvMin.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }

    [Serializable]
    public struct RectangleSize
    {
        public float width;
        public float height;
    }

    [Serializable]
    public struct Borders
    {
        public float left;
        public float right;
        public float top;
        public float bottom;

        public Borders(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }
    }

    public enum GizmosColor
    {
        White = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
    }
}
#endif
