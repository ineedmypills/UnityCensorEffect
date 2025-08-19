Shader "Hidden/CensorEffect/Censor"
{
    Properties
    {
        // _MainTex is provided by the Blit command.
        _MainTex ("Texture", 2D) = "white" {}

        // _PixelSize and _HardEdges are set by the CensorEffectRenderer script.
        _PixelSize ("Pixel Size", Float) = 50.0
        _HardEdges ("Hard Edges", Float) = 0.0
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

            sampler2D _MainTex;

            // This texture is now provided globally by the CensorMaskGenerator camera.
            // We just need to declare it here to be able to sample it.
            sampler2D _GlobalCensorMask;

            float _PixelSize;
            float _HardEdges;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 originalColor = tex2D(_MainTex, i.uv);
                float2 pixelatedUV = floor(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;

                if (_HardEdges > 0.5)
                {
                    // Sample the global mask texture at the pixelated UV for a hard-edged blocky effect.
                    float pixelatedMask = tex2D(_GlobalCensorMask, pixelatedUV).r;
                    if (pixelatedMask > 0.1)
                    {
                        return tex2D(_MainTex, pixelatedUV);
                    }
                }
                else
                {
                    // Sample the global mask texture for a soft-edged effect.
                    float mask = tex2D(_GlobalCensorMask, i.uv).r;
                    if (mask > 0.1)
                    {
                        fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);
                        return lerp(originalColor, pixelatedColor, mask);
                    }
                }

                return originalColor;
            }
            ENDCG
        }
    }
}
