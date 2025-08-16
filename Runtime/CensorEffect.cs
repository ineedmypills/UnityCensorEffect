using UnityEngine;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Effects/Censor Effect")]
    [RequireComponent(typeof(Camera))]
    public class CensorEffect : MonoBehaviour
    {
        #region Public Settings
        [Header("Censor Settings")]
        [Tooltip("The layer containing objects to be pixelated.")]
        public LayerMask CensorLayer = 0;

        [Tooltip("When enabled, censored objects will be hidden by other objects in front of them.")]
        public bool EnableOcclusion = true;

        [Header("Effect Settings")]
        [Tooltip("The number of pixel blocks across the screen's height. Smaller numbers mean larger pixels.")]
        [Range(16, 512)] public float PixelBlockCount = 64;

        [Tooltip("Expands the censored area in pixels to better cover objects.")]
        [Range(0, 50)] public int CensorAreaExpansion = 0;

        [Tooltip("Use soft, anti-aliased edges for the censor zone. Disabling creates a sharp, pixel-perfect border.")]
        public bool EnableAntiAliasing = true;
        #endregion

        #region Private Fields
        // Shaders
        private Shader _censorMaskShader;
        private Shader _censorEffectShader;
        private Shader _dilationShader;

        // Materials
        private Material _censorMaskMaterial;
        private Material _censorEffectMaterial;
        private Material _dilationMaterial;

        // Censor Camera
        private Camera _censorCamera;
        private GameObject _censorCameraObject;

        // Main Camera
        private Camera _mainCamera;
        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            _mainCamera = GetComponent<Camera>();

            CreateResources();
        }

        private void OnDisable()
        {
            CleanupResources();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!AreResourcesCreated() || CensorLayer == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // --- Step 1: Create Censor Mask ---
            // The mask texture needs a depth buffer for occlusion testing to work correctly.
            var maskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 24);
            RenderTexture censorMask = RenderTexture.GetTemporary(maskDescriptor);
            RenderCensorMask(censorMask);

            // --- Step 2: Expand Mask (Optional) ---
            RenderTexture expandedMask;
            if (CensorAreaExpansion > 0)
            {
                expandedMask = RenderTexture.GetTemporary(maskDescriptor);
                ApplyDilation(censorMask, expandedMask);
                RenderTexture.ReleaseTemporary(censorMask); // No longer need the original mask
            }
            else
            {
                expandedMask = censorMask; // Use the original mask directly
            }

            // --- Step 3: Composite Final Image ---
            _censorEffectMaterial.SetFloat("_PixelBlockCount", PixelBlockCount);
            _censorEffectMaterial.SetFloat("_EnableAntiAliasing", EnableAntiAliasing ? 1.0f : 0.0f);
            _censorEffectMaterial.SetTexture("_CensorMaskTex", expandedMask);
            Graphics.Blit(source, destination, _censorEffectMaterial);

            // --- Cleanup ---
            RenderTexture.ReleaseTemporary(expandedMask);
        }
        #endregion

        #region Core Logic
        private void RenderCensorMask(RenderTexture destination)
        {
            // Setup the temporary camera used to render the mask
            SetupCensorCamera();

            _censorCamera.cullingMask = CensorLayer;
            _censorCamera.targetTexture = destination;

            // Configure occlusion
            if (EnableOcclusion)
            {
                _mainCamera.depthTextureMode |= DepthTextureMode.Depth;
                _censorMaskMaterial.EnableKeyword("OCCLUSION_ON");
            }
            else
            {
                // We don't disable depth texture mode here because other effects might be using it.
                // This prevents conflicts and log spam if another system (e.g., motion vectors) needs depth.
                _censorMaskMaterial.DisableKeyword("OCCLUSION_ON");
            }

            // Render objects on the CensorLayer into the destination texture using our mask shader
            _censorCamera.RenderWithShader(_censorMaskShader, "RenderType");
        }

        private void ApplyDilation(RenderTexture source, RenderTexture destination)
        {
            _dilationMaterial.SetFloat("_DilationSize", CensorAreaExpansion);

            // We need a third texture for this operation
            RenderTexture temp = RenderTexture.GetTemporary(source.descriptor);

            // Horizontal Pass
            Graphics.Blit(source, temp, _dilationMaterial, 0);
            // Vertical Pass
            Graphics.Blit(temp, destination, _dilationMaterial, 1);

            RenderTexture.ReleaseTemporary(temp);
        }

        private void SetupCensorCamera()
        {
            if (_censorCameraObject == null)
            {
                _censorCameraObject = new GameObject("CensorEffect Camera")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _censorCamera = _censorCameraObject.AddComponent<Camera>();
                _censorCamera.enabled = false; // The camera is manually rendered, not enabled.
            }

            // Match the censor camera's transform and settings to the main camera.
            // This is the critical fix for occlusion, ensuring the censor camera
            // renders from the same viewpoint as the main camera.
            _censorCamera.transform.position = _mainCamera.transform.position;
            _censorCamera.transform.rotation = _mainCamera.transform.rotation;

            _censorCamera.CopyFrom(_mainCamera);

            // Override settings for mask rendering
            _censorCamera.clearFlags = CameraClearFlags.SolidColor;
            _censorCamera.backgroundColor = Color.clear;
            _censorCamera.renderingPath = RenderingPath.Forward;
            _censorCamera.allowMSAA = false;
            _censorCamera.allowHDR = false;
        }

        #endregion

        #region Resource Management
        private void CreateResources()
        {
            _censorMaskShader = Shader.Find("Hidden/CensorMask");
            _censorEffectShader = Shader.Find("Hidden/CensorEffect");
            _dilationShader = Shader.Find("Hidden/Dilation");

            _censorMaskMaterial = CreateMaterial(_censorMaskShader);
            _censorEffectMaterial = CreateMaterial(_censorEffectShader);
            _dilationMaterial = CreateMaterial(_dilationShader);
        }

        private void CleanupResources()
        {
            if (_censorCameraObject != null)
            {
                DestroyImmediate(_censorCameraObject);
            }

            DestroyImmediate(_censorMaskMaterial);
            DestroyImmediate(_censorEffectMaterial);
            DestroyImmediate(_dilationMaterial);
        }

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError($"Shader not found. Make sure the shader files are in a Resources folder.");
                return null;
            }
            if (!shader.isSupported)
            {
                Debug.LogError($"Shader {shader.name} is not supported on this platform.");
                return null;
            }
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private bool AreResourcesCreated()
        {
            return _censorMaskMaterial != null && _censorEffectMaterial != null && _dilationMaterial != null;
        }
        #endregion
    }
}
