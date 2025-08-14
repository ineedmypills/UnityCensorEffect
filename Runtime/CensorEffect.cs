using UnityEngine;

namespace CensorEffect.Runtime
{
    [ExecuteInEditMode]
    public class CensorEffect : MonoBehaviour
    {
        public LayerMask CensorLayer;
        [Min(0)]
        public float PixelSize = 10f;
        [Min(0)]
        public float CensorAreaExpansion;

        private Camera mainCamera;
        private Camera censorCamera;
        private RenderTexture censorTexture;
        private Material censorMaterial;

        private static readonly int CensorMask = Shader.PropertyToID("_CensorMask");
        private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
        private static readonly int CensorAreaExpansionID = Shader.PropertyToID("_CensorAreaExpansion");

        void OnEnable()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("CensorEffect requires a Camera component.");
                enabled = false;
                return;
            }

            Shader shader = Shader.Find("Hidden/PixelatedCensorEffect");
            if (shader == null)
            {
                Debug.LogError("Could not find 'Hidden/PixelatedCensorEffect' shader. Make sure the shader file is in the project.");
                enabled = false;
                return;
            }
            censorMaterial = new Material(shader);

            var censorCamGo = new GameObject("CensorCamera");
            censorCamGo.transform.parent = mainCamera.transform;
            censorCamera = censorCamGo.AddComponent<Camera>();
            censorCamera.CopyFrom(mainCamera);
            censorCamera.cullingMask = CensorLayer;
            censorCamera.clearFlags = CameraClearFlags.SolidColor;
            censorCamera.backgroundColor = Color.clear;
            censorCamera.enabled = false;

            censorTexture = new RenderTexture(mainCamera.pixelWidth, mainCamera.pixelHeight, 24, RenderTextureFormat.Default);
            censorCamera.targetTexture = censorTexture;
        }

        void OnDisable()
        {
            if (censorCamera != null)
            {
                DestroyImmediate(censorCamera.gameObject);
            }
            if (censorTexture != null)
            {
                censorTexture.Release();
                DestroyImmediate(censorTexture);
            }
            if (censorMaterial != null)
            {
                DestroyImmediate(censorMaterial);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (censorMaterial != null && censorCamera != null && censorTexture != null)
            {
                if (censorTexture.width != source.width || censorTexture.height != source.height)
                {
                    censorTexture.Release();
                    censorTexture.width = source.width;
                    censorTexture.height = source.height;
                    censorTexture.Create();
                }

                censorCamera.CopyFrom(mainCamera);
                censorCamera.cullingMask = CensorLayer;
                censorCamera.clearFlags = CameraClearFlags.SolidColor;
                censorCamera.backgroundColor = Color.clear;
                censorCamera.targetTexture = censorTexture;

                censorCamera.Render();

                censorMaterial.SetTexture(CensorMask, censorTexture);
                censorMaterial.SetFloat(PixelSizeID, PixelSize);
                censorMaterial.SetFloat(CensorAreaExpansionID, CensorAreaExpansion);
                Graphics.Blit(source, destination, censorMaterial);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}