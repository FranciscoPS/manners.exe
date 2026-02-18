Shader "Custom/FloatingCollectible"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _FloatSpeed ("Float Speed", Float) = 1.5
        _FloatAmount ("Float Amount", Float) = 0.3
        _RandomOffset ("Random Offset", Float) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _FloatSpeed;
                float _FloatAmount;
                float _RandomOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float time = _Time.y * _FloatSpeed + _RandomOffset;
                
                float3 offset;
                offset.x = sin(time * 0.7 + _RandomOffset) * _FloatAmount;
                offset.y = sin(time * 0.9 + 1.5 + _RandomOffset * 0.5) * _FloatAmount * 1.2;
                offset.z = cos(time * 0.6 + 3.0 + _RandomOffset * 0.3) * _FloatAmount;
                
                offset.x += cos(time * 0.4 + 2.1 + _RandomOffset * 0.7) * _FloatAmount * 0.5;
                offset.y += cos(time * 0.5 + 2.5 + _RandomOffset * 0.4) * _FloatAmount * 0.6;
                offset.z += sin(time * 0.3 + 4.0 + _RandomOffset * 0.6) * _FloatAmount * 0.5;
                
                float3 positionOS = input.positionOS.xyz + offset;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;

                Light mainLight = GetMainLight();
                half3 normalWS = normalize(input.normalWS);
                
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                NdotL = NdotL * 0.4 + 0.6;
                
                half3 lighting = mainLight.color * NdotL;
                
                lighting += half3(0.4, 0.4, 0.4);
                
                half3 color = albedo * lighting;
                
                color = MixFog(color, input.fogFactor);
                
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _FloatSpeed;
                float _FloatAmount;
                float _RandomOffset;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float time = _Time.y * _FloatSpeed + _RandomOffset;
                
                float3 offset;
                offset.x = sin(time * 0.7 + _RandomOffset) * _FloatAmount;
                offset.y = sin(time * 0.9 + 1.5 + _RandomOffset * 0.5) * _FloatAmount * 1.2;
                offset.z = cos(time * 0.6 + 3.0 + _RandomOffset * 0.3) * _FloatAmount;
                
                offset.x += cos(time * 0.4 + 2.1 + _RandomOffset * 0.7) * _FloatAmount * 0.5;
                offset.y += cos(time * 0.5 + 2.5 + _RandomOffset * 0.4) * _FloatAmount * 0.6;
               offset.z += sin(time * 0.3 + 4.0 + _RandomOffset * 0.6) * _FloatAmount * 0.5;
                
                float3 positionOS = input.positionOS.xyz + offset;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _FloatSpeed;
                float _FloatAmount;
                float _RandomOffset;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float time = _Time.y * _FloatSpeed + _RandomOffset;
                
                float3 offset;
                offset.x = sin(time * 0.7 + _RandomOffset) * _FloatAmount;
                offset.y = sin(time * 0.9 + 1.5 + _RandomOffset * 0.5) * _FloatAmount * 1.2;
                offset.z = cos(time * 0.6 + 3.0 + _RandomOffset * 0.3) * _FloatAmount;
                
                offset.x += cos(time * 0.4 + 2.1 + _RandomOffset * 0.7) * _FloatAmount * 0.5;
                offset.y += cos(time * 0.5 + 2.5 + _RandomOffset * 0.4) * _FloatAmount * 0.6;
                offset.z += sin(time * 0.3 + 4.0 + _RandomOffset * 0.6) * _FloatAmount * 0.5;
                
                float3 positionOS = input.positionOS.xyz + offset;
                output.positionCS = TransformObjectToHClip(positionOS);

                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
