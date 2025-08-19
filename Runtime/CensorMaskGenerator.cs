using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(Camera))]
[AddComponentMenu("Rendering/Censor Mask Generator")]
public class CensorMaskGenerator : MonoBehaviour
{
    [Tooltip("The resolution of the mask texture. Lower resolutions can improve performance.")]
    public int resolution = 1024;

    private RenderTexture _maskTexture;
    private Camera _maskCamera;
    private Shader _whiteShader;

    // We will find the main camera's PostProcessLayer to get the settings.
    private PostProcessLayer _postProcessLayer;
    private CensorEffect _censorEffect;

    public const string GlobalMaskTextureName = "_GlobalCensorMask";

    void OnEnable()
    {
        _maskCamera = GetComponent<Camera>();
        _whiteShader = Shader.Find("Hidden/CensorEffect/UnlitWhite");

        if (_whiteShader == null)
        {
            Debug.LogError("Censor Mask Generator: Could not find 'Hidden/CensorEffect/UnlitWhite' shader!");
            enabled = false;
            return;
        }

        _maskTexture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.R8);
        _maskCamera.targetTexture = _maskTexture;
        _maskCamera.enabled = true;

        Shader.SetGlobalTexture(GlobalMaskTextureName, _maskTexture);
        _maskCamera.SetReplacementShader(_whiteShader, "RenderType");
    }

    void OnDisable()
    {
        if (_maskCamera != null)
        {
            _maskCamera.ResetReplacementShader();
            _maskCamera.targetTexture = null;
        }

        if (_maskTexture != null)
        {
            _maskTexture.Release();
            DestroyImmediate(_maskTexture);
            _maskTexture = null;
        }

        Shader.SetGlobalTexture(GlobalMaskTextureName, null);
    }

    void Update()
    {
        // Find the PostProcessLayer and CensorEffect settings if we don't have them.
        // This is done in Update to be robust against scene changes.
        if (_postProcessLayer == null)
        {
            // We assume the main camera is the one with the PostProcessLayer.
            // A more complex scene might require a more specific way to find this.
            _postProcessLayer = FindObjectOfType<PostProcessLayer>();

            if (_postProcessLayer != null)
            {
                // Once we have the layer, we can get the specific settings for our effect.
                _censorEffect = _postProcessLayer.GetSettings<CensorEffect>();
            }
        }

        if (_censorEffect != null && _maskCamera != null)
        {
            // Dynamically update the camera's culling mask from the Post-Processing Profile.
            _maskCamera.cullingMask = _censorEffect.censorLayer.value;
        }

        // Ensure the camera is configured correctly every frame.
        _maskCamera.clearFlags = CameraClearFlags.SolidColor;
        _maskCamera.backgroundColor = Color.black;
    }
}
