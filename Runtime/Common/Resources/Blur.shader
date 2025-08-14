Shader "Hidden/CensorBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        // Pass 1: Horizontal Blur
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = 0;
                float2 uv = i.uv;
                float halfBlur = _BlurSize * 0.5;

                // Simple box blur for performance
                for (float x = -halfBlur; x <= halfBlur; x++)
                {
                    col += tex2D(_MainTex, uv + float2(x * _MainTex_TexelSize.x, 0));
                }
                return col / (_BlurSize);
            }
            ENDCG
        }

        // Pass 2: Vertical Blur
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = 0;
                float2 uv = i.uv;
                float halfBlur = _BlurSize * 0.5;

                for (float y = -halfBlur; y <= halfBlur; y++)
                {
                    col += tex2D(_MainTex, uv + float2(0, y * _MainTex_TexelSize.y));
                }
                return col / (_BlurSize);
            }
            ENDCG
        }
    }
}
