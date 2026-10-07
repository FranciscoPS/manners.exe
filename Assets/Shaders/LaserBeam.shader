Shader "Custom/LaserBeam"
{
    Properties
    {
        _MainTexture ("Main Texture", 2D) = "white" {}
        _Mask ("End Mask (only its length axis is used)", 2D) = "white" {}
        [HDR] _Color ("Beam Color", Color) = (9.35, 0.41, 0, 1)
        [HDR] _CoreColor ("Core Color", Color) = (4.5, 3.6, 2.4, 1)
        [HDR] _StreakColor ("Energy Streak Color", Color) = (6, 4.2, 2.6, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1

        [Header(Cross Section)]
        _CoreWidth ("Core Width (fraction of half width)", Range(0.02, 1)) = 0.3
        _CoreSharpness ("Core Sharpness", Range(0.5, 8)) = 2.5
        _BodyWidth ("Body Width (fraction of half width)", Range(0.1, 1)) = 0.62
        _BodySoftness ("Body Edge Softness", Range(0.01, 0.6)) = 0.22
        _HaloStrength ("Outer Halo Strength", Range(0, 2)) = 0.55
        _HaloPower ("Outer Halo Falloff", Range(0.5, 6)) = 2.2
        _EdgeJitter ("Edge Jitter", Range(0, 0.5)) = 0.12
        _EdgeJitterScale ("Edge Jitter Scale", Float) = 6
        _EdgeJitterSpeed ("Edge Jitter Speed", Float) = 9

        [Header(Pulse)]
        _PulseFrequency ("Pulse Frequency (Hz)", Float) = 7
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.12
        _SurgeFrequency ("Surge Frequency (Hz)", Float) = 1.3
        _SurgeAmount ("Surge Amount", Range(0, 0.5)) = 0.08

        [Header(Energy Streaks)]
        _StreakScale ("Streak Scale (per tile)", Float) = 3
        _StreakSpeed ("Streak Speed (tiles per second)", Float) = 4
        _StreakThreshold ("Streak Threshold", Range(0.3, 0.95)) = 0.62
        _StreakStrength ("Streak Strength", Range(0, 3)) = 1.2

        [Header(Texture Flow)]
        _TilesPerUnit ("Tiles Per World Unit", Float) = 0.52
        _MainSpeed ("Main Scroll Speed (tiles per second)", Vector) = (-0.5, 0, 0, 0)
        _BeamLength ("Beam Length (set by code)", Float) = 1.65

        [Header(Noise)]
        _NoiseScale ("Noise Scale", Float) = 32
        _NoiseSpeed ("Noise Scroll Speed", Vector) = (-5, 0, 0, 0)
        _NoiseAmount ("Noise Distortion", Range(0, 1)) = 0.1
        _NoisePower ("Noise Power", Range(0.1, 8)) = 1
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.9
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
            Name "LaserBeam"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTexture_ST;
                float4 _Mask_ST;
                half4 _Color;
                half4 _CoreColor;
                half4 _StreakColor;
                float _Intensity;
                float _CoreWidth;
                float _CoreSharpness;
                float _BodyWidth;
                float _BodySoftness;
                float _HaloStrength;
                float _HaloPower;
                float _EdgeJitter;
                float _EdgeJitterScale;
                float _EdgeJitterSpeed;
                float _PulseFrequency;
                float _PulseAmount;
                float _SurgeFrequency;
                float _SurgeAmount;
                float _StreakScale;
                float _StreakSpeed;
                float _StreakThreshold;
                float _StreakStrength;
                float _TilesPerUnit;
                float4 _MainSpeed;
                float _BeamLength;
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _NoiseAmount;
                float _NoisePower;
                float _DissolveAmount;
            CBUFFER_END

            float RandomValue(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float r0 = RandomValue(i);
                float r1 = RandomValue(i + float2(1.0, 0.0));
                float r2 = RandomValue(i + float2(0.0, 1.0));
                float r3 = RandomValue(i + float2(1.0, 1.0));

                float bottom = lerp(r0, r1, f.x);
                float top = lerp(r2, r3, f.x);
                return lerp(bottom, top, f.y);
            }

            float SimpleNoise(float2 uv, float scale)
            {
                float total = 0.0;
                total += ValueNoise(uv * (scale / 1.0)) * 0.125;
                total += ValueNoise(uv * (scale / 2.0)) * 0.25;
                total += ValueNoise(uv * (scale / 4.0)) * 0.5;
                return total;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float time = _Time.y;
                float along = uv.x * max(_BeamLength, 0.001) * _TilesPerUnit;

                float pulse = 1.0
                    + _PulseAmount * (0.65 * sin(time * TWO_PI * _PulseFrequency) + 0.35 * sin(time * TWO_PI * _PulseFrequency * 2.3 + 1.7))
                    + _SurgeAmount * sin(time * TWO_PI * _SurgeFrequency);

                float edgeNoise = ValueNoise(float2(along * _EdgeJitterScale - time * _EdgeJitterSpeed, uv.y * 2.0)) - 0.5;
                float across = abs(uv.y * 2.0 - 1.0);
                float acrossJittered = saturate(across * (1.0 + edgeNoise * _EdgeJitter * 2.0));

                float halo = pow(saturate(1.0 - across), _HaloPower) * _HaloStrength;
                float body = 1.0 - smoothstep(_BodyWidth - _BodySoftness, _BodyWidth + _BodySoftness, acrossJittered);
                float coreWidth = _CoreWidth * (1.0 + 0.5 * (pulse - 1.0));
                float core = pow(saturate(1.0 - acrossJittered / max(coreWidth, 0.001)), _CoreSharpness);

                float2 flowUV = float2(along, uv.y);
                float noise = SimpleNoise(flowUV + _NoiseSpeed.xy * time, _NoiseScale);
                noise = pow(max(noise, 0.0001), _NoisePower);

                float2 distorted = lerp(flowUV, noise.xx, _NoiseAmount);
                float2 mainUV = distorted + _MainSpeed.xy * time;

                float4 mainTex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, mainUV);
                float4 dissolved = lerp(mainTex, mainTex.a * noise, _DissolveAmount);
                float bodyAlpha = body * saturate(dissolved.a * 1.4);

                float streakNoise = ValueNoise(float2(along * _StreakScale - time * _StreakSpeed, uv.y * 4.0 + 7.0));
                float streak = smoothstep(_StreakThreshold, _StreakThreshold + 0.18, streakNoise) * body * (1.0 - core) * _StreakStrength;

                float endMask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, float2(uv.x, 0.5)).r;
                float envelope = endMask * IN.color.a;

                float3 tint = IN.color.rgb;
                float3 bodyColor = _Color.rgb * tint * dissolved.rgb * bodyAlpha;
                float3 haloColor = _Color.rgb * tint * halo;
                float3 coreColor = _CoreColor.rgb * core * (0.75 + 0.25 * noise);
                float3 streakColor = _StreakColor.rgb * streak;

                float alpha = saturate(halo * 0.5 + bodyAlpha * 0.9 + core);
                float3 color = (bodyColor + haloColor + coreColor + streakColor) * _Intensity * pulse;

                return half4(color * envelope, alpha * envelope);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
