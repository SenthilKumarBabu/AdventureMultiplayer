using System.Collections.Generic;
using DA_Assets.DAO.Internal;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.DAO
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/D.A. Outline/DA Outline Effect")]
    public sealed class DAOutlineEffect : BaseMeshEffect
    {
        [SerializeField] Vector2 sourceSize = Vector2.one;
        [SerializeField] List<DAOutlinePath> paths = new List<DAOutlinePath>();
        [SerializeField, Min(0f)] float strokeWidth = 1f;
        [SerializeField] DAOutlineAlign align = DAOutlineAlign.Outside;
        [SerializeField] DAOutlineJoin join = DAOutlineJoin.Miter;
        [SerializeField] List<float> dashes = new List<float>();
        [SerializeField, Min(0f)] float miterLimit = 4f;
        [SerializeField, Min(1)] int curveSegments = 24;
        [SerializeField, Min(0f)] float smoothing = 1f;
        [SerializeField] Color outlineColor = Color.white;

        public Vector2 SourceSize
        {
            get => sourceSize;
            set
            {
                sourceSize = value;
                SetGraphicDirty();
            }
        }

        public List<DAOutlinePath> Paths
        {
            get => paths;
            set
            {
                paths = value ?? new List<DAOutlinePath>();
                SetGraphicDirty();
            }
        }

        public float StrokeWidth
        {
            get => strokeWidth;
            set
            {
                strokeWidth = Mathf.Max(0f, value);
                SetGraphicDirty();
            }
        }

        public DAOutlineAlign Align
        {
            get => align;
            set
            {
                align = value;
                SetGraphicDirty();
            }
        }

        public DAOutlineJoin Join
        {
            get => join;
            set
            {
                join = value;
                SetGraphicDirty();
            }
        }

        public List<float> Dashes
        {
            get => dashes;
            set
            {
                dashes = value ?? new List<float>();
                SetGraphicDirty();
            }
        }

        public float MiterLimit
        {
            get => miterLimit;
            set
            {
                miterLimit = Mathf.Max(0f, value);
                SetGraphicDirty();
            }
        }

        public int CurveSegments
        {
            get => curveSegments;
            set
            {
                curveSegments = Mathf.Max(1, value);
                SetGraphicDirty();
            }
        }

        public float Smoothing
        {
            get => smoothing;
            set
            {
                smoothing = Mathf.Max(0f, value);
                SetGraphicDirty();
            }
        }

        public Color OutlineColor
        {
            get => outlineColor;
            set
            {
                outlineColor = value;
                SetGraphicDirty();
            }
        }

        public void SetData(DAOutlineData data)
        {
            sourceSize = data.Size;
            paths = data.Paths ?? new List<DAOutlinePath>();
            strokeWidth = Mathf.Max(0f, data.StrokeWidth);
            align = data.Align;
            join = data.Join;
            dashes = data.Dashes ?? new List<float>();
            miterLimit = Mathf.Max(0f, data.MiterLimit);
            curveSegments = Mathf.Max(1, data.CurveSegments);
            smoothing = Mathf.Max(0f, data.Smoothing);
            outlineColor = data.Color;
            SetGraphicDirty();
        }

        public DAOutlineData GetData()
        {
            DAOutlineData data = new DAOutlineData(sourceSize, paths, strokeWidth, align, join, dashes, outlineColor)
            {
                MiterLimit = miterLimit,
                CurveSegments = curveSegments,
                Smoothing = smoothing
            };

            return data;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            strokeWidth = Mathf.Max(0f, strokeWidth);
            miterLimit = Mathf.Max(0f, miterLimit);
            curveSegments = Mathf.Max(1, curveSegments);
            smoothing = Mathf.Max(0f, smoothing);
            SetGraphicDirty();
        }
#endif

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || graphic == null)
                return;

            DAOutlineData data = GetData();
            DAOutlineMeshBuilder.Populate(vh, graphic.rectTransform.rect, data, outlineColor);
        }

        private void SetGraphicDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}
