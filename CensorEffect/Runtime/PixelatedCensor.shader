Shader "Hidden/PixelatedCensorEffect"
{
    Properties
    {
        _MainTex ("Screen", 2D) = "white" {}
        _CensorMask ("Censor Mask", 2D) = "black" {}
        _PixelSize ("Pixel Size", Float) = 10.0
        _CensorAreaExpansion ("Censor Area Expansion", Float) = 0.0
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CensorMask;
            float _PixelSize;
            float _CensorAreaExpansion;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pixelatedUV = round(i.uv * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;
                
                float2 expansion = _CensorAreaExpansion / _ScreenParams.xy;
                float mask = 0;
                mask = max(mask, tex2D(_CensorMask, pixelatedUV).a);
                mask = max(mask, tex2D(_CensorMask, pixelatedUV + float2(expansion.x, 0)).a);
                mask = max(mask, tex2D(_CensorMask, pixelatedUV - float2(expansion.x, 0)).a);
                mask = max(mask, tex2D(_CensorMask, pixelatedUV + float2(0, expansion.y)).a);
                mask = max(mask, tex2D(_CensorMask, pixelatedUV - float2(0, expansion.y)).a);

                if (mask > 0)
                {
                    return tex2D(_MainTex, pixelatedUV);
                }
                else
                {
                    return tex2D(_MainTex, i.uv);
                }
            }
            ENDCG
        }
    }
}