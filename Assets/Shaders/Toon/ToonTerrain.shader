Shader "Manners/Toon Terrain"
{
    Properties
    {
        [Header(Color plano de cada capa del terreno)]
        _LayerColor0 ("Capa 0", Color) = (0.45, 0.68, 0.42, 1)
        _LayerColor1 ("Capa 1", Color) = (0.93, 0.76, 0.42, 1)
        _LayerColor2 ("Capa 2", Color) = (0.42, 0.38, 0.3, 1)
        _LayerColor3 ("Capa 3", Color) = (0.5, 0.5, 0.52, 1)
        _LayerColor4 ("Capa 4", Color) = (0.8, 0.8, 0.82, 1)
        _LayerColor5 ("Capa 5", Color) = (0.35, 0.35, 0.4, 1)
        _LayerColor6 ("Capa 6", Color) = (0.7, 0.69, 0.7, 1)
        _LayerColor7 ("Capa 7", Color) = (0.9, 0.9, 0.9, 1)

        [Header(Sombra toon)]
        _ShadeColor ("Color de sombra (multiplica el color plano)", Color) = (0.55, 0.6, 0.78, 1)
        _ShadeThreshold ("Umbral de sombra en laderas (mas alto = mas ladera en sombra)", Range(0, 1)) = 0.8
        _ShadeSoftness ("Suavidad del corte", Range(0, 0.5)) = 0.01
        _LightColorInfluence ("Influencia del color e intensidad de la luz", Range(0, 1)) = 0

        [Header(Linea de tinta entre capas)]
        _InkColor ("Color de la linea", Color) = (0.05, 0.05, 0.08, 1)
        _InkWidth ("Grosor en pixeles a 1080p (0 = sin linea)", Range(0, 8)) = 2
        _InkFadeStart ("Distancia a la que la linea empieza a adelgazar (m)", Float) = 50
        _InkFadeEnd ("Distancia a la que la linea desaparece (m)", Float) = 110

        [Header(Trama de detalle)]
        [NoScaleOffset] _DetailMap ("Trama (R = manchas, tileable)", 2D) = "white" {}
        _DetailTiling ("Tamano de la trama en metros", Float) = 12
        _DetailThreshold ("Cantidad de manchas", Range(0, 1)) = 0.5
        _DetailStrength0 ("Fuerza de la trama en capas 0 a 3", Vector) = (0, 0, 0, 0)
        _DetailStrength1 ("Fuerza de la trama en capas 4 a 7", Vector) = (0, 0, 0, 0)

        [HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
        [NoScaleOffset] _Control1 ("Mapa de las capas 4 a 7 (lo asigna la herramienta)", 2D) = "black" {}
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _BaseColor ("Main Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _TerrainHolesTexture ("Holes Map (RGB)", 2D) = "white" {}
    }

    HLSLINCLUDE
    #pragma multi_compile_fragment __ _ALPHATEST_ON
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-100"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "False"
            "TerrainCompatible" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Stencil
            {
                Ref 8
                WriteMask 8
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonTerrainVertex
            #pragma fragment ToonTerrainFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma shader_feature_local_fragment _TOON_TERRAIN_DETAIL

            #define _TERRAIN_INSTANCED_PERPIXEL_NORMAL 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Control1);
            TEXTURE2D(_DetailMap);
            SAMPLER(sampler_DetailMap);

            half4 _LayerColor0;
            half4 _LayerColor1;
            half4 _LayerColor2;
            half4 _LayerColor3;
            half4 _LayerColor4;
            half4 _LayerColor5;
            half4 _LayerColor6;
            half4 _LayerColor7;
            half4 _ShadeColor;
            half4 _InkColor;
            half4 _DetailStrength0;
            half4 _DetailStrength1;
            half _ShadeThreshold;
            half _ShadeSoftness;
            half _LightColorInfluence;
            half _InkWidth;
            float _InkFadeStart;
            float _InkFadeEnd;
            float _DetailTiling;
            half _DetailThreshold;

            struct ToonTerrainAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ToonTerrainVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ToonTerrainVaryings ToonTerrainVertex(ToonTerrainAttributes input)
            {
                ToonTerrainVaryings output = (ToonTerrainVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                TerrainInstancing(input.positionOS, input.normalOS, input.texcoord);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.texcoord;
                output.normalWS = half3(TransformObjectToWorldNormal(input.normalOS));
                output.fogFactor = half(ComputeFogFactor(vertexInput.positionCS.z));
                return output;
            }

            void ToonTerrainLayer(float weight, float2 gradient, half3 layerColor, half detail,
                inout float best, inout float second, inout float2 bestGradient, inout float2 secondGradient,
                inout half3 flatColor, inout half detailStrength)
            {
                if (weight > best)
                {
                    second = best;
                    secondGradient = bestGradient;
                    best = weight;
                    bestGradient = gradient;
                    flatColor = layerColor;
                    detailStrength = detail;
                }
                else if (weight > second)
                {
                    second = weight;
                    secondGradient = gradient;
                }
            }

            half ToonTerrainShadow(float3 positionWS)
            {
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                half realtime = MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
                half toon = smoothstep(half(0.35), half(0.65), realtime);
                return lerp(toon, half(1.0), GetMainLightShadowFade(positionWS));
                #else
                return half(1.0);
                #endif
            }

            half4 ToonTerrainFragment(ToonTerrainVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #ifdef _ALPHATEST_ON
                ClipHoles(input.uv);
                #endif

                float2 splatUV = (input.uv * (_Control_TexelSize.zw - 1.0) + 0.5) * _Control_TexelSize.xy;
                float4 control0 = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
                float4 control1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control, splatUV);
                float4 dx0 = ddx(control0);
                float4 dy0 = ddy(control0);
                float4 dx1 = ddx(control1);
                float4 dy1 = ddy(control1);

                float best = -1.0;
                float second = -1.0;
                float2 bestGradient = float2(0.0, 0.0);
                float2 secondGradient = float2(0.0, 0.0);
                half3 flatColor = half3(0.0, 0.0, 0.0);
                half detailStrength = half(0.0);
                ToonTerrainLayer(control0.r, float2(dx0.r, dy0.r), _LayerColor0.rgb, _DetailStrength0.x, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control0.g, float2(dx0.g, dy0.g), _LayerColor1.rgb, _DetailStrength0.y, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control0.b, float2(dx0.b, dy0.b), _LayerColor2.rgb, _DetailStrength0.z, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control0.a, float2(dx0.a, dy0.a), _LayerColor3.rgb, _DetailStrength0.w, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control1.r, float2(dx1.r, dy1.r), _LayerColor4.rgb, _DetailStrength1.x, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control1.g, float2(dx1.g, dy1.g), _LayerColor5.rgb, _DetailStrength1.y, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control1.b, float2(dx1.b, dy1.b), _LayerColor6.rgb, _DetailStrength1.z, best, second, bestGradient, secondGradient, flatColor, detailStrength);
                ToonTerrainLayer(control1.a, float2(dx1.a, dy1.a), _LayerColor7.rgb, _DetailStrength1.w, best, second, bestGradient, secondGradient, flatColor, detailStrength);

                #if defined(_TOON_TERRAIN_DETAIL)
                float pattern = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, input.positionWS.xz / max(_DetailTiling, 0.01)).r;
                float patternWidth = max(fwidth(pattern), 0.002);
                half spot = half(1.0 - smoothstep(_DetailThreshold - patternWidth, _DetailThreshold + patternWidth, pattern));
                flatColor *= half(1.0) - spot * detailStrength;
                #endif

                #if defined(UNITY_INSTANCING_ENABLED)
                float2 normalUV = (input.uv / _TerrainHeightmapRecipSize.zw + 0.5) * _TerrainHeightmapRecipSize.xy;
                half3 normalOS = half3(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, normalUV).rgb * 2.0 - 1.0);
                half3 normalWS = half3(TransformObjectToWorldNormal(normalize(normalOS)));
                #else
                half3 normalWS = normalize(input.normalWS);
                #endif

                Light mainLight = GetMainLight();
                half halfLambert = dot(normalWS, half3(mainLight.direction)) * half(0.5) + half(0.5);
                half softness = max(_ShadeSoftness, half(0.001));
                half lit = smoothstep(_ShadeThreshold - softness, _ShadeThreshold + softness, halfLambert);
                lit = min(lit, ToonTerrainShadow(input.positionWS));

                half3 lightTint = lerp(half3(1.0, 1.0, 1.0), mainLight.color, _LightColorInfluence);
                half3 color = flatColor * lerp(_ShadeColor.rgb, lightTint, lit);

                float viewDepth = abs(input.positionCS.w);
                float inkFade = 1.0 - smoothstep(_InkFadeStart, max(_InkFadeEnd, _InkFadeStart + 0.01), viewDepth);
                float halfWidth = 0.5 * _InkWidth * inkFade * _ScreenParams.y / 1080.0;
                float edgeGradient = length(bestGradient - secondGradient);
                float edgeDistance = (best - max(second, 0.0)) / max(edgeGradient, 1e-6);
                half ink = half((1.0 - smoothstep(halfWidth - 0.5, halfWidth + 0.5, edgeDistance)) * step(0.001, halfWidth));
                color = lerp(color, _InkColor.rgb, ink * _InkColor.a);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalOnlyVertex
            #pragma fragment DepthNormalOnlyFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #define SCENESELECTIONPASS
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Terrain/TerrainLitPasses.hlsl"
            ENDHLSL
        }

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }

    Dependency "AddPassShader" = "Hidden/Manners/Toon Terrain (Add Pass)"
    Dependency "BaseMapShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Base Pass)"
    Dependency "BaseMapGenShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Basemap Gen)"

    FallBack Off
}
