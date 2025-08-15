using UnityEngine;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Effects/Censor Effect")]
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

        // Materials used for the effect
        [HideInInspector] public Material CensorMaskMaterial;
        [HideInInspector] public Material CensorEffectMaterial;

        // Shader property IDs
        public static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        public static readonly int CensorAreaExpansionID = Shader.PropertyToID("_CensorAreaExpansion");
        public static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");
        public static readonly int CensorMaskID = Shader.PropertyToID("_CensorMask");
        public static readonly int ZTestID = Shader.PropertyToID("_ZTest");

        private void OnEnable()
        {
            InitializeMaterials();
        }

        private void OnDisable()
        {
            CleanupMaterials();
        }

        private void OnValidate()
        {
            CensorAreaExpansion = Mathf.Max(0, CensorAreaExpansion);
            InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            if (CensorMaskMaterial == null)
            {
                var shader = Shader.Find("Hidden/CensorMask");
                if (shader != null)
                {
                    CensorMaskMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }

            if (CensorEffectMaterial == null)
            {
                var shader = Shader.Find("Hidden/CensorEffect");
                if (shader != null)
                {
                    CensorEffectMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
            }
        }

        private void CleanupMaterials()
        {
            if (CensorMaskMaterial != null)
            {
                #if UNITY_EDITOR
                DestroyImmediate(CensorMaskMaterial);
                #else
                Destroy(CensorMaskMaterial);
                #endif
                CensorMaskMaterial = null;
            }

            if (CensorEffectMaterial != null)
            {
                #if UNITY_EDITOR
                DestroyImmediate(CensorEffectMaterial);
                #else
                Destroy(CensorEffectMaterial);
                #endif
                CensorEffectMaterial = null;
            }
        }

        public void UpdateMaterialProperties(Camera camera)
        {
            if (CensorEffectMaterial == null || camera == null) return;

            CensorEffectMaterial.SetFloat(PixelSizeID, PixelBlockCount);
            CensorEffectMaterial.SetFloat(CensorAreaExpansionID, CensorAreaExpansion); // This will be used by the blur shader
            CensorEffectMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1f : 0f);

            if (CensorMaskMaterial != null)
            {
                CensorMaskMaterial.SetInt(ZTestID, (int)(EnableOcclusion ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always));
            }
        }

        private void Awake()
        {
            // This effect only supports the Built-in Render Pipeline.
            // Ensure the CensorEffectBuiltin component is always present.
            if (GetComponent<CensorEffectBuiltin>() == null)
            {
                gameObject.AddComponent<CensorEffectBuiltin>();
            }
        }

        /// <summary>
        /// Nested class to handle the Built-in Render Pipeline implementation.
        /// This is added automatically by the main CensorEffect script.
        /// </summary>
        [DisallowMultipleComponent]
        private class CensorEffectBuiltin : MonoBehaviour
        {
            private CensorEffect _censorEffect;
            private Camera _mainCamera;
            private Camera _censorCamera;
            private Material _blurMaterial;

            private void OnEnable()
            {
                _censorEffect = GetComponent<CensorEffect>();
                _mainCamera = GetComponent<Camera>();

                if (_blurMaterial == null)
                {
                    var shader = Shader.Find("Hidden/CensorBlur");
                    if (shader != null)
                    {
                        _blurMaterial = new Material(shader);
                    }
                }
            }

            private void OnDisable()
            {
                if (_censorCamera != null)
                {
                    #if UNITY_EDITOR
                    DestroyImmediate(_censorCamera.gameObject);
                    #else
                    Destroy(_censorCamera.gameObject);
                    #endif
                    _censorCamera = null;
                }

                if (_blurMaterial != null)
                {
                    #if UNITY_EDITOR
                    DestroyImmediate(_blurMaterial);
                    #else
                    Destroy(_blurMaterial);
                    #endif
                    _blurMaterial = null;
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

            void OnRenderImage(RenderTexture source, RenderTexture destination)
            {
                if (_censorEffect == null || _censorEffect.CensorEffectMaterial == null || _censorEffect.CensorMaskMaterial == null)
                {
                    Graphics.Blit(source, destination);
                    return;
                }

                _censorEffect.UpdateMaterialProperties(_mainCamera);
                var censorMaskTexture = RenderTexture.GetTemporary(source.width, source.height, 24, UnityEngine.RenderTextureFormat.R8);

                var censorCam = GetCensorCamera();
                censorCam.CopyFrom(_mainCamera);
                censorCam.cullingMask = _censorEffect.CensorLayer;
                censorCam.targetTexture = censorMaskTexture;
                censorCam.clearFlags = CameraClearFlags.SolidColor;
                censorCam.backgroundColor = Color.clear;
                censorCam.RenderWithShader(_censorEffect.CensorMaskMaterial.shader, "RenderType");

                if (_censorEffect.CensorAreaExpansion > 0 && _blurMaterial != null)
                {
                    _blurMaterial.SetFloat("_BlurSize", _censorEffect.CensorAreaExpansion);
                    var tempBlurTex = RenderTexture.GetTemporary(source.width, source.height, 24, UnityEngine.RenderTextureFormat.R8);

                    Graphics.Blit(censorMaskTexture, tempBlurTex, _blurMaterial, 0);
                    Graphics.Blit(tempBlurTex, censorMaskTexture, _blurMaterial, 1);

                    RenderTexture.ReleaseTemporary(tempBlurTex);
                }

                _censorEffect.CensorEffectMaterial.SetTexture(CensorMaskID, censorMaskTexture);
                Graphics.Blit(source, destination, _censorEffect.CensorEffectMaterial);
                RenderTexture.ReleaseTemporary(censorMaskTexture);
            }
        }
    }
}