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
        private static readonly int ZTestID = Shader.PropertyToID("_ZTest");
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
            LoadShaders();
        }

        private void OnDisable()
        {
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

            // The mask texture will be downsampled for the blur pass
            var maskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 0);
            var censorMaskTexture = RenderTexture.GetTemporary(maskDescriptor);

            RenderCensorMask(censorMaskTexture);
            ApplyBlur(censorMaskTexture);

            CensorEffectMaterial.SetTexture(CensorMaskID, censorMaskTexture);
            Graphics.Blit(source, destination, CensorEffectMaterial);

            RenderTexture.ReleaseTemporary(censorMaskTexture);
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
            if (CensorAreaExpansion <= 0) return;

            // Downsample for performance
            var blurDescriptor = new RenderTextureDescriptor(texture.width / 4, texture.height / 4, RenderTextureFormat.R8, 0);
            var tempBlurTex = RenderTexture.GetTemporary(blurDescriptor);

            BlurMaterial.SetFloat(BlurSizeID, CensorAreaExpansion);

            // Blit from full-res mask to downsampled temp texture
            Graphics.Blit(texture, tempBlurTex);

            // Perform blur passes
            var tempBlurTex2 = RenderTexture.GetTemporary(blurDescriptor);
            Graphics.Blit(tempBlurTex, tempBlurTex2, BlurMaterial, 0); // Horizontal
            Graphics.Blit(tempBlurTex2, tempBlurTex, BlurMaterial, 1); // Vertical

            // Blit from downsampled blurred texture back to the full-res mask
            Graphics.Blit(tempBlurTex, texture);

            RenderTexture.ReleaseTemporary(tempBlurTex);
            RenderTexture.ReleaseTemporary(tempBlurTex2);
        }

        private void UpdateMaterialProperties()
        {
            CensorEffectMaterial.SetFloat(PixelSizeID, PixelBlockCount);
            CensorEffectMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1f : 0f);
            CensorMaskMaterial.SetInt(ZTestID, (int)(EnableOcclusion ? CompareFunction.LessEqual : CompareFunction.Always));
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
            }
            return _censorCamera;
        }

        private void UpdateCensorCamera(Camera source, Camera target)
        {
            if (source == null || target == null) return;

            target.transform.position = source.transform.position;
            target.transform.rotation = source.transform.rotation;
            target.fieldOfView = source.fieldOfView;
            target.nearClipPlane = source.nearClipPlane;
            target.farClipPlane = source.farClipPlane;
            target.orthographic = source.orthographic;
            target.orthographicSize = source.orthographicSize;
            target.aspect = source.aspect;

            target.cullingMask = CensorLayer;
            target.clearFlags = CameraClearFlags.SolidColor;
            target.backgroundColor = Color.clear;
        }

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null || !shader.isSupported) return null;
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        #endregion
    }
}
