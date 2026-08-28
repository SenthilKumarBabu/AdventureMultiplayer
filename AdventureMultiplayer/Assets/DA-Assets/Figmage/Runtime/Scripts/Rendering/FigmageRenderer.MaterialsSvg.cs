using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DA_Assets.Figmage
{
    sealed partial class FigmageRenderer
    {
        void ConfigureBodyMaterial(Material material, ShaderRenderSpec spec, bool renderShadowMask, Vector2 shapeOffsetOverride, ShadowSpec dropShadow)
        {
            material.SetFloat(PropDiamondExponent, ResolveDiamondExponent());
            material.SetFloat(PropStrokeAngularOffset, ResolveStrokeAngularOffset());
            material.SetVector(PropCanvasSize, new Vector4(spec.CanvasSize.x, spec.CanvasSize.y, 0f, 0f));
            material.SetVector(PropShapeSize, new Vector4(spec.ShapeSize.x, spec.ShapeSize.y, 0f, 0f));
            material.SetVector(PropShapeOffset, new Vector4(shapeOffsetOverride.x, shapeOffsetOverride.y, 0f, 0f));
            material.SetVector(PropCornerRadii, spec.CornerRadii);
            material.SetFloat(PropCornerSmoothing, spec.CornerSmoothing);
            material.SetFloat(PropShapeRotation, spec.ShapeRotationRadians);
            material.SetFloat(PropShapeOpacity, spec.ShapeOpacity);
            material.SetFloat(PropStrokeWeight, spec.StrokeWeight);
            material.SetFloat(PropStrokeAlign, spec.StrokeAlign);
            material.SetFloat(PropFillExtendsUnderStroke, spec.Strokes != null && spec.Strokes.Count > 0 ? 1f : 0f);
            material.SetFloat(PropAntialiasing, spec.Antialiasing);
            material.SetFloat(PropRenderMode, renderShadowMask ? 1f : 0f);
            material.SetInt(PropFillCount, spec.Fills.Count);
            material.SetInt(PropStrokeCount, spec.Strokes.Count);
            ApplyPathGeometry(material, spec.FillPathPoints, spec.StrokePathPoints);

            ClearLayerSlots(material);

            for (int i = 0; i < spec.Fills.Count && i < MaxFillLayers; i++)
            {
                ApplyLayer(material, spec.Fills[i], i, true);
            }

            for (int i = 0; i < spec.Strokes.Count && i < MaxStrokeLayers; i++)
            {
                ApplyLayer(material, spec.Strokes[i], i, false);
            }

            material.SetVector(PropDropShadow, new Vector4(dropShadow.BlurRadius, dropShadow.Spread, dropShadow.BlendMode, dropShadow.Enabled ? 1f : 0f));
            material.SetVector(PropDropShadowOffset, new Vector4(dropShadow.Offset.x, dropShadow.Offset.y, 0f, 0f));
            material.SetVector(PropDropShadowColor, dropShadow.Color);

            ShadowSpec innerShadow = spec.InnerShadow;
            material.SetVector(PropInnerShadow, new Vector4(innerShadow.BlurRadius, innerShadow.Spread, innerShadow.BlendMode, innerShadow.Enabled ? 1f : 0f));
            material.SetVector(PropInnerShadowOffset, new Vector4(innerShadow.Offset.x, innerShadow.Offset.y, 0f, 0f));
            material.SetVector(PropInnerShadowColor, innerShadow.Color);
            material.SetTexture(PropInputBodyTex, Texture2D.blackTexture);
            material.SetFloat(PropUseInputBodyTex, 0f);
            material.SetFloat(PropBodyCompositeMode, 0f);
            material.SetFloat(PropBodyDisjointComposite, 0f);
            material.SetTexture(PropLayerGradientTex, Texture2D.whiteTexture);
            material.SetFloat(PropUseLayerGradientTex, 0f);
        }

        void ConfigureShadowMaskMaterial(Material material, ShaderRenderSpec spec, int width, int height, Vector2 shapeOffsetOverride, float spreadPx)
        {
            material.SetFloat(PropDiamondExponent, ResolveDiamondExponent());
            material.SetFloat(PropStrokeAngularOffset, ResolveStrokeAngularOffset());
            material.SetVector(PropCanvasSize, new Vector4(width, height, 0f, 0f));
            material.SetVector(PropShapeSize, new Vector4(spec.ShapeSize.x, spec.ShapeSize.y, 0f, 0f));
            material.SetVector(PropShapeOffset, new Vector4(shapeOffsetOverride.x, shapeOffsetOverride.y, 0f, 0f));
            material.SetVector(PropCornerRadii, spec.CornerRadii);
            material.SetFloat(PropCornerSmoothing, spec.CornerSmoothing);
            material.SetFloat(PropShapeRotation, spec.ShapeRotationRadians);
            material.SetFloat(PropShapeOpacity, spec.ShapeOpacity);
            material.SetFloat(PropStrokeWeight, spec.StrokeWeight);
            material.SetFloat(PropStrokeAlign, spec.StrokeAlign);
            material.SetFloat(PropFillExtendsUnderStroke, 0f);
            material.SetFloat(PropAntialiasing, spec.Antialiasing);
            material.SetFloat(PropRenderMode, 1f);
            material.SetInt(PropFillCount, spec.Fills.Count);
            material.SetInt(PropStrokeCount, spec.Strokes.Count);
            ApplyPathGeometry(material, spec.FillPathPoints, spec.StrokePathPoints);
            ClearLayerSlots(material);

            material.SetVector(PropDropShadow, new Vector4(0f, spreadPx, 0f, 1f));
            material.SetVector(PropDropShadowOffset, Vector4.zero);
            material.SetVector(PropDropShadowColor, Vector4.zero);
            material.SetVector(PropInnerShadow, Vector4.zero);
            material.SetVector(PropInnerShadowOffset, Vector4.zero);
            material.SetVector(PropInnerShadowColor, Vector4.zero);
        }

        void ApplyPathGeometry(Material material, Vector4[] fillPathPoints, Vector4[] strokePathPoints)
        {
            Array.Clear(_pathPointBuffer, 0, _pathPointBuffer.Length);
            Array.Clear(_strokePathPointBuffer, 0, _strokePathPointBuffer.Length);

            if (fillPathPoints == null || fillPathPoints.Length < 2)
            {
                material.SetInt(PropPathPointCount, 0);
                material.SetVectorArray(PropPathPoints, _pathPointBuffer);
            }
            else
            {
                int fillPointCount = Mathf.Min(fillPathPoints.Length, MaxPathShaderPoints);
                for (int i = 0; i < fillPointCount; i++)
                {
                    _pathPointBuffer[i] = fillPathPoints[i];
                }

                material.SetInt(PropPathPointCount, fillPointCount);
                material.SetVectorArray(PropPathPoints, _pathPointBuffer);
            }

            if (strokePathPoints == null || strokePathPoints.Length < 2)
            {
                material.SetInt(PropStrokePathPointCount, 0);
                material.SetVectorArray(PropStrokePathPoints, _strokePathPointBuffer);
                return;
            }

            int strokePointCount = Mathf.Min(strokePathPoints.Length, MaxPathShaderPoints);
            for (int i = 0; i < strokePointCount; i++)
            {
                _strokePathPointBuffer[i] = strokePathPoints[i];
            }

            material.SetInt(PropStrokePathPointCount, strokePointCount);
            material.SetVectorArray(PropStrokePathPoints, _strokePathPointBuffer);
        }

        static void ClearLayerSlots(Material material)
        {
            for (int i = 0; i < MaxFillLayers; i++)
            {
                material.SetVector(FillLayerNames[i], Vector4.zero);
                material.SetVector(FillTransformANames[i], new Vector4(1f, 0f, 0f, 0f));
                material.SetVector(FillTransformBNames[i], new Vector4(0f, 1f, 0f, 0f));
                for (int stop = 0; stop < MaxStops; stop++)
                {
                    material.SetVector(FormatIndexedPropertyName(FillStopPropertyFormat, i, stop), Vector4.zero);
                    material.SetVector(FormatIndexedPropertyName(FillColorPropertyFormat, i, stop), Vector4.zero);
                }
            }

            for (int i = 0; i < MaxStrokeLayers; i++)
            {
                material.SetVector(StrokeLayerNames[i], Vector4.zero);
                material.SetVector(StrokeTransformANames[i], new Vector4(1f, 0f, 0f, 0f));
                material.SetVector(StrokeTransformBNames[i], new Vector4(0f, 1f, 0f, 0f));
                for (int stop = 0; stop < MaxStops; stop++)
                {
                    material.SetVector(FormatIndexedPropertyName(StrokeStopPropertyFormat, i, stop), Vector4.zero);
                    material.SetVector(FormatIndexedPropertyName(StrokeColorPropertyFormat, i, stop), Vector4.zero);
                }
            }
        }

        static void ApplyLayer(Material material, LayerSpec layer, int index, bool fillLayer)
        {
            string layerName = fillLayer ? FillLayerNames[index] : StrokeLayerNames[index];
            string transformAName = fillLayer ? FillTransformANames[index] : StrokeTransformANames[index];
            string transformBName = fillLayer ? FillTransformBNames[index] : StrokeTransformBNames[index];

            material.SetVector(layerName, new Vector4(layer.Kind, layer.Opacity, layer.BlendMode, layer.Stops.Count));
            material.SetVector(transformAName, new Vector4(layer.TransformRowA.x, layer.TransformRowA.y, layer.TransformRowA.z, 0f));
            material.SetVector(transformBName, new Vector4(layer.TransformRowB.x, layer.TransformRowB.y, layer.TransformRowB.z, 0f));

            for (int i = 0; i < layer.Stops.Count && i < MaxStops; i++)
            {
                StopSpec stop = layer.Stops[i];
                string stopName = fillLayer ? FormatIndexedPropertyName(FillStopPropertyFormat, index, i) : FormatIndexedPropertyName(StrokeStopPropertyFormat, index, i);
                string colorName = fillLayer ? FormatIndexedPropertyName(FillColorPropertyFormat, index, i) : FormatIndexedPropertyName(StrokeColorPropertyFormat, index, i);
                material.SetVector(stopName, new Vector4(stop.Position, stop.Alpha, 0f, 0f));
                material.SetVector(colorName, new Vector4(stop.Color.r, stop.Color.g, stop.Color.b, stop.Color.a));
            }
        }

        static string FormatIndexedPropertyName(string format, int layerIndex, int stopIndex)
        {
            return string.Format(CultureInfo.InvariantCulture, format, layerIndex, stopIndex);
        }

        static Vector4[] SampleSvgPath(string path, float width, float height)
        {
            List<SvgCommand> commands = ParseSvgPath(path);
            int cubicSampleSteps = DetermineCubicSampleSteps(commands);
            List<Vector4> points = new List<Vector4>(MaxPathShaderPoints);
            Vector2 current = Vector2.zero;
            Vector2 start = Vector2.zero;
            Vector2 lastControl = Vector2.zero;
            bool hasLastControl = false;
            int subpathCount = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                SvgCommand command = commands[i];
                switch (command.Type)
                {
                    case 'M':
                        current = new Vector2(command.Values[0], command.Values[1]);
                        start = current;
                        AddNormalized(points, current, width, height, true);
                        hasLastControl = false;
                        subpathCount++;
                        break;
                    case 'L':
                    {
                        Vector2 next = new Vector2(command.Values[0], command.Values[1]);
                        SampleLine(points, current, next, width, height);
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'C':
                    {
                        Vector2 c1 = new Vector2(command.Values[0], command.Values[1]);
                        Vector2 c2 = new Vector2(command.Values[2], command.Values[3]);
                        Vector2 next = new Vector2(command.Values[4], command.Values[5]);
                        SampleCubic(points, current, c1, c2, next, cubicSampleSteps, width, height);
                        current = next;
                        lastControl = c2;
                        hasLastControl = true;
                        break;
                    }
                    case 'Q':
                    {
                        Vector2 c = new Vector2(command.Values[0], command.Values[1]);
                        Vector2 next = new Vector2(command.Values[2], command.Values[3]);
                        Vector2 c1 = current + (2f / 3f) * (c - current);
                        Vector2 c2 = next + (2f / 3f) * (c - next);
                        SampleCubic(points, current, c1, c2, next, cubicSampleSteps, width, height);
                        current = next;
                        lastControl = c;
                        hasLastControl = true;
                        break;
                    }
                    case 'S':
                    {
                        Vector2 reflected = hasLastControl ? current + (current - lastControl) : current;
                        Vector2 c2 = new Vector2(command.Values[0], command.Values[1]);
                        Vector2 next = new Vector2(command.Values[2], command.Values[3]);
                        SampleCubic(points, current, reflected, c2, next, cubicSampleSteps, width, height);
                        current = next;
                        lastControl = c2;
                        hasLastControl = true;
                        break;
                    }
                    case 'A':
                    {
                        float rx = command.Values[0];
                        float ry = command.Values[1];
                        float xAxisRotation = command.Values[2];
                        bool largeArc = command.Values[3] > 0.5f;
                        bool sweep = command.Values[4] > 0.5f;
                        Vector2 next = new Vector2(command.Values[5], command.Values[6]);
                        SampleArc(points, current, rx, ry, xAxisRotation, largeArc, sweep, next, width, height);
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'Z':
                        SampleLine(points, current, start, width, height);
                        current = start;
                        hasLastControl = false;
                        break;
                }
            }

            if (points.Count == 0)
            {
                return Array.Empty<Vector4>();
            }

            if (subpathCount <= 1 && points.Count > 1 && !SamePoint(points[0], points[points.Count - 1]))
            {
                points.Add(new Vector4(points[0].x, points[0].y, 0f, 0f));
            }

            if (points.Count <= MaxPathShaderPoints)
            {
                return points.ToArray();
            }

            return points.Take(MaxPathShaderPoints).ToArray();
        }

        static int DetermineCubicSampleSteps(List<SvgCommand> commands)
        {
            if (commands == null || commands.Count == 0)
            {
                return DefaultCubicSampleSteps;
            }

            int cubicCount = 0;
            int lineCount = 0;
            int arcCount = 0;
            int moveCount = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                switch (commands[i].Type)
                {
                    case 'M':
                        moveCount++;
                        break;
                    case 'C':
                    case 'Q':
                    case 'S':
                        cubicCount++;
                        break;
                    case 'L':
                    case 'Z':
                        lineCount++;
                        break;
                    case 'A':
                        arcCount++;
                        break;
                }
            }

            if (cubicCount == 0)
            {
                return MinCubicSampleSteps;
            }

            for (int i = 0; i < CubicSampleStepCandidates.Length; i++)
            {
                int steps = CubicSampleStepCandidates[i];
                if (EstimatePathPointCount(moveCount, cubicCount, lineCount, arcCount, steps) <= MaxPathShaderPoints)
                {
                    return steps;
                }
            }

            return MinCubicSampleSteps;
        }

        static int EstimatePathPointCount(int moveCount, int cubicCount, int lineCount, int arcCount, int cubicSteps)
        {
            return Mathf.Max(1, moveCount) + cubicCount * cubicSteps + lineCount * LineSampleSteps + arcCount * ArcPointEstimateSamples;
        }

        static List<SvgCommand> ParseSvgPath(string path)
        {
            List<string> tokens = TokenizeSvgPath(path);
            List<SvgCommand> result = new List<SvgCommand>();
            int index = 0;
            char command = ' ';
            Vector2 current = Vector2.zero;
            Vector2 start = Vector2.zero;
            Vector2 lastControl = Vector2.zero;
            bool hasLastControl = false;

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
                    result.Add(new SvgCommand('Z', Array.Empty<float>()));
                    current = start;
                    hasLastControl = false;
                    continue;
                }

                int arity = lower switch
                {
                    'm' => 2,
                    'l' => 2,
                    'h' => 1,
                    'v' => 1,
                    'c' => 6,
                    's' => 4,
                    'q' => 4,
                    't' => 2,
                    'a' => 7,
                    _ => throw new InvalidOperationException(UnsupportedSvgPathCommandMessagePrefix + command)
                };

                if (index + arity > tokens.Count)
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
                        result.Add(new SvgCommand('M', new[] { next.x, next.y }));
                        current = next;
                        start = next;
                        command = char.IsLower(command) ? 'l' : 'L';
                        hasLastControl = false;
                        break;
                    }
                    case 'l':
                    {
                        Vector2 next = ToAbsolute(command, current, values[0], values[1]);
                        result.Add(new SvgCommand('L', new[] { next.x, next.y }));
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'h':
                    {
                        Vector2 next = char.IsLower(command)
                            ? new Vector2(current.x + values[0], current.y)
                            : new Vector2(values[0], current.y);
                        result.Add(new SvgCommand('L', new[] { next.x, next.y }));
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'v':
                    {
                        Vector2 next = char.IsLower(command)
                            ? new Vector2(current.x, current.y + values[0])
                            : new Vector2(current.x, values[0]);
                        result.Add(new SvgCommand('L', new[] { next.x, next.y }));
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                    case 'c':
                    {
                        Vector2 c1 = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 c2 = ToAbsolute(command, current, values[2], values[3]);
                        Vector2 next = ToAbsolute(command, current, values[4], values[5]);
                        result.Add(new SvgCommand('C', new[] { c1.x, c1.y, c2.x, c2.y, next.x, next.y }));
                        current = next;
                        lastControl = c2;
                        hasLastControl = true;
                        break;
                    }
                    case 's':
                    {
                        Vector2 c2 = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 next = ToAbsolute(command, current, values[2], values[3]);
                        result.Add(new SvgCommand('S', new[] { c2.x, c2.y, next.x, next.y }));
                        current = next;
                        lastControl = c2;
                        hasLastControl = true;
                        break;
                    }
                    case 'q':
                    {
                        Vector2 c = ToAbsolute(command, current, values[0], values[1]);
                        Vector2 next = ToAbsolute(command, current, values[2], values[3]);
                        result.Add(new SvgCommand('Q', new[] { c.x, c.y, next.x, next.y }));
                        current = next;
                        lastControl = c;
                        hasLastControl = true;
                        break;
                    }
                    case 't':
                    {
                        Vector2 reflected = hasLastControl ? current + (current - lastControl) : current;
                        Vector2 next = ToAbsolute(command, current, values[0], values[1]);
                        result.Add(new SvgCommand('Q', new[] { reflected.x, reflected.y, next.x, next.y }));
                        current = next;
                        lastControl = reflected;
                        hasLastControl = true;
                        break;
                    }
                    case 'a':
                    {
                        Vector2 next = ToAbsolute(command, current, values[5], values[6]);
                        result.Add(new SvgCommand('A', new[]
                        {
                            Mathf.Abs(values[0]),
                            Mathf.Abs(values[1]),
                            values[2],
                            values[3],
                            values[4],
                            next.x,
                            next.y
                        }));
                        current = next;
                        hasLastControl = false;
                        break;
                    }
                }
            }

            return result;
        }

        static List<string> TokenizeSvgPath(string path)
        {
            List<string> tokens = new List<string>();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < path.Length; i++)
            {
                char ch = path[i];
                if (IsSvgPathCommand(ch))
                {
                    FlushSvgToken(builder, tokens);
                    tokens.Add(ch.ToString());
                    continue;
                }

                if (char.IsWhiteSpace(ch) || ch == ',')
                {
                    FlushSvgToken(builder, tokens);
                    continue;
                }

                if ((ch == '-' || ch == '+') &&
                    builder.Length > 0 &&
                    builder[builder.Length - 1] != 'e' &&
                    builder[builder.Length - 1] != 'E')
                {
                    FlushSvgToken(builder, tokens);
                }

                builder.Append(ch);
            }

            FlushSvgToken(builder, tokens);
            return tokens;
        }

        static bool IsSvgPathCommand(char ch)
        {
            switch (ch)
            {
                case 'M':
                case 'm':
                case 'L':
                case 'l':
                case 'H':
                case 'h':
                case 'V':
                case 'v':
                case 'C':
                case 'c':
                case 'S':
                case 's':
                case 'Q':
                case 'q':
                case 'T':
                case 't':
                case 'A':
                case 'a':
                case 'Z':
                case 'z':
                    return true;
                default:
                    return false;
            }
        }

        static void FlushSvgToken(StringBuilder builder, List<string> tokens)
        {
            if (builder.Length == 0)
            {
                return;
            }

            tokens.Add(builder.ToString());
            builder.Clear();
        }

        static Vector2 ToAbsolute(char command, Vector2 current, float x, float y)
        {
            return char.IsLower(command)
                ? new Vector2(current.x + x, current.y + y)
                : new Vector2(x, y);
        }

        static void SampleLine(List<Vector4> points, Vector2 a, Vector2 b, float width, float height)
        {
            for (int i = 1; i <= LineSampleSteps; i++)
            {
                AddNormalized(points, Vector2.Lerp(a, b, i / (float)LineSampleSteps), width, height);
            }
        }

        static void SampleCubic(List<Vector4> points, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps, float width, float height)
        {
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
                AddNormalized(points, point, width, height);
            }
        }

        static void SampleArc(List<Vector4> points, Vector2 start, float rx, float ry, float xAxisRotation, bool largeArc, bool sweep, Vector2 end, float width, float height)
        {
            rx = Mathf.Abs(rx);
            ry = Mathf.Abs(ry);

            if (rx <= ArcDistanceEpsilon || ry <= ArcDistanceEpsilon || Vector2.Distance(start, end) <= ArcDistanceEpsilon)
            {
                SampleLine(points, start, end, width, height);
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

            float arcExtent = Mathf.Max(rx, ry) * Mathf.Abs(deltaTheta);
            int steps = Mathf.Clamp(Mathf.CeilToInt(arcExtent / ArcSamplingResolution), MinCubicSampleSteps, ArcPointEstimateSamples);
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = theta1 + deltaTheta * t;
                float cosAngle = Mathf.Cos(angle);
                float sinAngle = Mathf.Sin(angle);
                Vector2 point = new Vector2(
                    cx + cosPhi * rx * cosAngle - sinPhi * ry * sinAngle,
                    cy + sinPhi * rx * cosAngle + cosPhi * ry * sinAngle);
                AddNormalized(points, point, width, height);
            }
        }

        static float ArcAngle(float ux, float uy, float vx, float vy)
        {
            float dot = ux * vx + uy * vy;
            float mag = Mathf.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
            if (mag <= ArcComputationEpsilon)
            {
                return 0f;
            }

            float clamped = Mathf.Clamp(dot / mag, -1f, 1f);
            float angle = Mathf.Acos(clamped);
            float cross = ux * vy - uy * vx;
            return cross < 0f ? -angle : angle;
        }

        static bool SamePoint(Vector4 a, Vector4 b)
        {
            return Vector2.SqrMagnitude(new Vector2(a.x - b.x, a.y - b.y)) <= PointEqualityEpsilonSqr;
        }

        static void AddNormalized(List<Vector4> points, Vector2 point, float width, float height, bool startsSubpath = false)
        {
            Vector2 normalized = new Vector2(
                width > 0f ? point.x / width : 0f,
                height > 0f ? point.y / height : 0f);

            if (points.Count == 0 || startsSubpath)
            {
                points.Add(new Vector4(normalized.x, normalized.y, startsSubpath ? 1f : 0f, 0f));
                return;
            }

            Vector4 last = points[points.Count - 1];
            if (Vector2.SqrMagnitude(new Vector2(last.x - normalized.x, last.y - normalized.y)) > PointEqualityEpsilonSqr)
            {
                points.Add(new Vector4(normalized.x, normalized.y, 0f, 0f));
            }
        }
    }
}
