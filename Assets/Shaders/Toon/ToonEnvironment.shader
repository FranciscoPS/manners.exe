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
        [Enum(No, 0, Si, 8)] _StencilRef ("Recibe las sombras planas del suelo (solo pisos)", Float) = 0

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
        #include "Assets/Shaders/Toon/ToonInput.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            Stencil
            {
                Ref [_StencilRef]
                WriteMask 8
                Comp Always
                Pass Replace
            }

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

            #include "Assets/Shaders/Toon/ToonForwardPass.hlsl"
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

            #include "Assets/Shaders/Toon/ToonOutlinePass.hlsl"
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
