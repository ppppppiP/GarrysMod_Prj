Shader "Ancient Trap Hall/Mobile Bowl Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Текстура", 2D) = "white" {}
        [MainColor] _BaseColor("Цвет", Color) = (1,1,1,1)
        [HDR] _EmissionColor("Свечение", Color) = (0,0,0,0)
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END

            float4 _ATH_BowlCenter;
            float4 _ATH_BowlParameters; // x: curvature, y: flat radius, z: maximum height, w: enabled

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 BendIntoBowl(float3 positionWS)
            {
                float2 delta = positionWS.xz - _ATH_BowlCenter.xz;
                float distanceFromCenter = max(length(delta) - _ATH_BowlParameters.y, 0.0);
                float lift = min(distanceFromCenter * distanceFromCenter * _ATH_BowlParameters.x, _ATH_BowlParameters.z);
                positionWS.y += lift * _ATH_BowlParameters.w;
                return positionWS;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = BendIntoBowl(TransformObjectToWorld(input.positionOS.xyz));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = SampleSH(normalWS) + mainLight.color * diffuse;
                half3 color = albedo.rgb * lighting + _EmissionColor.rgb;
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

            float4 _ATH_BowlCenter;
            float4 _ATH_BowlParameters;
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 delta = positionWS.xz - _ATH_BowlCenter.xz;
                float distanceFromCenter = max(length(delta) - _ATH_BowlParameters.y, 0.0);
                positionWS.y += min(distanceFromCenter * distanceFromCenter * _ATH_BowlParameters.x, _ATH_BowlParameters.z) * _ATH_BowlParameters.w;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
