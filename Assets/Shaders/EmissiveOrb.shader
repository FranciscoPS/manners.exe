Shader "Custom/EmissiveOrb"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 3.0
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _FloatSpeed ("Float Speed", Float) = 1.5
        _FloatAmount ("Float Amount", Float) = 0.3
        _RandomOffset ("Random Offset", Float) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
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
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half _FresnelPower;
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
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half NdotL = saturate(dot(normalWS, lightDir));
                
                half3 diffuse = _BaseColor.rgb * mainLight.color * NdotL * 0.5;
                
                half3 baseColor = _BaseColor.rgb * 0.3;
                
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                
                half3 emission = _EmissionColor.rgb * _EmissionIntensity;
                emission += fresnel * _EmissionColor.rgb * _EmissionIntensity * 0.5;
                
                half3 finalColor = baseColor + diffuse + emission;
                
                finalColor = MixFog(finalColor, input.fogFactor);
                
                return half4(finalColor, _BaseColor.a);
            }
            ENDHLSL
        }
        
        // Shadow caster pass
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half _FresnelPower;
                float _FloatSpeed;
                float _FloatAmount;
                float _RandomOffset;
            CBUFFER_END

            float4 GetShadowPositionHClip(Attributes input)
            {
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
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EmissionIntensity;
                half _FresnelPower;
                float _FloatSpeed;
                float _FloatAmount;
                float _RandomOffset;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
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
                
                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
