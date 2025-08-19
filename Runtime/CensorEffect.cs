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
    private Shader _whiteMaskShader;
    private int _censorLayerMask;
    private Camera _censorCamera;
    private RenderTexture _censorLayerTexture;

    public override void Init()
    {
        _censorShader = Shader.Find("Hidden/Custom/CensorShader");
        _whiteMaskShader = Shader.Find("Hidden/Custom/WhiteMask");

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
        // If the main shader is missing, just pass the texture through to avoid breaking the chain.
        if (_censorShader == null)
        {
            context.command.BlitFullscreenTriangle(context.source, context.destination);
            return;
        }

        var sheet = context.propertySheets.Get(_censorShader);
        sheet.properties.SetFloat("_PixelSize", settings.pixelSize);
        sheet.properties.SetFloat("_HardEdges", settings.hardEdges ? 1.0f : 0.0f);

        _censorLayerMask = settings.censorLayer.value;

        // Only render the mask if a layer is selected and we have the shader for it
        if (_censorLayerMask != 0 && _whiteMaskShader != null)
        {
            _censorCamera.CopyFrom(context.camera);
            _censorCamera.cullingMask = _censorLayerMask;
            _censorCamera.clearFlags = CameraClearFlags.SolidColor;
            _censorCamera.backgroundColor = Color.clear;

            _censorLayerTexture = RenderTexture.GetTemporary(context.width, context.height, 0, RenderTextureFormat.R8);
            _censorCamera.targetTexture = _censorLayerTexture;

            // Render the objects with a solid white shader to create the mask
            _censorCamera.RenderWithShader(_whiteMaskShader, "RenderType");

            sheet.properties.SetTexture("_CensorMaskTex", _censorLayerTexture);
        }
        else
        {
            // If no layer is selected, or the mask shader is missing, use a blank texture
            sheet.properties.SetTexture("_CensorMaskTex", Texture2D.blackTexture);
        }

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);

        // Cleanup the temporary texture
        if (_censorLayerTexture != null)
        {
            RenderTexture.ReleaseTemporary(_censorLayerTexture);
            _censorLayerTexture = null; // Set to null to prevent double release
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
