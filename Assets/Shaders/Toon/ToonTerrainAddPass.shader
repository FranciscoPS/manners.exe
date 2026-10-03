Shader "Hidden/Manners/Toon Terrain (Add Pass)"
{
    Properties
    {
        [HideInInspector] _Control ("Control (RGBA)", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-99"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ToonTerrainSinglePass"
            Tags { "LightMode" = "ToonTerrainSinglePass" }
            ColorMask 0
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return float4(0.0, 0.0, 0.0, 1.0);
            }

            half4 Fragment() : SV_Target
            {
                return half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
