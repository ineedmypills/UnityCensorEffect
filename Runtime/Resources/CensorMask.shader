Shader "Hidden/CensorMask"
{
    Properties
    {
        // No properties needed now
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend One Zero
            ColorMask R
            ZWrite On // Keep ZWrite On for censored objects to occlude each other
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ _OCCLUSION_ON

            #include "UnityCG.cginc"

            // Declare the depth texture
            sampler2D _CameraDepthTexture;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0; // For depth texture sampling
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Compute screen coordinates for depth texture sampling
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #if _OCCLUSION_ON
                    // Sample the main depth texture
                    float sceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                    // Convert to linear depth from eye
                    float sceneLinearEyeDepth = LinearEyeDepth(sceneDepth);
                    // Current fragment's linear depth from eye
                    float myLinearEyeDepth = i.screenPos.w;

                    // Compare depths and discard if occluded
                    // Add a small bias to prevent z-fighting on surfaces
                    clip(sceneLinearEyeDepth - myLinearEyeDepth - 0.001);
                #endif

                // Output solid red for the mask
                return fixed4(1,0,0,0);
            }
            ENDCG
        }
    }
}
