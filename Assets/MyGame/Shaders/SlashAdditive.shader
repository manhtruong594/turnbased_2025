Shader "MyGame/VFX/URP/SlashAdditive"
{
    Properties
    {
        [MainTexture] _BaseMap("Slash Mask (RGBA)", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex("Noise", 2D) = "gray" {}

        [MainColor] _Color("Tint", Color) = (0.35, 0.9, 1.0, 0.9)
        _EdgeColor("Edge Color", Color) = (1.0, 1.0, 1.0, 1.0)

        _Intensity("Core Intensity", Range(0, 8)) = 2.2
        _EdgeIntensity("Edge Intensity", Range(0, 8)) = 3.0

        _FlowSpeed("Noise Flow Speed", Range(-5, 5)) = 1.2
        _NoiseAmount("Noise Amount", Range(0, 1)) = 0.22

        _Dissolve("Dissolve", Range(-1, 1)) = -0.2
        _EdgeWidth("Edge Width", Range(0.001, 0.5)) = 0.08

        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 3.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 4)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Color;
                float4 _EdgeColor;
                float _Intensity;
                float _EdgeIntensity;
                float _FlowSpeed;
                float _NoiseAmount;
                float _Dissolve;
                float _EdgeWidth;
                float _FresnelPower;
                float _FresnelIntensity;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nor = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = nor.normalWS;
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(pos.positionWS);
                OUT.color = IN.color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 flowUV = IN.uv + float2(_Time.y * _FlowSpeed, 0.0);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV).r;

                half dissolveField = baseTex.a - (_Dissolve + (noise - 0.5h) * _NoiseAmount);
                half width = max(_EdgeWidth, 0.001h);

                half alpha = saturate(dissolveField / width);
                half edge = 1.0h - saturate(abs(dissolveField) / width);

                half fresnel = pow(1.0h - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))), _FresnelPower);

                half3 coreCol = baseTex.rgb * _Color.rgb * _Intensity;
                half3 edgeCol = edge * _EdgeColor.rgb * _EdgeIntensity;
                half3 fresnelCol = fresnel * _EdgeColor.rgb * _FresnelIntensity;

                half3 finalRGB = (coreCol + edgeCol + fresnelCol) * IN.color.rgb;
                half finalA = alpha * _Color.a * baseTex.a * IN.color.a;

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
