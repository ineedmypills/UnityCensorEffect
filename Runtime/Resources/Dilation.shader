Shader "Hidden/Dilation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DilationSize ("Dilation Size", Float) = 1.0
    }
    SubShader
    {
        // No culling, depth testing, or depth writing. This is a 2D post-processing effect.
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Horizontal Dilation
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity provides the size of a texel for _MainTex.
            float _DilationSize;

            fixed4 frag (v2f i) : SV_Target
            {
                float maxVal = 0.0;
                // Sample pixels horizontally
                for (int j = -_DilationSize; j <= _DilationSize; j++)
                {
                    // Sample the mask texture to the left and right of the current pixel.
                    float val = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x * j, 0)).r;
                    // Keep the maximum value found.
                    maxVal = max(maxVal, val);
                }
                return fixed4(maxVal, maxVal, maxVal, 1.0);
            }
            ENDCG
        }

        // Pass 1: Vertical Dilation
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _DilationSize;

            fixed4 frag (v2f i) : SV_Target
            {
                float maxVal = 0.0;
                // Sample pixels vertically
                for (int j = -_DilationSize; j <= _DilationSize; j++)
                {
                    // Sample the mask texture above and below the current pixel.
                    float val = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y * j)).r;
                    // Keep the maximum value found.
                    maxVal = max(maxVal, val);
                }
                return fixed4(maxVal, maxVal, maxVal, 1.0);
            }
            ENDCG
        }
    }
}
