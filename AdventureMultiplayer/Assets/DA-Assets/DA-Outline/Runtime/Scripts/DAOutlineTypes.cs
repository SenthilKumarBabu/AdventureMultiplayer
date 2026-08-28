using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.DAO
{
    public enum DAOutlineAlign
    {
        Inside = 0,
        Center = 1,
        Outside = 2
    }

    public enum DAOutlineJoin
    {
        Miter = 0,
        Bevel = 1,
        Round = 2
    }

    [Serializable]
    public struct DAOutlinePath
    {
        [SerializeField, TextArea] string path;

        public string Path
        {
            get => path;
            set => path = value;
        }

        public DAOutlinePath(string path)
        {
            this.path = path;
        }
    }

    [Serializable]
    public struct DAOutlineData
    {
        [SerializeField] Vector2 size;
        [SerializeField] List<DAOutlinePath> paths;
        [SerializeField, Min(0f)] float strokeWidth;
        [SerializeField] DAOutlineAlign align;
        [SerializeField] DAOutlineJoin join;
        [SerializeField] List<float> dashes;
        [SerializeField, Min(0f)] float miterLimit;
        [SerializeField, Min(1)] int curveSegments;
        [SerializeField, Min(0f)] float smoothing;
        [SerializeField] Color color;

        public Vector2 Size
        {
            get => size;
            set => size = value;
        }

        public List<DAOutlinePath> Paths
        {
            get => paths;
            set => paths = value;
        }

        public float StrokeWidth
        {
            get => strokeWidth;
            set => strokeWidth = Mathf.Max(0f, value);
        }

        public DAOutlineAlign Align
        {
            get => align;
            set => align = value;
        }

        public DAOutlineJoin Join
        {
            get => join;
            set => join = value;
        }

        public List<float> Dashes
        {
            get => dashes;
            set => dashes = value;
        }

        public float MiterLimit
        {
            get => miterLimit;
            set => miterLimit = Mathf.Max(0f, value);
        }

        public int CurveSegments
        {
            get => curveSegments;
            set => curveSegments = Mathf.Max(1, value);
        }

        public float Smoothing
        {
            get => smoothing;
            set => smoothing = Mathf.Max(0f, value);
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public DAOutlineData(Vector2 size, List<DAOutlinePath> paths, float strokeWidth, DAOutlineAlign align, DAOutlineJoin join, List<float> dashes, Color color)
        {
            this.size = size;
            this.paths = paths;
            this.strokeWidth = Mathf.Max(0f, strokeWidth);
            this.align = align;
            this.join = join;
            this.dashes = dashes;
            this.miterLimit = 4f;
            this.curveSegments = 24;
            this.smoothing = 1f;
            this.color = color;
        }
    }
}
