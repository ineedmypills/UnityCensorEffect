using UnityEngine;

namespace CensorEffect.Runtime.Builtin
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(CensorEffect))]
    public class CensorEffectBuiltin : MonoBehaviour
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

            // Update materials
            _censorEffect.UpdateMaterialProperties(_mainCamera);

            // Get a temporary texture for the mask
            var censorMaskTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.A8);

            // Setup and render the mask
            var censorCam = GetCensorCamera();
            censorCam.CopyFrom(_mainCamera);
            censorCam.cullingMask = _censorEffect.CensorLayer;
            censorCam.targetTexture = censorMaskTexture;
            censorCam.clearFlags = CameraClearFlags.SolidColor;
            censorCam.backgroundColor = Color.clear;
            censorCam.RenderWithShader(_censorEffect.CensorMaskMaterial.shader, "RenderType");

            // Blur the mask
            if (_censorEffect.CensorAreaExpansion > 0 && _blurMaterial != null)
            {
                _blurMaterial.SetFloat("_BlurSize", _censorEffect.CensorAreaExpansion);
                var tempBlurTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.A8);

                Graphics.Blit(censorMaskTexture, tempBlurTex, _blurMaterial, 0); // Horizontal
                Graphics.Blit(tempBlurTex, censorMaskTexture, _blurMaterial, 1); // Vertical

                RenderTexture.ReleaseTemporary(tempBlurTex);
            }

            // Set the mask texture for the effect material
            _censorEffect.CensorEffectMaterial.SetTexture(CensorEffect.CensorMaskID, censorMaskTexture);

            // Blit the final effect
            Graphics.Blit(source, destination, _censorEffect.CensorEffectMaterial);

            // Cleanup
            RenderTexture.ReleaseTemporary(censorMaskTexture);
        }
    }
}
