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
                // Calculate pixelated coordinates
                float2 pixelGrid = _ScreenParams.xy / _PixelSize;
                float2 pixelatedUV = round(i.uv * pixelGrid) / pixelGrid;

                // Sample original color
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // Sample mask from the pixelated UV to ensure mask aligns with pixels
                fixed mask = tex2D(_CensorMask, pixelatedUV).r;

                if (mask > 0.01)
                {
                    fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);

                    // Apply anti-aliasing if enabled
                    if (_AntiAliasing > 0.5)
                    {
                        // Use the original (non-pixelated) mask sample for a smoother edge
                        fixed smoothMask = tex2D(_CensorMask, i.uv).r;
                        return lerp(originalColor, pixelatedColor, smoothstep(0.0, 1.0, smoothMask));
                    }
                    return pixelatedColor;
                }

                return originalColor;
            }
            ENDCG
        }
    }
    Fallback Off
}