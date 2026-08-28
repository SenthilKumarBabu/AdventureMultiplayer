Shader "D.A. Assets/Figmage/ShadowComposite"
{
    Properties
    {
        _ShadowTexPrepare ("Shadow Prepare", 2D) = "white" {}
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

            sampler2D _ShadowTexPrepare;
            float _ShadowAlphaMultiplier;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float4 ReweightShadowSample(float4 premultipliedShadow, float alphaScale)
            {
                float targetAlpha = saturate(premultipliedShadow.a * alphaScale);
                float visible = step(1e-5, premultipliedShadow.a);
                float inverseSourceAlpha = rcp(max(premultipliedShadow.a, 1e-5));
                float3 straightRgb = premultipliedShadow.rgb * inverseSourceAlpha;
                float3 repremultipliedRgb = straightRgb * targetAlpha * visible;
                return float4(repremultipliedRgb, targetAlpha);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 preparedShadow = tex2D(_ShadowTexPrepare, input.uv);
                return ReweightShadowSample(preparedShadow, _ShadowAlphaMultiplier);
            }
            ENDHLSL
        }

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

            sampler2D _BodyTex;
            sampler2D _ShadowTex;
            float4 _ShadowColor;
            float _ShadowAlphaMultiplier;
            float4 _CanvasSize;
            float4 _ShadowRect;
            float4 _ShadowOffset;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 body = tex2D(_BodyTex, input.uv);
                float2 fragCoord = input.uv * _CanvasSize.xy;
                float2 shadowCoord = fragCoord - _ShadowOffset.xy;
                float2 shadowUv = (shadowCoord - _ShadowRect.xy) / max(_ShadowRect.zw, 1e-5);
                float shadowMask = 0.0;
                if (shadowUv.x >= 0.0 && shadowUv.x <= 1.0 && shadowUv.y >= 0.0 && shadowUv.y <= 1.0)
                {
                    shadowMask = tex2D(_ShadowTex, shadowUv).a;
                }
                float shadowAlpha = saturate(shadowMask * _ShadowColor.a);

                float3 shadowPremul = _ShadowColor.rgb * shadowAlpha;
                float3 bodyPremul = body.rgb * body.a;

                float outAlpha = body.a + shadowAlpha * (1.0 - body.a);
                float3 outPremul = bodyPremul + shadowPremul * (1.0 - body.a);
                float3 outRgb = outAlpha > 1e-5 ? outPremul / outAlpha : 0.0;
                return float4(outRgb, outAlpha);
            }
            ENDHLSL
        }

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

            sampler2D _BodyTex;
            sampler2D _ShadowTex;
            float _PreserveShadowUnderBody;
            float _FillOpacityHint;
            float4 _ShadowColor;
            float4 _CanvasSize;
            float4 _ShadowRect;
            float4 _ShadowOffset;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 accum = tex2D(_BodyTex, input.uv);
                float2 fragCoord = input.uv * _CanvasSize.xy;
                float2 shadowCoord = fragCoord - _ShadowOffset.xy;
                float2 shadowUv = (shadowCoord - _ShadowRect.xy) / max(_ShadowRect.zw, 1e-5);
                float shadowMask = 0.0;
                if (shadowUv.x >= 0.0 && shadowUv.x <= 1.0 && shadowUv.y >= 0.0 && shadowUv.y <= 1.0)
                {
                    shadowMask = tex2D(_ShadowTex, shadowUv).a;
                }

                float shadowAlpha = saturate(shadowMask * _ShadowColor.a);
                float3 shadowPremul = _ShadowColor.rgb * shadowAlpha;
                float3 accumPremul = accum.rgb * accum.a;

                float outAlpha = shadowAlpha + accum.a * (1.0 - shadowAlpha);
                float3 outPremul = shadowPremul + accumPremul * (1.0 - shadowAlpha);
                float3 outRgb = outAlpha > 1e-5 ? outPremul / outAlpha : 0.0;
                return float4(outRgb, outAlpha);
            }
            ENDHLSL
        }

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

            sampler2D _BodyTex;
            sampler2D _ShadowTex;
            float _PreserveShadowUnderBody;
            float _FillOpacityHint;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 body = tex2D(_BodyTex, input.uv);
                float4 shadow = tex2D(_ShadowTex, input.uv);
                float3 bodyPremul = body.rgb * body.a;
                float3 shadowPremul = shadow.rgb * shadow.a;
                float outAlpha = body.a + shadow.a * (1.0 - body.a);
                float preserve = saturate(_PreserveShadowUnderBody);
                if (_FillOpacityHint > 1e-5 && _FillOpacityHint < 0.9999)
                {
                    float excessAlpha = saturate((body.a - _FillOpacityHint) / max(1.0 - _FillOpacityHint, 1e-5));
                    float fillOnlyWeight = 1.0 - smoothstep(0.02, 0.35, excessAlpha);
                    preserve *= fillOnlyWeight;
                }

                float shadowOcclusion = lerp(1.0 - body.a, 1.0, preserve);
                float3 outPremul = bodyPremul + shadowPremul * shadowOcclusion;
                float3 outRgb = outAlpha > 1e-5 ? outPremul / outAlpha : 0.0;
                return float4(outRgb, outAlpha);
            }
            ENDHLSL
        }

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

            sampler2D _BodyTex;
            sampler2D _ShadowTex;
            sampler2D _LayerBlurMidATex;
            sampler2D _LayerBlurMidBTex;
            sampler2D _LayerBlurMidCTex;
            sampler2D _LayerBlurMidDTex;
            sampler2D _LayerBlurMidETex;
            sampler2D _LayerBlurMidFTex;
            float4 _CanvasSize;
            float4 _ShapeSize;
            float4 _ShapeOffset;
            float _ShapeRotation;
            float4 _LayerBlur;
            float4 _LayerBlurOffsets;
            float4 _LayerBlurRamp;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float2 Rotate(float2 p, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 original = tex2D(_BodyTex, input.uv);
                float4 midA = tex2D(_LayerBlurMidATex, input.uv);
                float4 midB = tex2D(_LayerBlurMidBTex, input.uv);
                float4 midC = tex2D(_LayerBlurMidCTex, input.uv);
                float4 midD = tex2D(_LayerBlurMidDTex, input.uv);
                float4 midE = tex2D(_LayerBlurMidETex, input.uv);
                float4 midF = tex2D(_LayerBlurMidFTex, input.uv);
                float4 blurred = tex2D(_ShadowTex, input.uv);
                float2 fragCoord = float2(input.uv.x, 1.0 - input.uv.y) * _CanvasSize.xy;
                float2 centered = fragCoord - _CanvasSize.xy * 0.5;
                float2 shapeCentered = centered - _ShapeOffset.xy;
                float2 local = Rotate(shapeCentered, _ShapeRotation);
                float2 shapeUv = local / max(_ShapeSize.xy, 1.0) + 0.5;
                float2 canvasUv = float2(input.uv.x, 1.0 - input.uv.y);
                float2 localUv = lerp(shapeUv, canvasUv, _LayerBlurRamp.z);

                float2 startOffset = _LayerBlurOffsets.xy;
                float2 endOffset = _LayerBlurOffsets.zw;
                float2 axis = endOffset - startOffset;
                float t = dot(localUv - startOffset, axis) / max(dot(axis, axis), 1e-5);
                t = (t - 0.5) * max(_LayerBlurRamp.x, 0.1) + 0.5 + _LayerBlurRamp.y;
                float radius = lerp(_LayerBlur.x, _LayerBlur.y, saturate(t));
                float blurT = pow(saturate((radius - _LayerBlur.x) / max(_LayerBlur.y - _LayerBlur.x, 1e-5)), _LayerBlur.z);
                float segment = blurT * 7.0;
                float4 first = lerp(original, midA, saturate(segment));
                float4 second = lerp(midA, midB, saturate(segment - 1.0));
                float4 third = lerp(midB, midC, saturate(segment - 2.0));
                float4 fourth = lerp(midC, midD, saturate(segment - 3.0));
                float4 fifth = lerp(midD, midE, saturate(segment - 4.0));
                float4 sixth = lerp(midE, midF, saturate(segment - 5.0));
                float4 seventh = lerp(midF, blurred, saturate(segment - 6.0));
                return segment < 1.0 ? first : (segment < 2.0 ? second : (segment < 3.0 ? third : (segment < 4.0 ? fourth : (segment < 5.0 ? fifth : (segment < 6.0 ? sixth : seventh)))));
            }
            ENDHLSL
        }
    }
}
