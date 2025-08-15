Shader "Hidden/CensorEffect"
{
    Properties
    {
        [HideInInspector] _MainTex ("Screen", 2D) = "white" {}
        [HideInInspector] _CensorMask ("Censor Mask", 2D) = "black" {}
        [HideInInspector] _PixelSize ("Pixel Size", Float) = 10.0
        [HideInInspector] _AntiAliasing ("Anti-Aliasing", Float) = 1.0
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
            sampler2D _CensorMask;
            float _PixelSize;
            float _AntiAliasing;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Get original color
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // Get pixelated color
                float2 pixelGrid = float2(_PixelSize * _ScreenParams.x / _ScreenParams.y, _PixelSize);
                float2 pixelatedUV = round(i.uv * pixelGrid) / pixelGrid;
                fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);

                // Determine mask value based on AntiAliasing setting
                fixed mask;
                if (_AntiAliasing > 0.5)
                {
                    // Smooth mask sampling for soft edges
                    mask = tex2D(_CensorMask, i.uv).r;
                    mask = smoothstep(0.0, 1.0, mask);
                }
                else
                {
                    // 4-corner sampling for a sharp, expanded blocky edge
                    float2 pixelSize = 1.0 / pixelGrid;
                    float2 uv00 = pixelatedUV - pixelSize * 0.5;
                    float2 uv11 = pixelatedUV + pixelSize * 0.5;
                    float s0 = tex2D(_CensorMask, uv00).r;
                    float s1 = tex2D(_CensorMask, float2(uv11.x, uv00.y)).r;
                    float s2 = tex2D(_CensorMask, float2(uv00.x, uv11.y)).r;
                    float s3 = tex2D(_CensorMask, uv11).r;
                    mask = max(max(s0, s1), max(s2, s3)) > 0.5 ? 1.0 : 0.0;
                }

                // Final color calculation
                return lerp(originalColor, pixelatedColor, mask);
            }
            ENDCG
        }
    }
    Fallback Off
}