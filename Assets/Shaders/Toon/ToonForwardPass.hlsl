#ifndef MANNERS_TOON_FORWARD_PASS_INCLUDED
#define MANNERS_TOON_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#if !defined(_RECEIVE_SHADOWS_OFF) || defined(_TOON_RIM)
#define TOON_REQUIRES_POSITION_WS
#endif

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
    #if defined(TOON_REQUIRES_POSITION_WS)
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
    #if defined(TOON_REQUIRES_POSITION_WS)
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

    #if defined(_TOON_RIM)
    half3 viewDirectionWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));
    half rim = half(1.0) - saturate(dot(normalWS, viewDirectionWS));
    half rimSoftness = max(_RimSoftness, half(0.001));
    half rimMask = smoothstep(_RimThreshold - rimSoftness, _RimThreshold + rimSoftness, rim);
    color = lerp(color, _RimColor.rgb, rimMask * _RimColor.a);
    #endif

    #if defined(_EMISSION)
    half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(input.uv, _EmissionMap));
    half3 emission = emissionTex.rgb * _EmissionColor.rgb;
    emission = lerp(emission, emission * baseTex.rgb, _EmissionBaseTint);
    color += emission;
    #endif

    color = MixFog(color, input.fogFactor);
    return half4(color, _BaseColor.a);
}

#endif
