Shader "UI/WarningStripe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Global)]
        _Brightness ("Global Brightness", Range(0, 3)) = 1

        [Header(Band)]
        [HDR] _FillColor ("Band Fill Color", Color) = (0.32, 0.01, 0.01, 0.56)

        [Header(Hazard Hatch)]
        [HDR] _HatchColor ("Hatch Color", Color) = (1.1, 0.05, 0.04, 0.63)
        _HatchSpacing ("Hatch Spacing (canvas units)", Float) = 34
        _HatchWidth ("Hatch Width (fraction of spacing)", Range(0.05, 0.95)) = 0.4
        _HatchSlant ("Hatch Slant", Range(-3, 3)) = 1
        _HatchSpeed ("Hatch Speed (canvas units per second)", Float) = 45

        [Header(Edges)]
        [HDR] _EdgeColor ("Edge Line Color", Color) = (1.5, 0.09, 0.06, 1)
        _EdgeThickness ("Edge Line Thickness (fraction of height)", Range(0, 0.5)) = 0.07
        _EdgeGlow ("Edge Glow", Range(0, 1)) = 0.3

        [Header(Flicker)]
        _FlickerSpeed ("Flicker Speed (Hz)", Float) = 11
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 canvasPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float _Brightness;
            float4 _FillColor;
            float4 _HatchColor;
            float _HatchSpacing;
            float _HatchWidth;
            float _HatchSlant;
            float _HatchSpeed;
            float4 _EdgeColor;
            float _EdgeThickness;
            float _EdgeGlow;
            float _FlickerSpeed;
            float _FlickerAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.canvasPosition = v.vertex.xy;
                OUT.color = v.color;
                return OUT;
            }

            float4 frag(v2f IN) : SV_Target
            {
                float hatchCoord = (IN.canvasPosition.x + IN.canvasPosition.y * _HatchSlant + _Time.y * _HatchSpeed) / max(_HatchSpacing, 0.01);
                float hatchPhase = frac(hatchCoord);
                float hatchAA = max(fwidth(hatchCoord), 0.0001);
                float hatch = smoothstep(0.0, hatchAA, hatchPhase) * (1.0 - smoothstep(_HatchWidth - hatchAA, _HatchWidth + hatchAA, hatchPhase));

                float edgeDistance = min(IN.texcoord.y, 1.0 - IN.texcoord.y);
                float edgeAA = max(fwidth(edgeDistance), 0.0001);
                float edge = 1.0 - smoothstep(_EdgeThickness - edgeAA, _EdgeThickness + edgeAA, edgeDistance);
                float edgeGlow = saturate(1.0 - edgeDistance / max(_EdgeThickness * 3.0, 0.001)) * _EdgeGlow;
                hatch *= 1.0 - edge;

                float flickerSeed = frac(sin(floor(_Time.y * _FlickerSpeed) * 12.9898) * 43758.5453);
                float flicker = 1.0 - _FlickerAmount * step(0.5, flickerSeed);

                float3 color = _FillColor.rgb * _FillColor.a
                    + _HatchColor.rgb * _HatchColor.a * hatch
                    + _EdgeColor.rgb * (edge + edgeGlow);
                float alpha = saturate(_FillColor.a + _HatchColor.a * hatch + _EdgeColor.a * (edge + edgeGlow * 0.5));

                float visibility = IN.color.a * flicker * tex2D(_MainTex, IN.texcoord).a;
                return float4(color * IN.color.rgb * visibility * _Brightness, alpha * visibility);
            }
            ENDCG
        }
    }
}
