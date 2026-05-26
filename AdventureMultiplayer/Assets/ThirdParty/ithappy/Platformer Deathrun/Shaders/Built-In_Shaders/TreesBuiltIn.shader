Shader "TreesBuiltIn"
{
    Properties
    {
        [NoScaleOffset]_Albedo("Albedo", 2D) = "white" {}
        _Force("Force", Float) = 0
        _Speed("Speed", Float) = 0
        _Noise_Scale("Noise Scale", Float) = 0
        [HideInInspector]_BUILTIN_QueueOffset("Float", Float) = 0
        [HideInInspector]_BUILTIN_QueueControl("Float", Float) = -1
    }
    SubShader
    {
        Tags
        {
            // RenderPipeline: <None>
            "RenderType"="Opaque"
            "BuiltInMaterialType" = "Lit"
            "Queue"="Geometry"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="BuiltInLitSubTarget"
        }
        Pass
        {
            Name "BuiltIn Forward"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma multi_compile_fwdbase
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD
        #define BUILTIN_TARGET_API 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
             float2 lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
             float4 fogFactorAndVertexLight;
             float4 shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 lightmapUV : INTERP0;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP1;
            #endif
             float4 tangentWS : INTERP2;
             float4 texCoord0 : INTERP3;
             float4 fogFactorAndVertexLight : INTERP4;
             float4 shadowCoord : INTERP5;
             float3 positionWS : INTERP6;
             float3 normalWS : INTERP7;
             float3 viewDirectionWS : INTERP8;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.lightmapUV = input.lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.shadowCoord.xyzw = input.shadowCoord;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            output.viewDirectionWS.xyz = input.viewDirectionWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.lightmapUV = input.lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.shadowCoord = input.shadowCoord.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            output.viewDirectionWS = input.viewDirectionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ca924a6b5cc349d2842f4741313651f6_Out_0 = UnityBuildTexture2DStructNoScale(_Albedo);
            float4 _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0 = SAMPLE_TEXTURE2D(_Property_ca924a6b5cc349d2842f4741313651f6_Out_0.tex, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.samplerstate, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_R_4 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.r;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_G_5 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.g;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_B_6 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.b;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_A_7 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.a;
            surface.BaseColor = (_SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.xyz);
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = float(0);
            surface.Smoothness = float(0.5);
            surface.Occlusion = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.texcoord1  = attributes.uv1;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            result.worldPos = varyings.positionWS;
            result.worldNormal = varyings.normalWS;
            result.viewDir = varyings.viewDirectionWS;
            // World Tangent isn't an available input on v2f_surf
        
            result._ShadowCoord = varyings.shadowCoord;
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            result.sh = varyings.sh;
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            result.lmap.xy = varyings.lightmapUV;
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            result.positionWS = surfVertex.worldPos;
            result.normalWS = surfVertex.worldNormal;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
            result.shadowCoord = surfVertex._ShadowCoord;
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            result.sh = surfVertex.sh;
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            result.lightmapUV = surfVertex.lmap.xy;
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "BuiltIn ForwardAdd"
            Tags
            {
                "LightMode" = "ForwardAdd"
            }
        
        // Render State
        Blend SrcAlpha One, One One
        ZWrite Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma multi_compile_fwdadd_fullshadows
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ SHADOWS_SHADOWMASK
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_FORWARD_ADD
        #define BUILTIN_TARGET_API 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
             float2 lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
             float4 fogFactorAndVertexLight;
             float4 shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 lightmapUV : INTERP0;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP1;
            #endif
             float4 tangentWS : INTERP2;
             float4 texCoord0 : INTERP3;
             float4 fogFactorAndVertexLight : INTERP4;
             float4 shadowCoord : INTERP5;
             float3 positionWS : INTERP6;
             float3 normalWS : INTERP7;
             float3 viewDirectionWS : INTERP8;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.lightmapUV = input.lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.shadowCoord.xyzw = input.shadowCoord;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            output.viewDirectionWS.xyz = input.viewDirectionWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.lightmapUV = input.lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.shadowCoord = input.shadowCoord.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            output.viewDirectionWS = input.viewDirectionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ca924a6b5cc349d2842f4741313651f6_Out_0 = UnityBuildTexture2DStructNoScale(_Albedo);
            float4 _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0 = SAMPLE_TEXTURE2D(_Property_ca924a6b5cc349d2842f4741313651f6_Out_0.tex, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.samplerstate, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_R_4 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.r;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_G_5 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.g;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_B_6 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.b;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_A_7 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.a;
            surface.BaseColor = (_SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.xyz);
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = float(0);
            surface.Smoothness = float(0.5);
            surface.Occlusion = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.texcoord1  = attributes.uv1;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            result.worldPos = varyings.positionWS;
            result.worldNormal = varyings.normalWS;
            result.viewDir = varyings.viewDirectionWS;
            // World Tangent isn't an available input on v2f_surf
        
            result._ShadowCoord = varyings.shadowCoord;
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            result.sh = varyings.sh;
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            result.lmap.xy = varyings.lightmapUV;
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            result.positionWS = surfVertex.worldPos;
            result.normalWS = surfVertex.worldNormal;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
            result.shadowCoord = surfVertex._ShadowCoord;
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            result.sh = surfVertex.sh;
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            result.lightmapUV = surfVertex.lmap.xy;
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRForwardAddPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "BuiltIn Deferred"
            Tags
            {
                "LightMode" = "Deferred"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma multi_compile_instancing
        #pragma exclude_renderers nomrt
        #pragma multi_compile_prepassfinal
        #pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        #pragma multi_compile _ _SHADOWS_SOFT
        #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
        #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
        #pragma multi_compile _ _GBUFFER_NORMALS_OCT
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_TANGENT_WS
        #define VARYINGS_NEED_TEXCOORD0
        #define VARYINGS_NEED_VIEWDIRECTION_WS
        #define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEFERRED
        #define BUILTIN_TARGET_API 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float3 viewDirectionWS;
            #if defined(LIGHTMAP_ON)
             float2 lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
             float4 fogFactorAndVertexLight;
             float4 shadowCoord;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float3 TangentSpaceNormal;
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if defined(LIGHTMAP_ON)
             float2 lightmapUV : INTERP0;
            #endif
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP1;
            #endif
             float4 tangentWS : INTERP2;
             float4 texCoord0 : INTERP3;
             float4 fogFactorAndVertexLight : INTERP4;
             float4 shadowCoord : INTERP5;
             float3 positionWS : INTERP6;
             float3 normalWS : INTERP7;
             float3 viewDirectionWS : INTERP8;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.lightmapUV = input.lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS.xyzw = input.tangentWS;
            output.texCoord0.xyzw = input.texCoord0;
            output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
            output.shadowCoord.xyzw = input.shadowCoord;
            output.positionWS.xyz = input.positionWS;
            output.normalWS.xyz = input.normalWS;
            output.viewDirectionWS.xyz = input.viewDirectionWS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if defined(LIGHTMAP_ON)
            output.lightmapUV = input.lightmapUV;
            #endif
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.tangentWS = input.tangentWS.xyzw;
            output.texCoord0 = input.texCoord0.xyzw;
            output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
            output.shadowCoord = input.shadowCoord.xyzw;
            output.positionWS = input.positionWS.xyz;
            output.normalWS = input.normalWS.xyz;
            output.viewDirectionWS = input.viewDirectionWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 NormalTS;
            float3 Emission;
            float Metallic;
            float Smoothness;
            float Occlusion;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ca924a6b5cc349d2842f4741313651f6_Out_0 = UnityBuildTexture2DStructNoScale(_Albedo);
            float4 _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0 = SAMPLE_TEXTURE2D(_Property_ca924a6b5cc349d2842f4741313651f6_Out_0.tex, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.samplerstate, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_R_4 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.r;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_G_5 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.g;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_B_6 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.b;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_A_7 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.a;
            surface.BaseColor = (_SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.xyz);
            surface.NormalTS = IN.TangentSpaceNormal;
            surface.Emission = float3(0, 0, 0);
            surface.Metallic = float(0);
            surface.Smoothness = float(0.5);
            surface.Occlusion = float(1);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
            output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.texcoord1  = attributes.uv1;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            result.worldPos = varyings.positionWS;
            result.worldNormal = varyings.normalWS;
            result.viewDir = varyings.viewDirectionWS;
            // World Tangent isn't an available input on v2f_surf
        
            result._ShadowCoord = varyings.shadowCoord;
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            result.sh = varyings.sh;
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            result.lmap.xy = varyings.lightmapUV;
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            result.positionWS = surfVertex.worldPos;
            result.normalWS = surfVertex.worldNormal;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
            result.shadowCoord = surfVertex._ShadowCoord;
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            result.sh = surfVertex.sh;
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            result.lightmapUV = surfVertex.lmap.xy;
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/PBRDeferredPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_shadowcaster
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        #define BUILTIN_TARGET_API 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
        // Render State
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define BUILTIN_TARGET_API 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags
            {
                "LightMode" = "Meta"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define ATTRIBUTES_NEED_TEXCOORD1
        #define ATTRIBUTES_NEED_TEXCOORD2
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_META
        #define BUILTIN_TARGET_API 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.texCoord0.xyzw = input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.texCoord0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_ca924a6b5cc349d2842f4741313651f6_Out_0 = UnityBuildTexture2DStructNoScale(_Albedo);
            float4 _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0 = SAMPLE_TEXTURE2D(_Property_ca924a6b5cc349d2842f4741313651f6_Out_0.tex, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.samplerstate, _Property_ca924a6b5cc349d2842f4741313651f6_Out_0.GetTransformedUV(IN.uv0.xy));
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_R_4 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.r;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_G_5 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.g;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_B_6 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.b;
            float _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_A_7 = _SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.a;
            surface.BaseColor = (_SampleTexture2D_6c61955fc4cc435396f98bd79e0f1c36_RGBA_0.xyz);
            surface.Emission = float3(0, 0, 0);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.texcoord1  = attributes.uv1;
            result.texcoord2  = attributes.uv2;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SceneSelectionPass
        #define BUILTIN_TARGET_API 1
        #define SCENESELECTIONPASS 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Back
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        #define _NORMALMAP 1
        #define _NORMAL_DROPOFF_TS 1
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS ScenePickingPass
        #define BUILTIN_TARGET_API 1
        #define SCENEPICKINGPASS 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             float3 AbsoluteWorldSpacePosition;
             float3 TimeParameters;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 _Albedo_TexelSize;
        float _Force;
        float _Speed;
        float _Noise_Scale;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_Albedo);
        SAMPLER(sampler_Albedo);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        
        inline float Unity_SimpleNoise_RandomValue_float (float2 uv)
        {
            float angle = dot(uv, float2(12.9898, 78.233));
            #if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_VULKAN))
                // 'sin()' has bad precision on Mali GPUs for inputs > 10000
                angle = fmod(angle, TWO_PI); // Avoid large inputs to sin()
            #endif
            return frac(sin(angle)*43758.5453);
        }
        
        inline float Unity_SimpleNnoise_Interpolate_float (float a, float b, float t)
        {
            return (1.0-t)*a + (t*b);
        }
        
        
        inline float Unity_SimpleNoise_ValueNoise_float (float2 uv)
        {
            float2 i = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
        
            uv = abs(frac(uv) - 0.5);
            float2 c0 = i + float2(0.0, 0.0);
            float2 c1 = i + float2(1.0, 0.0);
            float2 c2 = i + float2(0.0, 1.0);
            float2 c3 = i + float2(1.0, 1.0);
            float r0 = Unity_SimpleNoise_RandomValue_float(c0);
            float r1 = Unity_SimpleNoise_RandomValue_float(c1);
            float r2 = Unity_SimpleNoise_RandomValue_float(c2);
            float r3 = Unity_SimpleNoise_RandomValue_float(c3);
        
            float bottomOfGrid = Unity_SimpleNnoise_Interpolate_float(r0, r1, f.x);
            float topOfGrid = Unity_SimpleNnoise_Interpolate_float(r2, r3, f.x);
            float t = Unity_SimpleNnoise_Interpolate_float(bottomOfGrid, topOfGrid, f.y);
            return t;
        }
        void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
        {
            float t = 0.0;
        
            float freq = pow(2.0, float(0));
            float amp = pow(0.5, float(3-0));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(1));
            amp = pow(0.5, float(3-1));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            freq = pow(2.0, float(2));
            amp = pow(0.5, float(3-2));
            t += Unity_SimpleNoise_ValueNoise_float(float2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;
        
            Out = t;
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Add_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A + B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float _Split_91ecf1b127cc44da8c32c719ca35551f_R_1 = IN.ObjectSpacePosition[0];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_G_2 = IN.ObjectSpacePosition[1];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_B_3 = IN.ObjectSpacePosition[2];
            float _Split_91ecf1b127cc44da8c32c719ca35551f_A_4 = 0;
            float _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3;
            Unity_Remap_float(_Split_91ecf1b127cc44da8c32c719ca35551f_G_2, float2 (0, 10), float2 (0, 1), _Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3);
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_b2bbae22ff40457ba9fcc24ac14cceb8_A_4 = 0;
            float2 _Vector2_73d23c8bae934517ada1138a7591bc07_Out_0 = float2(_Split_b2bbae22ff40457ba9fcc24ac14cceb8_R_1, _Split_b2bbae22ff40457ba9fcc24ac14cceb8_B_3);
            float _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0 = _Speed;
            float _Multiply_adcc65be134e44389444d888e8735c84_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_51c032e835e84ccc89b5c381cdfe722a_Out_0, _Multiply_adcc65be134e44389444d888e8735c84_Out_2);
            float2 _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3;
            Unity_TilingAndOffset_float(_Vector2_73d23c8bae934517ada1138a7591bc07_Out_0, float2 (1, 1), (_Multiply_adcc65be134e44389444d888e8735c84_Out_2.xx), _TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3);
            float _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0 = _Noise_Scale;
            float _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_3fd4f171aeae4470bcc40dfdfaee1782_Out_3, _Property_8ff5b838e81049acaac6578fe88ce0e7_Out_0, _SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2);
            float _Remap_cca77fa92945437aaabea57966df1d04_Out_3;
            Unity_Remap_float(_SimpleNoise_0203e9d40a5540d29d745d80d0b63a62_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_cca77fa92945437aaabea57966df1d04_Out_3);
            float _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0 = _Force;
            float _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2;
            Unity_Multiply_float_float(_Remap_cca77fa92945437aaabea57966df1d04_Out_3, _Property_4b3eb78c25e74ae19e582856ce35f91c_Out_0, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2);
            float _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_6699df9d1fd64ab183a56f60217addf9_Out_2, _Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2);
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_R_1 = IN.AbsoluteWorldSpacePosition[0];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_G_2 = IN.AbsoluteWorldSpacePosition[1];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3 = IN.AbsoluteWorldSpacePosition[2];
            float _Split_d85fc1962c6e46ccb334f41f074a59c2_A_4 = 0;
            float2 _Vector2_49a27396d267475b9d485f9ed0d76611_Out_0 = float2(_Split_d85fc1962c6e46ccb334f41f074a59c2_R_1, _Split_d85fc1962c6e46ccb334f41f074a59c2_B_3);
            float _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2;
            Unity_Add_float(IN.TimeParameters.x, float(50), _Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2);
            float _Property_4b8615b9f8094d4195da768683b6d515_Out_0 = _Speed;
            float _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2;
            Unity_Multiply_float_float(_Add_0d0f1f0e63594d71aabcda1f057c7437_Out_2, _Property_4b8615b9f8094d4195da768683b6d515_Out_0, _Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2);
            float2 _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3;
            Unity_TilingAndOffset_float(_Vector2_49a27396d267475b9d485f9ed0d76611_Out_0, float2 (1, 1), (_Multiply_1f329720054d46d6baa8f10386c6ee08_Out_2.xx), _TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3);
            float _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0 = _Noise_Scale;
            float _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2;
            Unity_SimpleNoise_float(_TilingAndOffset_070f346514a74cb6b2b84ba3d6c2f401_Out_3, _Property_539bc20a3fab4f83a3bbcbe86d3f486b_Out_0, _SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2);
            float _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3;
            Unity_Remap_float(_SimpleNoise_a47645494368496381103d16e4e4b7b8_Out_2, float2 (0, 1), float2 (-1, 1), _Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3);
            float _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0 = _Force;
            float _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2;
            Unity_Multiply_float_float(_Remap_f3b7fa09a1624918961ee6ef31728be5_Out_3, _Property_5dfafe3a550e4637a0f2f23657ae8399_Out_0, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2);
            float _Multiply_f710a1318d1c421faf6850b063531471_Out_2;
            Unity_Multiply_float_float(_Remap_5323a36a0bd1494bb2b0e9e2baf3f26c_Out_3, _Multiply_99cc943680034bf0a8d398e4e9f85cf1_Out_2, _Multiply_f710a1318d1c421faf6850b063531471_Out_2);
            float4 _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4;
            float3 _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5;
            float2 _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6;
            Unity_Combine_float(_Multiply_14ec1f8e493543079f2a32bd283d1a99_Out_2, float(0), _Multiply_f710a1318d1c421faf6850b063531471_Out_2, float(0), _Combine_34407bb3908a4b7195bec376ffd5b087_RGBA_4, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Combine_34407bb3908a4b7195bec376ffd5b087_RG_6);
            float3 _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            Unity_Add_float3(IN.ObjectSpacePosition, _Combine_34407bb3908a4b7195bec376ffd5b087_RGB_5, _Add_922c6568bb774e208f49bbb3d45b1698_Out_2);
            description.Position = _Add_922c6568bb774e208f49bbb3d45b1698_Out_2;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.AbsoluteWorldSpacePosition =                 GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            output.TimeParameters =                             _TimeParameters.xyz;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if !defined(LIGHTMAP_ON)
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        ENDHLSL
        }
    }
    CustomEditorForRenderPipeline "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInLitGUI" ""
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}