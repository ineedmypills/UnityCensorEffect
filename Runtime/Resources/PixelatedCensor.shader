Shader "Hidden/PixelatedCensorEffect"
{
    Properties
    {
        [HideInInspector] _MainTex ("Screen", 2D) = "white" {}
        [HideInInspector] _CensorMask ("Censor Mask", 2D) = "black" {}
        [HideInInspector] _PixelSize ("Pixel Size", Float) = 10.0
        [HideInInspector] _CensorAreaExpansion ("Censor Area Expansion", Float) = 0.0
        [HideInInspector] _AntiAliasing ("Anti-Aliasing", Float) = 1
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
            float _CensorAreaExpansion;
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
                
                // Calculate expansion radius in pixels
                float expansionRadius = max(1, ceil(_CensorAreaExpansion));
                float maxMask = 0;
                
                // Sample mask with circular expansion
                for (float x = -expansionRadius; x <= expansionRadius; x += 1) 
                {
                    for (float y = -expansionRadius; y <= expansionRadius; y += 1) 
                    {
                        if (x*x + y*y <= expansionRadius*expansionRadius)
                        {
                            float2 offset = float2(x, y) / _ScreenParams.xy;
                            float mask = tex2D(_CensorMask, pixelatedUV + offset).a;
                            maxMask = max(maxMask, mask);
                        }
                    }
                }
                
                // Apply pixelation if needed
                if (maxMask > 0.01)
                {
                    fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);
                    
                    // Apply anti-aliasing if enabled
                    if (_AntiAliasing > 0.5)
                    {
                        float smoothMask = smoothstep(0.2, 0.8, maxMask);
                        return lerp(originalColor, pixelatedColor, smoothMask);
                    }
                    return lerp(originalColor, pixelatedColor, maxMask);
                }
                return originalColor;
            }
            ENDCG
        }
    }
    Fallback Off
}