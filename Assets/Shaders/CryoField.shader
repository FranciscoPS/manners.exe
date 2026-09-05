Shader "Custom/CryoField"
{
    Properties
    {
        [HDR] _Color ("Frost Color", Color) = (0.55, 1.9, 2.1, 1)
        [HDR] _RimColor ("Rim Color", Color) = (1.4, 3.2, 3.6, 1)
        [HDR] _CrystalColor ("Crystal Color", Color) = (1.6, 2.6, 2.8, 1)
        _SnowflakeTex ("Snowflake Texture (alpha)", 2D) = "black" {}
        _Extent ("Quad Extent (quad half-size / radius)", Float) = 1.25
        _Reveal ("Reveal (set by code)", Range(0, 1.5)) = 1

        [Header(Area)]
        _FillOpacity ("Fill Opacity", Range(0, 1)) = 0.28
        _FillStrength ("Fill Glow", Range(0, 2)) = 0.35
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.08
        _EdgeWobble ("Edge Wobble", Range(0, 0.2)) = 0.04
        _EdgeWobbleScale ("Edge Wobble Scale (integer)", Float) = 5
        _EdgeWobbleSpeed ("Edge Wobble Speed", Float) = 0.6
        _InnerClarity ("Inner Clarity Radius", Range(0, 0.6)) = 0.3

        [Header(Rim)]
        _RimWidth ("Rim Width", Range(0.002, 0.1)) = 0.012
        _RimGlowWidth ("Rim Glow Width", Range(0.01, 0.6)) = 0.16
        _RimStrength ("Rim Strength", Range(0, 3)) = 1.4
        _BreathSpeed ("Breath Speed (Hz)", Float) = 0.55
        _BreathAmount ("Breath Amount", Range(0, 1)) = 0.3
        _RimSweepSpeed ("Rim Sweep Speed (turns per second)", Float) = 0.18
        _RimSweepStrength ("Rim Sweep Strength", Range(0, 3)) = 1

        [Header(Ice Crystals)]
        _CellScale ("Crystal Cell Scale", Float) = 7
        _CrackWidth ("Crack Width", Range(0.005, 0.2)) = 0.045
        _CrackStrength ("Crack Strength", Range(0, 3)) = 0.9
        _CellShade ("Cell Shade Variation", Range(0, 1)) = 0.35
        _CellDrift ("Cell Drift Speed", Float) = 0.25

        [Header(Sparkles)]
        _SparkleDensity ("Sparkle Density", Float) = 26
        _SparkleSpeed ("Sparkle Speed", Float) = 3
        _SparkleStrength ("Sparkle Strength", Range(0, 4)) = 1.6
        _SparkleSize ("Sparkle Size", Range(0.02, 0.5)) = 0.12

        [Header(Snowflakes)]
        _SnowflakeTiling ("Snowflake Tiling", Float) = 2.5
        _SnowflakeSpin ("Snowflake Spin Speed", Float) = 0.08
        _SnowflakeStrength ("Snowflake Strength", Range(0, 2)) = 0.5

        [Header(Mist)]
        _MistScale ("Mist Scale", Float) = 3
        _MistSpeed ("Mist Drift Speed", Vector) = (0.06, 0.03, 0, 0)
        _MistStrength ("Mist Strength", Range(0, 1)) = 0.35
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
            Name "CryoField"
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

            TEXTURE2D(_SnowflakeTex);
            SAMPLER(sampler_SnowflakeTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RimColor;
                half4 _CrystalColor;
                float4 _SnowflakeTex_ST;
                float _Extent;
                float _Reveal;
                float _FillOpacity;
                float _FillStrength;
                float _EdgeSoftness;
                float _EdgeWobble;
                float _EdgeWobbleScale;
                float _EdgeWobbleSpeed;
                float _InnerClarity;
                float _RimWidth;
                float _RimGlowWidth;
                float _RimStrength;
                float _BreathSpeed;
                float _BreathAmount;
                float _RimSweepSpeed;
                float _RimSweepStrength;
                float _CellScale;
                float _CrackWidth;
                float _CrackStrength;
                float _CellShade;
                float _CellDrift;
                float _SparkleDensity;
                float _SparkleSpeed;
                float _SparkleStrength;
                float _SparkleSize;
                float _SnowflakeTiling;
                float _SnowflakeSpin;
                float _SnowflakeStrength;
                float _MistScale;
                float4 _MistSpeed;
                float _MistStrength;
            CBUFFER_END

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float Hash2(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float RingNoise(float coord, float cells, float seed)
            {
                float i = floor(coord);
                float f = coord - i;
                float a = Hash(fmod(i, cells) + seed);
                float b = Hash(fmod(i + 1.0, cells) + seed);
                return lerp(a, b, f * f * (3.0 - 2.0 * f));
            }

            float RingNoiseAnimated(float coord, float cells, float time)
            {
                float frame = floor(time);
                float blend = time - frame;
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(RingNoise(coord, cells, frame * 17.0), RingNoise(coord, cells, (frame + 1.0) * 17.0), blend);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float r0 = Hash2(i);
                float r1 = Hash2(i + float2(1.0, 0.0));
                float r2 = Hash2(i + float2(0.0, 1.0));
                float r3 = Hash2(i + float2(1.0, 1.0));

                return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
            }

            float Fbm(float2 uv)
            {
                float total = 0.0;
                total += ValueNoise(uv) * 0.5;
                total += ValueNoise(uv * 2.0) * 0.3;
                total += ValueNoise(uv * 4.0) * 0.2;
                return total;
            }

            void Voronoi(float2 x, float time, out float f1, out float f2, out float id)
            {
                float2 n = floor(x);
                float2 f = frac(x);
                f1 = 8.0;
                f2 = 8.0;
                id = 0.0;

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 g = float2(i, j);
                        float2 cell = n + g;
                        float2 o = float2(Hash2(cell), Hash2(cell + 19.3));
                        o = 0.5 + 0.4 * sin(time + TWO_PI * o);
                        float2 d = g + o - f;
                        float dist = dot(d, d);

                        if (dist < f1)
                        {
                            f2 = f1;
                            f1 = dist;
                            id = Hash2(cell);
                        }
                        else if (dist < f2)
                        {
                            f2 = dist;
                        }
                    }
                }

                f1 = sqrt(f1);
                f2 = sqrt(f2);
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
                float reveal = max(_Reveal, 0.001);
                float r = len * _Extent / reveal;
                float angle = atan2(p.y, p.x) / TWO_PI + 0.5;
                float time = _Time.y;
                float2 world = p * _Extent;

                float wobbleCells = max(1.0, round(_EdgeWobbleScale));
                float wobble = RingNoiseAnimated(angle * wobbleCells, wobbleCells, time * _EdgeWobbleSpeed) - 0.5;
                float edge = 1.0 + wobble * _EdgeWobble * 2.0;

                float area = 1.0 - smoothstep(edge - _EdgeSoftness, edge, r);
                float inner = smoothstep(_InnerClarity * 0.3, _InnerClarity, r);

                float breath = 1.0 + _BreathAmount * 0.5 * sin(time * TWO_PI * _BreathSpeed);
                float rimDist = abs(r - edge);
                float rim = 1.0 - smoothstep(0.0, _RimWidth, rimDist);
                float rimGlow = exp(-rimDist / _RimGlowWidth) * (0.35 + 0.65 * step(r, edge));
                float sweep = pow(0.5 + 0.5 * cos(TWO_PI * (2.0 * angle - time * _RimSweepSpeed)), 6.0);
                float rimTotal = (rim + rimGlow * 0.6 + rim * sweep * _RimSweepStrength) * _RimStrength * breath;

                float f1;
                float f2;
                float cellId;
                Voronoi(world * _CellScale, time * _CellDrift, f1, f2, cellId);
                float shimmer = 0.5 + 0.5 * sin(time * 0.8 + cellId * TWO_PI);
                float cracks = 1.0 - smoothstep(0.0, _CrackWidth, f2 - f1);
                float crystal = cracks * _CrackStrength * (0.6 + 0.4 * shimmer);
                float cellShade = cellId * _CellShade;

                float2 grid = world * _SparkleDensity;
                float2 cell = floor(grid);
                float2 cellUV = frac(grid) - 0.5;
                float sparkleHash = Hash2(cell);
                float2 sparkleOffset = (float2(Hash2(cell + 7.1), Hash2(cell + 3.3)) - 0.5) * 0.6;
                float sparkleDist = length(cellUV - sparkleOffset);
                float twinkle = pow(0.5 + 0.5 * sin(time * _SparkleSpeed * (0.6 + sparkleHash * 0.8) + sparkleHash * TWO_PI * 3.0), 6.0);
                float sparkle = (1.0 - smoothstep(0.0, _SparkleSize, sparkleDist)) * twinkle * step(0.55, sparkleHash) * _SparkleStrength;

                float2 flakeUV = Rotate(p, time * _SnowflakeSpin * TWO_PI) * _SnowflakeTiling * 0.5;
                float flakes = SAMPLE_TEXTURE2D(_SnowflakeTex, sampler_SnowflakeTex, flakeUV).a * _SnowflakeStrength * (0.75 + 0.25 * sin(time * 0.7 + angle * TWO_PI));

                float mist = smoothstep(0.35, 0.75, Fbm(world * _MistScale + _MistSpeed.xy * time)) * _MistStrength;

                float quadMask = 1.0 - smoothstep(0.92, 1.0, len);

                float3 fillColor = _Color.rgb * (_FillStrength * (0.6 + 0.4 * saturate(1.0 - r)) + mist + cellShade * 0.5 + flakes * inner) * area;
                float3 crystalColor = _CrystalColor.rgb * (crystal + sparkle) * inner * area;
                float3 rimColor = _RimColor.rgb * rimTotal;

                float alpha = saturate(
                    (_FillOpacity * (0.7 + 0.3 * mist) + crystal * 0.3 * inner + flakes * 0.4 * inner + sparkle * 0.5) * area
                    + rim * 0.6 + rimGlow * 0.2);

                float3 color = fillColor + crystalColor + rimColor;
                return half4(color * quadMask, alpha * quadMask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
