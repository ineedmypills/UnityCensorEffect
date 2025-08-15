Shader "Hidden/CensorBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Horizontal Gaussian Blur
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
            float _BlurSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 col = 0;

                // 9-tap Gaussian kernel weights
                float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.05405405, 0.01621622 };

                // Center sample
                col += tex2D(_MainTex, i.uv) * weights[0];

                // Symmetric samples
                for (int j = 1; j < 5; j++)
                {
                    col += tex2D(_MainTex, i.uv + float2(texelSize.x * j, 0)) * weights[j];
                    col += tex2D(_MainTex, i.uv - float2(texelSize.x * j, 0)) * weights[j];
                }

                return col;
            }
            ENDCG
        }

        // Pass 1: Vertical Gaussian Blur
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
            float _BlurSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 col = 0;

                // 9-tap Gaussian kernel weights
                float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.05405405, 0.01621622 };

                // Center sample
                col += tex2D(_MainTex, i.uv) * weights[0];

                // Symmetric samples
                for (int j = 1; j < 5; j++)
                {
                    col += tex2D(_MainTex, i.uv + float2(0, texelSize.y * j)) * weights[j];
                    col += tex2D(_MainTex, i.uv - float2(0, texelSize.y * j)) * weights[j];
                }

                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
