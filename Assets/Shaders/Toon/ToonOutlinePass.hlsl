#ifndef MANNERS_TOON_OUTLINE_PASS_INCLUDED
#define MANNERS_TOON_OUTLINE_PASS_INCLUDED

float _ToonOutlineDisabled;

struct OutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVaryings
{
    float4 positionCS : SV_POSITION;
    half fogFactor : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

OutlineVaryings OutlineVertex(OutlineAttributes input)
{
    OutlineVaryings output = (OutlineVaryings)0;
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

half4 OutlineFragment(OutlineVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half3 color = MixFog(_OutlineColor.rgb, input.fogFactor);
    return half4(color, _OutlineColor.a * _BaseColor.a);
}

#endif
