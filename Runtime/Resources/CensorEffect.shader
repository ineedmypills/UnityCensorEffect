// This shader combines the original screen texture with a pixelated version
// based on a censor mask. It supports pixelation, aspect ratio correction,
// and two modes of edge filtering (hard and soft anti-aliasing).
Shader "Hidden/CensorEffect"
{
    Properties
    {
        // Input Textures and Parameters, hidden from the Inspector.
        [HideInInspector] _MainTex ("Screen", 2D) = "white" {}
        [HideInInspector] _CensorMask ("Censor Mask", 2D) = "black" {}
        [HideInInspector] _PixelSize ("Pixel Size", Float) = 10.0
        [HideInInspector] _AntiAliasing ("Anti-Aliasing", Float) = 1.0
    }
    SubShader
    {
        // Standard post-processing setup: no culling, depth writing, or depth testing.
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Input from the C# script (full-screen quad).
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // Data passed from the vertex to the fragment shader.
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Uniforms set by the C# script.
            sampler2D _MainTex;       // The original, pre-effect screen texture.
            sampler2D _CensorMask;    // The R8 mask texture (potentially dilated).
            float _PixelSize;         // The number of pixel blocks across the screen's height.
            float _AntiAliasing;      // A boolean-like float (0 or 1) to toggle soft edges.

            // A standard passthrough vertex shader for post-processing.
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // The core fragment shader for applying the pixelation effect.
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the original screen color at the current fragment's UV.
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // Calculate the UV coordinates for the pixelated version of the screen.
                // 1. Define a grid based on the desired block count, adjusted for aspect ratio.
                float2 pixelGrid = float2(_PixelSize * _ScreenParams.x / _ScreenParams.y, _PixelSize);
                // 2. Snap the current UV to the nearest point on this grid.
                float2 pixelatedUV = round(i.uv * pixelGrid) / pixelGrid;
                // 3. Sample the original texture at the snapped UV to get a blocky, pixelated color.
                fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);

                // Sample the censor mask. The method depends on the anti-aliasing setting.
                fixed mask;
                if (_AntiAliasing > 0.5)
                {
                    // For soft edges, sample the mask at the fragment's native UV.
                    // Then, use smoothstep to create a soft, anti-aliased transition
                    // between the non-censored (0) and censored (1) areas.
                    // The 0.01 lower bound prevents feathering from extending too far
                    // into the non-censored area, keeping the edge crisp.
                    mask = tex2D(_CensorMask, i.uv).r;
                    mask = smoothstep(0.01, 1.0, mask);
                }
                else
                {
                    // For hard, pixel-perfect edges, sample the mask using the same
                    // pixelated UV coordinates used for the color. This ensures the
                    // mask's boundary aligns perfectly with the pixel blocks,
                    // creating a clean, retro look without harsh, sub-pixel aliasing.
                    mask = tex2D(_CensorMask, pixelatedUV).r;
                }

                // Linearly interpolate between the original and pixelated colors
                // using the final processed mask value as the blend factor.
                return lerp(originalColor, pixelatedColor, mask);
            }
            ENDCG
        }
    }
    Fallback Off
}