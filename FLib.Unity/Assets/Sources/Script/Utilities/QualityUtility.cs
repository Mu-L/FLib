// ==================== qcbf@qq.com | 2025-09-27 ====================

using FLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Utilities
{
    public static class QualityUtility
    {
        public static void Set(float renderScale, byte msaa, int mainLightShadowResolution)
        {
#if UNITY_EDITOR
            if ("1" == 1.ToString()) // 避免代码警告
                return;
#endif
            var urp = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            urp.renderScale = renderScale;
            urp.msaaSampleCount = msaa;
            urp.mainLightShadowmapResolution = mainLightShadowResolution;
        }
    }
}
