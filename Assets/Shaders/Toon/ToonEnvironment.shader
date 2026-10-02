Shader "Manners/Toon Environment"
{
    Properties
    {
        [MainTexture] _BaseMap ("Textura base (color plano)", 2D) = "white" {}
        [MainColor] _BaseColor ("Tinte base (alfa = opacidad al desvanecer)", Color) = (1, 1, 1, 1)

        [Header(Sombra toon)]
        _ShadeColor ("Color de sombra (multiplica la textura en la cara sin luz)", Color) = (0.55, 0.6, 0.78, 1)
        _ShadeThreshold ("Umbral de sombra (0.5 = corte en el terminador)", Range(0, 1)) = 0.5
        _ShadeSoftness ("Suavidad del corte", Range(0, 0.5)) = 0.02
        _LightColorInfluence ("Influencia del color e intensidad de la luz", Range(0, 1)) = 0
        [Toggle(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff ("Ignorar sombras en tiempo real", Float) = 1

        [Header(Emision de ventanas y luces)]
        [Toggle(_EMISSION)] _EmissionEnabled ("Emision activa", Float) = 0
        _EmissionMap ("Mascara o textura de emision", 2D) = "white" {}
        [HDR] _EmissionColor ("Color de emision (HDR)", Color) = (0, 0, 0, 1)
        _EmissionBaseTint ("Tenir la emision con la textura base", Range(0, 1)) = 0

        [Header(Contorno)]
        _OutlineColor ("Color del contorno", Color) = (0.05, 0.05, 0.08, 1)
        _OutlineWidth ("Grosor del contorno en pixeles a 1080p", Range(0, 8)) = 2
        _OutlineFadeStart ("Distancia a la que el contorno empieza a adelgazar (m)", Float) = 50
        _OutlineFadeEnd ("Distancia a la que el contorno desaparece (m)", Float) = 110
        [Toggle(_OUTLINE_FROM_PIVOT)] _OutlineFromPivot ("Contorno desde el pivote (cajas con aristas duras)", Float) = 0

        [HideInInspector] _Surface ("__surface", Float) = 0
        [HideInInspector] _Blend ("__blend", Float) = 0
        [HideInInspector] _Cull ("__cull", Float) = 2
        [HideInInspector] _SrcBlend ("__src", Float) = 1
        [HideInInspector] _DstBlend ("__dst", Float) = 0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0
        [HideInInspector] _ZWrite ("__zw", Float) = 1
        [HideInInspector] _Cutoff ("__cutoff", Range(0, 1)) = 0.5
        [HideInInspector] _QueueOffset ("__queue", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _EmissionMap_ST;
            half4 _BaseColor;
            half4 _ShadeColor;
            half4 _EmissionColor;
            half4 _OutlineColor;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half _LightColorInfluence;
            half _EmissionBaseTint;
            half _OutlineWidth;
            float _OutlineFadeStart;
            float _OutlineFadeEnd;
            half _Cutoff;
            half _Surface;
        CBUFFER_END

        float _ToonOutlineDisabled;
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _EMISSION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                #if !defined(_RECEIVE_SHADOWS_OFF)
                float3 positionWS : TEXCOORD3;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = half3(TransformObjectToWorldNormal(input.normalOS));
                output.fogFactor = half(ComputeFogFactor(vertexInput.positionCS.z));
                #if !defined(_RECEIVE_SHADOWS_OFF)
                output.positionWS = vertexInput.positionWS;
                #endif
                return output;
            }

            half4 ToonFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseTex.rgb * _BaseColor.rgb;
                half3 normalWS = normalize(input.normalWS);

                #if defined(_RECEIVE_SHADOWS_OFF)
                Light mainLight = GetMainLight();
                half shadow = half(1.0);
                #else
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half shadow = mainLight.shadowAttenuation;
                #endif

                half halfLambert = dot(normalWS, half3(mainLight.direction)) * half(0.5) + half(0.5);
                half softness = max(_ShadeSoftness, half(0.001));
                half lit = smoothstep(_ShadeThreshold - softness, _ShadeThreshold + softness, halfLambert);
                lit = min(lit, smoothstep(half(0.35), half(0.65), shadow));

                half3 lightTint = lerp(half3(1.0, 1.0, 1.0), mainLight.color, _LightColorInfluence);
                half3 color = albedo * lerp(_ShadeColor.rgb, lightTint, lit);

                #if defined(_EMISSION)
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(input.uv, _EmissionMap));
                half3 emission = emissionTex.rgb * _EmissionColor.rgb;
                emission = lerp(emission, emission * baseTex.rgb, _EmissionBaseTint);
                color += emission;
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "ToonOutline" }
            Cull Front

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            #pragma shader_feature_local_vertex _OUTLINE_FROM_PIVOT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                if (_ToonOutlineDisabled > 0.5)
                {
                    output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
                    return output;
                }

                #if defined(_OUTLINE_FROM_PIVOT)
                float3 directionWS = TransformObjectToWorldDir(input.positionOS.xyz, false);
                #else
                float3 directionWS = TransformObjectToWorldNormal(input.normalOS, false);
                #endif

                float2 directionCS = TransformWorldToHClipDir(directionWS).xy;
                float directionLength = length(directionCS);
                directionCS = directionLength > 1e-5 ? directionCS / directionLength : float2(0.0, 0.0);

                float viewDepth = abs(positionCS.w);
                float fadeEnd = max(_OutlineFadeEnd, _OutlineFadeStart + 0.01);
                float distanceFade = 1.0 - smoothstep(_OutlineFadeStart, fadeEnd, viewDepth);
                float widthNdc = 2.0 * _OutlineWidth * distanceFade / 1080.0;
                float2 aspectFix = float2(_ScreenParams.y / max(_ScreenParams.x, 1.0), 1.0);
                positionCS.xy += directionCS * aspectFix * widthNdc * positionCS.w;

                output.positionCS = positionCS;
                output.fogFactor = half(ComputeFogFactor(positionCS.z));
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 color = MixFog(_OutlineColor.rgb, input.fogFactor);
                return half4(color, _OutlineColor.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
