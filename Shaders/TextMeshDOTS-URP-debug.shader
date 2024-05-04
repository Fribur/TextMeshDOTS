Shader "TextMeshDOTS/TextMeshDOTS-URP-debug"
{
    Properties
    {
        [HDR]_FaceColor("Face Color", Color) = (1, 1, 1, 1)
        _IsoPerimeter("Outline Width", Vector) = (0, 0, 0, 0)
        [HDR]_OutlineColor1("Outline Color 1", Color) = (0, 1, 1, 1)
        [HDR]_OutlineColor2("Outline Color 2", Color) = (0.009433985, 0.02534519, 1, 1)
        [HDR]_OutlineColor3("Outline Color 3", Color) = (0, 0, 0, 1)
        _OutlineOffset1("Outline Offset 1", Vector) = (0, 0, 0, 0)
        _OutlineOffset2("Outline Offset 2", Vector) = (0, 0, 0, 0)
        _OutlineOffset3("Outline Offset 3", Vector) = (0, 0, 0, 0)
        [ToggleUI]_OutlineMode("OutlineMode", Float) = 0
        _Softness("Softness", Vector) = (0, 0, 0, 0)
        [NoScaleOffset]_FaceTex("Face Texture", 2D) = "white" {}
        _FaceUVSpeed("_FaceUVSpeed", Vector) = (0, 0, 0, 0)
        _FaceTex_ST("_FaceTex_ST", Vector) = (1, 1, 0, 0)
        [NoScaleOffset]_OutlineTex("Outline Texture", 2D) = "white" {}
        _OutlineTex_ST("_OutlineTex_ST", Vector) = (1, 1, 0, 0)
        _OutlineUVSpeed("_OutlineUVSpeed", Vector) = (0, 0, 0, 0)
        _UnderlayColor("_UnderlayColor", Color) = (0, 0, 0, 1)
        _UnderlayOffset("Underlay Offset", Vector) = (0, 0, 0, 0)
        _UnderlayDilate("Underlay Dilate", Float) = 0
        _UnderlaySoftness("_UnderlaySoftness", Float) = 0
        [ToggleUI]_BevelType("Bevel Type", Float) = 0
        _BevelAmount("Bevel Amount", Range(0, 1)) = 0
        _BevelOffset("Bevel Offset", Range(-0.5, 0.5)) = 0
        _BevelWidth("Bevel Width", Range(0, 0.5)) = 0
        _BevelRoundness("Bevel Roundness", Range(0, 1)) = 0
        _BevelClamp("Bevel Clamp", Range(0, 1)) = 0
        [HDR]_SpecularColor("Light Color", Color) = (1, 1, 1, 1)
        _LightAngle("Light Angle", Range(0, 6.28)) = 0
        _SpecularPower("Specular Power", Range(0, 4)) = 0
        _Reflectivity("Reflectivity Power", Range(5, 15)) = 5
        _Diffuse("Diffuse Shadow", Range(0, 1)) = 0.3
        _Ambient("Ambient Shadow", Range(0, 1)) = 0.3
        [NoScaleOffset]_MainTex("_MainTex", 2D) = "white" {}
        _GradientScale("_GradientScale", Float) = 10
        _WeightNormal("WeightNormal", Float) = 0
        _WeightBold("WeightBold", Float) = 0.75
        _TextShaderIndex("TextShaderIndex", Vector) = (0, 0, 0, 0)
        _TextMaterialMaskShaderIndex("TextMaterialMaskShaderIndex", Float) = 0
        [HideInInspector]_CastShadows("_CastShadows", Float) = 1
        [HideInInspector]_Surface("_Surface", Float) = 1
        [HideInInspector]_Blend("_Blend", Float) = 0
        [HideInInspector]_AlphaClip("_AlphaClip", Float) = 1
        [HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
        [HideInInspector]_DstBlend("_DstBlend", Float) = 0
        [HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 0
        [HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
        [HideInInspector]_ZTest("_ZTest", Float) = 4
        [HideInInspector]_Cull("_Cull", Float) = 0
        [HideInInspector]_AlphaToMask("_AlphaToMask", Float) = 0
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent"
            "DisableBatching"="False"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="UniversalUnlitSubTarget"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                // LightMode: <None>
            }
        
        // Render State
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        AlphaToMask [_AlphaToMask]
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        #pragma enable_d3d11_debug_symbols
        
        // Keywords
        #pragma multi_compile _ LIGHTMAP_ON
        #pragma multi_compile _ DIRLIGHTMAP_COMBINED
        #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
        #pragma shader_feature _ _SAMPLE_GI
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
        #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_UNLIT
        #define _FOG_FRAGMENT 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float4 packed_positionWS_CustomUV1x : INTERP2;
             float4 packed_normalWS_CustomUV1y : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.packed_positionWS_CustomUV1x.xyz = input.positionWS;
            output.packed_positionWS_CustomUV1x.w = input.CustomUV1.x;
            output.packed_normalWS_CustomUV1y.xyz = input.normalWS;
            output.packed_normalWS_CustomUV1y.w = input.CustomUV1.y;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.positionWS = input.packed_positionWS_CustomUV1x.xyz;
            output.CustomUV1.x = input.packed_positionWS_CustomUV1x.w;
            output.normalWS = input.packed_normalWS_CustomUV1y.xyz;
            output.CustomUV1.y = input.packed_normalWS_CustomUV1y.w;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.BaseColor = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
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
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        ColorMask R
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float2 CustomUV1 : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.CustomUV1.xy = input.CustomUV1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.CustomUV1 = input.CustomUV1.xy;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
        
        // Render State
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        ColorMask RG
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.5
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_VERTEXID
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_MOTION_VECTORS
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float2 CustomUV1 : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.CustomUV1.xy = input.CustomUV1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.CustomUV1 = input.CustomUV1.xy;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "DepthNormalsOnly"
            Tags
            {
                "LightMode" = "DepthNormalsOnly"
            }
        
        // Render State
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
        #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
        #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float3 normalWS : INTERP2;
             float2 CustomUV1 : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.normalWS.xyz = input.normalWS;
            output.CustomUV1.xy = input.CustomUV1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.normalWS = input.normalWS.xyz;
            output.CustomUV1 = input.CustomUV1.xy;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
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
        Cull [_Cull]
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float3 normalWS : INTERP2;
             float2 CustomUV1 : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.normalWS.xyz = input.normalWS;
            output.CustomUV1.xy = input.CustomUV1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.normalWS = input.normalWS.xyz;
            output.CustomUV1 = input.CustomUV1.xy;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
        Pass
        {
            Name "GBuffer"
            Tags
            {
                "LightMode" = "UniversalGBuffer"
            }
        
        // Render State
        Cull [_Cull]
        Blend [_SrcBlend] [_DstBlend]
        ZTest [_ZTest]
        ZWrite [_ZWrite]
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 4.5
        #pragma exclude_renderers gles3 glcore
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma instancing_options renderinglayer
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
        #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
        #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_POSITION_WS
        #define VARYINGS_NEED_NORMAL_WS
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_GBUFFER
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float3 positionWS;
             float3 normalWS;
            #if !defined(LIGHTMAP_ON)
             float3 sh;
            #endif
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
            #if !defined(LIGHTMAP_ON)
             float3 sh : INTERP0;
            #endif
             float4 CustomUV0 : INTERP1;
             float4 Color : INTERP2;
             float4 packed_positionWS_CustomUV1x : INTERP3;
             float4 packed_normalWS_CustomUV1y : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.packed_positionWS_CustomUV1x.xyz = input.positionWS;
            output.packed_positionWS_CustomUV1x.w = input.CustomUV1.x;
            output.packed_normalWS_CustomUV1y.xyz = input.normalWS;
            output.packed_normalWS_CustomUV1y.w = input.CustomUV1.y;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            #if !defined(LIGHTMAP_ON)
            output.sh = input.sh;
            #endif
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.positionWS = input.packed_positionWS_CustomUV1x.xyz;
            output.CustomUV1.x = input.packed_positionWS_CustomUV1x.w;
            output.normalWS = input.packed_normalWS_CustomUV1y.xyz;
            output.CustomUV1.y = input.packed_normalWS_CustomUV1y.w;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.BaseColor = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
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
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENESELECTIONPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float2 CustomUV1 : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.CustomUV1.xy = input.CustomUV1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.CustomUV1 = input.CustomUV1.xy;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
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
        Cull [_Cull]
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 2.0
        #pragma vertex vert
        #pragma fragment frag
        
        // Keywords
        #pragma shader_feature_local_fragment _ _ALPHATEST_ON
        // GraphKeywords: <None>
        
        // Defines
        
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_VERTEXID
        #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
        #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
        #define VARYINGS_NEED_CULLFACE
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_DEPTHONLY
        #define SCENEPICKINGPASS 1
        #define ALPHA_CLIP_THRESHOLD 1
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
        #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
             uint vertexID : VERTEXID_SEMANTIC;
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct SurfaceDescriptionInputs
        {
             float FaceSign;
             float4 CustomUV0;
             float2 CustomUV1;
             float4 Color;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
             uint VertexID;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 CustomUV0 : INTERP0;
             float4 Color : INTERP1;
             float2 CustomUV1 : INTERP2;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0.xyzw = input.CustomUV0;
            output.Color.xyzw = input.Color;
            output.CustomUV1.xy = input.CustomUV1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
            output.CustomUV0 = input.CustomUV0.xyzw;
            output.Color = input.Color.xyzw;
            output.CustomUV1 = input.CustomUV1.xy;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
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
        float4 _FaceColor;
        float4 _IsoPerimeter;
        float4 _OutlineColor1;
        float4 _OutlineColor2;
        float4 _OutlineColor3;
        float2 _OutlineOffset1;
        float2 _OutlineOffset2;
        float2 _OutlineOffset3;
        float _OutlineMode;
        float4 _Softness;
        float4 _FaceTex_TexelSize;
        float2 _FaceUVSpeed;
        float4 _FaceTex_ST;
        float4 _OutlineTex_TexelSize;
        float4 _OutlineTex_ST;
        float2 _OutlineUVSpeed;
        float4 _UnderlayColor;
        float2 _UnderlayOffset;
        float _UnderlayDilate;
        float _UnderlaySoftness;
        float _BevelType;
        float _BevelAmount;
        float _BevelOffset;
        float _BevelWidth;
        float _BevelRoundness;
        float _BevelClamp;
        float4 _SpecularColor;
        float _LightAngle;
        float _SpecularPower;
        float _Reflectivity;
        float _Diffuse;
        float _Ambient;
        float4 _MainTex_TexelSize;
        float _GradientScale;
        float _WeightNormal;
        float _WeightBold;
        float2 _TextShaderIndex;
        float _TextMaterialMaskShaderIndex;
        UNITY_TEXTURE_STREAMING_DEBUG_VARS;
        CBUFFER_END
        
        #if defined(DOTS_INSTANCING_ON)
        // DOTS instancing definitions
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float2, _TextShaderIndex)
            UNITY_DOTS_INSTANCED_PROP_OVERRIDE_SUPPORTED(float, _TextMaterialMaskShaderIndex)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
        // DOTS instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(type, var)
        #elif defined(UNITY_INSTANCING_ENABLED)
        // Unity instancing definitions
        UNITY_INSTANCING_BUFFER_START(SGPerInstanceData)
            UNITY_DEFINE_INSTANCED_PROP(float2, _TextShaderIndex)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextMaterialMaskShaderIndex)
        UNITY_INSTANCING_BUFFER_END(SGPerInstanceData)
        // Unity instancing usage macros
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) UNITY_ACCESS_INSTANCED_PROP(SGPerInstanceData, var)
        #else
        #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
        #endif
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_FaceTex);
        SAMPLER(sampler_FaceTex);
        TEXTURE2D(_OutlineTex);
        SAMPLER(sampler_OutlineTex);
        SAMPLER(SamplerState_Linear_Clamp);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        
        // Graph Includes
        #include "Packages/com.textmeshdots/Shaders/TextGlyphParsing.hlsl"
        #include "Packages/com.textmeshdots/Shaders/SDFFunctions.hlsl"
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Functions
        
        void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
        {
            RGBA = float4(R, G, B, A);
            RGB = float3(R, G, B);
            RG = float2(R, G);
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
        Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
        Out = A * B;
        }
        
        struct Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float
        {
        float FaceSign;
        };
        
        void SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(UnityTexture2D _MainTex, float4 _Outline_Color_2, float4 _Outline_Color_3, float _GradientScale, float4 _Outline_Width, float4 _Softness, float _OutlineMode, float _Underlay_Dilate, float _UnderlaySoftness, float4 _UnderlayColor, UnityTexture2D _Outline_Texture, float4 _OutlineTex_ST, float2 _OutlineUVSpeed, float4 _Outline_Color_1, UnityTexture2D _Face_Texture, float4 _FaceTex_ST, float2 _FaceUVSpeed, float4 _Face_Color, float2 _Underlay_Offset, float2 _Outline_Offset_1, float2 _Outline_Offset_2, float2 _Outline_Offset_3, float4 _UVA, float2 _UVB, float4 _VertexColor, float _WeightNormal, float _WeightBold, Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float IN, out float3 Out_Color_2, out float Out_Alpha_1, out float3 Out_Normal_3)
        {
        float4 _Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4 = _UVA;
        UnityTexture2D _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D = _MainTex;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.z;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Height_2_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.w;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelWidth_3_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.x;
        float _TexelSize_8e05cc3b39154eedb7a6173a822a5350_TexelHeight_4_Float = _Property_fa8337bcd0644049b3b9808297db8a63_Out_0_Texture2D.texelSize.y;
        float _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float;
        ScreenSpaceRatio_float((_Property_bb48acf41fbc4fbd9ce559e26d1e15c4_Out_0_Vector4.xy), _TexelSize_8e05cc3b39154eedb7a6173a822a5350_Width_0_Float, 0, _ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float);
        UnityTexture2D _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D = _MainTex;
        float4 _Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4 = _UVA;
        float4 _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV((_Property_30232709e34e481982e9ac9a21e5443e_Out_0_Vector4.xy)) );
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_R_4_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.r;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_G_5_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.g;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_B_6_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.b;
        float _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float = _SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_RGBA_0_Vector4.a;
        float4 _Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4 = _UVA;
        float2 _Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2 = _Outline_Offset_1;
        float _Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float = _GradientScale;
        UnityTexture2D _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D = _MainTex;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.z;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.w;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelWidth_3_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.x;
        float _TexelSize_3acf71b741eb441e983b41037f81305d_TexelHeight_4_Float = _Property_0fb12e8bcdde4bff93e814f9f8572523_Out_0_Texture2D.texelSize.y;
        float4 _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4;
        float3 _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3;
        float2 _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2;
        Unity_Combine_float(_TexelSize_3acf71b741eb441e983b41037f81305d_Width_0_Float, _TexelSize_3acf71b741eb441e983b41037f81305d_Height_2_Float, float(0), float(0), _Combine_bb939d69209b497a9b80c193def93376_RGBA_4_Vector4, _Combine_bb939d69209b497a9b80c193def93376_RGB_5_Vector3, _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2);
        float2 _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2;
        Unity_Divide_float2((_Property_2575f007e8ef4d1189f8a0adb10314ef_Out_0_Float.xx), _Combine_bb939d69209b497a9b80c193def93376_RG_6_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2);
        float2 _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_129d3ac774004ce4aa614b8e5743f813_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2);
        float2 _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_c8f800878b004a6794881b441676758c_Out_2_Vector2, _Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2);
        float4 _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_d4535cd5c5f4410b94b2b16292f5b20a_Out_2_Vector2) );
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_R_4_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.r;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_G_5_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.g;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_B_6_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.b;
        float _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float = _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_RGBA_0_Vector4.a;
        float2 _Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2 = _Outline_Offset_2;
        float2 _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_8a61dbd9b230490492c725b2492533c3_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2);
        float2 _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_6642958232204f13b4c48ec4928dc935_Out_2_Vector2, _Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2);
        float4 _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_e1eb51dc000342989c6e1019ed12a07c_Out_2_Vector2) );
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_R_4_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.r;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_G_5_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.g;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_B_6_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.b;
        float _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float = _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_RGBA_0_Vector4.a;
        float2 _Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2 = _Outline_Offset_3;
        float2 _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_cc08eafc2eef4be1be6961a14444e2e1_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2);
        float2 _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2;
        Unity_Subtract_float2((_Property_a7cafb7977fb4a63b109ab28bc481d03_Out_0_Vector4.xy), _Multiply_fd1ba879babd4d49b2ae7b711bd9a5de_Out_2_Vector2, _Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2);
        float4 _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.tex, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.samplerstate, _Property_777b7d67d7f34fae939594add984b28b_Out_0_Texture2D.GetTransformedUV(_Subtract_63109762c12b44d99c5403fe77b5956f_Out_2_Vector2) );
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_R_4_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.r;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_G_5_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.g;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_B_6_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.b;
        float _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float = _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_RGBA_0_Vector4.a;
        float4 _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4;
        float3 _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3;
        float2 _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2;
        Unity_Combine_float(_SampleTexture2D_ec20bff40dda4108a7f2da0e53de24e2_A_7_Float, _SampleTexture2D_1c8b26de16064d1eb8a72bc023c83d42_A_7_Float, _SampleTexture2D_e0e48b5bd41b4be78b3214b7c6f1259b_A_7_Float, _SampleTexture2D_51bdfa4de2b742ce9cab17c2f2095dce_A_7_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Combine_e897d67d20194a57915c991dcb5f208c_RGB_5_Vector3, _Combine_e897d67d20194a57915c991dcb5f208c_RG_6_Vector2);
        float _Property_0449f47aec884a089269912619a7f84a_Out_0_Float = _GradientScale;
        float4 _Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4 = _UVA;
        float4 _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4 = _Outline_Width;
        float _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float = _WeightNormal;
        float _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float = _WeightBold;
        float4 _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4;
        GetFontWeight_float(_Property_10bda4f78df34e8e96989a3d3d187e0e_Out_0_Vector4, _Property_157b1b9945f3473f8e1d95cbc671e56d_Out_0_Vector4, _Property_456eb6af42fc43deafed58a65aaabef9_Out_0_Float, _Property_2c6722e943e44bfc9116f88e07a01f58_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4);
        float4 _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4 = _Softness;
        float _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean = _OutlineMode;
        float4 _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4;
        ComputeSDF44_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _Combine_e897d67d20194a57915c991dcb5f208c_RGBA_4_Vector4, _Property_0449f47aec884a089269912619a7f84a_Out_0_Float, _GetFontWeightCustomFunction_76b6e1ed19c8416bb3fa6fc5ed808f0f_OutlineWeightOut_4_Vector4, _Property_109450f3ce9a4337bd5959f03381d0a0_Out_0_Vector4, _Property_f1879301e8824bf1b9b4929b94def63d_Out_0_Boolean, _ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4);
        float4 _Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4 = _VertexColor;
        float4 _Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4 = _Face_Color;
        UnityTexture2D _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D = _Face_Texture;
        float2 _Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2 = _UVB;
        float4 _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4 = _FaceTex_ST;
        float2 _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2 = _FaceUVSpeed;
        float2 _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2;
        GenerateUV_float(_Property_5a2b942403c844ad80addcf5f217a183_Out_0_Vector2, _Property_b2c034f364d841ea99e28eb5405f0a83_Out_0_Vector4, _Property_9c32679ce3104d21b3bc771046859f1c_Out_0_Vector2, _GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2);
        float4 _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.tex, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.samplerstate, _Property_48f81fe856144e6da7a8408d0635f978_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_efaed87aa3ff4f05963112535747aab9_UV_2_Vector2) );
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_R_4_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.r;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_G_5_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.g;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_B_6_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.b;
        float _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_A_7_Float = _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4.a;
        float4 _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_3cd4eacf7adb4386bf6cb66419ca855c_Out_0_Vector4, _SampleTexture2D_e5fdee087d774c94ae457af7fc4ddbb3_RGBA_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4);
        float4 _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_cae034e9c7cf4fdd988183235c5f1584_Out_0_Vector4, _Multiply_cd97806a2bb34dddb2af3ce49f5d262b_Out_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4);
        float4 _Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4 = _Outline_Color_1;
        UnityTexture2D _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D = _Outline_Texture;
        float2 _Property_71144076219842a7b2026c28187912cd_Out_0_Vector2 = _UVB;
        float4 _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4 = _OutlineTex_ST;
        float2 _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2 = _OutlineUVSpeed;
        float2 _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2;
        GenerateUV_float(_Property_71144076219842a7b2026c28187912cd_Out_0_Vector2, _Property_c8e83172e5f944298a6b2c20013d0a55_Out_0_Vector4, _Property_ab63bd61bf8c4e24885e76a4335be4ff_Out_0_Vector2, _GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2);
        float4 _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.tex, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.samplerstate, _Property_9d0f99c6c455479cb558635222d60ef0_Out_0_Texture2D.GetTransformedUV(_GenerateUVCustomFunction_ff152fce255a4c4e869f204dbd82856a_UV_2_Vector2) );
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_R_4_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.r;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_G_5_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.g;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_B_6_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.b;
        float _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_A_7_Float = _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4.a;
        float4 _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4;
        Unity_Multiply_float4_float4(_Property_99da205468de438f9bbbbf70f7782054_Out_0_Vector4, _SampleTexture2D_2082f049aaa84a32a1fbd9d76bcd5350_RGBA_0_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4);
        float4 _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4 = _Outline_Color_2;
        float4 _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4 = _Outline_Color_3;
        float4 _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4;
        Layer4_float(_ComputeSDF44CustomFunction_a51ac58765024cfeb503994dbe813c5d_Alpha_2_Vector4, _Multiply_a77976119abd4d0f93c95a36ff86c4a2_Out_2_Vector4, _Multiply_dd0b76e948534e87b96ee2952f3f6024_Out_2_Vector4, _Property_627140e801ad45e89ba76d98cc6e03b1_Out_0_Vector4, _Property_a8e71c4f8d304c73adc4eee7c7b31668_Out_0_Vector4, _Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4);
        UnityTexture2D _Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D = _MainTex;
        UnityTexture2D _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D = _MainTex;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.z;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.w;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelWidth_3_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.x;
        float _TexelSize_0045adc0b3e7434cbc269eed0843a29c_TexelHeight_4_Float = _Property_aecb6284acf845f49fbf04bcc320b1a7_Out_0_Texture2D.texelSize.y;
        float4 _Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4 = _UVA;
        float _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean = max(0, IN.FaceSign.x);
        float3 _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        GetSurfaceNormal_float(_Property_c61d242f63df48dabcf16fbb88fe730f_Out_0_Texture2D.tex, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Width_0_Float, _TexelSize_0045adc0b3e7434cbc269eed0843a29c_Height_2_Float, (_Property_a1ebf3df65f9458091af78a04bcc4a08_Out_0_Vector4.xy), _IsFrontFace_273e8df50fdf40a191db4058b6d205a4_Out_0_Boolean, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3);
        float4 _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4;
        EvaluateLight_float(_Layer4CustomFunction_8cc2f2a2624042db92d36b52bdefc95c_RGBA_2_Vector4, _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3, _EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4);
        UnityTexture2D _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D = _MainTex;
        float4 _Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4 = _UVA;
        float2 _Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2 = _Underlay_Offset;
        float2 _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2;
        Unity_Multiply_float2_float2(_Property_df24939a3c2e46e4a664dbc743892459_Out_0_Vector2, _Divide_750701742ff8485487acefb998ff1637_Out_2_Vector2, _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2);
        float2 _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2;
        Unity_Subtract_float2((_Property_d50c785662a3484587e7c6190c77f4af_Out_0_Vector4.xy), _Multiply_569ef478823a4060868b92b0b2397a4b_Out_2_Vector2, _Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2);
        float4 _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.tex, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.samplerstate, _Property_df8fa09eb3024c7c952760eb21788d30_Out_0_Texture2D.GetTransformedUV(_Subtract_686b823ddee247bd848044814d7ba393_Out_2_Vector2) );
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_R_4_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.r;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_G_5_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.g;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_B_6_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.b;
        float _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float = _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_RGBA_0_Vector4.a;
        float _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float = _GradientScale;
        float _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float = _Underlay_Dilate;
        float _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float = _UnderlaySoftness;
        float _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float;
        ComputeSDF_float(_ScreenSpaceRatioCustomFunction_080d727b9567469aba403b90b764d578_SSR_2_Float, _SampleTexture2D_e164bccc43f347c6bd39de122dbb0695_A_7_Float, _Property_8a2365b644ac4388b594676087fc65d3_Out_0_Float, _Property_7d2c6d60ff2c46d79f9ebae4a2fc72b9_Out_0_Float, _Property_98e1c7c0bf14465a882e2746d9ca7573_Out_0_Float, _ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float);
        float4 _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4 = _UnderlayColor;
        float4 _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4;
        Layer1_float(_ComputeSDFCustomFunction_35962e85967f42ea8cfd284d58a635e9_Alpha_2_Float, _Property_753c4a08f1174c17b91e43970951dbe0_Out_0_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4);
        float4 _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4;
        Composite_float(_EvaluateLightCustomFunction_3d591240919940febe0a5e18cb5fecea_Color_1_Vector4, _Layer1CustomFunction_47ee0bb4f7f742cea4ee4aefb8f755de_RGBA_2_Vector4, _CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4);
        float4 _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4 = _VertexColor;
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_R_1_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[0];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_G_2_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[1];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_B_3_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[2];
        float _Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float = _Property_dc26fd8c14e74708a2039de2eda710d5_Out_0_Vector4[3];
        float4 _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4;
        Unity_Multiply_float4_float4(_CompositeCustomFunction_8e6719817e8447509888d17c5da33faa_RGBA_2_Vector4, (_Split_80a2fedf4d3147d0bf1dab0725e89b94_A_4_Float.xxxx), _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4);
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[0];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[1];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[2];
        float _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float = _Multiply_c9a004339b8f463aaecd9aa0d80f57bd_Out_2_Vector4[3];
        float4 _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4;
        float3 _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        float2 _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2;
        Unity_Combine_float(_Split_0ffecc27c38e43238b9e51b5c3253cc3_R_1_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_G_2_Float, _Split_0ffecc27c38e43238b9e51b5c3253cc3_B_3_Float, float(0), _Combine_ee9b0fa11c594d75858730e635842b51_RGBA_4_Vector4, _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3, _Combine_ee9b0fa11c594d75858730e635842b51_RG_6_Vector2);
        Out_Color_2 = _Combine_ee9b0fa11c594d75858730e635842b51_RGB_5_Vector3;
        Out_Alpha_1 = _Split_0ffecc27c38e43238b9e51b5c3253cc3_A_4_Float;
        Out_Normal_3 = _GetSurfaceNormalCustomFunction_446b52ae1f504d9682dd501c4298082f_Normal_0_Vector3;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
            float4 CustomUV0;
            float2 CustomUV1;
            float4 Color;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            float2 _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2 = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextShaderIndex, float2);
            float _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float = UNITY_ACCESS_HYBRID_INSTANCED_PROP(_TextMaterialMaskShaderIndex, float);
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            float3 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            float2 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            float4 _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            SampleGlyph_float(IN.VertexID, _Property_9ace3d94c09a4cf1bd1b29b52df41340_Out_0_Vector2, _Property_74a676e86f86416091a926c3960ac5c0_Out_0_Float, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2, _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4);
            description.Position = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Position_4_Vector3;
            description.Normal = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Normal_5_Vector3;
            description.Tangent = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Tangent_6_Vector3;
            description.CustomUV0 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVA_7_Vector4;
            description.CustomUV1 = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_UVB_8_Vector2;
            description.Color = _SampleGlyphCustomFunction_33837fbf17514afa808c58d077f0511d_Color_9_Vector4;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
            float4 _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor2) : _OutlineColor2;
            float4 _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor3) : _OutlineColor3;
            float _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float = _GradientScale;
            float4 _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4 = _IsoPerimeter;
            float4 _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4 = _Softness;
            float _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean = _OutlineMode;
            float _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float = _UnderlayDilate;
            float _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float = _UnderlaySoftness;
            float4 _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4 = _UnderlayColor;
            UnityTexture2D _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_OutlineTex);
            float4 _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4 = _OutlineTex_ST;
            float2 _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2 = _OutlineUVSpeed;
            float4 _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_OutlineColor1) : _OutlineColor1;
            UnityTexture2D _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_FaceTex);
            float4 _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4 = _FaceTex_ST;
            float2 _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2 = _FaceUVSpeed;
            float4 _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FaceColor) : _FaceColor;
            float2 _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2 = _UnderlayOffset;
            float2 _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2 = _OutlineOffset1;
            float2 _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2 = _OutlineOffset2;
            float2 _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2 = _OutlineOffset3;
            float _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float = _WeightNormal;
            float _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float = _WeightBold;
            Bindings_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198;
            _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198.FaceSign = IN.FaceSign;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            float _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            float3 _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3;
            SG_TextMeshDOTSTMPSubgraph_673ff67095eddd741a3b9427c33c2f2d_float(_Property_007c75c776ac4f1babe9cd7ae1fc4f14_Out_0_Texture2D, _Property_8135ca333f8f4ea78163743e6ec1f55c_Out_0_Vector4, _Property_85b5940eb77e4625812ded7215bab8d7_Out_0_Vector4, _Property_9147636b0cfa466a9b37a013d8f693bf_Out_0_Float, _Property_1c4df61c2fea404eb3b87b270d7c59bc_Out_0_Vector4, _Property_19075add867e4757b9520d18fe8de1d0_Out_0_Vector4, _Property_c9d7f0dbae7d422985a1cc87c025e76b_Out_0_Boolean, _Property_aa87c72ac0e64469acc34f936f00b3d0_Out_0_Float, _Property_7e0fadb2533f496192c1ad3e78642010_Out_0_Float, _Property_4488af8ff6a7421298a7e827f567263b_Out_0_Vector4, _Property_2db15d90c2204143b225ec4ef08d0755_Out_0_Texture2D, _Property_a535f3bcbeb14622bb177eb6f46e76f4_Out_0_Vector4, _Property_9e87ce9607e14015a3790c528ca5dfda_Out_0_Vector2, _Property_285f6a9863d54ed2a8150727ad749456_Out_0_Vector4, _Property_04dc152dd2ba4d519391577eb1156235_Out_0_Texture2D, _Property_ec184d6d9fb2494897774c9e7d279e6d_Out_0_Vector4, _Property_95928bcb6a284b8d88105a84c2e1d3ce_Out_0_Vector2, _Property_4f194ff591484e908fc2bcdacbcf2570_Out_0_Vector4, _Property_105b1ed1aa714e41bbe1ef5472bdb11f_Out_0_Vector2, _Property_63c7cd57fc3c45a9a97b514fdae32693_Out_0_Vector2, _Property_d4df208fc23b42f2b52364124f1b661c_Out_0_Vector2, _Property_aef5c44f84e04c3185e0b93e95e34204_Out_0_Vector2, IN.CustomUV0, IN.CustomUV1, IN.Color, _Property_7545c57adf674fc28c440b1dc59f8c82_Out_0_Float, _Property_582354ef6247410ebbdd1fee066e7896_Out_0_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float, _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutNormal_3_Vector3);
            surface.BaseColor = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutColor_2_Vector3;
            surface.Alpha = _TextMeshDOTSTMPSubgraph_1aeceb680fac4d9ebb968853c1ade198_OutAlpha_1_Float;
            surface.AlphaClipThreshold = float(0.0001);
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        #ifdef HAVE_VFX_MODIFICATION
        #define VFX_SRP_ATTRIBUTES Attributes
        #define VFX_SRP_VARYINGS Varyings
        #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
        #endif
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
            output.VertexID =                                   input.vertexID;
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
        #ifdef HAVE_VFX_MODIFICATION
        #if VFX_USE_GRAPH_VALUES
            uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
            /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
        #endif
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            output.CustomUV0 = input.CustomUV0;
        output.CustomUV1 = input.CustomUV1;
        output.Color = input.Color;
        
        
        
        
        
        
            #if UNITY_UV_STARTS_AT_TOP
            #else
            #endif
        
        
        #if UNITY_ANY_INSTANCING_ENABLED
        #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
        #endif
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
            BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"
        
        // --------------------------------------------------
        // Visual Effect Vertex Invocations
        #ifdef HAVE_VFX_MODIFICATION
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
        #endif
        
        ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "UnityEditor.ShaderGraphUnlitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}