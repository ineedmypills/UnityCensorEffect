// This shader renders objects on the CensorLayer as a solid color to create a mask.
// It performs a depth test against a manually provided depth texture (_SceneDepthTexture)
// to correctly occlude censored objects behind other scene geometry.
Shader "Hidden/CensorMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ColorMask R
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ _OCCLUSION_ON

            #include "UnityCG.cginc"

            // The main camera's depth texture, provided manually from the C# script.
            sampler2D _SceneDepthTexture;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #if _OCCLUSION_ON
                    // Perform the depth test for occlusion.
                    float sceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_SceneDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                    float sceneLinearEyeDepth = LinearEyeDepth(sceneDepth);
                    // A small bias is subtracted to prevent z-fighting artifacts.
                    clip(sceneLinearEyeDepth - i.screenPos.w - 0.001);
                #endif

                // If not clipped, output solid white into the Red channel.
                return fixed4(1,0,0,0);
            }
            ENDCG
        }
    }
}
