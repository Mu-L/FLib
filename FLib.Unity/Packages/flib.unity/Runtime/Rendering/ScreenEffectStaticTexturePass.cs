// =================================================={By Qcbf|qcbf@qq.com|2024-11-07}==================================================

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FLib.Unity
{
    public class ScreenEffectStaticTexture : SimpleRendererFeature.Pass, IDisposable
    {
        public static GraphicsFormat DefaultFormat = SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
        public Option Options = new();
        public RTHandle RT;

        public override string Name => nameof(ScreenEffectStaticTexture);

        [Serializable]
        public class Option
        {
            public string Type = string.Empty;
            public Material EffectMaterial;
            public FilterMode Filter = FilterMode.Bilinear;

            [Range(0, 2)]
            public float Scale = 1f;

            public RenderPassEvent Event = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public void ReleaseRT()
        {
            if (RT != null)
            {
                RT.Release();
                RT = null;
            }
        }

        public void Dispose()
        {
            ReleaseRT();
            SimpleRendererFeature.RemovePass(Options.Type, this);
        }

        public ScreenEffectStaticTexture Generate(Vector2 size)
        {
            renderPassEvent = Options.Event;

            var realSize = Vector2Int.CeilToInt(size * Options.Scale);
            if (RT == null || RT.rt.width != size.x || RT.rt.height != size.y)
            {
                RT?.Release();
                RT = RTHandles.Alloc(realSize.x, realSize.y, wrapMode: TextureWrapMode.Clamp, filterMode: FilterMode.Bilinear, colorFormat: DefaultFormat, autoGenerateMips: false, name: nameof(ScreenEffectStaticTexture));
            }

            SimpleRendererFeature.AddPass(Options.Type, this);
            return this;
        }
#pragma warning disable CS0672,CS0618

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // if (renderingData.cameraData.cameraType != CameraType.Game)
            //     return;

            using var cmd = CommandBufferPool.Get(nameof(ScreenEffectStaticTexture));
#if UNITY_6000_0_OR_NEWER
            var srcRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
            var srcRT = renderingData.cameraData.renderer.cameraColorTarget;
#endif
            Blit(cmd, srcRT, RT, Options.EffectMaterial);
            context.ExecuteCommandBuffer(cmd);
        }

        public static implicit operator Texture(ScreenEffectStaticTexture v) => v.RT.rt;
    }
}
