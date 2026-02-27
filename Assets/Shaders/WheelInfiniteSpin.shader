Shader "Custom/WheelInfiniteSpin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SpinSpeed ("Spin Speed", Float) = 10
        _BlurStrength ("Blur Strength", Range(0,1)) = 0.5
        _SpinAxis ("Spin Axis (0=X,1=Y,2=Z)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        ZWrite On
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _SpinSpeed;
            float _BlurStrength;
            float _SpinAxis;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float3 RotateAroundAxis(float3 pos, float angle, float3 axis)
            {
                float s = sin(angle);
                float c = cos(angle);
                float oc = 1.0 - c;

                return float3(
                    oc * axis.x * axis.x + c,
                    oc * axis.x * axis.y - axis.z * s,
                    oc * axis.z * axis.x + axis.y * s
                ) * pos.x
                +
                float3(
                    oc * axis.x * axis.y + axis.z * s,
                    oc * axis.y * axis.y + c,
                    oc * axis.y * axis.z - axis.x * s
                ) * pos.y
                +
                float3(
                    oc * axis.z * axis.x - axis.y * s,
                    oc * axis.y * axis.z + axis.x * s,
                    oc * axis.z * axis.z + c
                ) * pos.z;
            }

            v2f vert (appdata v)
            {
                v2f o;

                float angle = _Time.y * _SpinSpeed;

                float3 axis = float3(1,0,0);
                if (_SpinAxis == 1) axis = float3(0,1,0);
                if (_SpinAxis == 2) axis = float3(0,0,1);

                float3 rotated = RotateAroundAxis(v.vertex.xyz, angle, axis);

                o.vertex = UnityObjectToClipPos(float4(rotated,1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float blur = abs(sin(_Time.y * _SpinSpeed * 2));
                col.rgb = lerp(col.rgb, col.rgb * 0.4, blur * _BlurStrength);

                col.a = 1; 

                return col;
            }
            ENDCG
        }
    }
}
