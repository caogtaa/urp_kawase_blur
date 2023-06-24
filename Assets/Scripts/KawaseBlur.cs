using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2,15)]
        public int blurPasses = 1;

        [Range(1,4)]
        public int downsample = 1;
        public bool copyToFramebuffer;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blurMaterial;
        public int passes;              // 模糊迭代次数
        public int downsample;          // 控制src降采样率
        public bool copyToFramebuffer;  // 为true时，直接替换原始colorAttachment（是否兼容全平台需要测试，尤其注意开了pp的情况）
        public string targetName;       // 全局纹理的名字
        string profilerTag;

        int tmpId1;
        int tmpId2;

        RenderTargetIdentifier tmpRT1;
        RenderTargetIdentifier tmpRT2;
        
        private RenderTargetIdentifier source { get; set; }

        public void Setup(RenderTargetIdentifier source) {
            this.source = source;
        }

        public CustomRenderPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        // 每一帧调用
        // Execute()前调用
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width / downsample;
            var height = cameraTextureDescriptor.height / downsample;

            tmpId1 = Shader.PropertyToID("tmpBlurRT1");
            tmpId2 = Shader.PropertyToID("tmpBlurRT2");
            cmd.GetTemporaryRT(tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);      // 只添加cmd，不做真实创建
            cmd.GetTemporaryRT(tmpId2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            tmpRT1 = new RenderTargetIdentifier(tmpId1);
            tmpRT2 = new RenderTargetIdentifier(tmpId2);
            
            ConfigureTarget(tmpRT1);        // 并不会创建或者bind RT。ConfigureTarget传入的RT，在Execute()结束后会自动释放
            ConfigureTarget(tmpRT2);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // first pass
            // cmd.GetTemporaryRT(tmpId1, opaqueDesc, FilterMode.Bilinear);
            cmd.SetGlobalFloat("_offset", 1.5f);
            cmd.Blit(source, tmpRT1, blurMaterial);

            for (var i=1; i<passes-1; i++) {
                cmd.SetGlobalFloat("_offset", 0.5f + i);
                cmd.Blit(tmpRT1, tmpRT2, blurMaterial);

                // pingpong
                var rttmp = tmpRT1;
                tmpRT1 = tmpRT2;
                tmpRT2 = rttmp;
            }

            // final pass
            cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
            if (copyToFramebuffer) {
                cmd.Blit(tmpRT1, source, blurMaterial);
            } else {
                cmd.Blit(tmpRT1, tmpRT2, blurMaterial);
                cmd.SetGlobalTexture(targetName, tmpRT2);       // RT复制给全局RT
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new CustomRenderPass("KawaseBlur");
        scriptablePass.blurMaterial = settings.blurMaterial;
        scriptablePass.passes = settings.blurPasses;
        scriptablePass.downsample = settings.downsample;
        scriptablePass.copyToFramebuffer = settings.copyToFramebuffer;
        scriptablePass.targetName = settings.targetName;

        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var src = renderer.cameraColorTarget;
        scriptablePass.Setup(src);
        renderer.EnqueuePass(scriptablePass);
    }
}


