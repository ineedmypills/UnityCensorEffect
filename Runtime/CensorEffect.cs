using UnityEngine;
using System;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Effects/Pixelated Censor")]
    public class CensorEffect : MonoBehaviour
    {
        [Header("Censor Settings")]
        [Tooltip("Layers to be censored")]
        public LayerMask CensorLayer = 0;

        [Tooltip("Physical size of pixel blocks (world units)")]
        [Min(0.001f)]
        public float PixelWorldSize = 0.1f;

        [Tooltip("Expand censored area (world units)")]
        [Min(0)]
        public float CensorAreaExpansion = 0.1f;

        [Header("Appearance")]
        [Tooltip("Enable smooth edges on censored areas")]
        public bool EnableAntiAliasing = true;

        [Tooltip("Censor shader (auto-detected if empty)")]
        public Shader CensorShader;

        // Internal resources
        private Camera _mainCamera;
        private Camera _censorCamera;
        private Material _censorMaterial;
        private RenderTexture _censorTexture;
        
        // Shader property IDs
        private static readonly int CensorMask = Shader.PropertyToID("_CensorMask");
        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        private static readonly int CensorAreaExpansionID = Shader.PropertyToID("_CensorAreaExpansion");
        private static readonly int AntiAliasingID = Shader.PropertyToID("_AntiAliasing");

        #region Properties
        public Camera MainCamera
        {
            get
            {
                if (_mainCamera == null) 
                    _mainCamera = GetComponent<Camera>();
                return _mainCamera;
            }
        }

        private Camera CensorCamera
        {
            get
            {
                if (_censorCamera == null)
                {
                    var go = new GameObject("CensorCamera")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    go.transform.SetParent(MainCamera.transform, false);
                    _censorCamera = go.AddComponent<Camera>();
                    _censorCamera.enabled = false;
                }
                return _censorCamera;
            }
        }

        private Material CensorMaterial
        {
            get
            {
                if (_censorMaterial == null)
                {
                    if (CensorShader == null)
                        CensorShader = Shader.Find("Hidden/PixelatedCensorEffect");
                    
                    if (CensorShader != null && CensorShader.isSupported)
                    {
                        _censorMaterial = new Material(CensorShader);
                        _censorMaterial.hideFlags = HideFlags.HideAndDontSave;
                    }
                }
                return _censorMaterial;
            }
        }
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            InitializeResources();
        }

        private void OnDisable()
        {
            CleanupResources();
        }

        private void OnValidate()
        {
            PixelWorldSize = Mathf.Max(0.001f, PixelWorldSize);
            CensorAreaExpansion = Mathf.Max(0, CensorAreaExpansion);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CensorMaterial == null || CensorLayer == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            try
            {
                RenderCensorEffect(source, destination);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Graphics.Blit(source, destination);
            }
        }
        #endregion

        #region Rendering
        private void RenderCensorEffect(RenderTexture source, RenderTexture destination)
        {
            // Calculate pixel size based on physical dimensions
            float pixelSize = CalculatePixelSize();
            
            // Update render texture
            UpdateCensorTexture(source.width, source.height);
            
            // Update censor camera settings
            UpdateCensorCamera();

            // Render censor objects
            RenderCensorObjects();

            // Apply material properties
            CensorMaterial.SetTexture(CensorMask, _censorTexture);
            CensorMaterial.SetFloat(PixelSizeID, pixelSize);
            CensorMaterial.SetFloat(CensorAreaExpansionID, CalculateExpansionSize());
            CensorMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1 : 0);

            // Final composition
            Graphics.Blit(source, destination, CensorMaterial);
        }

        private void RenderCensorObjects()
        {
            CensorCamera.targetTexture = _censorTexture;
            RenderTexture.active = _censorTexture;
            GL.Clear(true, true, Color.clear);
            CensorCamera.Render();
        }
        #endregion

        #region Helper Methods
        private float CalculatePixelSize()
        {
            // Calculate based on screen height and FOV
            float distance = MainCamera.farClipPlane * 0.5f;
            float worldHeight = 2.0f * distance * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return PixelWorldSize * (Screen.height / worldHeight);
        }

        private float CalculateExpansionSize()
        {
            // Convert world expansion to screen pixels
            float distance = MainCamera.farClipPlane * 0.5f;
            float worldHeight = 2.0f * distance * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return CensorAreaExpansion * (Screen.height / worldHeight);
        }

        private void InitializeResources()
        {
            // Force initialization
            var cam = CensorCamera;
            var mat = CensorMaterial;
        }

        private void CleanupResources()
        {
            if (_censorCamera != null)
            {
                DestroyImmediate(_censorCamera.gameObject);
                _censorCamera = null;
            }
            
            if (_censorMaterial != null)
            {
                DestroyImmediate(_censorMaterial);
                _censorMaterial = null;
            }
            
            ReleaseTexture(ref _censorTexture);
        }

        private void UpdateCensorTexture(int width, int height)
        {
            if (_censorTexture == null || _censorTexture.width != width || _censorTexture.height != height)
            {
                ReleaseTexture(ref _censorTexture);
                _censorTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    wrapMode = TextureWrapMode.Clamp
                };
                _censorTexture.Create();
            }
        }

        private void UpdateCensorCamera()
        {
            CensorCamera.CopyFrom(MainCamera);
            CensorCamera.enabled = false;
            CensorCamera.cullingMask = CensorLayer;
            CensorCamera.backgroundColor = Color.clear;
            CensorCamera.clearFlags = CameraClearFlags.SolidColor;
            CensorCamera.depth = MainCamera.depth - 1;
        }

        private void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                DestroyImmediate(texture);
                texture = null;
            }
        }
        #endregion
    }
}