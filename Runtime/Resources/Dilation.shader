Shader "Hidden/CensorDilation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DilationSize ("Dilation Size", Float) = 1.0
    }
    SubShader
    {
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _DilationSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _DilationSize;
                fixed maxVal = 0;

                // 9-tap kernel
                for (int j = -4; j <= 4; j++)
                {
                    float sample = tex2D(_MainTex, i.uv + float2(texelSize.x * j, 0)).r;
                    maxVal = max(maxVal, sample);
                }

                return fixed4(maxVal, maxVal, maxVal, 1);
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

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _DilationSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _DilationSize;
                fixed maxVal = 0;

                // 9-tap kernel
                for (int j = -4; j <= 4; j++)
                {
                     float sample = tex2D(_MainTex, i.uv + float2(0, texelSize.y * j)).r;
                     maxVal = max(maxVal, sample);
                }

                return fixed4(maxVal, maxVal, maxVal, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
