Shader "Hidden/Custom/CensorShader"
{
    Properties
    {
        // This property is not used in the shader, but is required for the post-processing stack
        _MainTex ("Texture", 2D) = "white" {}
        // The mask texture that defines the areas to be pixelated
        _CensorMaskTex ("Censor Mask", 2D) = "black" {}
        // The size of the pixelation blocks
        _PixelSize ("Pixel Size", Float) = 50.0
        // A toggle for hard edges (0 = soft, 1 = hard)
        _HardEdges ("Hard Edges", Range(0, 1)) = 0
    }

    SubShader
    {
        // This pass is a fullscreen post-processing effect
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

                // Include the standard library from the Post-Processing Stack
                #include "Packages/com.unity.post-processing/PostProcessing/Shaders/StdLib.hlsl"

                // Texture samplers
                TEXTURE2D_SAMPLER2D(_CensorMaskTex, sampler_CensorMaskTex);

                // Shader properties
                float _PixelSize;
                half _HardEdges; // Use half for a 0-1 range value

                // Fragment shader
                half4 Frag(VaryingsDefault i) : SV_Target
                {
                    // Get the screen space UV coordinates
                    float2 screen_uv = i.texcoord;

                    // Calculate the UV coordinates for the pixelated version of the screen
                    float2 pixelated_uv = floor(screen_uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;

                    // Sample the original color and the pixelated color
                    half4 original_color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screen_uv);
                    half4 pixelated_color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelated_uv);

                    // Sample the mask at the current pixel's UV (for hard edges)
                    half hard_mask = SAMPLE_TEXTURE2D(_CensorMaskTex, sampler_CensorMaskTex, screen_uv).r;

                    // Sample the mask at the pixelated UV (for soft edges)
                    half soft_mask = SAMPLE_TEXTURE2D(_CensorMaskTex, sampler_CensorMaskTex, pixelated_uv).r;

                    // Linearly interpolate between the soft and hard mask based on the _HardEdges uniform.
                    // If _HardEdges is 0, we use soft_mask. If it's 1, we use hard_mask.
                    half final_mask = lerp(soft_mask, hard_mask, _HardEdges);

                    // Linearly interpolate between the original color and the pixelated color based on the final mask.
                    // If the mask is 0, we use the original color. If it's 1, we use the pixelated color.
                    half4 final_color = lerp(original_color, pixelated_color, final_mask);

                    return final_color;
                }

            ENDCGPROGRAM
        }
    }
}
