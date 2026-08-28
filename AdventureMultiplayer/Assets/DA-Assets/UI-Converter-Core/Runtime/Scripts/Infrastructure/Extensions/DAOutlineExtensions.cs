#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DA_Assets.DAO;
using DA_Assets.UCC.Model;
using UnityEngine;

namespace DA_Assets.UCC.Extensions
{
    public static class DAOutlineExtensions
    {
        public static bool TryToDAOutlineData(this Node fobject, out DAOutlineData data)
        {
            data = default;

            FGraphic graphic = fobject.Data.Graphic;
            if (!graphic.HasStroke || fobject.StrokeWeight <= 0f || !graphic.Stroke.HasSolid)
                return false;

            List<DAOutlinePath> paths = ToOutlinePaths(fobject.FillGeometry);
            if (paths.Count == 0)
                return false;

            Color color = graphic.Stroke.SolidPaint.Color;
            color.a *= Mathf.Clamp01(graphic.Stroke.SolidPaint.Opacity ?? 1f);

            data = new DAOutlineData(
                ResolveSourceSize(fobject),
                paths,
                fobject.StrokeWeight,
                ToDAOutlineAlign(fobject.StrokeAlign),
                ToDAOutlineJoin(fobject.StrokeJoin),
                ToDashes(fobject.StrokeDashes),
                color);

            return true;
        }

        private static List<float> ToDashes(List<float> dashes)
        {
            if (dashes == null || dashes.Count == 0)
                return null;

            List<float> result = new List<float>();
            for (int i = 0; i < dashes.Count; i++)
            {
                if (dashes[i] > 0f)
                {
                    result.Add(dashes[i]);
                }
            }

            return result.Count == 0 ? null : result;
        }

        private static Vector2 ResolveSourceSize(Node fobject)
        {
            if (fobject.Size.x > 0f && fobject.Size.y > 0f)
                return fobject.Size;

            BoundingBox bounds = fobject.AbsoluteBoundingBox;
            return new Vector2(
                Mathf.Max(1f, bounds.Width ?? 1f),
                Mathf.Max(1f, bounds.Height ?? 1f));
        }

        private static List<DAOutlinePath> ToOutlinePaths(List<FillGeometry> geometry)
        {
            List<DAOutlinePath> paths = new List<DAOutlinePath>();
            if (geometry == null)
                return paths;

            for (int i = 0; i < geometry.Count; i++)
            {
                string path = geometry[i].Path;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(new DAOutlinePath(path));
                }
            }

            return paths;
        }

        private static DAOutlineAlign ToDAOutlineAlign(StrokeAlign align)
        {
            switch (align)
            {
                case StrokeAlign.INSIDE:
                    return DAOutlineAlign.Inside;
                case StrokeAlign.CENTER:
                    return DAOutlineAlign.Center;
                case StrokeAlign.OUTSIDE:
                    return DAOutlineAlign.Outside;
                default:
                    return DAOutlineAlign.Center;
            }
        }

        private static DAOutlineJoin ToDAOutlineJoin(string join)
        {
            if (string.Equals(join, "ROUND", StringComparison.OrdinalIgnoreCase))
                return DAOutlineJoin.Round;

            if (string.Equals(join, "BEVEL", StringComparison.OrdinalIgnoreCase))
                return DAOutlineJoin.Bevel;

            return DAOutlineJoin.Miter;
        }
    }
}
#endif