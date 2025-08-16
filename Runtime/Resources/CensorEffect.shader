Shader "Hidden/CensorEffect"
{
    Properties
    {
        // The main texture, which will be the screen content.
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Standard setup for a 2D post-processing effect.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CensorMaskTex;
            float _PixelBlockCount;
            float _EnableAntiAliasing;

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the original, unmodified color from the screen.
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // --- Calculate Pixelated UVs ---
                // This logic scales the UVs to create large, blocky pixels.
                // It respects the screen's aspect ratio to ensure pixels are square.
                float2 screenUV = i.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 scaledUV = screenUV;
                scaledUV.y *= _PixelBlockCount;
                scaledUV.x *= _PixelBlockCount * aspect;

                // Floor the scaled UVs to snap to the top-left corner of a block.
                scaledUV = floor(scaledUV);

                // Scale the UVs back down to the 0-1 range.
                scaledUV.y /= _PixelBlockCount;
                scaledUV.x /= (_PixelBlockCount * aspect);

                // Sample the main texture with the new, blocky UVs to get the pixelated color.
                fixed4 pixelatedColor = tex2D(_MainTex, scaledUV);

                // --- Determine Masking ---
                // For soft, anti-aliased edges, we sample the mask normally.
                float softMask = tex2D(_CensorMaskTex, i.uv).r;

                // For sharp, pixel-perfect edges, we sample the mask at the center of the pixel block.
                // This ensures that a block is either fully on or fully off, creating a hard edge.
                float2 blockCenterUV = scaledUV + float2(1.0 / (_PixelBlockCount * aspect * 2.0), 1.0 / (_PixelBlockCount * 2.0));
                float hardMaskSample = tex2D(_CensorMaskTex, blockCenterUV).r;
                float hardMask = step(0.5, hardMaskSample); // Use step to make it a binary 0 or 1.

                // --- Blend Final Color ---
                // Choose which mask to use based on the anti-aliasing setting.
                // _EnableAntiAliasing is a float (0 or 1) that acts like a boolean.
                float finalMask = lerp(hardMask, softMask, _EnableAntiAliasing);

                // Linearly interpolate between the original color and the pixelated color using the final mask.
                return lerp(originalColor, pixelatedColor, finalMask);
            }
            ENDCG
        }
    }
}
