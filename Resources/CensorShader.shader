Shader "Hidden/CensorEffect/Censor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CensorMaskTex ("Censor Mask", 2D) = "black" {}
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
            sampler2D _CensorMaskTex;
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
                float mask = tex2D(_CensorMaskTex, i.uv).r;

                if (mask > 0.1)
                {
                    float2 pixelatedUV = floor(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                    fixed4 pixelatedColor = tex2D(_MainTex, pixelatedUV);

                    if (_HardEdges > 0.5)
                    {
                        return pixelatedColor;
                    }
                    else
                    {
                        return lerp(originalColor, pixelatedColor, mask);
                    }
                }

                return originalColor;
            }
            ENDCG
        }
    }
}
