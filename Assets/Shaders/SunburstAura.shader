Shader "UI/SunburstAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RayColor ("Ray Color", Color) = (1,1,1,1)
        _RaySegments ("Ray Segments", Float) = 14
        _RaySharpness ("Ray Sharpness", Float) = 3
        _CoreIntensity ("Core Intensity", Float) = 1.1
        _RectSize ("Rect Size (normalized)", Vector) = (1,1,0,0)

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
        Blend SrcAlpha OneMinusSrcAlpha
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
            fixed4 _RayColor;
            float _RaySegments;
            float _RaySharpness;
            float _CoreIntensity;
            float4 _RectSize;
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

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = (IN.texcoord - 0.5) * _RectSize.xy;
                float radius = length(uv) * 2.0;
                float angle = atan2(uv.y, uv.x);

                float rays = pow(abs(sin(angle * _RaySegments * 0.5)), _RaySharpness);
                float outerFalloff = saturate(1.0 - radius);
                float core = saturate(1.0 - radius * 2.2);
                core = core * core;

                float intensity = saturate(rays * outerFalloff + core * _CoreIntensity);

                fixed4 color;
                color.rgb = _RayColor.rgb;
                color.a = intensity * _RayColor.a * IN.color.a;

                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return color;
            }
            ENDCG
        }
    }
}
