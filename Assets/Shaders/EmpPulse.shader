Shader "Custom/EmpPulse"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (1, 0.82, 0.18, 1)
        [HDR] _CoreColor ("Core Color", Color) = (2.6, 2.3, 1.3, 1)
        _Progress ("Progress (0 = center, 1 = full radius)", Range(0, 1)) = 0.6
        _Fade ("Fade", Range(0, 1)) = 1
        _Extent ("Quad Extent (quad half-size / pulse radius)", Float) = 1.35

        [Header(Wavefront)]
        _RingWidth ("Ring Width", Range(0.002, 0.2)) = 0.025
        _GlowWidth ("Glow Width", Range(0.01, 0.6)) = 0.14
        _EchoOffset ("Echo Ring Offset", Range(0, 0.5)) = 0.14
        _EchoStrength ("Echo Ring Strength", Range(0, 1)) = 0.45

        [Header(Inner Field)]
        _FillStrength ("Fill Glow", Range(0, 1)) = 0.45
        _FillOpacity ("Fill Opacity", Range(0, 1)) = 0.35
        _RippleFrequency ("Ripple Frequency", Float) = 28
        _RippleSpeed ("Ripple Speed", Float) = 14

        [Header(Electric Arcs)]
        _ArcStrength ("Arc Strength", Range(0, 3)) = 1.4
        _ArcJitter ("Arc Jitter", Range(0, 0.4)) = 0.09
        _ArcSegments ("Arc Segments", Float) = 18
        _FlickerRate ("Flicker Rate", Float) = 24
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                float _Progress;
                float _Fade;
                float _Extent;
                float _RingWidth;
                float _GlowWidth;
                float _EchoOffset;
                float _EchoStrength;
                float _FillStrength;
                float _FillOpacity;
                float _RippleFrequency;
                float _RippleSpeed;
                float _ArcStrength;
                float _ArcJitter;
                float _ArcSegments;
                float _FlickerRate;
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
                float len = length(p);
                float r = len * _Extent;
                float front = _Progress;
                float dist = r - front;

                float core = 1.0 - smoothstep(0.0, _RingWidth, abs(dist));
                float glow = exp(-abs(dist) / _GlowWidth);

                float inside = smoothstep(0.0, _RingWidth * 2.0, -dist);
                float ripple = 0.5 + 0.5 * sin(r * _RippleFrequency - _Time.y * _RippleSpeed);
                float radial = 0.25 + 0.75 * saturate(r / max(front, 0.001));
                float fill = inside * _FillStrength * radial * (0.65 + 0.35 * ripple);

                float angle = atan2(p.y, p.x) / (2.0 * PI) + 0.5;
                float seed = floor(_Time.y * _FlickerRate) * 37.0;
                float jitter = RingNoise(angle * _ArcSegments, _ArcSegments, seed) - 0.5;
                float gate = smoothstep(0.35, 0.65, RingNoise(angle * _ArcSegments * 2.0 + 11.0, _ArcSegments * 2.0, seed + 5.0));
                float arcRadius = front + jitter * _ArcJitter;
                float arc = (1.0 - smoothstep(0.0, _RingWidth * 0.7, abs(r - arcRadius))) * gate * _ArcStrength;

                float echoFront = front - _EchoOffset;
                float echo = (1.0 - smoothstep(0.0, _RingWidth * 1.5, abs(r - echoFront))) * _EchoStrength * step(0.0, echoFront);

                float mask = (1.0 - smoothstep(0.9, 1.0, len)) * _Fade;

                float3 color = _Color.rgb * (glow * 0.9 + fill) + _CoreColor.rgb * (core + arc + echo);
                float alpha = saturate(inside * _FillOpacity + core * 0.5 + glow * 0.25);

                return half4(color * mask, alpha * mask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
