Shader "Hidden/CensorEffect/WhiteMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float sceneZ = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
                float currentZ = i.screenPos.z / i.screenPos.w;

                // Discard this fragment if it's behind what's already in the depth buffer
                // (Unity uses a reversed-Z buffer on modern APIs, so LESS means FURTHER)
                if (currentZ < sceneZ - 0.0001) {
                    discard;
                }

                // Otherwise, it's visible, so draw the white mask
                return fixed4(1.0, 1.0, 1.0, 1.0);
            }
            ENDCG
        }
    }
}
