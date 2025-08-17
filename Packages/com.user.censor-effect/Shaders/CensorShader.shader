Shader "Hidden/Custom/CensorShader"
{
    HLSLINCLUDE

        #include "Packages/com.unity.post-processing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_CensorMaskTex, sampler_CensorMaskTex);

        float _PixelSize;
        int _HardEdges;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float2 screen_uv = i.texcoord;

            // Get the censor mask value for the current pixel
            float mask = SAMPLE_TEXTURE2D(_CensorMaskTex, sampler_CensorMaskTex, screen_uv).r;

            if (mask > 0)
            {
                // Pixelate the UV coordinates
                float2 pixelated_uv = floor(screen_uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;

                float4 censored_color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelated_uv);

                if (_HardEdges == 1)
                {
                    // Check the mask value at the center of the pixelated block
                    float hard_mask = SAMPLE_TEXTURE2D(_CensorMaskTex, sampler_CensorMaskTex, pixelated_uv).r;
                    if (hard_mask > 0)
                    {
                        return censored_color;
                    }
                    else
                    {
                        // If the center of the block is not on the object, don't draw this pixel
                        return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screen_uv);
                    }
                }

                return censored_color;
            }

            // If not in the mask, return the original color
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screen_uv);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
