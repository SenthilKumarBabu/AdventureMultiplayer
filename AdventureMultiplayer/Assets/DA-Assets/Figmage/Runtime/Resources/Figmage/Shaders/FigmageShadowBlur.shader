Shader "D.A. Assets/Figmage/ShadowBlur"
{
    Properties
    {
        _SourceTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
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

            sampler2D _SourceTex;
            float4 _SourceTex_TexelSize;
            sampler2D _BlueNoise;
            float4 _BlueNoise_TexelSize;
            float2 _TargetSize;
            float _BlueNoiseStrength;
            float _BlurAxis;
            float _PremultiplyInput;
            float _UnpremultiplyOutput;
            float _BlurSrgb;
            float _ProgressiveLayerBlur;
            float4 _CanvasSize;
            float4 _ShapeSize;
            float4 _ShapeOffset;
            float _ShapeRotation;
            float4 _LayerBlur;
            float4 _LayerBlurOffsets;
            int _TapCount;
            float _Weights[64];
            float _Offsets[64];

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float3 LinearToSrgb(float3 c)
            {
                return pow(max(c, 0.0), 1.0 / 2.2);
            }

            float3 SrgbToLinear(float3 c)
            {
                return pow(max(c, 0.0), 2.2);
            }

            float4 SampleSource(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return 0.0;
                }

                float4 sample = tex2D(_SourceTex, uv);
                if (_PremultiplyInput > 0.5)
                {
                    if (_BlurSrgb > 0.5)
                    {
                        sample.rgb = LinearToSrgb(sample.rgb);
                    }
                    sample.rgb *= sample.a;
                }

                return sample;
            }

            float2 Rotate(float2 p, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            float ResolveProgressiveSigma(float2 uv)
            {
                float2 fragCoord = float2(uv.x, 1.0 - uv.y) * _CanvasSize.xy;
                float2 centered = fragCoord - _CanvasSize.xy * 0.5;
                float2 shapeCentered = centered - _ShapeOffset.xy;
                float2 local = Rotate(shapeCentered, _ShapeRotation);
                float2 localUv = local / max(_ShapeSize.xy, 1.0) + 0.5;

                float2 startOffset = _LayerBlurOffsets.xy;
                float2 endOffset = _LayerBlurOffsets.zw;
                float2 axis = endOffset - startOffset;
                float t = dot(localUv - startOffset, axis) / max(dot(axis, axis), 1e-5);
                return lerp(_LayerBlur.x, _LayerBlur.y, saturate(t));
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 texel = _SourceTex_TexelSize.xy;
                float2 axis = _BlurAxis < 0.5 ? float2(texel.x, 0.0) : float2(0.0, texel.y);
                if (_ProgressiveLayerBlur > 0.5)
                {
                    float sigma = max(ResolveProgressiveSigma(input.uv), 0.001);
                    float maxOffset = min(63.0, ceil(sigma * 3.0));
                    float4 progressiveResult = SampleSource(input.uv);
                    float weightSum = 1.0;

                    [loop]
                    for (int j = 1; j < 64; j++)
                    {
                        float sampleOffset = maxOffset * ((float)j / 63.0);
                        float weight = exp(-0.5 * (sampleOffset * sampleOffset) / (sigma * sigma));
                        progressiveResult += SampleSource(input.uv + axis * sampleOffset) * weight;
                        progressiveResult += SampleSource(input.uv - axis * sampleOffset) * weight;
                        weightSum += 2.0 * weight;
                    }

                    progressiveResult /= max(weightSum, 1e-5);

                if (_UnpremultiplyOutput > 0.5)
                {
                    progressiveResult.rgb = progressiveResult.a > 1e-5 ? progressiveResult.rgb / progressiveResult.a : 0.0;
                    if (_BlurSrgb > 0.5)
                    {
                        progressiveResult.rgb = SrgbToLinear(progressiveResult.rgb);
                    }
                }

                    return progressiveResult;
                }

                float4 result = SampleSource(input.uv) * _Weights[0];

                [loop]
                for (int i = 1; i < 64; i++)
                {
                    if (i >= _TapCount)
                    {
                        break;
                    }

                    float2 delta = axis * _Offsets[i];
                    result += SampleSource(input.uv + delta) * _Weights[i];
                    result += SampleSource(input.uv - delta) * _Weights[i];
                }

                float2 noiseUv = (input.uv - 0.5) * _TargetSize;
                float noise = tex2D(_BlueNoise, noiseUv * _BlueNoise_TexelSize.xy).r - 0.5;
                result += noise * (_BlueNoiseStrength / 255.0);

                if (_UnpremultiplyOutput > 0.5)
                {
                    result.rgb = result.a > 1e-5 ? result.rgb / result.a : 0.0;
                    if (_BlurSrgb > 0.5)
                    {
                        result.rgb = SrgbToLinear(result.rgb);
                    }
                }

                return result;
            }
            ENDHLSL
        }
    }
}
