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

            // This is now populated by a dedicated command buffer at a reliable pipeline stage
            sampler2D _CensorEffectDepthTexture;

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
                // Sample the raw depth from the main camera's depth texture.
                float rawSceneZ = tex2Dproj(_CensorEffectDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r;
                // Get the raw depth of the current fragment being rendered.
                float rawCurrentZ = i.screenPos.z / i.screenPos.w;

                // To ensure the comparison is accurate at all distances and not prone to
                // precision issues with non-linear depth buffers, we linearize both values.
                // Linear01Depth converts the raw depth value to a linear value between 0 (near) and 1 (far).
                float linearSceneZ = Linear01Depth(rawSceneZ);
                float linearCurrentZ = Linear01Depth(rawCurrentZ);

                // Now we can do a simple, direct comparison. If the current object's linear
                // depth is greater than the scene's, it must be behind it.
                // We add a small epsilon to prevent z-fighting on surfaces that are very close.
                if (linearCurrentZ > linearSceneZ + 0.00001) {
                    discard;
                }

                // Otherwise, it's visible, so draw the white mask.
                return fixed4(1.0, 1.0, 1.0, 1.0);
            }
            ENDCG
        }
    }
}
