//==================={By Qcbf|qcbf@qq.com|11/6/2023 2:31:02 PM}===================

#if UNITY_6000_0_OR_NEWER
using FLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace FLib.Unity
{
    public class ScreenDistortionFeature : ScriptableRendererFeature
    {
        private static MaterialPropertyBlock _shaderPropertyBlock;
        private static readonly int ShaderIdCameraTexture = Shader.PropertyToID("_CameraTexture");
        private static readonly int ShaderIdMaskTexture = Shader.PropertyToID("_MaskTexture");

        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader DistortionShader;
        private Material _material;
        private ScreenDistortionPass _pass;

        public override void Create()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            _shaderPropertyBlock ??= new MaterialPropertyBlock();
            if (DistortionShader != null)
            {
                _material = new Material(DistortionShader);
                _pass = new ScreenDistortionPass { Feature = this, renderPassEvent = Event };
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_material != null)
            {
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
                _material = null;
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            if (_material == null)
                TryInitialize();
#endif
            if ((renderingData.cameraData.cameraType & (CameraType.Game | CameraType.SceneView)) != 0)
                renderer.EnqueuePass(_pass);
        }

        private class MaskPassData
        {
            public RendererListHandle RendererListHdl;
        }

        private class CopyCameraPassData
        {
            public TextureHandle Src;
        }

        private class DistortionPassData
        {
            public Material MainMaterial;
            public TextureHandle MaskTex;
            public TextureHandle CameraTex;
        }


        public class ScreenDistortionPass : ScriptableRenderPass
        {
            public ScreenDistortionFeature Feature;
            public GraphicsFormat Format = GraphicsFormat.R8G8B8A8_UNorm;

            // TOOD: 增加忽略忽略层
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var renderingData = frameData.Get<UniversalRenderingData>();
                var resourceData = frameData.Get<UniversalResourceData>();

                TextureHandle maskTex;
                TextureHandle cameraTex;
                // 渲染mask纹理
                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(passName, out var passData, profilingSampler))
                {
                    var texDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
                    // texDesc.width >>= 1;
                    // texDesc.height >>= 1;
                    texDesc.filterMode = FilterMode.Bilinear;
                    texDesc.colorFormat = Format;
                    texDesc.clearColor = default;
                    texDesc.msaaSamples = MSAASamples.None;
                    maskTex = renderGraph.CreateTexture(texDesc);
                    builder.SetRenderAttachment(maskTex, 0);
                    // builder.SetRenderAttachmentDepth(resourceData.cameraDepthTexture, AccessFlags.Read);
                    passData.RendererListHdl = renderGraph.CreateRendererList(new RendererListParams(
                        renderingData.cullResults,
                        new DrawingSettings(new ShaderTagId("DistortionMask"), new SortingSettings(cameraData.camera)),
                        new FilteringSettings(RenderQueueRange.all)));
                    builder.UseRendererList(passData.RendererListHdl);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (MaskPassData passData, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(passData.RendererListHdl));
                }

                // copy当前camera rt
                using (var builder = renderGraph.AddRasterRenderPass<CopyCameraPassData>(passName, out var passData, profilingSampler))
                {
                    var texDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);
                    texDesc.clearBuffer = false;
                    texDesc.msaaSamples = MSAASamples.None;
                    texDesc.wrapMode = TextureWrapMode.Mirror;
                    builder.UseTexture(passData.Src = resourceData.cameraColor);
                    cameraTex = renderGraph.CreateTexture(texDesc);
                    builder.SetRenderAttachment(cameraTex, 0);
                    builder.SetRenderFunc(static (CopyCameraPassData passData, RasterGraphContext ctx) => Blitter.BlitTexture(ctx.cmd, passData.Src, new Vector4(1, 1, 0, 0), 0, false));
                }

                // 混合mask扭曲后输出rt
                using (var builder = renderGraph.AddRasterRenderPass<DistortionPassData>(passName, out var passData, profilingSampler))
                {
                    passData.MainMaterial = Feature._material;
                    builder.UseTexture(passData.MaskTex = maskTex);
                    builder.UseTexture(passData.CameraTex = cameraTex);
                    builder.SetRenderAttachment(resourceData.cameraColor, 0);
                    builder.SetRenderFunc(static (DistortionPassData passData, RasterGraphContext ctx) =>
                    {
                        _shaderPropertyBlock.Clear();
                        _shaderPropertyBlock.SetTexture(ShaderIdMaskTexture, passData.MaskTex);
                        _shaderPropertyBlock.SetTexture(ShaderIdCameraTexture, passData.CameraTex);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, passData.MainMaterial, 0, MeshTopology.Triangles, 3, 1, _shaderPropertyBlock);
                    });
                }
            }
        }
    }
}

#endif
