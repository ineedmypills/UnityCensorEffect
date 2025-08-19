using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CensorEffectRenderer), PostProcessEvent.AfterStack, "Custom/Censor")]
public sealed class CensorEffect : PostProcessEffectSettings
{
    [Tooltip("The layer(s) to apply the censor effect to.")]
    public LayerMaskParameter censorLayer = new LayerMaskParameter();

    [Range(1, 256), Tooltip("The size of the pixelation blocks.")]
    public IntParameter pixelSize = new IntParameter { value = 50 };

    [Tooltip("Crops the censorship to the object's boundary with a hard edge.")]
    public BoolParameter hardEdges = new BoolParameter { value = false };
}

public sealed class CensorEffectRenderer : PostProcessEffectRenderer<CensorEffect>
{
    private Shader _censorShader;
    private int _censorLayerMask;
    private Camera _censorCamera;
    private RenderTexture _censorLayerTexture;

    public override void Init()
    {
        _censorShader = Shader.Find("Hidden/Custom/CensorShader");

        // Create a temporary camera for rendering the censor layer
        var go = new GameObject("Censor Camera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _censorCamera = go.AddComponent<Camera>();
        _censorCamera.enabled = false;
    }

    public override void Render(PostProcessRenderContext context)
    {
        if (_censorShader == null) return;

        // Setup the property sheet
        var sheet = context.propertySheets.Get(_censorShader);
        sheet.properties.SetFloat("_PixelSize", settings.pixelSize);
        sheet.properties.SetInt("_HardEdges", settings.hardEdges ? 1 : 0);

        // Render the objects on the specified layer to a separate render texture
        _censorLayerMask = settings.censorLayer.value;
        if (_censorLayerMask != 0)
        {
            // Match the camera settings
            _censorCamera.CopyFrom(context.camera);
            _censorCamera.cullingMask = _censorLayerMask;
            _censorCamera.clearFlags = CameraClearFlags.SolidColor;
            _censorCamera.backgroundColor = Color.clear;

            // Create a render texture for the mask
            _censorLayerTexture = RenderTexture.GetTemporary(context.width, context.height, 0, RenderTextureFormat.R8);
            _censorCamera.targetTexture = _censorLayerTexture;
            _censorCamera.Render();

            sheet.properties.SetTexture("_CensorMaskTex", _censorLayerTexture);
        }
        else
        {
            // If no layer is selected, use a blank texture
            sheet.properties.SetTexture("_CensorMaskTex", Texture2D.blackTexture);
        }

        // Apply the effect
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);

        // Cleanup
        if (_censorLayerTexture != null)
        {
            RenderTexture.ReleaseTemporary(_censorLayerTexture);
        }
    }

    public override void Release()
    {
        base.Release();
        if (_censorCamera != null)
        {
            UnityEngine.Object.DestroyImmediate(_censorCamera.gameObject);
        }
    }
}
