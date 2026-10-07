#ifndef MANNERS_TOON_INPUT_INCLUDED
#define MANNERS_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _EmissionMap_ST;
    half4 _BaseColor;
    half4 _ShadeColor;
    half4 _EmissionColor;
    half4 _OutlineColor;
    half4 _RimColor;
    half4 _GroundShadowColor;
    half _ShadeThreshold;
    half _ShadeSoftness;
    half _LightColorInfluence;
    half _EmissionBaseTint;
    half _OutlineWidth;
    float _OutlineFadeStart;
    float _OutlineFadeEnd;
    half _RimThreshold;
    half _RimSoftness;
    half _Cutoff;
    half _Surface;
CBUFFER_END

#endif
