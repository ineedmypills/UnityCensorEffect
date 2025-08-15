using UnityEngine;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Effects/Censor Effect")]
    [RequireComponent(typeof(Camera))]
    public class CensorEffect : MonoBehaviour
    {
        [Header("Censor Settings")]
        [Tooltip("Layers to be censored.")]
        public LayerMask CensorLayer = 0;

        [Tooltip("Use the depth buffer to hide censored objects behind other objects.")]
        public bool EnableOcclusion = true;

        [Tooltip("The number of pixel blocks to draw across the screen's height. Smaller numbers mean larger blocks.")]
        [Range(1, 512)]
        public float PixelBlockCount = 100f;

        [Tooltip("Expand the censored area in world units.")]
        [Min(0)]
        public float CensorAreaExpansion = 0.1f;

        [Header("Appearance")]
        [Tooltip("Enable smooth edges on censored areas.")]
        public bool EnableAntiAliasing = true;

        // Materials
        private Material _censorMaskMaterial;
        private Material _censorEffectMaterial;
        private Material _blurMaterial;

        // Cameras
        private Camera _mainCamera;
        private Camera _censorCamera;

        // Shader property IDs
        public static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        public static readonly int CensorAreaExpansionID = Shader.PropertyToID("_CensorAreaExpansion");
        public static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");
        public static readonly int CensorMaskID = Shader.PropertyToID("_CensorMask");
        public static readonly int ZTestID = Shader.PropertyToID("_ZTest");

        private void OnEnable()
        {
            _mainCamera = GetComponent<Camera>();
            InitializeMaterials();
        }

        private void OnDisable()
        {
            CleanupMaterials();
            CleanupCensorCamera();
        }

        private void OnValidate()
        {
            CensorAreaExpansion = Mathf.Max(0, CensorAreaExpansion);
            InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            if (_censorMaskMaterial == null)
            {
                var shader = Shader.Find("Hidden/CensorMask");
                if (shader != null)
                {
                    _censorMaskMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }

            if (_censorEffectMaterial == null)
            {
                var shader = Shader.Find("Hidden/CensorEffect");
                if (shader != null)
                {
                    _censorEffectMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }

            if (_blurMaterial == null)
            {
                var shader = Shader.Find("Hidden/CensorBlur");
                if (shader != null)
                {
                    _blurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }
        }

        private void CleanupMaterials()
        {
            if (_censorMaskMaterial != null)
            {
                DestroyImmediate(_censorMaskMaterial);
                _censorMaskMaterial = null;
            }

            if (_censorEffectMaterial != null)
            {
                DestroyImmediate(_censorEffectMaterial);
                _censorEffectMaterial = null;
            }

            if (_blurMaterial != null)
            {
                DestroyImmediate(_blurMaterial);
                _blurMaterial = null;
            }
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

        private void UpdateMaterialProperties()
        {
            if (_censorEffectMaterial == null || _mainCamera == null) return;

            _censorEffectMaterial.SetFloat(PixelSizeID, PixelBlockCount);
            _censorEffectMaterial.SetFloat(CensorAreaExpansionID, CensorAreaExpansion);
            _censorEffectMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1f : 0f);

            if (_censorMaskMaterial != null)
            {
                _censorMaskMaterial.SetInt(ZTestID, (int)(EnableOcclusion ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always));
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_censorEffectMaterial == null || _censorMaskMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            UpdateMaterialProperties();

            var censorMaskTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.R8);

            var censorCam = GetCensorCamera();
            censorCam.CopyFrom(_mainCamera);
            censorCam.cullingMask = CensorLayer;
            censorCam.targetTexture = censorMaskTexture;
            censorCam.clearFlags = CameraClearFlags.SolidColor;
            censorCam.backgroundColor = Color.clear;
            censorCam.RenderWithShader(_censorMaskMaterial.shader, "RenderType");

            if (CensorAreaExpansion > 0 && _blurMaterial != null)
            {
                _blurMaterial.SetFloat("_BlurSize", CensorAreaExpansion);
                var tempBlurTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.R8);

                Graphics.Blit(censorMaskTexture, tempBlurTex, _blurMaterial, 0);
                Graphics.Blit(tempBlurTex, censorMaskTexture, _blurMaterial, 1);

                RenderTexture.ReleaseTemporary(tempBlurTex);
            }

            _censorEffectMaterial.SetTexture(CensorMaskID, censorMaskTexture);
            Graphics.Blit(source, destination, _censorEffectMaterial);
            RenderTexture.ReleaseTemporary(censorMaskTexture);
        }
    }
}
