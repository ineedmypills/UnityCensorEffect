Shader "Hidden/Custom/CensorShader"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

                #include "Packages/com.unity.post-processing/PostProcessing/Shaders/StdLib.hlsl"

                TEXTURE2D_SAMPLER2D(_CensorMaskTex, sampler_CensorMaskTex);

                float _PixelSize;
                int _HardEdges;

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    float2 screen_uv = i.texcoord;
                    float4 original_color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screen_uv);

                    if (_HardEdges == 1)
                    {
                        // Hard Edges: Pixelate only if the current pixel is within the mask.
                        // This creates a sharp silhouette.
                        float mask = SAMPLE_TEXTURE2D(_CensorMaskTex, sampler_CensorMaskTex, screen_uv).r;
                        if (mask > 0)
                        {
                            float2 pixelated_uv = floor(screen_uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelated_uv);
                        }
                        return original_color;
                    }
                    else
                    {
                        // Soft Edges: Pixelate if the source of the pixelation is within the mask.
                        // This allows the pixelation effect to "bleed" over the edges of the mask.
                        float2 pixelated_uv = floor(screen_uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                        float source_mask = SAMPLE_TEXTURE2D(_CensorMaskTex, sampler_CensorMaskTex, pixelated_uv).r;
                        if (source_mask > 0)
                        {
                            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelated_uv);
                        }
                        return original_color;
                    }
                }

            ENDCGPROGRAM
        }
    }
}
