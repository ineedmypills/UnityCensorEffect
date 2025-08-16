using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Effects/Censor Effect")]
    [RequireComponent(typeof(Camera))]
    public class CensorEffect : MonoBehaviour
    {
        #region Public Settings
        [Header("Censor Settings")]
        public LayerMask CensorLayer = 0;
        public bool EnableOcclusion = true;

        [Header("Effect Settings")]
        [Range(1, 512)] public float PixelBlockCount = 100f;
        [Range(0, 50)] public int CensorAreaExpansionPixels = 5;
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
        private List<Renderer> _renderersToCensor = new List<Renderer>();
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (!SystemInfo.supportsImageEffects) {
                enabled = false;
                return;
            }

            _mainCamera = GetComponent<Camera>();
            _mainCamera.depthTextureMode |= DepthTextureMode.Depth;

            CreateResources();
            FindAndCacheRenderers();
        }

        private void OnDisable()
        {
            CleanupResources();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!AreResourcesCreated()) {
                Graphics.Blit(source, destination);
                return;
            }

            // Find renderers every frame in the editor to catch newly added objects.
            if (Application.isEditor) {
                FindAndCacheRenderers();
            }

            // --- Mask Generation ---
            var maskDescriptor = new RenderTextureDescriptor(source.width, source.height, RenderTextureFormat.R8, 16);
            RenderTexture maskRT = RenderTexture.GetTemporary(maskDescriptor);
            RenderCensorMask(maskRT);

            // --- Dilation ---
            RenderTexture processedMask;
            if (CensorAreaExpansionPixels > 0) {
                processedMask = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.R8);
                ApplyDilation(maskRT, processedMask);
                RenderTexture.ReleaseTemporary(maskRT); // Release original mask
            } else {
                processedMask = maskRT; // Use original mask
            }

            // --- Final Composite ---
            _censorEffectMaterial.SetFloat("_PixelSize", PixelBlockCount);
            _censorEffectMaterial.SetFloat("_AntiAliasing", EnableAntiAliasing ? 1.0f : 0.0f);
            _censorEffectMaterial.SetTexture("_CensorMask", processedMask);
            Graphics.Blit(source, destination, _censorEffectMaterial);

            // --- Cleanup ---
            RenderTexture.ReleaseTemporary(processedMask);
        }
        #endregion

        #region Core Logic
        private void RenderCensorMask(RenderTexture destination)
        {
            // Use a temporary CommandBuffer to reliably render the mask with occlusion.
            var cmd = new CommandBuffer { name = "Censor Mask Generation" };

            // Set the scene's depth texture as a global variable for the shader to sample.
            cmd.SetGlobalTexture("_SceneDepthTexture", new RenderTargetIdentifier(BuiltinRenderTextureType.Depth));

            // Draw the renderers into our mask texture.
            cmd.SetRenderTarget(destination);
            cmd.ClearRenderTarget(true, true, Color.clear);

            _censorMaskMaterial.EnableKeyword(EnableOcclusion ? "_OCCLUSION_ON" : "__");

            foreach (var renderer in _renderersToCensor) {
                if (renderer != null && renderer.isVisible) {
                    cmd.DrawRenderer(renderer, _censorMaskMaterial);
                }
            }

            // Execute and release the command buffer.
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        private void ApplyDilation(RenderTexture source, RenderTexture destination)
        {
            _dilationMaterial.SetInt("_DilationSize", CensorAreaExpansionPixels);
            var tempRT = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

            // Horizontal Pass
            Graphics.Blit(source, tempRT, _dilationMaterial, 0);
            // Vertical Pass
            Graphics.Blit(tempRT, destination, _dilationMaterial, 1);

            RenderTexture.ReleaseTemporary(tempRT);
        }

        private void FindAndCacheRenderers()
        {
            _renderersToCensor.Clear();
            var allRenderers = FindObjectsOfType<Renderer>();
            foreach (var renderer in allRenderers) {
                if (renderer.gameObject.activeInHierarchy && renderer.isVisible && (CensorLayer & (1 << renderer.gameObject.layer)) != 0) {
                    _renderersToCensor.Add(renderer);
                }
            }
        }
        #endregion

        #region Resource Management
        private void CreateResources()
        {
            if (_censorMaskShader == null) _censorMaskShader = Shader.Find("Hidden/CensorMask");
            if (_censorEffectShader == null) _censorEffectShader = Shader.Find("Hidden/CensorEffect");
            if (_dilationShader == null) _dilationShader = Shader.Find("Hidden/CensorDilation");

            _censorMaskMaterial = CreateMaterial(_censorMaskShader);
            _censorEffectMaterial = CreateMaterial(_censorEffectShader);
            _dilationMaterial = CreateMaterial(_dilationShader);
        }

        private void CleanupResources()
        {
            if (_censorMaskMaterial != null) DestroyImmediate(_censorMaskMaterial);
            if (_censorEffectMaterial != null) DestroyImmediate(_censorEffectMaterial);
            if (_dilationMaterial != null) DestroyImmediate(_dilationMaterial);
        }

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null || !shader.isSupported) {
                Debug.LogError($"Shader {shader.name} not found or not supported.", this);
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
