// Performs a two-pass separable dilation on a texture.
// Used here to expand the censor mask.
Shader "Hidden/CensorDilation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DilationSize ("Dilation Size", Int) = 1
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            int _DilationSize;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed maxVal = 0;
                for (int j = -_DilationSize; j <= _DilationSize; j++) {
                    float2 offset = float2(_MainTex_TexelSize.x * j, 0);
                    maxVal = max(maxVal, tex2D(_MainTex, i.uv + offset).r);
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

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            int _DilationSize;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed maxVal = 0;
                for (int j = -_DilationSize; j <= _DilationSize; j++) {
                    float2 offset = float2(0, _MainTex_TexelSize.y * j);
                    maxVal = max(maxVal, tex2D(_MainTex, i.uv + offset).r);
                }
                return fixed4(maxVal, maxVal, maxVal, 1);
            }
            ENDCG
        }
    }
}
