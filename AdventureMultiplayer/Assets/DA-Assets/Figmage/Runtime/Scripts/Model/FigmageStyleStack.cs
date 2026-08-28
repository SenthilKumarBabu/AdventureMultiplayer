using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.Figmage
{
    public static class FigmageStyleStack
    {
        public static FigmagePaint ClonePaint(FigmagePaint source)
        {
            if (source == null)
                return null;

            return new FigmagePaint
            {
                Type = source.Type,
                Color = source.Color,
                Visible = source.Visible,
                BlendMode = source.BlendMode,
                Opacity = source.Opacity,
                GradientHandlePositions = source.GradientHandlePositions != null
                    ? new List<Vector2>(source.GradientHandlePositions)
                    : null,
                GradientStops = source.GradientStops != null
                    ? new List<FigmageGradientStop>(source.GradientStops)
                    : null
            };
        }

        public static FigmageEffect CloneEffect(FigmageEffect source)
        {
            if (source == null)
                return null;

            return new FigmageEffect
            {
                Type = source.Type,
                Visible = source.Visible,
                Color = source.Color,
                Opacity = source.Opacity,
                BlendMode = source.BlendMode,
                Offset = source.Offset,
                Radius = source.Radius,
                Spread = source.Spread,
                BlurType = source.BlurType,
                StartRadius = source.StartRadius,
                StartOffset = source.StartOffset,
                EndOffset = source.EndOffset
            };
        }

        public static void Move<T>(List<T> items, int fromIndex, int toIndex)
        {
            if (items == null || fromIndex < 0 || fromIndex >= items.Count || items.Count <= 1)
                return;

            int targetIndex = Mathf.Clamp(toIndex, 0, items.Count - 1);
            if (targetIndex == fromIndex)
                return;

            T item = items[fromIndex];
            items.RemoveAt(fromIndex);
            items.Insert(targetIndex, item);
        }

        public static void RemoveAt<T>(List<T> items, int index)
        {
            if (items == null || index < 0 || index >= items.Count)
                return;

            items.RemoveAt(index);
        }
    }
}
