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
                // Get original screen color
                fixed4 originalColor = tex2D(_MainTex, i.uv);

                // Calculate UV for pixelated color
                float2 pixelGrid = float2(_PixelSize * _ScreenParams.x / _ScreenParams.y, _PixelSize);
                float2 pixelatedUV = round(i.uv * pixelGrid) / pixelGrid;
                fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);

                // Sample the pre-processed (resolved and dilated) mask
                fixed mask = tex2D(_CensorMask, i.uv).r;

                // Apply anti-aliasing if enabled
                if (_AntiAliasing > 0.5)
                {
                    // Use smoothstep for soft edges. The 0.01 lower bound prevents
                    // feathering from extending too far into the non-censored area.
                    mask = smoothstep(0.01, 1.0, mask);
                }
                else
                {
                    // Use ceil for a sharp, blocky edge that perfectly matches the mask.
                    mask = ceil(mask);
                }

                // Blend between original and pixelated color based on the final mask
                return lerp(originalColor, pixelatedColor, mask);
            }
            ENDCG
        }
    }
    Fallback Off
}