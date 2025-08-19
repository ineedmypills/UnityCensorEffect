using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CensorEffectRenderer), PostProcessEvent.BeforeStack, "Custom/Censor")]
public sealed class CensorEffect : PostProcessEffectSettings
{
    [Tooltip("The layers to apply the censor effect to. This will be used by the CensorMaskGenerator in the scene.")]
    public LayerMaskParameter censorLayer = new LayerMaskParameter { value = 0 };

    [Range(1, 256), Tooltip("The size of the pixelation blocks.")]
    public IntParameter pixelSize = new IntParameter { value = 50 };

    [Tooltip("Crops the censorship to the object's boundary with a hard edge. This requires the mask to be generated with high enough resolution.")]
    public BoolParameter hardEdges = new BoolParameter { value = false };

    // The LayerMask parameter has been removed, as this is now handled by the CensorMaskGenerator component.

    public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    {
        // The effect is enabled if the shader can be found and the pixel size is greater than 0.
        // We also check if the global mask texture exists, which is created by the CensorMaskGenerator.
        return enabled.value
            && pixelSize.value > 0
            && Shader.Find("Hidden/CensorEffect/Censor") != null
            && Shader.GetGlobalTexture(CensorMaskGenerator.GlobalMaskTextureName) != null;
    }
}

public sealed class CensorEffectRenderer : PostProcessEffectRenderer<CensorEffect>
{
    private Material _censorMaterial;

    public override void Init()
    {
        // In the new architecture, the renderer only needs to manage the material for the final composite.
        var censorShader = Shader.Find("Hidden/CensorEffect/Censor");
        if (censorShader != null)
        {
            _censorMaterial = new Material(censorShader);
            _censorMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    public override void Render(PostProcessRenderContext context)
    {
        if (_censorMaterial == null)
        {
            // If the material failed to initialize, just blit the source to the destination.
            context.command.Blit(context.source, context.destination);
            return;
        }

        // Set the shader properties from the settings.
        _censorMaterial.SetFloat("_PixelSize", settings.pixelSize);
        _censorMaterial.SetFloat("_HardEdges", settings.hardEdges ? 1.0f : 0.0f);

        // The _GlobalCensorMask texture is set globally by the CensorMaskGenerator and is automatically
        // available to this shader. We no longer need to manage cameras or masks here.

        var cmd = context.command;
        cmd.BeginSample("CensorEffect");

        // This two-step blit is a workaround for compiler errors on newer Unity versions.
        // It is more robust than a single blit call with a RenderTargetIdentifier source.
        var temp = context.GetScreenSpaceTemporaryRT(0, context.sourceFormat);
        cmd.Blit(context.source, temp);
        cmd.Blit(temp, context.destination, _censorMaterial, 0);
        RenderTexture.ReleaseTemporary(temp);

        cmd.EndSample("CensorEffect");
    }

    public override void Release()
    {
        base.Release();
        if (_censorMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(_censorMaterial);
        }
    }
}
