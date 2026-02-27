Shader "Custom/VehicleShader"
{
   Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color Tint", Color) = (1,1,1,1)

        _ShakeStrength ("Shake Strength", Range(0,0.2)) = 0.02
        _ShakeSpeed ("Shake Speed", Float) = 10
        _LateralAmount ("Lateral Amount", Range(0,1)) = 0.3

        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            float4 _BaseColor;

            float _ShakeStrength;
            float _ShakeSpeed;
            float _LateralAmount;
            float _Metallic;
            float _Smoothness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float t = _Time.y * _ShakeSpeed;

                float verticalShake = sin(t) * _ShakeStrength;
                float lateralShake = cos(t * 1.7) * _ShakeStrength * _LateralAmount;

                float3 shakenPos = IN.positionOS.xyz;
                shakenPos.y += verticalShake;
                shakenPos.x += lateralShake;

                float3 positionWS = TransformObjectToWorld(shakenPos);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.positionWS = positionWS;

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));

                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float3 lighting = albedo.rgb * (mainLight.color * NdotL + 0.2);

                return float4(lighting, 1);
            }

            ENDHLSL
        }
    }
}
