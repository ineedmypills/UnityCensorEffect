Shader "Hidden/CensorMask"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4 // LEqual
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="UniversalForward" }
        LOD 100

        Pass
        {
            Blend One Zero
            ColorMask R // Use Red channel, as we use R8 format
            ZTest [_ZTest]
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Output solid red
                return fixed4(1,0,0,0);
            }
            ENDCG
        }
    }
}
