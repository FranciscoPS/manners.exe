Shader "UI/Manners Plate"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _PatternMul ("Trama: multiplica el relleno (rgb), intensidad (a)", Color) = (0.9, 0.9, 0.95, 1)
        _PatternAdd ("Trama: suma al relleno (rgb), intensidad (a)", Color) = (0, 0, 0, 0)
        _PatternSize ("Periodo de la trama (unidades de canvas)", Float) = 6
        _PatternDuty ("Proporcion de linea", Range(0, 1)) = 0.5
        _PatternDir ("Direccion de la trama (x, y)", Vector) = (0, 1, 0, 0)
        _PatternSpeed ("Velocidad (unidades por segundo)", Float) = 8
        _FillMin ("Luminancia minima del relleno con trama", Range(-1, 1)) = 0.3
        _FillMax ("Luminancia maxima del relleno con trama", Range(0, 2)) = 2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            fixed4 _PatternMul;
            fixed4 _PatternAdd;
            float _PatternSize;
            float _PatternDuty;
            float4 _PatternDir;
            float _PatternSpeed;
            float _FillMin;
            float _FillMax;
            float _UIStyleTime;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if (!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0 / alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;

                half4 tex = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                half4 color = IN.color * tex;

                float coordinate = (dot(IN.worldPosition.xy, _PatternDir.xy) - _UIStyleTime * _PatternSpeed) / max(_PatternSize, 0.001);
                float phase = frac(coordinate);
                float edge = max(fwidth(coordinate), 0.0001);
                float stripe = smoothstep(0.0, edge, phase) * (1.0 - smoothstep(_PatternDuty, _PatternDuty + edge, phase));

                half luminance = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half inside = saturate((luminance - _FillMin) / 0.06) * (1.0 - saturate((luminance - _FillMax) / 0.06));
                half fillMask = inside * stripe;

                color.rgb *= lerp(half3(1, 1, 1), _PatternMul.rgb, fillMask * _PatternMul.a);
                color.rgb += _PatternAdd.rgb * (fillMask * _PatternAdd.a);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
