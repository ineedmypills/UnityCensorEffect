using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CensorEffect.Runtime.URP
{
    public class CensorRenderPass : ScriptableRenderPass
    {
        private const string ProfilerTag = "Censor Pass";

        private CensorEffect _censorEffect;
        private Material _blurMaterial;
        private RenderTargetIdentifier _cameraColorTarget;
        private RenderTargetHandle _censorMaskTexture;
        private FilteringSettings _filteringSettings;
        private DrawingSettings _drawingSettings;

        public CensorRenderPass(CensorEffect censorEffect)
        {
            _censorEffect = censorEffect;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            _filteringSettings = new FilteringSettings(RenderQueueRange.all, _censorEffect.CensorLayer);
            _censorMaskTexture.Init("_CensorMask");

            if (_blurMaterial == null)
            {
                var shader = Shader.Find("Hidden/CensorBlur");
                if (shader != null)
                {
                    _blurMaterial = new Material(shader);
                }
            }

            // Drawing settings only need to be created once
            _drawingSettings = new DrawingSettings(new ShaderTagId("UniversalForward"), new SortingSettings());
        }

        public void Dispose()
        {
            if (_blurMaterial != null)
            {
                #if UNITY_EDITOR
                Object.DestroyImmediate(_blurMaterial);
                #else
                Object.Destroy(_blurMaterial);
                #endif
            }
        }

        public void Setup(RenderTargetIdentifier cameraColorTarget)
        {
            _cameraColorTarget = cameraColorTarget;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(_censorMaskTexture.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.A8);
            ConfigureTarget(_censorMaskTexture.Identifier());
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_censorEffect == null || _censorEffect.CensorMaskMaterial == null || _censorEffect.CensorEffectMaterial == null)
            {
                return;
            }

            var cmd = CommandBufferPool.Get(ProfilerTag);

            // 1. Draw the censor mask
            _censorEffect.UpdateMaterialProperties(renderingData.cameraData.camera);

            _drawingSettings.sortingSettings = new SortingSettings(renderingData.cameraData.camera) { criteria = SortingCriteria.CommonOpaque };
            _drawingSettings.overrideMaterial = _censorEffect.CensorMaskMaterial;
            _drawingSettings.overrideMaterialPassIndex = 0;

            context.DrawRenderers(renderingData.cullResults, ref _drawingSettings, ref _filteringSettings);

            // 2. Blur the mask
            if (_censorEffect.CensorAreaExpansion > 0 && _blurMaterial != null)
            {
                _blurMaterial.SetFloat("_BlurSize", _censorEffect.CensorAreaExpansion);
                int tempBlurTexId = Shader.PropertyToID("_TempBlurTexture");
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.colorFormat = RenderTextureFormat.A8;
                cmd.GetTemporaryRT(tempBlurTexId, desc, FilterMode.Bilinear);

                // Horizontal blur
                cmd.Blit(_censorMaskTexture.Identifier(), tempBlurTexId, _blurMaterial, 0);
                // Vertical blur
                cmd.Blit(tempBlurTexId, _censorMaskTexture.Identifier(), _blurMaterial, 1);

                cmd.ReleaseTemporaryRT(tempBlurTexId);
            }

            // 3. Set the mask texture globally
            cmd.SetGlobalTexture(_censorMaskTexture.id, _censorMaskTexture.Identifier());

            // 4. Blit the final effect
            var source = _cameraColorTarget;
            int destination = Shader.PropertyToID("_TempTex");
            cmd.GetTemporaryRT(destination, renderingData.cameraData.cameraTargetDescriptor);
            Blit(cmd, source, destination, _censorEffect.CensorEffectMaterial);
            Blit(cmd, destination, source);
            cmd.ReleaseTemporaryRT(destination);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_censorMaskTexture.id);
        }
    }
}
