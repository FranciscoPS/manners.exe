Shader "Custom/LaserImpact"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (9, 1.2, 0.1, 1)
        [HDR] _CoreColor ("Core Color", Color) = (6, 4.2, 2.6, 1)
        _Fade ("Fade (set by code)", Range(0, 1)) = 1

        [Header(Ember)]
        _CoreSize ("Core Size", Range(0.02, 0.6)) = 0.16
        _GlowSize ("Glow Size", Range(0.05, 1)) = 0.4
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.7

        [Header(Shock Rings)]
        _RingCount ("Ring Count (1-3)", Float) = 2
        _RingSpeed ("Ring Speed (rings per second)", Float) = 2.2
        _RingWidth ("Ring Width", Range(0.005, 0.2)) = 0.035
        _RingStrength ("Ring Strength", Range(0, 2)) = 0.9

        [Header(Sparks)]
        _SparkCount ("Spark Count", Float) = 22
        _SparkRate ("Spark Reshuffle Rate", Float) = 16
        _SparkThreshold ("Spark Threshold", Range(0.3, 0.95)) = 0.7
        _SparkReach ("Spark Reach", Range(0.2, 1)) = 0.85
        _SparkFlow ("Spark Flow Speed", Float) = 6
        _SparkStrength ("Spark Strength", Range(0, 3)) = 1.3

        [Header(Scorch)]
        _ScorchSize ("Scorch Size", Range(0.05, 1)) = 0.5
        _ScorchStrength ("Scorch Darkening", Range(0, 1)) = 0.55

        [Header(Flicker)]
        _FlickerRate ("Flicker Rate", Float) = 28
        _FlickerAmount ("Flicker Amount", Range(0, 0.5)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "LaserImpact"
            Tags { "LightMode" = "UniversalForward" }

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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                float _Fade;
                float _CoreSize;
                float _GlowSize;
                float _GlowStrength;
                float _RingCount;
                float _RingSpeed;
                float _RingWidth;
                float _RingStrength;
                float _SparkCount;
                float _SparkRate;
                float _SparkThreshold;
                float _SparkReach;
                float _SparkFlow;
                float _SparkStrength;
                float _ScorchSize;
                float _ScorchStrength;
                float _FlickerRate;
                float _FlickerAmount;
            CBUFFER_END

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float RingNoise(float coord, float cells, float seed)
            {
                float i = floor(coord);
                float f = coord - i;
                float a = Hash(fmod(i, cells) + seed);
                float b = Hash(fmod(i + 1.0, cells) + seed);
                return lerp(a, b, f * f * (3.0 - 2.0 * f));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float r = length(p);
                float angle = atan2(p.y, p.x) / TWO_PI + 0.5;
                float time = _Time.y;

                float flicker = 1.0 - _FlickerAmount * Hash(floor(time * _FlickerRate));

                float core = exp(-(r * r) / (_CoreSize * _CoreSize));
                float glow = exp(-r / _GlowSize) * _GlowStrength;

                float rings = 0.0;
                for (int i = 0; i < 3; i++)
                {
                    float active = step(float(i) + 0.5, _RingCount);
                    float ringT = frac(time * _RingSpeed + float(i) / max(_RingCount, 1.0));
                    float width = _RingWidth + ringT * 0.04;
                    float ring = 1.0 - smoothstep(0.0, width, abs(r - ringT));
                    rings += active * ring * pow(1.0 - ringT, 1.5);
                }

                float seed = floor(time * _SparkRate) * 13.0;
                float sparkNoise = RingNoise(angle * _SparkCount, _SparkCount, seed);
                float spark = smoothstep(_SparkThreshold, _SparkThreshold + 0.1, sparkNoise);
                float dash = smoothstep(0.35, 0.7, frac(r * 4.0 - time * _SparkFlow + Hash(floor(angle * _SparkCount) + seed)));
                float reach = (1.0 - smoothstep(_SparkReach * 0.5, _SparkReach, r)) * smoothstep(_CoreSize * 0.6, _CoreSize * 1.2, r);
                float sparks = spark * dash * reach * _SparkStrength;

                float scorch = (1.0 - smoothstep(_ScorchSize * 0.3, _ScorchSize, r)) * _ScorchStrength;

                float mask = (1.0 - smoothstep(0.85, 1.0, r)) * _Fade;

                float3 color = (_CoreColor.rgb * core + _Color.rgb * (glow + rings * _RingStrength + sparks)) * flicker;
                float alpha = saturate(core + glow * 0.6 + rings * _RingStrength * 0.5 + sparks * 0.6 + scorch);

                return half4(color * mask, alpha * mask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
