Shader "Custom/WallWarningHex"
{
    Properties
    {
        [Header(Hexagons)]
        _CellSize ("Cell Size (meters, flat to flat)", Float) = 0.55
        _LineWidth ("Line Width (fraction of cell)", Range(0.005, 0.25)) = 0.045
        _LineGlow ("Line Glow Width (fraction of cell)", Range(0.01, 0.5)) = 0.16
        [HDR] _LineColor ("Line Color", Color) = (2.4, 0.16, 0.1, 0.9)
        [HDR] _FillColor ("Cell Fill Color", Color) = (0.85, 0.03, 0.03, 0.14)

        [Header(Cell Activity)]
        _LitCellRatio ("Lit Cells Ratio", Range(0, 1)) = 0.16
        [HDR] _LitCellColor ("Lit Cell Color", Color) = (1.5, 0.07, 0.05, 0.38)
        _LitCellRate ("Lit Cells Change Rate (Hz)", Float) = 2.2
        _FlickerSpeed ("Cell Flicker Speed (Hz)", Float) = 1.4
        _FlickerAmount ("Cell Flicker Amount", Range(0, 1)) = 0.4

        [Header(Scan And Ripple)]
        _ScanSpeed ("Scan Speed (sweeps per second)", Float) = 0.55
        _ScanWidth ("Scan Width (fraction of height)", Range(0.01, 0.6)) = 0.18
        _ScanStrength ("Scan Strength", Range(0, 3)) = 0.9
        _RippleSpacing ("Ripple Spacing (meters)", Float) = 1.3
        _RippleSpeed ("Ripple Speed (meters per second)", Float) = 1.6
        _RippleStrength ("Ripple Strength", Range(0, 3)) = 0.9
        _ScanlineDensity ("Hologram Scanlines per Meter", Float) = 36
        _ScanlineStrength ("Hologram Scanline Strength", Range(0, 1)) = 0.22

        [Header(Shape)]
        _BorderFade ("Border Fade (fraction of zone)", Range(0.01, 0.5)) = 0.1
        _RevealJitter ("Reveal Jitter", Range(0, 1)) = 0.35
        _RevealSoftness ("Reveal Softness", Range(0.01, 1)) = 0.2
        _GroundGlow ("Ground Glow", Range(0, 3)) = 0.7
        _GroundGlowHeight ("Ground Glow Height (meters)", Float) = 0.35

        [Header(Set By Code)]
        _Intensity ("Intensity (set by code)", Range(0, 1)) = 1
        _Opacity ("Opacity (set by code)", Range(0, 1)) = 1
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
            Name "WallWarningHex"
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
                float2 wallPosition : TEXCOORD1;
                float2 panelSize : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LineColor;
                half4 _FillColor;
                half4 _LitCellColor;
                float _CellSize;
                float _LineWidth;
                float _LineGlow;
                float _LitCellRatio;
                float _LitCellRate;
                float _FlickerSpeed;
                float _FlickerAmount;
                float _ScanSpeed;
                float _ScanWidth;
                float _ScanStrength;
                float _RippleSpacing;
                float _RippleSpeed;
                float _RippleStrength;
                float _ScanlineDensity;
                float _ScanlineStrength;
                float _BorderFade;
                float _RevealJitter;
                float _RevealSoftness;
                float _GroundGlow;
                float _GroundGlowHeight;
                float _Intensity;
                float _Opacity;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float HexDistance(float2 p)
            {
                p = abs(p);
                return max(dot(p, float2(0.5, 0.8660254)), p.x);
            }

            void HexCell(float2 p, out float2 local, out float2 id)
            {
                const float2 r = float2(1.0, 1.7320508);
                const float2 h = r * 0.5;
                float2 a = p - r * floor(p / r) - h;
                float2 q = p - h;
                float2 b = q - r * floor(q / r) - h;
                local = dot(a, a) < dot(b, b) ? a : b;
                id = p - local;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float3 axisX = float3(objectToWorld._m00, objectToWorld._m10, objectToWorld._m20);
                float3 axisY = float3(objectToWorld._m01, objectToWorld._m11, objectToWorld._m21);
                float scaleX = max(length(axisX), 0.0001);
                float scaleY = max(length(axisY), 0.0001);

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv = IN.uv;
                OUT.wallPosition = float2(dot(positionWS, axisX / scaleX), positionWS.y);
                OUT.panelSize = float2(scaleX, scaleY);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;
                float cellSize = max(_CellSize, 0.01);
                float2 panelSize = max(IN.panelSize, 0.01);

                float2 local;
                float2 id;
                HexCell(IN.wallPosition / cellSize, local, id);

                float edgeDistance = 0.5 - HexDistance(local);
                float aa = max(fwidth(edgeDistance), 0.0001);
                float lineMask = 1.0 - smoothstep(_LineWidth - aa, _LineWidth + aa, edgeDistance);
                float glow = saturate(1.0 - edgeDistance / max(_LineGlow, 0.001));
                glow *= glow;

                float cellRandom = Hash21(id);
                float2 cellCenterUV = IN.uv - local * cellSize / panelSize;
                float radial = length((cellCenterUV - 0.5) * 2.0);
                float threshold = radial * (1.0 - _RevealJitter * 0.5) + cellRandom * _RevealJitter * 0.5;
                float cellVisible = smoothstep(threshold, threshold + _RevealSoftness, _Intensity * (1.05 + _RevealSoftness));

                float2 edgeUV = min(IN.uv, 1.0 - IN.uv);
                float border = smoothstep(0.0, _BorderFade, edgeUV.x) * smoothstep(0.0, _BorderFade, edgeUV.y);

                float flicker = 1.0 - _FlickerAmount * (0.5 + 0.5 * sin(time * TWO_PI * _FlickerSpeed + cellRandom * TWO_PI));
                float litSeed = Hash21(id + floor(time * _LitCellRate + cellRandom) * 17.13);
                float lit = step(1.0 - _LitCellRatio, litSeed) * (0.55 + 0.45 * saturate(1.0 - edgeDistance * 2.0));

                float2 metersFromCenter = (IN.uv - 0.5) * panelSize;
                float spacing = max(_RippleSpacing, 0.01);
                float ripplePhase = frac(length(metersFromCenter) / spacing - time * _RippleSpeed / spacing);
                float ripple = pow(1.0 - abs(ripplePhase - 0.5) * 2.0, 4.0) * _RippleStrength * _Intensity;

                float scanPhase = frac(IN.uv.y - time * _ScanSpeed);
                float scan = smoothstep(1.0 - _ScanWidth, 1.0, scanPhase) * _ScanStrength;

                float scanlines = 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin(IN.wallPosition.y * _ScanlineDensity * TWO_PI - time * 3.0));

                float energy = (1.0 + ripple) * flicker;
                float lineTerm = (lineMask + glow * 0.45) * energy + scan * (lineMask + 0.3);
                float fillTerm = flicker * (1.0 + scan * 0.5);

                float3 color = _LineColor.rgb * lineTerm
                    + _FillColor.rgb * _FillColor.a * fillTerm
                    + _LitCellColor.rgb * _LitCellColor.a * lit;
                float alpha = _LineColor.a * (lineMask + glow * 0.3) * energy
                    + _FillColor.a * fillTerm
                    + _LitCellColor.a * lit
                    + scan * 0.12;

                float brightness = lerp(0.55, 1.0, _Intensity);
                float cellMask = cellVisible * border * brightness * scanlines;

                float groundHeight = max(_GroundGlowHeight, 0.01);
                float ground = saturate(1.0 - IN.uv.y * panelSize.y / groundHeight);
                ground *= ground * _GroundGlow * _Intensity * smoothstep(0.0, _BorderFade * 2.0, edgeUV.x);

                float3 finalColor = (color * cellMask + _LineColor.rgb * ground) * _Opacity;
                float finalAlpha = saturate(alpha * cellMask + ground * 0.35) * _Opacity;
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
