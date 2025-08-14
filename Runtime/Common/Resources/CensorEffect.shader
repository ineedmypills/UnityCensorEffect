Shader "Hidden/CensorEffect"
{
    Properties
    {
        [HideInInspector] _MainTex("Screen", 2D) = "white" {}
        [HideInInspector] _CensorMask("Censor Mask", 2D) = "black" {}
        [HideInInspector] _PixelSize("Pixel Size", Float) = 10.0
        [HideInInspector] _CensorAreaExpansion("Censor Area Expansion", Float) = 0.0
        [HideInInspector] _AntiAliasing("Anti-Aliasing", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "CensorEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CensorMask);
            SAMPLER(sampler_CensorMask);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _PixelSize;
            float _CensorAreaExpansion;
            float _AntiAliasing;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Calculate pixelated coordinates
                float2 pixelGrid = _ScreenParams.xy / _PixelSize;
                float2 pixelatedUV = round(input.uv * pixelGrid) / pixelGrid;

                // Sample original color
                half4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Sample mask
                half mask = SAMPLE_TEXTURE2D(_CensorMask, sampler_CensorMask, pixelatedUV).a;

                if (mask > 0.01)
                {
                    half4 pixelatedColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedUV);

                    if (_AntiAliasing > 0.5)
                    {
                        half smoothMask = smoothstep(0.2, 0.8, mask);
                        return lerp(originalColor, pixelatedColor, smoothMask);
                    }
                    return lerp(originalColor, pixelatedColor, mask);
                }

                return originalColor;
            }
            ENDHLSL
        }
    }
    Fallback Off
}