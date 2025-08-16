Shader "Hidden/CensorMask"
{
    SubShader
    {
        // This pass renders the object as a solid white color.
        // It supports depth testing to allow other objects to occlude the censored object.
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ OCCLUSION_ON

            #include "UnityCG.cginc"

            // The input vertex data
            struct appdata
            {
                float4 vertex : POSITION;
            };

            // The data passed from the vertex to the fragment shader
            struct v2f
            {
                float4 vertex : SV_POSITION;
                #if OCCLUSION_ON
                float4 screenPos : TEXCOORD0;
                #endif
            };

            #if OCCLUSION_ON
            // Our manually-copied depth texture.
            sampler2D_float _ManualDepthTexture;
            #endif

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                #if OCCLUSION_ON
                // Calculate screen position to sample the depth texture
                o.screenPos = ComputeScreenPos(o.vertex);
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                #if OCCLUSION_ON
                // Read the depth from the scene's depth buffer
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_ManualDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                // The current pixel's depth
                float pixelZ = i.screenPos.w;

                // If the scene depth is closer than the pixel's depth, it means this pixel is occluded.
                // We discard it so it doesn't appear in the mask.
                if (sceneZ < pixelZ)
                {
                    discard;
                }
                #endif

                // If not occluded (or if occlusion is off), draw a white pixel.
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
    // Fallback for older hardware
    FallBack "Transparent/VertexLit"
}
