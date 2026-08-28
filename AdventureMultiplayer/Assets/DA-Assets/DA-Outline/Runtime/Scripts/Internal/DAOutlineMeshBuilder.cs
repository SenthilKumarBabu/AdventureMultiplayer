using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.DAO.Internal
{
    internal static class DAOutlineMeshBuilder
    {
        private const float Epsilon = 0.0001f;

        public static void Populate(VertexHelper vertexHelper, Rect rect, DAOutlineData data, Color color)
        {
            if (vertexHelper == null || data.StrokeWidth <= 0f || data.Paths == null || data.Paths.Count == 0)
                return;

            Vector2 sourceSize = data.Size;
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
                sourceSize = new Vector2(Mathf.Max(1f, rect.width), Mathf.Max(1f, rect.height));

            GetStrokeOffsets(data.Align, data.StrokeWidth, out float outerOffset, out float innerOffset);

            for (int i = 0; i < data.Paths.Count; i++)
            {
                string path = data.Paths[i].Path;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                List<DAPathSubpath> subpaths = DASvgPathParser.Parse(path, data.CurveSegments);
                for (int j = 0; j < subpaths.Count; j++)
                {
                    if (HasDashes(data.Dashes))
                    {
                        BuildDashedSubpath(vertexHelper, rect, sourceSize, subpaths[j], data.Dashes, data.StrokeWidth, data.Align, data.MiterLimit, data.Join, color);
                    }
                    else
                    {
                        BuildSubpath(vertexHelper, rect, sourceSize, subpaths[j], outerOffset, innerOffset, data.Align, data.Smoothing, data.MiterLimit, data.Join, color);
                    }
                }
            }
        }

        private static bool HasDashes(List<float> dashes)
        {
            if (dashes == null || dashes.Count == 0)
                return false;

            for (int i = 0; i < dashes.Count; i++)
            {
                if (dashes[i] > Epsilon)
                    return true;
            }

            return false;
        }

        private static void GetStrokeOffsets(DAOutlineAlign align, float width, out float outerOffset, out float innerOffset)
        {
            switch (align)
            {
                case DAOutlineAlign.Inside:
                    outerOffset = 0f;
                    innerOffset = -width;
                    break;
                case DAOutlineAlign.Center:
                    outerOffset = width * 0.5f;
                    innerOffset = -width * 0.5f;
                    break;
                default:
                    outerOffset = width;
                    innerOffset = 0f;
                    break;
            }
        }

        private static void BuildSubpath(VertexHelper vertexHelper, Rect rect, Vector2 sourceSize, DAPathSubpath subpath, float outerOffset, float innerOffset, DAOutlineAlign align, float smoothing, float miterLimit, DAOutlineJoin join, Color color)
        {
            if (subpath == null || !subpath.Closed || subpath.Points.Count < 3)
                return;

            List<Vector2> localPath = ToLocalPath(subpath.Points, rect, sourceSize);
            if (localPath.Count < 3)
                return;

            List<Vector2> outer = OffsetClosedPath(localPath, outerOffset, miterLimit, join);
            List<Vector2> inner = OffsetClosedPath(localPath, innerOffset, miterLimit, join);
            AddRing(vertexHelper, outer, inner, color, color);

            if (smoothing <= Epsilon)
                return;

            Color transparent = color;
            transparent.a = 0f;

            List<Vector2> outerFeather = OffsetClosedPath(localPath, outerOffset + smoothing, miterLimit, join);
            AddRing(vertexHelper, outerFeather, outer, transparent, color);

            float innerSmoothing = ClampInnerSmoothing(localPath, innerOffset, smoothing);
            if (innerSmoothing > Epsilon)
            {
                List<Vector2> innerFeather = OffsetClosedPath(localPath, innerOffset - innerSmoothing, miterLimit, join);
                AddRing(vertexHelper, inner, innerFeather, color, transparent);
            }
        }

        private static void BuildDashedSubpath(VertexHelper vertexHelper, Rect rect, Vector2 sourceSize, DAPathSubpath subpath, List<float> dashes, float strokeWidth, DAOutlineAlign align, float miterLimit, DAOutlineJoin join, Color color)
        {
            if (subpath == null || !subpath.Closed || subpath.Points.Count < 3 || strokeWidth <= Epsilon)
                return;

            List<Vector2> localPath = ToLocalPath(subpath.Points, rect, sourceSize);
            if (localPath.Count < 3)
                return;

            float centerOffset = GetDashCenterOffset(align, strokeWidth);
            if (IsFourPointPath(localPath))
            {
                AddDashedClosedPathWithSegmentOffsets(vertexHelper, localPath, dashes, strokeWidth, centerOffset, color);
                return;
            }

            List<Vector2> centerPath = OffsetClosedPath(localPath, centerOffset, miterLimit, join);
            AddDashedClosedPath(vertexHelper, centerPath, dashes, strokeWidth, color);
        }

        private static bool IsFourPointPath(List<Vector2> path)
        {
            return path != null && path.Count == 4;
        }

        private static float GetDashCenterOffset(DAOutlineAlign align, float width)
        {
            switch (align)
            {
                case DAOutlineAlign.Inside:
                    return -width * 0.5f;
                case DAOutlineAlign.Outside:
                    return width * 0.5f;
                default:
                    return 0f;
            }
        }

        private static void AddDashedClosedPath(VertexHelper vertexHelper, List<Vector2> path, List<float> dashes, float strokeWidth, Color color)
        {
            if (path == null || path.Count < 2)
                return;

            List<float> pattern = NormalizeDashPattern(dashes);
            if (pattern.Count == 0)
                return;

            int patternIndex = 0;
            bool draw = true;
            float patternRemaining = pattern[patternIndex];

            for (int i = 0; i < path.Count; i++)
            {
                Vector2 segmentStart = path[i];
                Vector2 segmentEnd = path[(i + 1) % path.Count];
                Vector2 direction = segmentEnd - segmentStart;
                float edgeLength = direction.magnitude;
                if (edgeLength <= Epsilon)
                    continue;

                Vector2 normalizedDirection = direction / edgeLength;
                float edgeDistance = 0f;

                while (edgeDistance < edgeLength - Epsilon)
                {
                    float step = Mathf.Min(patternRemaining, edgeLength - edgeDistance);
                    if (draw && step > Epsilon)
                    {
                        Vector2 dashStart = segmentStart + normalizedDirection * edgeDistance;
                        Vector2 dashEnd = segmentStart + normalizedDirection * (edgeDistance + step);
                        AddDashSegment(vertexHelper, dashStart, dashEnd, strokeWidth, color);
                    }

                    edgeDistance += step;
                    patternRemaining -= step;
                    if (patternRemaining > Epsilon)
                        continue;

                    patternIndex = (patternIndex + 1) % pattern.Count;
                    draw = !draw;
                    patternRemaining = pattern[patternIndex];
                }
            }
        }

        private static List<float> NormalizeDashPattern(List<float> dashes)
        {
            List<float> pattern = new List<float>();
            for (int i = 0; i < dashes.Count; i++)
            {
                if (dashes[i] > Epsilon)
                    pattern.Add(dashes[i]);
            }

            if (pattern.Count % 2 == 1)
            {
                int count = pattern.Count;
                for (int i = 0; i < count; i++)
                {
                    pattern.Add(pattern[i]);
                }
            }

            return pattern;
        }

        private static void AddDashedClosedPathWithSegmentOffsets(VertexHelper vertexHelper, List<Vector2> path, List<float> dashes, float strokeWidth, float centerOffset, Color color)
        {
            if (path == null || path.Count < 2)
                return;

            List<float> pattern = NormalizeDashPattern(dashes);
            if (pattern.Count == 0)
                return;

            bool ccw = SignedArea(path) > 0f;
            bool[] startsWithDash = new bool[path.Count];
            bool[] endsWithDash = new bool[path.Count];

            for (int i = 0; i < path.Count; i++)
            {
                int patternIndex = 0;
                bool draw = true;
                float patternRemaining = pattern[patternIndex];
                Vector2 segmentStart = path[i];
                Vector2 segmentEnd = path[(i + 1) % path.Count];
                Vector2 direction = segmentEnd - segmentStart;
                float edgeLength = direction.magnitude;
                if (edgeLength <= Epsilon)
                    continue;

                Vector2 normalizedDirection = direction / edgeLength;
                Vector2 offset = OutwardNormal(normalizedDirection, ccw) * centerOffset;
                segmentStart += offset;
                if (TryAddAdjustedBevelEdgeDashes(vertexHelper, segmentStart, normalizedDirection, edgeLength, pattern, strokeWidth, color))
                {
                    startsWithDash[i] = true;
                    endsWithDash[i] = true;
                    continue;
                }

                float edgeDistance = 0f;

                while (edgeDistance < edgeLength - Epsilon)
                {
                    float step = Mathf.Min(patternRemaining, edgeLength - edgeDistance);
                    if (draw && step > Epsilon)
                    {
                        Vector2 dashStart = segmentStart + normalizedDirection * edgeDistance;
                        Vector2 dashEnd = segmentStart + normalizedDirection * (edgeDistance + step);
                        AddDashSegment(vertexHelper, dashStart, dashEnd, strokeWidth, color);

                        if (edgeDistance <= Epsilon)
                            startsWithDash[i] = true;

                        if (edgeDistance + step >= edgeLength - Epsilon)
                            endsWithDash[i] = true;
                    }

                    edgeDistance += step;
                    patternRemaining -= step;
                    if (patternRemaining > Epsilon)
                        continue;

                    patternIndex = (patternIndex + 1) % pattern.Count;
                    draw = !draw;
                    patternRemaining = pattern[patternIndex];
                }
            }

            for (int i = 0; i < path.Count; i++)
            {
                int previous = (i - 1 + path.Count) % path.Count;
                if (endsWithDash[previous] && startsWithDash[i])
                    AddBevelCorner(vertexHelper, path, i, strokeWidth, centerOffset, ccw, color);
            }
        }

        private static bool TryAddAdjustedBevelEdgeDashes(VertexHelper vertexHelper, Vector2 segmentStart, Vector2 normalizedDirection, float edgeLength, List<float> pattern, float strokeWidth, Color color)
        {
            if (pattern == null || pattern.Count != 2 || pattern[0] <= Epsilon || pattern[1] <= Epsilon)
                return false;

            float dash = pattern[0];
            float gap = pattern[1];
            int gapCount = Mathf.Max(1, Mathf.RoundToInt(edgeLength / (dash + gap)));
            float adjustedCycleLength = gapCount * (dash + gap);
            if (adjustedCycleLength <= Epsilon)
                return false;

            float scale = edgeLength / adjustedCycleLength;
            float halfDashLength = dash * 0.5f * scale;
            float dashLength = dash * scale;
            float gapLength = gap * scale;
            float edgeDistance = 0f;

            AddDashSegment(vertexHelper, segmentStart, segmentStart + normalizedDirection * halfDashLength, strokeWidth, color, false);
            edgeDistance += halfDashLength;

            for (int i = 0; i < gapCount - 1; i++)
            {
                edgeDistance += gapLength;
                Vector2 dashStart = segmentStart + normalizedDirection * edgeDistance;
                edgeDistance += dashLength;
                Vector2 dashEnd = segmentStart + normalizedDirection * edgeDistance;
                AddDashSegment(vertexHelper, dashStart, dashEnd, strokeWidth, color, false);
            }

            edgeDistance += gapLength;
            AddDashSegment(vertexHelper, segmentStart + normalizedDirection * edgeDistance, segmentStart + normalizedDirection * edgeLength, strokeWidth, color, false);
            return true;
        }

        private static void AddDashSegment(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color color)
        {
            AddDashSegment(vertexHelper, start, end, width, color, true);
        }

        private static void AddDashSegment(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color color, bool overlapEnds)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length <= Epsilon)
                return;

            Vector2 normalizedDirection = direction / length;
            if (overlapEnds)
            {
                float overlap = Mathf.Min(0.5f, length * 0.25f);
                start -= normalizedDirection * overlap;
                end += normalizedDirection * overlap;
            }

            float halfWidth = width * 0.5f;
            Vector2 normal = Perpendicular(normalizedDirection) * halfWidth;
            AddQuad(vertexHelper, start + normal, end + normal, end - normal, start - normal, color, color, color, color);
        }

        private static void AddBevelCorner(VertexHelper vertexHelper, List<Vector2> path, int cornerIndex, float width, float centerOffset, bool ccw, Color color)
        {
            float halfWidth = width * 0.5f;
            float outerOffset = centerOffset + (centerOffset < 0f ? -halfWidth : halfWidth);

            Vector2 previous = path[(cornerIndex - 1 + path.Count) % path.Count];
            Vector2 current = path[cornerIndex];
            Vector2 next = path[(cornerIndex + 1) % path.Count];

            Vector2 previousDirection = SafeNormalize(current - previous);
            Vector2 nextDirection = SafeNormalize(next - current);
            Vector2 previousOuter = current + OutwardNormal(previousDirection, ccw) * outerOffset;
            Vector2 nextOuter = current + OutwardNormal(nextDirection, ccw) * outerOffset;

            AddTriangle(vertexHelper, current, previousOuter, nextOuter, color, color, color);
        }

        private static Vector2 Perpendicular(Vector2 direction)
        {
            return new Vector2(-direction.y, direction.x);
        }

        private static List<Vector2> ToLocalPath(List<Vector2> source, Rect rect, Vector2 sourceSize)
        {
            List<Vector2> points = new List<Vector2>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                Vector2 point = source[i];
                float x = Mathf.Lerp(rect.xMin, rect.xMax, sourceSize.x > 0f ? point.x / sourceSize.x : 0f);
                float y = Mathf.Lerp(rect.yMax, rect.yMin, sourceSize.y > 0f ? point.y / sourceSize.y : 0f);
                AddCleanPoint(points, new Vector2(x, y));
            }

            return points;
        }

        private static void AddCleanPoint(List<Vector2> points, Vector2 point)
        {
            if (points.Count == 0 || Vector2.SqrMagnitude(points[points.Count - 1] - point) > Epsilon * Epsilon)
            {
                points.Add(point);
            }
        }

        private static List<Vector2> OffsetClosedPath(List<Vector2> path, float distance, float miterLimit, DAOutlineJoin join)
        {
            List<Vector2> result = new List<Vector2>(path.Count);
            if (Mathf.Abs(distance) <= Epsilon)
            {
                result.AddRange(path);
                return result;
            }

            bool ccw = SignedArea(path) > 0f;
            float maxMiter = Mathf.Max(Mathf.Abs(distance), Mathf.Abs(distance) * Mathf.Max(1f, miterLimit));

            for (int i = 0; i < path.Count; i++)
            {
                Vector2 prev = path[(i - 1 + path.Count) % path.Count];
                Vector2 current = path[i];
                Vector2 next = path[(i + 1) % path.Count];

                Vector2 prevDir = SafeNormalize(current - prev);
                Vector2 nextDir = SafeNormalize(next - current);
                Vector2 prevNormal = OutwardNormal(prevDir, ccw) * distance;
                Vector2 nextNormal = OutwardNormal(nextDir, ccw) * distance;

                Vector2 lineA = current + prevNormal;
                Vector2 lineB = current + nextNormal;

                if (join != DAOutlineJoin.Bevel && TryLineIntersection(lineA, prevDir, lineB, nextDir, out Vector2 intersection))
                {
                    Vector2 fromCurrent = intersection - current;
                    float sqrMagnitude = fromCurrent.sqrMagnitude;
                    if (sqrMagnitude <= maxMiter * maxMiter)
                    {
                        result.Add(intersection);
                        continue;
                    }

                    if (sqrMagnitude > Epsilon * Epsilon)
                    {
                        result.Add(current + fromCurrent.normalized * maxMiter);
                        continue;
                    }
                }

                result.Add(current + (prevNormal + nextNormal) * 0.5f);
            }

            return result;
        }

        private static float ClampInnerSmoothing(List<Vector2> path, float innerOffset, float smoothing)
        {
            if (path == null || path.Count == 0 || smoothing <= Epsilon)
                return 0f;

            float minX = path[0].x;
            float maxX = path[0].x;
            float minY = path[0].y;
            float maxY = path[0].y;

            for (int i = 1; i < path.Count; i++)
            {
                Vector2 point = path[i];
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            float halfMinSize = Mathf.Min(maxX - minX, maxY - minY) * 0.5f;
            float existingInset = Mathf.Max(0f, -innerOffset);
            float maxSmoothing = Mathf.Max(0f, halfMinSize * 0.5f - existingInset - Epsilon);

            return Mathf.Min(smoothing, maxSmoothing);
        }

        private static float SignedArea(List<Vector2> path)
        {
            float area = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 a = path[i];
                Vector2 b = path[(i + 1) % path.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        private static Vector2 OutwardNormal(Vector2 direction, bool ccw)
        {
            return ccw
                ? new Vector2(direction.y, -direction.x)
                : new Vector2(-direction.y, direction.x);
        }

        private static Vector2 SafeNormalize(Vector2 value)
        {
            float magnitude = value.magnitude;
            return magnitude > Epsilon ? value / magnitude : Vector2.right;
        }

        private static bool TryLineIntersection(Vector2 pointA, Vector2 dirA, Vector2 pointB, Vector2 dirB, out Vector2 intersection)
        {
            float cross = Cross(dirA, dirB);
            if (Mathf.Abs(cross) <= Epsilon)
            {
                intersection = pointA;
                return false;
            }

            float t = Cross(pointB - pointA, dirB) / cross;
            intersection = pointA + dirA * t;
            return true;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static void AddRing(VertexHelper vertexHelper, List<Vector2> outer, List<Vector2> inner, Color outerColor, Color innerColor)
        {
            if (outer == null || inner == null || outer.Count < 3 || outer.Count != inner.Count)
                return;

            int count = outer.Count;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                AddQuad(vertexHelper, outer[i], outer[next], inner[next], inner[i], outerColor, outerColor, innerColor, innerColor);
            }
        }

        private static void AddQuad(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colorA, Color colorB, Color colorC, Color colorD)
        {
            int index = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, a, colorA);
            AddVertex(vertexHelper, b, colorB);
            AddVertex(vertexHelper, c, colorC);
            AddVertex(vertexHelper, d, colorD);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddTriangle(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Color colorA, Color colorB, Color colorC)
        {
            int index = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, a, colorA);
            AddVertex(vertexHelper, b, colorB);
            AddVertex(vertexHelper, c, colorC);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
        }

        private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = Vector2.zero;
            vertexHelper.AddVert(vertex);
        }
    }
}
