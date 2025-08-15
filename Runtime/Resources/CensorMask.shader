// This shader renders objects on the CensorLayer as a solid color to create a mask.
// It optionally performs depth testing against the main camera's depth buffer
// to correctly occlude censored objects behind other scene geometry.
Shader "Hidden/CensorMask"
{
    Properties
    {
        // No properties needed for this shader.
    }
    SubShader
    {
        // Rendered with other opaque geometry.
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // --- Render States ---
            Blend One Zero      // No blending, just overwrite.
            ColorMask R         // Only write to the Red channel (for R8 texture).
            ZWrite On           // Write to depth buffer so censored objects can occlude each other.
            Cull Off            // Render both front and back faces to prevent holes from one-sided meshes.

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Compile two shader variants: one with occlusion on, one with it off.
            // The C# script will enable the appropriate keyword.
            #pragma multi_compile __ _OCCLUSION_ON

            #include "UnityCG.cginc"

            // The main camera's depth texture, provided manually from the C# script
            // to ensure it's available during the manual camera render.
            sampler2D _CensorDepthTexture;

            // Input mesh data (vertex position).
            struct appdata
            {
                float4 vertex : POSITION;
            };

            // Data passed from vertex to fragment shader.
            struct v2f
            {
                float4 vertex : SV_POSITION;
                // Screen-space position is needed to sample the depth texture correctly.
                float4 screenPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // ComputeScreenPos is a built-in Unity function that prepares coordinates for depth sampling.
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // This entire block is compiled out if _OCCLUSION_ON is not defined.
                #if _OCCLUSION_ON
                    // Sample the main camera's depth texture at the fragment's screen position.
                    float sceneDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CensorDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                    // The raw depth value is non-linear. Convert it to linear eye-space depth for a correct comparison.
                    float sceneLinearEyeDepth = LinearEyeDepth(sceneDepth);
                    // The current fragment's distance from the camera (w component of screenPos). Already linear.
                    float myLinearEyeDepth = i.screenPos.w;

                    // The core occlusion test:
                    // If the scene's depth is less than this fragment's depth, it means something
                    // is in front of this object. The `clip` function discards the fragment if the input is negative.
                    // A small bias (0.001) is subtracted to prevent "z-fighting" artifacts on co-planar surfaces.
                    clip(sceneLinearEyeDepth - myLinearEyeDepth - 0.001);
                #endif

                // If the fragment has not been clipped, output a solid value (1) into the
                // single Red channel of the R8 render target.
                return fixed4(1,0,0,0);
            }
            ENDCG
        }
    }
}
