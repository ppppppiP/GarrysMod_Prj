Shader "Jungle Runner/Mobile Curved Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Текстура", 2D) = "white" {}
        [MainColor] _BaseColor("Цвет", Color) = (1,1,1,1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Вырезать прозрачность", Float) = 0
        _Cutoff("Порог прозрачности", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Отсечение граней", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
            CBUFFER_END

            float4 _JR_BendOrigin;
            float4 _JR_BendParameters;
            float4 _JR_SideBendParameters;
            float4 _JR_BendForward;
            float4 _JR_BendRight;
            half4 _JR_DepthGradientNear;
            half4 _JR_DepthGradientFar;
            float4 _JR_DepthGradientParameters;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                half viewDistance : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 BendWorldPosition(float3 positionWS)
            {
                float signedDistance = dot(positionWS - _JR_BendOrigin.xyz, _JR_BendForward.xyz);
                float bendDistance = min(max(abs(signedDistance) - _JR_BendParameters.z, 0.0), _JR_BendParameters.w);
                float bend = bendDistance * bendDistance;
                positionWS += _JR_BendRight.xyz * (bend * _JR_BendParameters.x);
                positionWS.y -= bend * _JR_BendParameters.y;

                float signedSideDistance = dot(positionWS - _JR_BendOrigin.xyz, _JR_BendRight.xyz);
                float sideDistance = min(max(abs(signedSideDistance) - _JR_SideBendParameters.z, 0.0), _JR_SideBendParameters.w);
                float sideBend = sideDistance * sideDistance;
                positionWS -= _JR_BendRight.xyz * (sign(signedSideDistance) * sideBend * _JR_SideBendParameters.x);
                positionWS.y += sideBend * _JR_SideBendParameters.y;
                return positionWS;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = BendWorldPosition(positionWS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.viewDistance = distance(_WorldSpaceCameraPos, positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = SampleSH(normalWS) + mainLight.color * diffuse;
                half3 color = albedo.rgb * lighting * _JR_DepthGradientNear.rgb;
                half gradientRange = max(0.01h, _JR_DepthGradientParameters.y - _JR_DepthGradientParameters.x);
                half gradient = saturate((input.viewDistance - _JR_DepthGradientParameters.x) / gradientRange);
                gradient = gradient * gradient * (3.0h - 2.0h * gradient);
                color = lerp(color, _JR_DepthGradientFar.rgb, gradient * _JR_DepthGradientParameters.z);
                color = MixFog(color, input.fogFactor);
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _JR_BendOrigin;
            float4 _JR_BendParameters;
            float4 _JR_SideBendParameters;
            float4 _JR_BendForward;
            float4 _JR_BendRight;

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float signedDistance = dot(positionWS - _JR_BendOrigin.xyz, _JR_BendForward.xyz);
                float bendDistance = min(max(abs(signedDistance) - _JR_BendParameters.z, 0.0), _JR_BendParameters.w);
                float bend = bendDistance * bendDistance;
                positionWS += _JR_BendRight.xyz * (bend * _JR_BendParameters.x);
                positionWS.y -= bend * _JR_BendParameters.y;
                float signedSideDistance = dot(positionWS - _JR_BendOrigin.xyz, _JR_BendRight.xyz);
                float sideDistance = min(max(abs(signedSideDistance) - _JR_SideBendParameters.z, 0.0), _JR_SideBendParameters.w);
                float sideBend = sideDistance * sideDistance;
                positionWS -= _JR_BendRight.xyz * (sign(signedSideDistance) * sideBend * _JR_SideBendParameters.x);
                positionWS.y += sideBend * _JR_SideBendParameters.y;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
