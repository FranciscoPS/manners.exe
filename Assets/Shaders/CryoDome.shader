Shader "Custom/CryoDome"
{
    Properties
    {
        [HDR] _Color ("Dome Color", Color) = (0.5, 1.6, 1.9, 1)
        [HDR] _RimColor ("Rim Color", Color) = (1.2, 2.8, 3.2, 1)
        _Opacity ("Base Opacity", Range(0, 1)) = 0.1
        _TopClarity ("Top Clarity", Range(0, 1)) = 0.75

        [Header(Fresnel Rim)]
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3
        _FresnelStrength ("Fresnel Strength", Range(0, 3)) = 1.2
        _BreathSpeed ("Breath Speed (Hz)", Float) = 0.55
        _BreathAmount ("Breath Amount", Range(0, 1)) = 0.3

        [Header(Base Fog)]
        _BaseFogHeight ("Base Fog Height", Range(0.01, 0.6)) = 0.22
        _BaseFogStrength ("Base Fog Strength", Range(0, 2)) = 0.8

        [Header(Frost Bands)]
        _BandScale ("Band Scale (integer)", Float) = 6
        _BandHeightScale ("Band Height Scale", Float) = 4
        _BandSpeed ("Band Speed", Float) = 0.12
        _BandStrength ("Band Strength", Range(0, 2)) = 0.6

        [Header(Crystals)]
        _CellScale ("Crystal Cell Scale (integer)", Float) = 8
        _CrackWidth ("Crack Width", Range(0.005, 0.2)) = 0.05
        _CrackStrength ("Crack Strength", Range(0, 2)) = 0.35
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
        Cull Back

        Pass
        {
            Name "CryoDome"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _RimColor;
                float _Opacity;
                float _TopClarity;
                float _FresnelPower;
                float _FresnelStrength;
                float _BreathSpeed;
                float _BreathAmount;
                float _BaseFogHeight;
                float _BaseFogStrength;
                float _BandScale;
                float _BandHeightScale;
                float _BandSpeed;
                float _BandStrength;
                float _CellScale;
                float _CrackWidth;
                float _CrackStrength;
            CBUFFER_END

            float Hash2(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 WrapX(float2 cell, float cells)
            {
                cell.x = fmod(cell.x + cells * 4.0, cells);
                return cell;
            }

            float PeriodicNoise(float2 uv, float cells)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float r0 = Hash2(WrapX(i, cells));
                float r1 = Hash2(WrapX(i + float2(1.0, 0.0), cells));
                float r2 = Hash2(WrapX(i + float2(0.0, 1.0), cells));
                float r3 = Hash2(WrapX(i + float2(1.0, 1.0), cells));

                return lerp(lerp(r0, r1, f.x), lerp(r2, r3, f.x), f.y);
            }

            void PeriodicVoronoi(float2 x, float cells, float time, out float f1, out float f2, out float id)
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
                        float2 cell = WrapX(n + g, cells);
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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positions.positionCS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 pos = IN.positionOS;
                float height = saturate(pos.y * 2.0);
                float aboveGround = smoothstep(-0.03, 0.02, pos.y);
                float angle = atan2(pos.x, pos.z) / TWO_PI + 0.5;
                float time = _Time.y;

                float fresnel = pow(1.0 - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))), _FresnelPower) * _FresnelStrength;
                float breath = 1.0 + _BreathAmount * 0.5 * sin(time * TWO_PI * _BreathSpeed);

                float bandCells = max(1.0, round(_BandScale));
                float bandNoise = PeriodicNoise(float2(angle * bandCells + time * _BandSpeed * bandCells, height * _BandHeightScale - time * _BandSpeed * 0.5), bandCells);
                float bands = smoothstep(0.35, 0.8, bandNoise) * _BandStrength * (1.0 - height);

                float f1;
                float f2;
                float cellId;
                float crystalCells = max(1.0, round(_CellScale));
                PeriodicVoronoi(float2(angle * crystalCells, height * crystalCells * 0.5), crystalCells, time * 0.2, f1, f2, cellId);
                float shimmer = 0.5 + 0.5 * sin(time * 0.9 + cellId * TWO_PI);
                float crystal = (1.0 - smoothstep(0.0, _CrackWidth, f2 - f1)) * _CrackStrength * shimmer * (1.0 - height * 0.6);

                float baseFog = (1.0 - smoothstep(0.0, _BaseFogHeight, height)) * _BaseFogStrength;
                float baseOpacity = _Opacity * (1.0 - _TopClarity * height);

                float3 color = _Color.rgb * (baseOpacity + bands + baseFog) + _RimColor.rgb * (fresnel * breath + crystal);
                float alpha = saturate(baseOpacity + fresnel * 0.5 + bands * 0.3 + baseFog * 0.4 + crystal * 0.3);

                return half4(color * aboveGround, alpha * aboveGround);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
