using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CensorEffect.Runtime.URP
{
    public class CensorFeature : ScriptableRendererFeature
    {
        private CensorRenderPass _censorPass;
        private CensorEffect _censorEffect;

        protected override void Dispose(bool disposing)
        {
            _censorPass?.Dispose();
            _censorPass = null;
        }

        public override void Create()
        {
            // This is called by the URP renderer
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!Application.isPlaying)
            {
                // Attempt to find the effect in the scene during edit mode
                _censorEffect = FindObjectOfType<CensorEffect>();
            }

            if (_censorEffect == null)
            {
                // Try to find the component on the camera
                renderingData.cameraData.camera.TryGetComponent(out _censorEffect);
                if (_censorEffect == null) return;
            }

            if (_censorPass == null)
            {
                _censorPass = new CensorRenderPass(_censorEffect);
            }

            _censorPass.Setup(renderer.cameraColorTarget);
            _censorPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(_censorPass);
        }
    }
}
