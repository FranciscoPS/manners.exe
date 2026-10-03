Shader "Manners/Toon Ground Shadow"
{
    Properties
    {
        _GroundShadowColor ("Color de la sombra (multiplica el suelo, alfa = fuerza)", Color) = (0.55, 0.6, 0.78, 1)
        [HideInInspector] _BaseColor ("Opacidad al destruirse (alfa)", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GroundShadow"
            Tags { "LightMode" = "ToonGroundShadow" }
            Blend DstColor Zero
            ZWrite Off
            ZTest LEqual
            Cull Off

            Stencil
            {
                Ref 8
                ReadMask 8
                WriteMask 8
                Comp Equal
                Pass Zero
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex GroundShadowVertex
            #pragma fragment GroundShadowFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GroundShadowColor;
            CBUFFER_END

            #include "Assets/Shaders/Toon/ToonGroundShadowPass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
