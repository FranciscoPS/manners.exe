Shader "Custom/LaserBeam"
{
    Properties
    {
        _MainTexture ("Main Texture", 2D) = "white" {}
        _Mask ("Mask (edges and ends)", 2D) = "white" {}
        [HDR] _Color ("Beam Color", Color) = (9.35, 0.41, 0, 1)
        [HDR] _CoreColor ("Core Color", Color) = (4.5, 3.6, 2.4, 1)
        _CoreWidth ("Core Width (fraction of half width)", Range(0.02, 1)) = 0.3
        _CoreSharpness ("Core Sharpness", Range(0.5, 8)) = 2.5
        _Intensity ("Intensity", Range(0, 4)) = 1

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
                float _CoreWidth;
                float _CoreSharpness;
                float _Intensity;
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

                float2 c0 = i;
                float2 c1 = i + float2(1.0, 0.0);
                float2 c2 = i + float2(0.0, 1.0);
                float2 c3 = i + float2(1.0, 1.0);

                float r0 = RandomValue(c0);
                float r1 = RandomValue(c1);
                float r2 = RandomValue(c2);
                float r3 = RandomValue(c3);

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
                float2 flowUV = float2(along, uv.y);

                float noise = SimpleNoise(flowUV + _NoiseSpeed.xy * time, _NoiseScale);
                noise = pow(max(noise, 0.0001), _NoisePower);

                float2 distorted = lerp(flowUV, noise.xx, _NoiseAmount);
                float2 mainUV = distorted + _MainSpeed.xy * time;

                float4 mainTex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, mainUV);
                float4 dissolved = lerp(mainTex, mainTex.a * noise, _DissolveAmount);
                float4 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, uv);

                float3 body = _Color.rgb * mask.rgb * dissolved.rgb * IN.color.rgb;
                float alpha = saturate(mask.r * dissolved.a * IN.color.a);

                float across = abs(uv.y * 2.0 - 1.0);
                float coreProfile = pow(saturate(1.0 - across / _CoreWidth), _CoreSharpness);
                float core = coreProfile * mask.r * (0.7 + 0.3 * noise) * IN.color.a;

                float3 color = (body * alpha + _CoreColor.rgb * core) * _Intensity;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
