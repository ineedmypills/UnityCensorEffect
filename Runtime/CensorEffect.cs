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

        [Tooltip("How much to expand the censored area. This controls the blur radius.")]
        [Min(0)]
        public float CensorAreaExpansion = 0.1f;

        [Header("Appearance")]
        [Tooltip("Enable smooth edges on censored areas.")]
        public bool EnableAntiAliasing = true;

        #endregion

        #region Private Fields

        // Shaders - Loaded from Resources to avoid brittle Shader.Find
        private Shader _censorMaskShader;
        private Shader _censorEffectShader;
        private Shader _blurShader;

        // Materials (Lazy-loaded)
        private Material _censorMaskMaterial;
        private Material _censorEffectMaterial;
        private Material _blurMaterial;

        // Cameras
        private Camera _mainCamera;
        private Camera _censorCamera;

        // Shader Property IDs
        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        private static readonly int CensorMaskID = Shader.PropertyToID("_CensorMask");
        private static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");
        private static readonly int BlurSizeID = Shader.PropertyToID("_BlurSize");

        #endregion

        #region Material Properties (Lazy-Loading)

        private Material CensorMaskMaterial => _censorMaskMaterial != null ? _censorMaskMaterial : (_censorMaskMaterial = CreateMaterial(_censorMaskShader));
        private Material CensorEffectMaterial => _censorEffectMaterial != null ? _censorEffectMaterial : (_censorEffectMaterial = CreateMaterial(_censorEffectShader));
        private Material BlurMaterial => _blurMaterial != null ? _blurMaterial : (_blurMaterial = CreateMaterial(_blurShader));

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            _mainCamera = GetComponent<Camera>();
            _mainCamera.depthTextureMode |= DepthTextureMode.Depth;
            LoadShaders();
        }

        private void OnDisable()
        {
            if (_mainCamera != null)
            {
                _mainCamera.depthTextureMode &= ~DepthTextureMode.Depth;
            }
            CleanupMaterials();
            CleanupCensorCamera();
        }

        private void OnValidate()
        {
            // Ensure expansion is non-negative
            CensorAreaExpansion = Mathf.Max(0, CensorAreaExpansion);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CensorEffectMaterial == null || CensorMaskMaterial == null || BlurMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            UpdateMaterialProperties();

            // 1. Create the initial mask texture (potentially with MSAA)
            var msaaMaskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 16);
            msaaMaskDescriptor.msaaSamples = EnableAntiAliasing ? source.descriptor.msaaSamples : 1;
            var censorMaskMsaaTexture = RenderTexture.GetTemporary(msaaMaskDescriptor);

            // 2. Render the base mask
            RenderCensorMask(censorMaskMsaaTexture);

            // 3. Create a resolved texture for blurring and final use
            var resolvedMaskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 0);
            var resolvedMaskTexture = RenderTexture.GetTemporary(resolvedMaskDescriptor);

            // 4. Blit to resolve MSAA
            Graphics.Blit(censorMaskMsaaTexture, resolvedMaskTexture);
            RenderTexture.ReleaseTemporary(censorMaskMsaaTexture);

            // 5. Apply blur if needed
            if (CensorAreaExpansion > 0)
            {
                ApplyBlur(resolvedMaskTexture);
            }

            // 6. Use the final mask in the effect shader
            CensorEffectMaterial.SetTexture(CensorMaskID, resolvedMaskTexture);
            Graphics.Blit(source, destination, CensorEffectMaterial);

            // 7. Cleanup
            RenderTexture.ReleaseTemporary(resolvedMaskTexture);
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

        private void ApplyBlur(RenderTexture texture)
        {
            // Get a temporary texture for the blur passes that matches the source
            var tempBlurTex = RenderTexture.GetTemporary(texture.descriptor);

            BlurMaterial.SetFloat(BlurSizeID, CensorAreaExpansion);

            // Perform blur passes
            Graphics.Blit(texture, tempBlurTex, BlurMaterial, 0); // Horizontal
            Graphics.Blit(tempBlurTex, texture, BlurMaterial, 1); // Vertical

            RenderTexture.ReleaseTemporary(tempBlurTex);
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
            _blurShader = Shader.Find("Hidden/CensorBlur");
        }

        private void CleanupMaterials()
        {
            if (_censorMaskMaterial != null) DestroyImmediate(_censorMaskMaterial);
            if (_censorEffectMaterial != null) DestroyImmediate(_censorEffectMaterial);
            if (_blurMaterial != null) DestroyImmediate(_blurMaterial);

            _censorMaskMaterial = null;
            _censorEffectMaterial = null;
            _blurMaterial = null;
        }

        private void CleanupCensorCamera()
        {
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

            // Copy all settings from the source camera. This is more robust than
            // manually copying properties, as it includes settings like cullingMatrix.
            target.CopyFrom(source);

            // Override specific settings for the censor mask rendering
            target.cullingMask = CensorLayer;
            target.clearFlags = CameraClearFlags.SolidColor;
            target.backgroundColor = Color.clear;
            target.useOcclusionCulling = false; // Occlusion is handled by the shader
        }

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null || !shader.isSupported) return null;
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        #endregion
    }
}
