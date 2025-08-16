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

        // Common definitions for both passes
        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

        v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _DilationSize;
        ENDCG

        // Pass 0: Horizontal Dilation
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 frag (v2f i) : SV_Target
            {
                float2 step = float2(_MainTex_TexelSize.x * _DilationSize, 0);

                // Optimized sampling. Instead of a slow loop, we use a fixed number of taps spread out.
                // This is much faster for large dilation sizes and gives a visually similar result.
                float maxVal = tex2D(_MainTex, i.uv).r;
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step * 0.25).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step * 0.25).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step * 0.5).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step * 0.5).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step * 0.75).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step * 0.75).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step).r);

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

            fixed4 frag (v2f i) : SV_Target
            {
                float2 step = float2(0, _MainTex_TexelSize.y * _DilationSize);

                // Optimized sampling, same as the horizontal pass.
                float maxVal = tex2D(_MainTex, i.uv).r;
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step * 0.25).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step * 0.25).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step * 0.5).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step * 0.5).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step * 0.75).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step * 0.75).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv + step).r);
                maxVal = max(maxVal, tex2D(_MainTex, i.uv - step).r);

                return fixed4(maxVal, maxVal, maxVal, 1.0);
            }
            ENDCG
        }
    }
}
