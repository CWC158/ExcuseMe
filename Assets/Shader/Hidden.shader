Shader "Custom/Hidden"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [IntRange] _index("Reference", Range(0, 255)) = 0
        [HDR]_Emission("Emission", Color) = (0, 0 ,0, 1)
        _Speed("Breath Speed", Float) = 0
        _FresnelPower("Fresnel", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Stencil
        {
            Ref[_index]
            Comp Equal
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0; // 世界空间法线
                float3 viewDirWS  : TEXCOORD1; // 世界空间视线方向
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _Speed;
                float4 _Emission;
                float4 _FresnelPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                // 计算视线方向：摄像机位置 - 顶点世界位置
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(worldPos);
    
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);
                float4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.normalWS) * _BaseColor;
                float time = _Time.y * _Speed;
                float fresnel = 1.0 - saturate(dot(normal, viewDir));
                fresnel = pow(fresnel, _FresnelPower);
                float pulse = sin(time) * 0.5 + 0.5;
                color.rgb += _Emission.rgb * pulse * fresnel;
                return color;
            }
            ENDHLSL
        }
    }
}
