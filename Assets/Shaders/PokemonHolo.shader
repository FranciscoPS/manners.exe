Shader "UI/PokemonHolo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Angle ("Diagonal Angle (rad)", Float) = 0.785398
        _Frequency ("Band Frequency", Float) = 3
        _Offset ("Scroll Offset", Float) = 0
        _Saturation ("Saturation", Float) = 0.9
        _Intensity ("Sheen Intensity", Float) = 0.55

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
        Blend One One
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
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float _Angle;
            float _Frequency;
            float _Offset;
            float _Saturation;
            float _Intensity;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float3 holoHsv2rgb(float h, float s, float v)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(h.xxx + K.xyz) * 6.0 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float diag = uv.x * cos(_Angle) + uv.y * sin(_Angle);

                float phase = diag * _Frequency + _Offset;
                float hue = frac(phase);
                float3 rainbow = holoHsv2rgb(hue, _Saturation, 1.0);

                float band = pow(abs(sin(phase * 3.14159)), 2.0);

                float3 color = rainbow * band * _Intensity;
                float alpha = band * _Intensity * IN.color.a;

                fixed4 outColor;
                outColor.rgb = color;
                outColor.a = alpha;

                outColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return outColor;
            }
            ENDCG
        }
    }
}
