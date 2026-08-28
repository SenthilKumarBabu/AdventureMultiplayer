Shader "D.A. Assets/Figmage/StyledRoundedRect"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _CanvasSize;
            float4 _ShapeSize;
            float4 _ShapeOffset;
            float4 _CornerRadii;
            float _CornerSmoothing;
            float _ShapeRotation;
            float _ShapeOpacity;
            float _StrokeWeight;
            float _StrokeAlign;
            float _FillExtendsUnderStroke;
            float _Antialiasing;
            float _RenderMode;
            float _DiamondExponent;
            float _StrokeAngularOffset;
            #define MAX_PATH_POINTS 512
            int _PathPointCount;
            float4 _PathPoints[MAX_PATH_POINTS];
            int _StrokePathPointCount;
            float4 _StrokePathPoints[MAX_PATH_POINTS];

            int _FillCount;
            float4 _FillLayer0;
            float4 _FillLayer1;
            float4 _FillLayer2;
            float4 _FillTransformA0;
            float4 _FillTransformA1;
            float4 _FillTransformA2;
            float4 _FillTransformB0;
            float4 _FillTransformB1;
            float4 _FillTransformB2;

            int _StrokeCount;
            float4 _StrokeLayer0;
            float4 _StrokeLayer1;
            float4 _StrokeTransformA0;
            float4 _StrokeTransformA1;
            float4 _StrokeTransformB0;
            float4 _StrokeTransformB1;

            float4 _FillStop0_0; float4 _FillStop0_1; float4 _FillStop0_2; float4 _FillStop0_3; float4 _FillStop0_4;
            float4 _FillStop1_0; float4 _FillStop1_1; float4 _FillStop1_2; float4 _FillStop1_3; float4 _FillStop1_4;
            float4 _FillStop2_0; float4 _FillStop2_1; float4 _FillStop2_2; float4 _FillStop2_3; float4 _FillStop2_4;
            float4 _FillColor0_0; float4 _FillColor0_1; float4 _FillColor0_2; float4 _FillColor0_3; float4 _FillColor0_4;
            float4 _FillColor1_0; float4 _FillColor1_1; float4 _FillColor1_2; float4 _FillColor1_3; float4 _FillColor1_4;
            float4 _FillColor2_0; float4 _FillColor2_1; float4 _FillColor2_2; float4 _FillColor2_3; float4 _FillColor2_4;

            float4 _StrokeStop0_0; float4 _StrokeStop0_1; float4 _StrokeStop0_2; float4 _StrokeStop0_3; float4 _StrokeStop0_4;
            float4 _StrokeStop1_0; float4 _StrokeStop1_1; float4 _StrokeStop1_2; float4 _StrokeStop1_3; float4 _StrokeStop1_4;
            float4 _StrokeColor0_0; float4 _StrokeColor0_1; float4 _StrokeColor0_2; float4 _StrokeColor0_3; float4 _StrokeColor0_4;
            float4 _StrokeColor1_0; float4 _StrokeColor1_1; float4 _StrokeColor1_2; float4 _StrokeColor1_3; float4 _StrokeColor1_4;

            float4 _DropShadow;
            float4 _DropShadowOffset;
            float4 _DropShadowColor;
            float4 _InnerShadow;
            float4 _InnerShadowOffset;
            float4 _InnerShadowColor;
            sampler2D _InputBodyTex;
            float _UseInputBodyTex;
            float _BodyCompositeMode;
            float _BodyDisjointComposite;
            sampler2D _LayerGradientTex;
            float _UseLayerGradientTex;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float2 Rotate(float2 p, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            float3 LinearToSrgb(float3 c)
            {
                c = max(c, 0.0);
                return lerp(c * 12.92, 1.055 * pow(c, 1.0 / 2.4) - 0.055, step(0.0031308, c));
            }

            float3 SrgbToLinear(float3 c)
            {
                c = saturate(c);
                return lerp(c / 12.92, pow((c + 0.055) / 1.055, 2.4), step(0.04045, c));
            }

            float DistanceToSegment(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float denom = max(dot(ab, ab), 1e-6);
                float t = saturate(dot(p - a, ab) / denom);
                return length(p - lerp(a, b, t));
            }

            float SignedEdge(float2 a, float2 b, float2 p)
            {
                float2 ab = b - a;
                float2 ap = p - a;
                return ab.x * ap.y - ab.y * ap.x;
            }

            bool FillPathContainsUv(float2 uv)
            {
                int winding = 0;

                [loop] for (int i = 0; i < (MAX_PATH_POINTS - 1); i++)
                {
                    if (i + 1 >= _PathPointCount)
                    {
                        break;
                    }

                    float2 aUv = _PathPoints[i].xy;
                    float2 bUv = _PathPoints[i + 1].xy;
                    if (_PathPoints[i + 1].z > 0.5)
                    {
                        continue;
                    }

                    float edge = SignedEdge(aUv, bUv, uv);
                    if (aUv.y <= uv.y)
                    {
                        if (bUv.y > uv.y && edge > 0.0)
                        {
                            winding++;
                        }
                    }
                    else if (bUv.y <= uv.y && edge < 0.0)
                    {
                        winding--;
                    }
                }

                return winding != 0;
            }

            bool StrokePathContainsUv(float2 uv)
            {
                int winding = 0;

                [loop] for (int i = 0; i < (MAX_PATH_POINTS - 1); i++)
                {
                    if (i + 1 >= _StrokePathPointCount)
                    {
                        break;
                    }

                    float2 aUv = _StrokePathPoints[i].xy;
                    float2 bUv = _StrokePathPoints[i + 1].xy;
                    if (_StrokePathPoints[i + 1].z > 0.5)
                    {
                        continue;
                    }

                    float edge = SignedEdge(aUv, bUv, uv);
                    if (aUv.y <= uv.y)
                    {
                        if (bUv.y > uv.y && edge > 0.0)
                        {
                            winding++;
                        }
                    }
                    else if (bUv.y <= uv.y && edge < 0.0)
                    {
                        winding--;
                    }
                }

                return winding != 0;
            }

            float FillPathCoverage(float2 p, float2 size)
            {
                float coverage = 0.0;
                const float offsets[8] = { -0.4375, -0.3125, -0.1875, -0.0625, 0.0625, 0.1875, 0.3125, 0.4375 };

                [unroll(8)] for (int yi = 0; yi < 8; yi++)
                {
                    [unroll(8)] for (int xi = 0; xi < 8; xi++)
                    {
                        float2 sample = p + float2(offsets[xi], offsets[yi]);
                        float2 uv = sample / max(size, 1.0) + 0.5;
                        coverage += FillPathContainsUv(uv) ? 1.0 : 0.0;
                    }
                }

                return coverage * (1.0 / 64.0);
            }

            float StrokePathCoverage(float2 p, float2 size)
            {
                float coverage = 0.0;
                const float offsets[8] = { -0.4375, -0.3125, -0.1875, -0.0625, 0.0625, 0.1875, 0.3125, 0.4375 };

                [unroll(8)] for (int yi = 0; yi < 8; yi++)
                {
                    [unroll(8)] for (int xi = 0; xi < 8; xi++)
                    {
                        float2 sample = p + float2(offsets[xi], offsets[yi]);
                        float2 uv = sample / max(size, 1.0) + 0.5;
                        coverage += StrokePathContainsUv(uv) ? 1.0 : 0.0;
                    }
                }

                return coverage * (1.0 / 64.0);
            }

            float sdFillPolylinePath(float2 p, float2 size)
            {
                float2 uv = p / max(size, 1.0) + 0.5;
                float minDist = 1e6;
                int winding = 0;

                [loop] for (int i = 0; i < (MAX_PATH_POINTS - 1); i++)
                {
                    if (i + 1 >= _PathPointCount)
                    {
                        break;
                    }

                    float2 aUv = _PathPoints[i].xy;
                    float2 bUv = _PathPoints[i + 1].xy;
                    if (_PathPoints[i + 1].z > 0.5)
                    {
                        continue;
                    }

                    float2 a = (aUv - 0.5) * size;
                    float2 b = (bUv - 0.5) * size;
                    minDist = min(minDist, DistanceToSegment(p, a, b));

                    float edge = SignedEdge(aUv, bUv, uv);
                    if (aUv.y <= uv.y)
                    {
                        if (bUv.y > uv.y && edge > 0.0)
                        {
                            winding++;
                        }
                    }
                    else if (bUv.y <= uv.y && edge < 0.0)
                    {
                        winding--;
                    }
                }

                return winding != 0 ? -minDist : minDist;
            }

            float sdStrokePolylinePath(float2 p, float2 size)
            {
                float2 uv = p / max(size, 1.0) + 0.5;
                float minDist = 1e6;
                int winding = 0;

                [loop] for (int i = 0; i < (MAX_PATH_POINTS - 1); i++)
                {
                    if (i + 1 >= _StrokePathPointCount)
                    {
                        break;
                    }

                    float2 aUv = _StrokePathPoints[i].xy;
                    float2 bUv = _StrokePathPoints[i + 1].xy;
                    if (_StrokePathPoints[i + 1].z > 0.5)
                    {
                        continue;
                    }

                    float2 a = (aUv - 0.5) * size;
                    float2 b = (bUv - 0.5) * size;
                    minDist = min(minDist, DistanceToSegment(p, a, b));

                    float edge = SignedEdge(aUv, bUv, uv);
                    if (aUv.y <= uv.y)
                    {
                        if (bUv.y > uv.y && edge > 0.0)
                        {
                            winding++;
                        }
                    }
                    else if (bUv.y <= uv.y && edge < 0.0)
                    {
                        winding--;
                    }
                }

                return winding != 0 ? -minDist : minDist;
            }

            float SelectCornerRadius(float2 p, float4 radii)
            {
                if (p.x < 0.0)
                {
                    return p.y < 0.0 ? radii.x : radii.w;
                }

                return p.y < 0.0 ? radii.y : radii.z;
            }

            float sdRoundedRect(float2 p, float2 size, float4 radii)
            {
                float radius = SelectCornerRadius(p, radii);
                radius = clamp(radius, 0.0, min(size.x, size.y) * 0.5);
                float2 halfSize = size * 0.5;
                float2 q = abs(p) - (halfSize - radius);
                float2 corner = max(q, 0.0);
                float smoothing = saturate(_CornerSmoothing);
                float exponent = lerp(2.0, 4.0, smoothing);
                float cornerDistance = radius <= 1e-4
                    ? length(corner)
                    : (pow(pow(corner.x / radius, exponent) + pow(corner.y / radius, exponent), 1.0 / exponent) - 1.0) * radius;
                return cornerDistance + min(max(q.x, q.y), 0.0);
            }

            float ShapeDistance(float2 p, float2 size, float4 radii)
            {
                float result = sdRoundedRect(p, size, radii);
                if (_PathPointCount > 1)
                {
                    result = sdFillPolylinePath(p, size);
                }

                return result;
            }

            float ShapeAlpha(float2 p, float2 size, float4 radii)
            {
                float distance = ShapeDistance(p, size, radii);
                return saturate(0.5 - distance / max(0.5, _Antialiasing));
            }

            float ShapeMask(float2 p, float2 size, float4 radii)
            {
                float aa = max(0.5, _Antialiasing);
                float distance = ShapeDistance(p, size, radii);
                float result = saturate(0.5 - distance / aa);

                if (_PathPointCount > 1 && abs(distance) < 1.0)
                {
                    result = FillPathCoverage(p, size);
                }

                return result;
            }

            float ContentMask(float2 p, float2 size, float4 radii, float strokeWeight, float strokeAlign, float strokeCount)
            {
                float aa = max(0.5, _Antialiasing);
                float distance = ShapeDistance(p, size, radii);
                float inset = 0.0;
                if (strokeCount > 0.5)
                {
                    inset = strokeAlign < 0.5 ? strokeWeight : (strokeAlign < 1.5 ? strokeWeight * 0.5 : 0.0);
                }

                float result = saturate(0.5 - (distance + inset) / aa);
                if (_PathPointCount > 1 && strokeCount < 0.5 && abs(distance + inset) < 1.0)
                {
                    result = FillPathCoverage(p, size);
                }

                return result;
            }

            float FillMask(float2 p, float2 size, float4 radii, float strokeWeight, float strokeAlign, float strokeCount)
            {
                float result = ContentMask(p, size, radii, strokeWeight, strokeAlign, strokeCount);
                if (_FillExtendsUnderStroke > 0.5)
                {
                    result = ShapeMask(p, size, radii);
                }

                return result;
            }

            float4 InsetRadii(float4 radii, float inset)
            {
                return max(radii - inset, 0.0);
            }

            float StrokeMask(float2 p, float2 size, float4 radii, float strokeWeight, float strokeAlign)
            {
                float aa = max(0.5, _Antialiasing);
                float result = 0.0;

                if (strokeWeight <= 0.0 && _StrokePathPointCount <= 1)
                {
                    result = 0.0;
                }
                else if (_StrokePathPointCount > 1)
                {
                    float strokeDistance = sdStrokePolylinePath(p, size);
                    if (abs(strokeDistance) < 1.0)
                    {
                        result = StrokePathCoverage(p, size);
                    }
                    else
                    {
                        result = saturate(0.5 - strokeDistance / aa);
                    }

                    if (strokeAlign < 1.5)
                    {
                        result = saturate(result * ShapeMask(p, size, radii));
                    }
                }
                else
                {
                    float distance = ShapeDistance(p, size, radii);
                    float outer = (_PathPointCount > 1 && abs(distance) < 1.0)
                        ? FillPathCoverage(p, size)
                        : saturate(0.5 - distance / aa);

                    if (strokeAlign < 0.5)
                    {
                        float inner = saturate(0.5 - (distance + strokeWeight) / aa);
                        result = saturate(outer - inner);
                    }
                    else if (strokeAlign < 1.5)
                    {
                        float halfStroke = strokeWeight * 0.5;
                        float outer2 = saturate(0.5 - (distance - halfStroke) / aa);
                        float inner2 = saturate(0.5 - (distance + halfStroke) / aa);
                        result = saturate(outer2 - inner2);
                    }
                    else
                    {
                        float outer3 = saturate(0.5 - (distance - strokeWeight) / aa);
                        result = saturate(outer3 - outer);
                    }
                }

                return result;
            }

            float2 Invert2x3(float3 rowA, float3 rowB, float2 samplePoint)
            {
                float det = rowA.x * rowB.y - rowA.y * rowB.x;
                det = abs(det) < 1e-5 ? 1e-5 : det;
                float2 d = samplePoint - float2(rowA.z, rowB.z);
                return float2((rowB.y * d.x - rowA.y * d.y) / det, (-rowB.x * d.x + rowA.x * d.y) / det);
            }

            float2 Apply2x3(float3 rowA, float3 rowB, float2 samplePoint)
            {
                return float2(
                    rowA.x * samplePoint.x + rowA.y * samplePoint.y + rowA.z,
                    rowB.x * samplePoint.x + rowB.y * samplePoint.y + rowB.z);
            }

            float SampleStops(float t, int stopCount, float4 stop0, float4 stop1, float4 stop2, float4 stop3, float4 stop4, float4 col0, float4 col1, float4 col2, float4 col3, float4 col4, out float4 color)
            {
                float4 stops[5] = { stop0, stop1, stop2, stop3, stop4 };
                float4 cols[5] = { col0, col1, col2, col3, col4 };
                color = cols[0];
                if (stopCount <= 1)
                {
                    color.a *= stop0.y;
                    return t;
                }

                t = saturate(t);
                [unroll] for (int i = 0; i < 4; i++)
                {
                    if (i + 1 >= stopCount)
                    {
                        break;
                    }

                    float p0 = stops[i].x;
                    float p1 = stops[i + 1].x;
                    if (t >= p0 && t <= p1)
                    {
                        float denom = max(1e-5, p1 - p0);
                        float u = saturate((t - p0) / denom);
                        float alpha0 = stops[i].y;
                        float alpha1 = stops[i + 1].y;
                        float alpha = lerp(alpha0, alpha1, u);
                        float3 color0 = LinearToSrgb(cols[i].rgb);
                        float3 color1 = LinearToSrgb(cols[i + 1].rgb);
                        if (alpha0 >= 0.999 && alpha1 >= 0.999)
                        {
                            const float gradientGamma = 1.1;
                            float3 g0 = pow(max(color0, 1e-6), gradientGamma);
                            float3 g1 = pow(max(color1, 1e-6), gradientGamma);
                            float3 srgb = pow(max(lerp(g0, g1, u), 0.0), 1.0 / gradientGamma);
                            color.rgb = SrgbToLinear(srgb);
                        }
                        else
                        {
                            float3 srgb = lerp(color0, color1, u);
                            color.rgb = SrgbToLinear(srgb);
                        }
                        color.a = alpha;
                        return t;
                    }
                }

                color = cols[stopCount - 1];
                color.a *= stops[stopCount - 1].y;
                return t;
            }

            float4 EvaluatePaint(float2 paintUv, float2 localUv, float4 layer, float3 rowA, float3 rowB, float4 stop0, float4 stop1, float4 stop2, float4 stop3, float4 stop4, float4 col0, float4 col1, float4 col2, float4 col3, float4 col4, float angularMode)
            {
                int kind = (int)layer.x;
                int stopCount = (int)layer.w;
                float4 color;
                float t = 0.0;
                float2 uv = localUv;

                if (kind == 0)
                {
                    color = col0;
                    color.a *= layer.y;
                    return color;
                }

                if (kind == 1)
                {
                    float2 gradUv = Apply2x3(rowA, rowB, uv);
                    t = gradUv.x;
                }
                else if (kind == 2)
                {
                    float2 gradUv = Apply2x3(rowA, rowB, uv);
                    t = length(gradUv);
                }
                else if (kind == 3)
                {
                    float2 gradUv = Apply2x3(rowA, rowB, uv);
                    float angle = atan2(gradUv.y, gradUv.x);
                    t = frac(angle / (UNITY_TWO_PI) + 1.0);
                }
                else if (kind == 4)
                {
                    float2 gradUv = Apply2x3(rowA, rowB, uv);
                    t = pow(abs(gradUv.x) + abs(gradUv.y), max(_DiamondExponent, 0.01));
                }

                if (_UseLayerGradientTex > 0.5)
                {
                    color = tex2D(_LayerGradientTex, float2(saturate(t), 0.5));
                }
                else
                {
                    SampleStops(t, stopCount, stop0, stop1, stop2, stop3, stop4, col0, col1, col2, col3, col4, color);
                }
                color.a *= layer.y;
                return color;
            }

            float EvaluateShapeCoverage(float2 localSample, float2 size, float4 radii)
            {
                float distance = ShapeDistance(localSample, size, radii);
                return saturate(0.5 - distance);
            }

            float EvaluateExpandedShapeAlpha(float2 localSample, float2 size, float4 radii, float expansion)
            {
                float aa = max(0.5, _Antialiasing);
                float distance = ShapeDistance(localSample, size, radii) - expansion;
                return saturate(0.5 - distance / aa);
            }

            float3 BlendRgbSrgb(float3 dstSrgb, float3 srcSrgb, int mode)
            {
                if (mode == 1)
                {
                    return 1.0 - (1.0 - dstSrgb) * (1.0 - srcSrgb);
                }
                if (mode == 2)
                {
                    return saturate(dstSrgb + srcSrgb);
                }
                if (mode == 3)
                {
                    float3 lo = 2.0 * dstSrgb * srcSrgb;
                    float3 hi = 1.0 - 2.0 * (1.0 - dstSrgb) * (1.0 - srcSrgb);
                    return lerp(lo, hi, step(0.5, dstSrgb));
                }
                return srcSrgb;
            }

            float3 BlendRgb(float3 dst, float3 src, int mode)
            {
                float3 dstSrgb = LinearToSrgb(dst);
                float3 srcSrgb = LinearToSrgb(src);
                return SrgbToLinear(BlendRgbSrgb(dstSrgb, srcSrgb, mode));
            }

            float4 Composite(float4 dst, float4 src, int mode)
            {
                float dstAlpha = saturate(dst.a);
                float srcAlpha = saturate(src.a);
                float3 dstSrgb = LinearToSrgb(dst.rgb);
                float3 srcSrgb = LinearToSrgb(src.rgb);
                float outAlpha = srcAlpha + dstAlpha * (1.0 - srcAlpha);
                float3 blendedSrgb = mode == 0 ? srcSrgb : LinearToSrgb(BlendRgb(dst.rgb, src.rgb, mode));
                float3 sourceBackdropSrgb = lerp(srcSrgb, blendedSrgb, dstAlpha);
                float3 outPremulSrgb = sourceBackdropSrgb * srcAlpha + dstSrgb * dstAlpha * (1.0 - srcAlpha);
                float3 outSrgb = outAlpha > 1e-5 ? outPremulSrgb / outAlpha : 0.0;
                float4 result = float4(SrgbToLinear(outSrgb), outAlpha);

                if (mode == 2 && dstAlpha > 1e-5 && srcAlpha > 1e-5)
                {
                    float3 dstPremul = dstSrgb * dstAlpha;
                    float3 srcPremul = srcSrgb * srcAlpha;
                    float3 resPremul = saturate(dstPremul + srcPremul);
                    float resAlpha = srcAlpha + dstAlpha * (1.0 - srcAlpha);
                    float3 resSrgb = resAlpha > 1e-5 ? resPremul / resAlpha : 0.0;
                    result = float4(SrgbToLinear(resSrgb), resAlpha);
                }
                else if (mode != 0 && dstAlpha > 1e-5 && srcAlpha > 1e-5)
                {
                    float3 blendedModeSrgb = BlendRgbSrgb(dstSrgb, srcSrgb, mode);
                    float3 sourceBackdropModeSrgb = lerp(srcSrgb, blendedModeSrgb, dstAlpha);
                    float outAlphaSrgb = srcAlpha + dstAlpha * (1.0 - srcAlpha);
                    float3 outPremulModeSrgb = sourceBackdropModeSrgb * srcAlpha + dstSrgb * dstAlpha * (1.0 - srcAlpha);
                    float3 outModeSrgb = outAlphaSrgb > 1e-5 ? outPremulModeSrgb / outAlphaSrgb : 0.0;
                    result = float4(SrgbToLinear(outModeSrgb), outAlphaSrgb);
                }

                return result;
            }

            float4 CompositeDisjoint(float4 a, float4 b)
            {
                float alphaA = saturate(a.a);
                float alphaB = saturate(b.a);
                float outAlpha = saturate(alphaA + alphaB);
                float3 outPremul = a.rgb * alphaA + b.rgb * alphaB;
                float3 outRgb = outAlpha > 1e-5 ? outPremul / outAlpha : 0.0;
                return float4(outRgb, outAlpha);
            }

            float ShadowMask(float distance, float radius)
            {
                radius = max(1.0, radius * 0.5);
                if (distance <= 0.0)
                {
                    return 1.0;
                }

                return exp(-(distance * distance) / (2.0 * radius * radius));
            }

            float4 EvaluateScene(float2 screenUv)
            {
                float2 fragCoord = screenUv * _CanvasSize.xy;
                float2 centered = fragCoord - _CanvasSize.xy * 0.5;
                float2 shapeCentered = centered - _ShapeOffset.xy;
                float2 local = Rotate(shapeCentered, _ShapeRotation);
                float2 size = _ShapeSize.xy;
                float2 paintUv = shapeCentered / max(size, 1.0) + 0.5;
                float2 localUv = local / max(size, 1.0) + 0.5;
                float4 radii = _CornerRadii;
                float4 result = float4(0, 0, 0, 0);

                if (_RenderMode > 0.5)
                {
                    float shadowOuterExpand = _StrokeCount > 0.5 ? (_StrokeAlign > 1.5 ? _StrokeWeight : (_StrokeAlign > 0.5 ? _StrokeWeight * 0.5 : 0.0)) : 0.0;
                    float shadowDistance = ShapeDistance(local, size, radii) - shadowOuterExpand - _DropShadow.y;
                    float alpha = saturate(0.5 - shadowDistance / max(0.5, _Antialiasing));
                    result = float4(alpha, alpha, alpha, alpha);
                }
                else
                {
                    result = _UseInputBodyTex > 0.5 ? tex2D(_InputBodyTex, screenUv) : float4(0, 0, 0, 0);
                    if (_UseInputBodyTex > 0.5)
                    {
                        result.rgb = SrgbToLinear(result.rgb);
                    }
                    if (_DropShadow.w > 0.5)
                    {
                        float2 shadowScreen = shapeCentered - _DropShadowOffset.xy;
                        float shadowOuterExpand = _StrokeCount > 0.5 ? (_StrokeAlign > 1.5 ? _StrokeWeight : (_StrokeAlign > 0.5 ? _StrokeWeight * 0.5 : 0.0)) : 0.0;
                        float sigma = max(1.0, _DropShadow.x * 0.5);
                        float axisWeight = exp(-0.5);
                        float diagWeight = exp(-1.0);
                        float totalWeight = 1.0 + 4.0 * axisWeight + 4.0 * diagWeight;
                        float2 axisX = float2(sigma, 0.0);
                        float2 axisY = float2(0.0, sigma);
                        float2 diag = float2(sigma * 0.70710678, sigma * 0.70710678);

                        float shadowAlpha = 0.0;
                        float2 shadowLocal0 = Rotate(shadowScreen, _ShapeRotation);
                        float2 shadowLocal1 = Rotate(shadowScreen + axisX, _ShapeRotation);
                        float2 shadowLocal2 = Rotate(shadowScreen - axisX, _ShapeRotation);
                        float2 shadowLocal3 = Rotate(shadowScreen + axisY, _ShapeRotation);
                        float2 shadowLocal4 = Rotate(shadowScreen - axisY, _ShapeRotation);
                        float2 shadowLocal5 = Rotate(shadowScreen + diag, _ShapeRotation);
                        float2 shadowLocal6 = Rotate(shadowScreen + float2(-diag.x, diag.y), _ShapeRotation);
                        float2 shadowLocal7 = Rotate(shadowScreen + float2(diag.x, -diag.y), _ShapeRotation);
                        float2 shadowLocal8 = Rotate(shadowScreen - diag, _ShapeRotation);

                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal0, size, radii) - shadowOuterExpand - _DropShadow.y));
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal1, size, radii) - shadowOuterExpand - _DropShadow.y)) * axisWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal2, size, radii) - shadowOuterExpand - _DropShadow.y)) * axisWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal3, size, radii) - shadowOuterExpand - _DropShadow.y)) * axisWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal4, size, radii) - shadowOuterExpand - _DropShadow.y)) * axisWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal5, size, radii) - shadowOuterExpand - _DropShadow.y)) * diagWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal6, size, radii) - shadowOuterExpand - _DropShadow.y)) * diagWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal7, size, radii) - shadowOuterExpand - _DropShadow.y)) * diagWeight;
                        shadowAlpha += saturate(0.5 - (ShapeDistance(shadowLocal8, size, radii) - shadowOuterExpand - _DropShadow.y)) * diagWeight;
                        shadowAlpha = shadowAlpha / totalWeight * _DropShadowColor.a;
                        result = Composite(result, float4(_DropShadowColor.rgb, shadowAlpha), (int)_DropShadow.z);
                    }

                float fillMask = FillMask(local, size, radii, _StrokeWeight, _StrokeAlign, _StrokeCount);
                float4 fillColor = float4(0, 0, 0, 0);
                if (_FillCount > 0)
                {
                    fillColor = Composite(fillColor, EvaluatePaint(paintUv, localUv, _FillLayer0, _FillTransformA0.xyz, _FillTransformB0.xyz, _FillStop0_0, _FillStop0_1, _FillStop0_2, _FillStop0_3, _FillStop0_4, _FillColor0_0, _FillColor0_1, _FillColor0_2, _FillColor0_3, _FillColor0_4, 0.0), (int)_FillLayer0.z);
                }
                if (_FillCount > 1)
                {
                    fillColor = Composite(fillColor, EvaluatePaint(paintUv, localUv, _FillLayer1, _FillTransformA1.xyz, _FillTransformB1.xyz, _FillStop1_0, _FillStop1_1, _FillStop1_2, _FillStop1_3, _FillStop1_4, _FillColor1_0, _FillColor1_1, _FillColor1_2, _FillColor1_3, _FillColor1_4, 0.0), (int)_FillLayer1.z);
                }
                if (_FillCount > 2)
                {
                    fillColor = Composite(fillColor, EvaluatePaint(paintUv, localUv, _FillLayer2, _FillTransformA2.xyz, _FillTransformB2.xyz, _FillStop2_0, _FillStop2_1, _FillStop2_2, _FillStop2_3, _FillStop2_4, _FillColor2_0, _FillColor2_1, _FillColor2_2, _FillColor2_3, _FillColor2_4, 0.0), (int)_FillLayer2.z);
                }
                fillColor.a *= fillMask * _ShapeOpacity;
                float4 bodyColor = fillColor;

                if (_StrokeCount > 0)
                {
                    float strokeMask = StrokeMask(local, size, radii, _StrokeWeight, _StrokeAlign);
                    float4 strokeLayer0 = EvaluatePaint(paintUv, localUv, _StrokeLayer0, _StrokeTransformA0.xyz, _StrokeTransformB0.xyz, _StrokeStop0_0, _StrokeStop0_1, _StrokeStop0_2, _StrokeStop0_3, _StrokeStop0_4, _StrokeColor0_0, _StrokeColor0_1, _StrokeColor0_2, _StrokeColor0_3, _StrokeColor0_4, 1.0);

                    if (_StrokeCount > 1 && _StrokeAlign < 1.5)
                    {
                        strokeLayer0.a *= strokeMask * _ShapeOpacity;
                        bodyColor = Composite(bodyColor, strokeLayer0, 0);

                        float4 strokeLayer1 = EvaluatePaint(paintUv, localUv, _StrokeLayer1, _StrokeTransformA1.xyz, _StrokeTransformB1.xyz, _StrokeStop1_0, _StrokeStop1_1, _StrokeStop1_2, _StrokeStop1_3, _StrokeStop1_4, _StrokeColor1_0, _StrokeColor1_1, _StrokeColor1_2, _StrokeColor1_3, _StrokeColor1_4, 1.0);
                        strokeLayer1.a *= strokeMask * _ShapeOpacity;
                        bodyColor = Composite(bodyColor, strokeLayer1, (int)_StrokeLayer1.z);
                    }
                    else
                    {
                        float4 strokeColor = strokeLayer0;
                        if (_StrokeCount > 1)
                        {
                            float4 strokeLayer1 = EvaluatePaint(paintUv, localUv, _StrokeLayer1, _StrokeTransformA1.xyz, _StrokeTransformB1.xyz, _StrokeStop1_0, _StrokeStop1_1, _StrokeStop1_2, _StrokeStop1_3, _StrokeStop1_4, _StrokeColor1_0, _StrokeColor1_1, _StrokeColor1_2, _StrokeColor1_3, _StrokeColor1_4, 1.0);
                            strokeColor = Composite(strokeColor, strokeLayer1, (int)_StrokeLayer1.z);
                        }

                        strokeColor.a *= strokeMask * _ShapeOpacity;
                        bodyColor = _StrokeAlign > 1.5
                            ? CompositeDisjoint(bodyColor, strokeColor)
                            : Composite(bodyColor, strokeColor, 0);
                    }
                }

                result = _BodyDisjointComposite > 0.5
                    ? CompositeDisjoint(result, bodyColor)
                    : Composite(result, bodyColor, _UseInputBodyTex > 0.5 ? (int)_BodyCompositeMode : 0);

                if (_InnerShadow.w > 0.5)
                {
                    float outsideStrokeInnerScale = (_StrokeCount > 0.5 && _StrokeAlign > 1.5) ? 0.1 : 1.0;
                    float outsideStrokeOffsetScale = (_StrokeCount > 0.5 && _StrokeAlign > 1.5) ? 0.25 : 1.0;
                    float smallNormalInnerOffsetScale = 1.0;
                    if (_InnerShadow.z < 0.5)
                    {
                        smallNormalInnerOffsetScale = lerp(0.85, 1.0, saturate((_InnerShadow.x - 10.0) / 20.0));
                    }

                    float2 innerScreen = shapeCentered - (_InnerShadowOffset.xy * outsideStrokeOffsetScale * smallNormalInnerOffsetScale);
                    float sigma = max(1.0, _InnerShadow.x * 0.5);
                    float halfAxisWeight = exp(-0.125);
                    float halfDiagWeight = halfAxisWeight;
                    float axisWeight = exp(-0.5);
                    float diagWeight = axisWeight;
                    float farAxisWeight = exp(-1.125);
                    float farDiagWeight = farAxisWeight;
                    float outerAxisWeight = exp(-2.0);
                    float outerDiagWeight = outerAxisWeight;
                    float totalWeight = 1.0 + 4.0 * (halfAxisWeight + halfDiagWeight + axisWeight + diagWeight + farAxisWeight + farDiagWeight + outerAxisWeight + outerDiagWeight);
                    float2 halfAxisX = float2(sigma * 0.5, 0.0);
                    float2 halfAxisY = float2(0.0, sigma * 0.5);
                    float2 halfDiag = float2(sigma * 0.35355339, sigma * 0.35355339);
                    float2 axisX = float2(sigma, 0.0);
                    float2 axisY = float2(0.0, sigma);
                    float2 diag = float2(sigma * 0.70710678, sigma * 0.70710678);
                    float2 farAxisX = float2(sigma * 1.5, 0.0);
                    float2 farAxisY = float2(0.0, sigma * 1.5);
                    float2 farDiag = float2(sigma * 1.06066017, sigma * 1.06066017);
                    float2 outerAxisX = float2(sigma * 2.0, 0.0);
                    float2 outerAxisY = float2(0.0, sigma * 2.0);
                    float2 outerDiag = float2(sigma * 1.41421356, sigma * 1.41421356);
                    float innerExpansion = -_InnerShadow.y;

                    float innerBlurredMask = 0.0;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen, _ShapeRotation), size, radii, innerExpansion);
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + halfAxisX, _ShapeRotation), size, radii, innerExpansion) * halfAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - halfAxisX, _ShapeRotation), size, radii, innerExpansion) * halfAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + halfAxisY, _ShapeRotation), size, radii, innerExpansion) * halfAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - halfAxisY, _ShapeRotation), size, radii, innerExpansion) * halfAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + halfDiag, _ShapeRotation), size, radii, innerExpansion) * halfDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(-halfDiag.x, halfDiag.y), _ShapeRotation), size, radii, innerExpansion) * halfDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(halfDiag.x, -halfDiag.y), _ShapeRotation), size, radii, innerExpansion) * halfDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - halfDiag, _ShapeRotation), size, radii, innerExpansion) * halfDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + axisX, _ShapeRotation), size, radii, innerExpansion) * axisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - axisX, _ShapeRotation), size, radii, innerExpansion) * axisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + axisY, _ShapeRotation), size, radii, innerExpansion) * axisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - axisY, _ShapeRotation), size, radii, innerExpansion) * axisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + diag, _ShapeRotation), size, radii, innerExpansion) * diagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(-diag.x, diag.y), _ShapeRotation), size, radii, innerExpansion) * diagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(diag.x, -diag.y), _ShapeRotation), size, radii, innerExpansion) * diagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - diag, _ShapeRotation), size, radii, innerExpansion) * diagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + farAxisX, _ShapeRotation), size, radii, innerExpansion) * farAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - farAxisX, _ShapeRotation), size, radii, innerExpansion) * farAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + farAxisY, _ShapeRotation), size, radii, innerExpansion) * farAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - farAxisY, _ShapeRotation), size, radii, innerExpansion) * farAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + farDiag, _ShapeRotation), size, radii, innerExpansion) * farDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(-farDiag.x, farDiag.y), _ShapeRotation), size, radii, innerExpansion) * farDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(farDiag.x, -farDiag.y), _ShapeRotation), size, radii, innerExpansion) * farDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - farDiag, _ShapeRotation), size, radii, innerExpansion) * farDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + outerAxisX, _ShapeRotation), size, radii, innerExpansion) * outerAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - outerAxisX, _ShapeRotation), size, radii, innerExpansion) * outerAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + outerAxisY, _ShapeRotation), size, radii, innerExpansion) * outerAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - outerAxisY, _ShapeRotation), size, radii, innerExpansion) * outerAxisWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + outerDiag, _ShapeRotation), size, radii, innerExpansion) * outerDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(-outerDiag.x, outerDiag.y), _ShapeRotation), size, radii, innerExpansion) * outerDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen + float2(outerDiag.x, -outerDiag.y), _ShapeRotation), size, radii, innerExpansion) * outerDiagWeight;
                    innerBlurredMask += EvaluateExpandedShapeAlpha(Rotate(innerScreen - outerDiag, _ShapeRotation), size, radii, innerExpansion) * outerDiagWeight;
                    innerBlurredMask /= totalWeight;

                    float contentMask = ContentMask(local, size, radii, _StrokeWeight, _StrokeAlign, _StrokeCount);
                    float inner = contentMask * saturate(1.0 - innerBlurredMask) * _InnerShadowColor.a * outsideStrokeInnerScale;
                    result = Composite(result, float4(_InnerShadowColor.rgb, inner * _ShapeOpacity), (int)_InnerShadow.z);
                }
                }

                return result;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 scene = EvaluateScene(float2(input.uv.x, 1.0 - input.uv.y));
                if (_RenderMode < 0.5)
                {
                    scene.a = max(scene.a - (1.0 / 255.0), 0.0);
                }
                scene.rgb = LinearToSrgb(scene.rgb);
                return scene;
            }
            ENDHLSL
        }
    }
}
