using UnityEngine;
using UnityEngine.Rendering;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Effects/Censor Effect")]
    [RequireComponent(typeof(Camera))]
    public class CensorEffect : MonoBehaviour
    {
        #region Public Settings

        [Header("Censor Settings")]
        [Tooltip("Layers to be censored.")]
        public LayerMask CensorLayer = 0;

        [Tooltip("Use the depth buffer to hide censored objects behind other objects.")]
        public bool EnableOcclusion = true;

        [Tooltip("The number of pixel blocks to draw across the screen's height. Smaller numbers mean larger blocks.")]
        [Range(1, 512)]
        public float PixelBlockCount = 100f;

        [Tooltip("How much to expand the censored area. This controls the dilation radius.")]
        [Min(0)]
        public float CensorAreaExpansion = 0.1f;

        [Header("Appearance")]
        [Tooltip("Enable smooth edges on censored areas.")]
        public bool EnableAntiAliasing = true;

        #endregion

        #region Private Fields

        // Shaders
        private Shader _censorMaskShader;
        private Shader _censorEffectShader;
        private Shader _dilationShader;

        // Materials (Lazy-loaded)
        private Material _censorMaskMaterial;
        private Material _censorEffectMaterial;
        private Material _dilationMaterial;

        // Cameras
        private Camera _mainCamera;
        private Camera _censorCamera;

        // Shader Property IDs
        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        private static readonly int CensorMaskID = Shader.PropertyToID("_CensorMask");
        private static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");
        private static readonly int DilationSizeID = Shader.PropertyToID("_DilationSize");

        #endregion

        #region Material Properties (Lazy-Loading)

        private Material CensorMaskMaterial => _censorMaskMaterial != null ? _censorMaskMaterial : (_censorMaskMaterial = CreateMaterial(_censorMaskShader));
        private Material CensorEffectMaterial => _censorEffectMaterial != null ? _censorEffectMaterial : (_censorEffectMaterial = CreateMaterial(_censorEffectShader));
        private Material DilationMaterial => _dilationMaterial != null ? _dilationMaterial : (_dilationMaterial = CreateMaterial(_dilationShader));

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            _mainCamera = GetComponent<Camera>();
            // Ensure the main camera has the depth texture enabled for occlusion to work.
            _mainCamera.depthTextureMode |= DepthTextureMode.Depth;
            LoadShaders();
        }

        private void OnDisable()
        {
            // It's good practice to clean up the depth texture mode flag if this component added it.
            if (_mainCamera != null)
            {
                _mainCamera.depthTextureMode &= ~DepthTextureMode.Depth;
            }
            CleanupResources();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CensorEffectMaterial == null || CensorMaskMaterial == null || DilationMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            UpdateMaterialProperties();

            // --- Censor Mask Rendering ---
            // 1. Get a render texture for the censor mask. We use R8 format for efficiency.
            // Anti-aliasing is handled by using MSAA on this texture.
            var maskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 0);
            maskDescriptor.msaaSamples = EnableAntiAliasing ? GetMsaaSampleCount(source) : 1;
            var censorMask = RenderTexture.GetTemporary(maskDescriptor);

            // 2. Render the objects on the CensorLayer into the mask texture.
            RenderCensorMask(censorMask);

            // --- Mask Processing ---
            RenderTexture processedMask;
            if (CensorAreaExpansion > 0)
            {
                // 3. If expansion is enabled, we need a texture to hold the dilated result.
                // We create a new texture because the dilation is a multi-pass operation.
                var dilatedMask = RenderTexture.GetTemporary(maskDescriptor);
                ApplyDilation(censorMask, dilatedMask);

                // The original mask is no longer needed.
                RenderTexture.ReleaseTemporary(censorMask);
                processedMask = dilatedMask;
            }
            else
            {
                // If no expansion, we use the original mask directly.
                processedMask = censorMask;
            }

            // --- Final Compositing ---
            // 4. Apply the final pixelation effect, using the processed mask to blend.
            CensorEffectMaterial.SetTexture(CensorMaskID, processedMask);
            Graphics.Blit(source, destination, CensorEffectMaterial);

            // 5. Clean up the last temporary texture.
            RenderTexture.ReleaseTemporary(processedMask);
        }

        #endregion

        #region Core Logic

        private void RenderCensorMask(RenderTexture destination)
        {
            var censorCam = GetCensorCamera();
            UpdateCensorCamera(_mainCamera, censorCam);

            censorCam.targetTexture = destination;
            censorCam.RenderWithShader(CensorMaskMaterial.shader, "RenderType");
        }

        private void ApplyDilation(RenderTexture source, RenderTexture destination)
        {
            // A temporary texture is needed for the multi-pass dilation.
            var tempDilateTex = RenderTexture.GetTemporary(source.descriptor);

            DilationMaterial.SetFloat(DilationSizeID, CensorAreaExpansion);

            // Perform dilation passes
            Graphics.Blit(source, tempDilateTex, DilationMaterial, 0); // Horizontal
            Graphics.Blit(tempDilateTex, destination, DilationMaterial, 1); // Vertical

            RenderTexture.ReleaseTemporary(tempDilateTex);
        }

        private void UpdateMaterialProperties()
        {
            CensorEffectMaterial.SetFloat(PixelSizeID, PixelBlockCount);
            CensorEffectMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1f : 0f);

            if (EnableOcclusion)
            {
                CensorMaskMaterial.EnableKeyword("_OCCLUSION_ON");
            }
            else
            {
                CensorMaskMaterial.DisableKeyword("_OCCLUSION_ON");
            }
        }

        #endregion

        #region Resource Management

        private void LoadShaders()
        {
            _censorMaskShader = Shader.Find("Hidden/CensorMask");
            _censorEffectShader = Shader.Find("Hidden/CensorEffect");
            _dilationShader = Shader.Find("Hidden/CensorDilation");
        }

        private void CleanupResources()
        {
            if (_censorMaskMaterial != null) DestroyImmediate(_censorMaskMaterial);
            if (_censorEffectMaterial != null) DestroyImmediate(_censorEffectMaterial);
            if (_dilationMaterial != null) DestroyImmediate(_dilationMaterial);

            _censorMaskMaterial = null;
            _censorEffectMaterial = null;
            _dilationMaterial = null;

            if (_censorCamera != null)
            {
                DestroyImmediate(_censorCamera.gameObject);
                _censorCamera = null;
            }
        }

        private Camera GetCensorCamera()
        {
            if (_censorCamera == null)
            {
                var go = new GameObject("Censor Mask Camera", typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _censorCamera = go.GetComponent<Camera>();
                _censorCamera.enabled = false;
                _censorCamera.allowMSAA = true;
            }
            return _censorCamera;
        }

        private void UpdateCensorCamera(Camera source, Camera target)
        {
            if (source == null || target == null) return;

            target.CopyFrom(source);

            // BUG FIX: The censor camera needs the depth texture mode enabled for occlusion to work.
            // CopyFrom() does not copy this property.
            target.depthTextureMode |= DepthTextureMode.Depth;

            target.cullingMask = CensorLayer;
            target.clearFlags = CameraClearFlags.SolidColor;
            target.backgroundColor = Color.clear;
            target.useOcclusionCulling = false;
        }

        private int GetMsaaSampleCount(RenderTexture source)
        {
            // Use source MSAA level, but fallback to 1 if it's not a RenderTexture
            return source.antiAliasing > 1 ? source.antiAliasing : 1;
        }

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null || !shader.isSupported) return null;
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        #endregion
    }
}
