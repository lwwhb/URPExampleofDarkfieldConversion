using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorGradingFeatures : ScriptableRendererFeature
{
    [System.Serializable]
    public class ColorGradingSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material colorGradingMaterial = null;
        public Color filterColor = Color.white;
    }

    public ColorGradingSettings settings = new ColorGradingSettings();

    class ColorGradingPass : ScriptableRenderPass
    {
        string profilerTag;
        public Material colorGradingMaterial;
        public Color filterColor;

        private RenderTargetIdentifier source { get; set; }

        int tmpId1;
        RenderTargetIdentifier tmpRT1;

        public ColorGradingPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }
        
        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            tmpId1 = Shader.PropertyToID("tmpColorRT1");
            cmd.GetTemporaryRT(tmpId1, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            tmpRT1 = new RenderTargetIdentifier(tmpId1);
            ConfigureTarget(tmpRT1);

        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            cmd.SetGlobalColor("_color", filterColor);
            cmd.Blit(source, tmpRT1, colorGradingMaterial);
            cmd.Blit(tmpRT1, source);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    ColorGradingPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new ColorGradingPass("ColorGradingPass");
        m_ScriptablePass.colorGradingMaterial = settings.colorGradingMaterial;
        m_ScriptablePass.filterColor = settings.filterColor;
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var src = renderer.cameraColorTarget;
        m_ScriptablePass.Setup(src);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


