using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(CensorEffectRenderer), PostProcessEvent.BeforeStack, "Custom/Censor")]
public sealed class CensorEffect : PostProcessEffectSettings
{
    [Tooltip("The layer(s) to apply the censor effect to.")]
    public LayerMaskParameter censorLayer = new LayerMaskParameter { value = 0 };

    [Range(1, 256), Tooltip("The size of the pixelation blocks.")]
    public IntParameter pixelSize = new IntParameter { value = 50 };

    [Tooltip("Crops the censorship to the object's boundary with a hard edge.")]
    public BoolParameter hardEdges = new BoolParameter { value = false };

    [Header("Debug")]
    [Tooltip("Shows the generated censor mask instead of the final effect.")]
    public BoolParameter showMask = new BoolParameter { value = false };

    public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    {
        return enabled.value && censorLayer.value != 0;
    }
}

public sealed class CensorEffectRenderer : PostProcessEffectRenderer<CensorEffect>
{
    private Camera _censorCamera;
    private Material _censorMaterial;
    private Shader _censorShader;
    private Shader _whiteMaskShader;

    public override void Init()
    {
        _censorShader = Shader.Find("Hidden/CensorEffect/Censor");
        _whiteMaskShader = Shader.Find("Hidden/CensorEffect/WhiteMask");

        if (_censorShader != null)
        {
            _censorMaterial = new Material(_censorShader);
            _censorMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        var go = new GameObject("Censor Mask Camera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _censorCamera = go.AddComponent<Camera>();
        _censorCamera.enabled = false;
    }

    public override void Render(PostProcessRenderContext context)
    {
        context.camera.depthTextureMode |= DepthTextureMode.Depth;

        if (_censorMaterial == null || _whiteMaskShader == null)
        {
            context.command.Blit(context.source, context.destination);
            return;
        }

        var cmd = context.command;
        cmd.BeginSample("CensorEffect");

        var maskTexture = RenderTexture.GetTemporary(context.width, context.height, 16, RenderTextureFormat.R8);

        _censorCamera.CopyFrom(context.camera);
        // Explicitly copy projection matrix for robust depth testing
        _censorCamera.projectionMatrix = context.camera.projectionMatrix;
        _censorCamera.cullingMask = settings.censorLayer.value;
        _censorCamera.clearFlags = CameraClearFlags.SolidColor;
        _censorCamera.backgroundColor = Color.clear;
        _censorCamera.targetTexture = maskTexture;
        _censorCamera.RenderWithShader(_whiteMaskShader, "RenderType");

        _censorMaterial.SetFloat("_PixelSize", settings.pixelSize);
        _censorMaterial.SetFloat("_HardEdges", settings.hardEdges ? 1.0f : 0.0f);
        _censorMaterial.SetTexture("_CensorMaskTex", maskTexture);

        if (settings.showMask.value)
        {
            cmd.Blit(maskTexture, context.destination);
        }
        else
        {
            cmd.Blit(context.source, context.destination, _censorMaterial, 0);
        }

        RenderTexture.ReleaseTemporary(maskTexture);
        cmd.EndSample("CensorEffect");
    }

    public override void Release()
    {
        base.Release();
        if (_censorCamera != null) UnityEngine.Object.DestroyImmediate(_censorCamera.gameObject);
        if (_censorMaterial != null) UnityEngine.Object.DestroyImmediate(_censorMaterial);
    }
}
