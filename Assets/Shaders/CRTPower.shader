Shader "UI/Manners CRT Power"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captura de pantalla", 2D) = "white" {}

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

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
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            static const float TwoPi = 6.2831853;
            static const float EdgeMargin = 0.01;
            static const float Epsilon = 0.0001;

            sampler2D _MainTex;

            float _CRTOpenX;
            float _CRTOpenY;
            float _CRTFlash;
            float _CRTGlow;
            float _CRTSignal;
            float _CRTLive;
            float _CRTAspect;
            half4 _CRTOffColor;
            half4 _CRTLineColor;
            half4 _CRTGlowColor;
            float _CRTLineThickness;
            float _CRTDotSize;
            float _CRTGlowSize;
            float _CRTScanlineCount;
            half _CRTScanlineStrength;
            float _CRTEdgeSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            float RoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            half4 frag(v2f IN) : SV_Target
            {
                float aspect = max(_CRTAspect, Epsilon);
                float openX = saturate(_CRTOpenX);
                float openY = saturate(_CRTOpenY);
                float2 centered = IN.texcoord - 0.5;
                float2 p = centered * float2(aspect, 1.0);

                float radius = 0.5 * lerp(_CRTDotSize, _CRTLineThickness, openX);
                float2 halfSize = max(float2(openX * (0.5 * aspect + EdgeMargin), openY * (0.5 + EdgeMargin)), radius);
                float dist = RoundedBox(p, halfSize, radius);
                float edge = max(fwidth(p.y), Epsilon) * _CRTEdgeSoftness;
                half inside = 1.0 - smoothstep(-edge, edge, dist);

                float2 sampleUV = saturate(0.5 + centered / max(float2(openX, openY), Epsilon));
                half3 picture = tex2D(_MainTex, sampleUV).rgb;

                half scanWave = 0.5 + 0.5 * cos(IN.texcoord.y * _CRTScanlineCount * TwoPi);
                half scan = 1.0 - _CRTScanlineStrength * (1.0 - openY) * scanWave;

                half flash = saturate(_CRTFlash);
                half live = saturate(_CRTLive);
                half3 capturedColor = lerp(picture * scan, _CRTLineColor.rgb, flash);
                half3 liveColor = _CRTLineColor.rgb * flash;
                half liveAlpha = 1.0 - scan * (1.0 - flash);
                half3 content = lerp(capturedColor, liveColor, live);
                half contentAlpha = lerp(1.0, liveAlpha, live);

                half signal = saturate(_CRTSignal);
                content = content * signal + _CRTOffColor.rgb * (1.0 - signal);
                contentAlpha = contentAlpha * signal + (1.0 - signal);

                half3 color = content * inside + _CRTOffColor.rgb * (1.0 - inside);
                half alpha = contentAlpha * inside + (1.0 - inside);

                half halo = exp(-max(dist, 0.0) / max(_CRTGlowSize, Epsilon)) * saturate(_CRTGlow) * (1.0 - inside);
                color += _CRTGlowColor.rgb * halo;

                return half4(color, alpha) * IN.color.a;
            }
            ENDCG
        }
    }
}
