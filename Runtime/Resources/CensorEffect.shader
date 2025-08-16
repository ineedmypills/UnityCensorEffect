// Combines the original screen texture with a pixelated version based on a censor mask.
Shader "Hidden/CensorEffect"
{
    Properties
    {
        _MainTex ("Screen", 2D) = "white" {}
        _CensorMask ("Censor Mask", 2D) = "black" {}
        _PixelSize ("Pixel Size", Float) = 10.0
        _AntiAliasing ("Anti-Aliasing", Float) = 1.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

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
            sampler2D _CensorMask;
            float _PixelSize;
            float _AntiAliasing;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                float2 pixelGrid = float2(_PixelSize * _ScreenParams.x / _ScreenParams.y, _PixelSize);
                float2 pixelatedUV = round(i.uv * pixelGrid) / pixelGrid;
                fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);

                fixed mask;
                if (_AntiAliasing > 0.5) {
                    mask = tex2D(_CensorMask, i.uv).r;
                    mask = smoothstep(0.01, 1.0, mask);
                } else {
                    mask = tex2D(_CensorMask, pixelatedUV).r;
                }

                return lerp(originalColor, pixelatedColor, mask);
            }
            ENDCG
        }
    }
}
