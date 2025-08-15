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

        [Tooltip("How much to expand the censored area, in pixels.")]
        [Range(0, 50)]
        public int CensorAreaExpansionPixels = 5;

        [Header("Appearance")]
        [Tooltip("Enable smooth edges on censored areas.")]
        public bool EnableAntiAliasing = true;

        #endregion

        #region Private Fields

        [Header("Shader References")]
        [SerializeField] private Shader _censorMaskShader;
        [SerializeField] private Shader _censorEffectShader;
        [SerializeField] private Shader _dilationShader;

        private Material _censorMaskMaterial;
        private Material _censorEffectMaterial;
        private Material _dilationMaterial;

        private Camera _mainCamera;
        private Camera _censorCamera;

        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        private static readonly int CensorMaskID = Shader.PropertyToID("_CensorMask");
        private static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");
        private static readonly int DilationSizeID = Shader.PropertyToID("_DilationSize");

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            _mainCamera = GetComponent<Camera>();
            _mainCamera.depthTextureMode |= DepthTextureMode.Depth;

            // Initialize resources
            CleanupResources(); // Ensure a clean state
            CreateResources();
        }

        private void OnDisable()
        {
            CleanupResources();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!AreResourcesCreated())
            {
                Graphics.Blit(source, destination);
                return;
            }

            UpdateMaterialProperties();

            var maskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 0)
            {
                msaaSamples = EnableAntiAliasing ? GetMsaaSampleCount(source) : 1
            };
            var censorMask = RenderTexture.GetTemporary(maskDescriptor);

            RenderCensorMask(censorMask);

            RenderTexture processedMask;
            if (CensorAreaExpansionPixels > 0)
            {
                var dilatedMask = RenderTexture.GetTemporary(maskDescriptor);
                ApplyDilation(censorMask, dilatedMask);
                RenderTexture.ReleaseTemporary(censorMask);
                processedMask = dilatedMask;
            }
            else
            {
                processedMask = censorMask;
            }

            _censorEffectMaterial.SetTexture(CensorMaskID, processedMask);
            Graphics.Blit(source, destination, _censorEffectMaterial);

            RenderTexture.ReleaseTemporary(processedMask);
        }

        #endregion

        #region Core Logic

        private void RenderCensorMask(RenderTexture destination)
        {
            if (_censorCamera == null)
            {
                _censorCamera = CreateCensorCamera();
            }

            UpdateCensorCamera(_mainCamera, _censorCamera);
            _censorCamera.targetTexture = destination;
            _censorCamera.RenderWithShader(_censorMaskShader, "RenderType");
        }

        private void ApplyDilation(RenderTexture source, RenderTexture destination)
        {
            var tempDilateTex = RenderTexture.GetTemporary(source.descriptor);
            _dilationMaterial.SetInt(DilationSizeID, CensorAreaExpansionPixels);

            Graphics.Blit(source, tempDilateTex, _dilationMaterial, 0); // Horizontal
            Graphics.Blit(tempDilateTex, destination, _dilationMaterial, 1); // Vertical

            RenderTexture.ReleaseTemporary(tempDilateTex);
        }

        private void UpdateMaterialProperties()
        {
            _censorEffectMaterial.SetFloat(PixelSizeID, PixelBlockCount);
            _censorEffectMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1f : 0f);

            if (EnableOcclusion)
            {
                _censorMaskMaterial.EnableKeyword("_OCCLUSION_ON");
            }
            else
            {
                _censorMaskMaterial.DisableKeyword("_OCCLUSION_ON");
            }
        }

        #endregion

        #region Resource Management

        private void CreateResources()
        {
            _censorMaskMaterial = CreateMaterial(_censorMaskShader);
            _censorEffectMaterial = CreateMaterial(_censorEffectShader);
            _dilationMaterial = CreateMaterial(_dilationShader);
        }

        private bool AreResourcesCreated()
        {
            return _censorEffectMaterial != null && _censorMaskMaterial != null && _dilationMaterial != null;
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

        private Camera CreateCensorCamera()
        {
            var go = new GameObject("Censor Mask Camera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = go.GetComponent<Camera>();
            camera.enabled = false;
            camera.allowMSAA = true;
            return camera;
        }

        private void UpdateCensorCamera(Camera source, Camera target)
        {
            if (source == null || target == null) return;

            // Manually copy essential properties instead of using Camera.CopyFrom()
            target.transform.position = source.transform.position;
            target.transform.rotation = source.transform.rotation;
            target.fieldOfView = source.fieldOfView;
            target.nearClipPlane = source.nearClipPlane;
            target.farClipPlane = source.farClipPlane;
            target.orthographic = source.orthographic;
            target.orthographicSize = source.orthographicSize;
            target.aspect = source.aspect;

            target.depthTextureMode |= DepthTextureMode.Depth;
            target.cullingMask = CensorLayer;
            target.clearFlags = CameraClearFlags.SolidColor;
            target.backgroundColor = Color.clear;
            target.useOcclusionCulling = false;
        }

        private int GetMsaaSampleCount(RenderTexture source)
        {
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
