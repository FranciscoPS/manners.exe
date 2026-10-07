Shader "Custom/EmpPulse"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (1, 0.82, 0.18, 1)
        [HDR] _CoreColor ("Core Color", Color) = (2.6, 2.3, 1.3, 1)
        _ArcTex ("Electric Veins Texture", 2D) = "black" {}
        _Progress ("Lead Wave Progress (set by code)", Float) = 0.6
        _Fade ("Fade (set by code)", Range(0, 1)) = 1
        _Extent ("Quad Extent (quad half-size / pulse radius)", Float) = 1.35

        [Header(Wave Train)]
        _WaveCount ("Wave Count (set by code)", Float) = 3
        _WaveDelay ("Wave Delay (fraction of expansion, set by code)", Float) = 0.2
        _WaveFalloff ("Trailing Wave Strength Falloff", Range(0, 1)) = 0.25
        _RingWidth ("Ring Width", Range(0.002, 0.2)) = 0.025
        _GlowWidth ("Glow Width", Range(0.01, 0.6)) = 0.14
        _RippleFrequency ("Trailing Ripple Frequency", Float) = 22
        _RippleDecay ("Trailing Ripple Decay", Range(0.01, 1)) = 0.12
        _RippleStrength ("Trailing Ripple Strength", Range(0, 2)) = 0.8

        [Header(Inner Field)]
        _FillStrength ("Fill Glow", Range(0, 1)) = 0.4
        _FillOpacity ("Fill Opacity", Range(0, 1)) = 0.3
        _VeinStrength ("Electric Veins Strength", Range(0, 3)) = 1.2
        _VeinFlickerRate ("Electric Veins Flicker Rate", Float) = 18
        _VeinScale ("Electric Veins Tiling", Float) = 1.6

        [Header(Electric Arcs)]
        _ArcStrength ("Arc Strength", Range(0, 3)) = 1.4
        _ArcJitter ("Arc Jitter", Range(0, 0.4)) = 0.09
        _ArcSegments ("Arc Segments", Float) = 18
        _FlickerRate ("Flicker Rate", Float) = 24

        [Header(Center Flash)]
        _FlashSize ("Flash Size", Range(0.02, 0.6)) = 0.18
        _FlashDecay ("Flash Decay", Float) = 2.5
        _FlashStrength ("Flash Strength", Range(0, 4)) = 1.6
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
            Name "EmpPulse"
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

            TEXTURE2D(_ArcTex);
            SAMPLER(sampler_ArcTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                float4 _ArcTex_ST;
                float _Progress;
                float _Fade;
                float _Extent;
                float _WaveCount;
                float _WaveDelay;
                float _WaveFalloff;
                float _RingWidth;
                float _GlowWidth;
                float _RippleFrequency;
                float _RippleDecay;
                float _RippleStrength;
                float _FillStrength;
                float _FillOpacity;
                float _VeinStrength;
                float _VeinFlickerRate;
                float _VeinScale;
                float _ArcStrength;
                float _ArcJitter;
                float _ArcSegments;
                float _FlickerRate;
                float _FlashSize;
                float _FlashDecay;
                float _FlashStrength;
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

            float2 Rotate(float2 p, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
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
                float len = length(p);
                float r = len * _Extent;
                float angle = atan2(p.y, p.x) / TWO_PI + 0.5;
                float seed = floor(_Time.y * _FlickerRate) * 37.0;
                float lead = saturate(_Progress);

                float core = 0.0;
                float glow = 0.0;
                float ripple = 0.0;
                float arc = 0.0;

                for (int i = 0; i < 4; i++)
                {
                    float index = float(i);
                    float active = step(index + 0.5, _WaveCount);
                    float progress = saturate(_Progress - index * _WaveDelay);
                    float started = step(0.001, progress);
                    float weight = active * started * saturate(1.0 - index * _WaveFalloff);

                    float dist = r - progress;
                    core += weight * (1.0 - smoothstep(0.0, _RingWidth, abs(dist)));
                    glow += weight * exp(-abs(dist) / _GlowWidth);

                    float trail = step(dist, 0.0) * exp(dist / _RippleDecay);
                    ripple += weight * (0.5 + 0.5 * cos(dist * _RippleFrequency)) * trail;

                    float waveSeed = seed + index * 11.0;
                    float jitter = RingNoise(angle * _ArcSegments, _ArcSegments, waveSeed) - 0.5;
                    float gate = smoothstep(0.35, 0.65, RingNoise(angle * _ArcSegments * 2.0 + 11.0, _ArcSegments * 2.0, waveSeed + 5.0));
                    float arcRadius = progress + jitter * _ArcJitter;
                    arc += weight * (1.0 - smoothstep(0.0, _RingWidth * 0.7, abs(r - arcRadius))) * gate;
                }

                float inside = smoothstep(0.0, _RingWidth * 2.0, lead - r);
                float radial = 0.25 + 0.75 * saturate(r / max(lead, 0.001));
                float fill = inside * _FillStrength * radial;

                float veinSeed = floor(_Time.y * _VeinFlickerRate);
                float2 veinUV = Rotate(p * 0.5, Hash(veinSeed) * TWO_PI) * _VeinScale + 0.5;
                float veinPatch = smoothstep(0.3, 0.7, RingNoise(angle * 6.0, 6.0, veinSeed * 3.0));
                float veins = SAMPLE_TEXTURE2D(_ArcTex, sampler_ArcTex, veinUV).r * inside * radial * veinPatch * _VeinStrength * (0.6 + 0.4 * Hash(veinSeed + 3.0));

                float flash = exp(-(r * r) / (_FlashSize * _FlashSize)) * saturate(1.0 - _Progress * _FlashDecay) * _FlashStrength;

                float mask = (1.0 - smoothstep(0.9, 1.0, len)) * _Fade;

                float3 color = _Color.rgb * (glow * 0.9 + fill + ripple * _RippleStrength * 0.6 + veins * 0.7)
                    + _CoreColor.rgb * (core + arc * _ArcStrength + flash + veins * 0.5 + ripple * _RippleStrength * 0.35);
                float alpha = saturate(inside * _FillOpacity + core * 0.5 + glow * 0.25 + ripple * _RippleStrength * 0.2 + flash * 0.5);

                return half4(color * mask, alpha * mask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
