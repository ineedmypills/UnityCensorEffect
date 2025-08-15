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

        [Tooltip("Physical size of pixel blocks in world units.")]
        [Min(0.001f)]
        public float PixelWorldSize = 0.1f;

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
            PixelWorldSize = Mathf.Max(0.001f, PixelWorldSize);
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

            float pixelSize = CalculatePixelSize(camera);
            float expansionSize = CalculateExpansionSize(camera);

            CensorEffectMaterial.SetFloat(PixelSizeID, pixelSize);
            CensorEffectMaterial.SetFloat(CensorAreaExpansionID, expansionSize);
            CensorEffectMaterial.SetFloat(AntiAliasingID, EnableAntiAliasing ? 1f : 0f);

            if (CensorMaskMaterial != null)
            {
                CensorMaskMaterial.SetInt(ZTestID, (int)(EnableOcclusion ? UnityEngine.Rendering.CompareFunction.LessEqual : UnityEngine.Rendering.CompareFunction.Always));
            }
        }

        private float CalculatePixelSize(Camera camera)
        {
            float size = PixelWorldSize;
            if (camera.orthographic)
            {
                return camera.orthographicSize * 2f / camera.pixelHeight * size;
            }

            float fov = camera.fieldOfView * Mathf.Deg2Rad;
            float worldHeight = 2.0f * 1.0f * Mathf.Tan(fov * 0.5f);
            return size * (camera.pixelHeight / worldHeight);
        }

        private float CalculateExpansionSize(Camera camera)
        {
            float expansion = CensorAreaExpansion;
            if (camera.orthographic)
            {
                return expansion / (camera.orthographicSize * 2f) * camera.pixelHeight;
            }

            float fov = camera.fieldOfView * Mathf.Deg2Rad;
            float worldHeight = 2.0f * 1.0f * Mathf.Tan(fov * 0.5f);
            return expansion * (camera.pixelHeight / worldHeight);
        }
    }
}