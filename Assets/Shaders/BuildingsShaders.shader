Shader "Custom/BuildingsShaders"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale("Noise Scale", Float) = 3
        _EdgeWidth("Edge Width", Range(0.001,0.2)) = 0.05
        _EdgeColor("Edge Color", Color) = (1,0.5,0,1)

        _FallStrength("Fall Strength", Float) = 3
        _FallSpread("Fall Spread", Float) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            AlphaToMask On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float dissolveVal : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;

                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                half4 _EdgeColor;

                float _FallStrength;
                float _FallSpread;
            CBUFFER_END


            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) +
                       (c - a) * u.y * (1.0 - u.x) +
                       (d - b) * u.x * u.y;
            }


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                float height = pos.y;
                float heightFactor = saturate(height);

                float2 worldPattern = pos.xz * _NoiseScale;
                float n = noise(worldPattern);

                n = floor(n * 2.0) / 2.0;

                float dissolve = (1.0 - heightFactor) + n - _DissolveAmount;

                if (dissolve < 0)
                {
                    float fall = -_FallStrength * _DissolveAmount;
                    pos.y += fall;
                    pos.x += (n - 0.5) * _FallSpread * _DissolveAmount;
                }

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.dissolveVal = dissolve;

                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                clip(IN.dissolveVal);

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float edge = smoothstep(0, _EdgeWidth, IN.dissolveVal);
                half3 finalColor = lerp(_EdgeColor.rgb, baseColor.rgb, edge);

                return half4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}
