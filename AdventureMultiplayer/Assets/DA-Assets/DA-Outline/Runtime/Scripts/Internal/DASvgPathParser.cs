using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace DA_Assets.DAO.Internal
{
    internal sealed class DAPathSubpath
    {
        public readonly List<Vector2> Points = new List<Vector2>();
        public bool Closed;
    }

    internal static class DASvgPathParser
    {
        private const float ArcDistanceEpsilon = 1e-4f;
        private const float ArcComputationEpsilon = 1e-6f;
        private const float FullRotationRadians = 6.28318530718f;
        private const float PointEqualityEpsilonSqr = 1e-6f;

        public static List<DAPathSubpath> Parse(string path, int curveSegments)
        {
            List<string> tokens = Tokenize(path);
            List<DAPathSubpath> subpaths = new List<DAPathSubpath>();
            DAPathSubpath currentSubpath = null;
            int index = 0;
            char command = ' ';
            Vector2 current = Vector2.zero;
            Vector2 start = Vector2.zero;
            Vector2 lastControl = Vector2.zero;
            bool hasLastControl = false;
            curveSegments = Mathf.Max(1, curveSegments);

            while (index < tokens.Count)
            {
                if (tokens[index].Length == 1 && char.IsLetter(tokens[index][0]))
                {
                    command = tokens[index][0];
                    index++;
                }

                char lower = char.ToLowerInvariant(command);
                if (lower == 'z')
                {
                    if (currentSubpath != null)
                    {
                        AddPoint(currentSubpath.Points, start);
                        currentSubpath.Closed = true;
                    }

                    current = start;
                    hasLastControl = false;
                    command = ' ';
                    continue;
                }

                int arity = GetArity(lower);
                if (arity <= 0 || index + arity > tokens.Count)
                {
                    break;
                }

                float[] values = new float[arity];
                for (int i = 0; i < arity; i++)
                {
                    values[i] = float.Parse(tokens[index + i], CultureInfo.InvariantCulture);
                }

                index += arity;
                switch (lower)
                {
                    case 'm':
                    {
                        Vector2 next = ToAbsolute(command, current, values[0], values[1]);
                        currentSubpath = new DAPathSubpath();
                        subpaths.Add(currentSubpath);
                        AddPoint(currentSubpath.Points, next);
                        current = next;
                        start = next;
                        command = char.IsLower(command) ? 'l' : 'L';
                        hasLastControl = false;
                        break;
                    }
                    case 'l':
                    {
                        Vector2 next = ToAbsolute(command, current, values[0], values[1]);
                        AddPoint(currentSubpath?.Points, next);
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'h':
                    {
                        Vector2 next = char.IsLower(command)
                            ? new Vector2(current.x + values[0], current.y)
                            : new Vector2(values[0], current.y);
                        AddPoint(currentSubpath?.Points, next);
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'v':
                    {
                        Vector2 next = char.IsLower(command)
                            ? new Vector2(current.x, current.y + values[0])
                            : new Vector2(current.x, values[0]);
                        AddPoint(currentSubpath?.Points, next);
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'c':
                    {
                        Vector2 c1 = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 c2 = ToAbsolute(command, current, values[2], values[3]);
                        Vector2 next = ToAbsolute(command, current, values[4], values[5]);
                        SampleCubic(currentSubpath?.Points, current, c1, c2, next, curveSegments);
                        current = next;
                        lastControl = c2;
                        hasLastControl = true;
                        break;
                    }
                    case 's':
                    {
                        Vector2 c1 = hasLastControl ? current + (current - lastControl) : current;
                        Vector2 c2 = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 next = ToAbsolute(command, current, values[2], values[3]);
                        SampleCubic(currentSubpath?.Points, current, c1, c2, next, curveSegments);
                        current = next;
                        lastControl = c2;
                        hasLastControl = true;
                        break;
                    }
                    case 'q':
                    {
                        Vector2 c = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 next = ToAbsolute(command, current, values[2], values[3]);
                        Vector2 c1 = current + (2f / 3f) * (c - current);
                        Vector2 c2 = next + (2f / 3f) * (c - next);
                        SampleCubic(currentSubpath?.Points, current, c1, c2, next, curveSegments);
                        current = next;
                        lastControl = c;
                        hasLastControl = true;
                        break;
                    }
                    case 't':
                    {
                        Vector2 c = hasLastControl ? current + (current - lastControl) : current;
                        Vector2 next = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 c1 = current + (2f / 3f) * (c - current);
                        Vector2 c2 = next + (2f / 3f) * (c - next);
                        SampleCubic(currentSubpath?.Points, current, c1, c2, next, curveSegments);
                        current = next;
                        lastControl = c;
                        hasLastControl = true;
                        break;
                    }
                    case 'a':
                    {
                        Vector2 next = ToAbsolute(command, current, values[5], values[6]);
                        SampleArc(currentSubpath?.Points, current, Mathf.Abs(values[0]), Mathf.Abs(values[1]), values[2], values[3] > 0.5f, values[4] > 0.5f, next);
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                }
            }

            for (int i = 0; i < subpaths.Count; i++)
            {
                RemoveDuplicateClosingPoint(subpaths[i]);
            }

            return subpaths;
        }

        private static int GetArity(char command)
        {
            switch (command)
            {
                case 'm':
                case 'l':
                case 't':
                    return 2;
                case 'h':
                case 'v':
                    return 1;
                case 'c':
                    return 6;
                case 's':
                case 'q':
                    return 4;
                case 'a':
                    return 7;
                default:
                    return 0;
            }
        }

        private static List<string> Tokenize(string path)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(path))
                return tokens;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < path.Length; i++)
            {
                char ch = path[i];
                if (char.IsLetter(ch))
                {
                    FlushToken(builder, tokens);
                    tokens.Add(ch.ToString());
                    continue;
                }

                if (char.IsWhiteSpace(ch) || ch == ',')
                {
                    FlushToken(builder, tokens);
                    continue;
                }

                if ((ch == '-' || ch == '+') && builder.Length > 0 && builder[builder.Length - 1] != 'e' && builder[builder.Length - 1] != 'E')
                {
                    FlushToken(builder, tokens);
                }

                builder.Append(ch);
            }

            FlushToken(builder, tokens);
            return tokens;
        }

        private static void FlushToken(StringBuilder builder, List<string> tokens)
        {
            if (builder.Length == 0)
                return;

            tokens.Add(builder.ToString());
            builder.Clear();
        }

        private static Vector2 ToAbsolute(char command, Vector2 current, float x, float y)
        {
            return char.IsLower(command)
                ? new Vector2(current.x + x, current.y + y)
                : new Vector2(x, y);
        }

        private static void SampleCubic(List<Vector2> points, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps)
        {
            if (points == null)
                return;

            steps = Mathf.Max(1, steps);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float u = 1f - t;
                Vector2 point =
                    (u * u * u) * p0 +
                    (3f * u * u * t) * p1 +
                    (3f * u * t * t) * p2 +
                    (t * t * t) * p3;
                AddPoint(points, point);
            }
        }

        private static void SampleArc(List<Vector2> points, Vector2 start, float rx, float ry, float xAxisRotation, bool largeArc, bool sweep, Vector2 end)
        {
            if (points == null)
                return;

            if (rx <= ArcDistanceEpsilon || ry <= ArcDistanceEpsilon || Vector2.Distance(start, end) <= ArcDistanceEpsilon)
            {
                AddPoint(points, end);
                return;
            }

            float phi = xAxisRotation * Mathf.Deg2Rad;
            float cosPhi = Mathf.Cos(phi);
            float sinPhi = Mathf.Sin(phi);
            float dx2 = (start.x - end.x) * 0.5f;
            float dy2 = (start.y - end.y) * 0.5f;

            float x1p = cosPhi * dx2 + sinPhi * dy2;
            float y1p = -sinPhi * dx2 + cosPhi * dy2;
            float x1pSq = x1p * x1p;
            float y1pSq = y1p * y1p;

            float rxSq = rx * rx;
            float rySq = ry * ry;
            float lambda = x1pSq / rxSq + y1pSq / rySq;
            if (lambda > 1f)
            {
                float scale = Mathf.Sqrt(lambda);
                rx *= scale;
                ry *= scale;
                rxSq = rx * rx;
                rySq = ry * ry;
            }

            float numerator = rxSq * rySq - rxSq * y1pSq - rySq * x1pSq;
            float denominator = rxSq * y1pSq + rySq * x1pSq;
            float centerFactor = denominator > ArcComputationEpsilon ? Mathf.Sqrt(Mathf.Max(0f, numerator / denominator)) : 0f;
            if (largeArc == sweep)
            {
                centerFactor = -centerFactor;
            }

            float cxp = centerFactor * (rx * y1p / Mathf.Max(ry, ArcComputationEpsilon));
            float cyp = centerFactor * (-ry * x1p / Mathf.Max(rx, ArcComputationEpsilon));

            float cx = cosPhi * cxp - sinPhi * cyp + (start.x + end.x) * 0.5f;
            float cy = sinPhi * cxp + cosPhi * cyp + (start.y + end.y) * 0.5f;

            float theta1 = ArcAngle(1f, 0f, (x1p - cxp) / Mathf.Max(rx, ArcComputationEpsilon), (y1p - cyp) / Mathf.Max(ry, ArcComputationEpsilon));
            float deltaTheta = ArcAngle(
                (x1p - cxp) / Mathf.Max(rx, ArcComputationEpsilon),
                (y1p - cyp) / Mathf.Max(ry, ArcComputationEpsilon),
                (-x1p - cxp) / Mathf.Max(rx, ArcComputationEpsilon),
                (-y1p - cyp) / Mathf.Max(ry, ArcComputationEpsilon));

            if (!sweep && deltaTheta > 0f)
            {
                deltaTheta -= FullRotationRadians;
            }
            else if (sweep && deltaTheta < 0f)
            {
                deltaTheta += FullRotationRadians;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(rx, ry) * Mathf.Abs(deltaTheta) / 8f), 4, 96);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = theta1 + deltaTheta * t;
                float cosAngle = Mathf.Cos(angle);
                float sinAngle = Mathf.Sin(angle);
                Vector2 point = new Vector2(
                    cx + cosPhi * rx * cosAngle - sinPhi * ry * sinAngle,
                    cy + sinPhi * rx * cosAngle + cosPhi * ry * sinAngle);
                AddPoint(points, point);
            }
        }

        private static float ArcAngle(float ux, float uy, float vx, float vy)
        {
            float dot = ux * vx + uy * vy;
            float mag = Mathf.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            if (mag <= ArcComputationEpsilon)
                return 0f;

            float angle = Mathf.Acos(Mathf.Clamp(dot / mag, -1f, 1f));
            float cross = ux * vy - uy * vx;
            return cross < 0f ? -angle : angle;
        }

        private static void AddPoint(List<Vector2> points, Vector2 point)
        {
            if (points == null)
                return;

            if (points.Count == 0 || Vector2.SqrMagnitude(points[points.Count - 1] - point) > PointEqualityEpsilonSqr)
            {
                points.Add(point);
            }
        }

        private static void RemoveDuplicateClosingPoint(DAPathSubpath subpath)
        {
            if (subpath.Points.Count < 2)
                return;

            int last = subpath.Points.Count - 1;
            if (Vector2.SqrMagnitude(subpath.Points[0] - subpath.Points[last]) <= PointEqualityEpsilonSqr)
            {
                subpath.Points.RemoveAt(last);
            }
        }
    }
}
