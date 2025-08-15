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

        // Command Buffer for rendering the censor mask
        private CommandBuffer _commandBuffer;
        private int _censorMaskID;

        // List of renderers to be censored
        private List<Renderer> _renderersToCensor = new List<Renderer>();

        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        private static readonly int CensorMaskGlobalID = Shader.PropertyToID("_CensorMask");
        private static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");
        private static readonly int DilationSizeID = Shader.PropertyToID("_DilationSize");

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            _mainCamera = GetComponent<Camera>();
            _mainCamera.depthTextureMode |= DepthTextureMode.Depth;

            _censorMaskID = Shader.PropertyToID("_CensorMaskRT");

            CreateResources();

            // Initial setup. The command buffer will be rebuilt if properties change.
            if (AreResourcesCreated())
            {
                FindAndCacheRenderers();
                SetupCommandBuffer();
            }
        }

        private void OnDisable()
        {
            CleanupCommandBuffer();
            CleanupResources();
        }

        // This method is now empty because all rendering is handled by the CommandBuffer.
        // We keep the method to ensure the effect can be disabled by disabling the component.
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // If we are here, the command buffer is not active or has been removed.
            // Just blit the source to ensure the screen is not black.
            Graphics.Blit(source, destination);
        }

        #endregion

        #region Core Logic

        private void FindAndCacheRenderers()
        {
            _renderersToCensor.Clear();
            var allRenderers = FindObjectsOfType<Renderer>();
            foreach (var renderer in allRenderers)
            {
                if (renderer.isVisible && (CensorLayer & (1 << renderer.gameObject.layer)) != 0)
                {
                    _renderersToCensor.Add(renderer);
                }
            }
        }

        private void SetupCommandBuffer()
        {
            if (_commandBuffer != null)
            {
                CleanupCommandBuffer();
            }

            _commandBuffer = new CommandBuffer { name = "Censor Effect" };

            // Ensure material properties (like shader keywords) are up to date.
            UpdateMaterialProperties();

            // Part 1: Generate the Censor Mask
            var maskDescriptor = new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight, RenderTextureFormat.R8, 16);
            _commandBuffer.GetTemporaryRT(_censorMaskID, maskDescriptor, FilterMode.Bilinear);
            _commandBuffer.SetRenderTarget(_censorMaskID);
            _commandBuffer.ClearRenderTarget(true, true, Color.clear);
            foreach (var renderer in _renderersToCensor)
            {
                if (renderer != null && renderer.isVisible)
                {
                    _commandBuffer.DrawRenderer(renderer, _censorMaskMaterial);
                }
            }

            // Part 2: Dilate the mask if required
            if (CensorAreaExpansionPixels > 0)
            {
                int dilatedMaskID = Shader.PropertyToID("_DilatedCensorMaskTemp");
                var dilatedMaskDescriptor = new RenderTextureDescriptor(_mainCamera.pixelWidth, _mainCamera.pixelHeight, RenderTextureFormat.R8, 0);
                _commandBuffer.GetTemporaryRT(dilatedMaskID, dilatedMaskDescriptor, FilterMode.Bilinear);

                _commandBuffer.Blit(_censorMaskID, dilatedMaskID, _dilationMaterial, 0); // Horizontal
                _commandBuffer.Blit(dilatedMaskID, _censorMaskID, _dilationMaterial, 1); // Vertical
                _commandBuffer.ReleaseTemporaryRT(dilatedMaskID);
            }

            // Part 3: Apply the final pixelation effect
            _commandBuffer.SetGlobalTexture(CensorMaskGlobalID, _censorMaskID);

            // Blit the screen to itself using the effect material.
            // BuiltinRenderTextureType.CameraTarget refers to the camera's current render target.
            _commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, _censorEffectMaterial);

            // Part 4: Cleanup
            _commandBuffer.ReleaseTemporaryRT(_censorMaskID);

            _mainCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
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
            // Find shaders if they are not assigned in the inspector.
            if (_censorMaskShader == null) _censorMaskShader = Shader.Find("Hidden/CensorMask");
            if (_censorEffectShader == null) _censorEffectShader = Shader.Find("Hidden/CensorEffect");
            if (_dilationShader == null) _dilationShader = Shader.Find("Hidden/CensorDilation");

            // Create materials from the shaders.
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
        }

        private void CleanupCommandBuffer()
        {
            if (_commandBuffer != null)
            {
                _mainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
                _commandBuffer.Release();
                _commandBuffer = null;
            }
        }

        private Material CreateMaterial(Shader shader)
        {
            if (shader == null || !shader.isSupported) return null;
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        #endregion
    }
}
