// This shader performs a two-pass separable dilation on a texture.
// Dilation is a morphological operation that expands bright areas of an image.
// It's used here to expand the censor mask, making the censored area larger.
Shader "Hidden/CensorDilation"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _DilationSize ("Dilation Size", Int) = 1
    }
    SubShader
    {
        // Standard post-processing setup.
        Cull Off ZWrite Off ZTest Always

        // --- Pass 0: Horizontal Dilation ---
        // This pass finds the maximum pixel value in a horizontal line.
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity provides texel size (1/width, 1/height)
            int _DilationSize;         // The radius of dilation in pixels, from C# script.

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Start with the darkest possible value.
                fixed maxVal = 0;

                // Loop from -radius to +radius to sample a horizontal kernel.
                // The total number of samples is (2 * _DilationSize + 1).
                for (int j = -_DilationSize; j <= _DilationSize; j++)
                {
                    // Calculate the UV offset for the current sample.
                    float2 offset = float2(_MainTex_TexelSize.x * j, 0);
                    // Sample the texture and get its red channel value.
                    float sample = tex2D(_MainTex, i.uv + offset).r;
                    // Keep track of the maximum value found.
                    maxVal = max(maxVal, sample);
                }

                // Output the maximum value found. This pixel now represents the brightest
                // value in its horizontal neighborhood.
                return fixed4(maxVal, maxVal, maxVal, 1);
            }
            ENDCG
        }

        // --- Pass 1: Vertical Dilation ---
        // This pass takes the result from the horizontal pass and finds the
        // maximum pixel value in a vertical line.
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            int _DilationSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed maxVal = 0;

                // Loop from -radius to +radius to sample a vertical kernel.
                for (int j = -_DilationSize; j <= _DilationSize; j++)
                {
                    float2 offset = float2(0, _MainTex_TexelSize.y * j);
                    float sample = tex2D(_MainTex, i.uv + offset).r;
                    maxVal = max(maxVal, sample);
                }

                // The final result is the maximum value in a 2D square neighborhood,
                // effectively dilating the bright areas of the original texture.
                return fixed4(maxVal, maxVal, maxVal, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
