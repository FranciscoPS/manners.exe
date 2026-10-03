#ifndef MANNERS_TOON_GROUND_SHADOW_PASS_INCLUDED
#define MANNERS_TOON_GROUND_SHADOW_PASS_INCLUDED

float _ToonGroundHeight;

struct GroundShadowAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GroundShadowVaryings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

GroundShadowVaryings GroundShadowVertex(GroundShadowAttributes input)
{
    GroundShadowVaryings output = (GroundShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 lightDirection = _MainLightPosition.xyz;
    bool hasSun = dot(_MainLightColor.rgb, float3(1.0, 1.0, 1.0)) > 0.0001 && lightDirection.y > 0.01;
    if (!hasSun)
    {
        output.positionCS = float4(0.0, 0.0, 0.0, 1.0);
        return output;
    }

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float height = max(positionWS.y - _ToonGroundHeight, 0.0);
    positionWS.xz -= lightDirection.xz * (height / max(lightDirection.y, 0.2));
    positionWS.y = _ToonGroundHeight;

    float4 positionCS = TransformWorldToHClip(positionWS);
    float4 liftedCS = TransformWorldToHClip(positionWS + float3(0.0, 0.3, 0.0));
    positionCS.z = liftedCS.z / liftedCS.w * positionCS.w;
    output.positionCS = positionCS;
    return output;
}

half4 GroundShadowFragment(GroundShadowVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half strength = _GroundShadowColor.a * _BaseColor.a;
    return half4(lerp(half3(1.0, 1.0, 1.0), _GroundShadowColor.rgb, strength), 1.0);
}

#endif
