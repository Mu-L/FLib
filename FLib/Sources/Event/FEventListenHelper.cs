// ==================== qcbf@qq.com | 2025-07-25 ====================

using System;
using FLib;

namespace FLib
{
    /// <summary>
    /// 事件监听辅助处理
    /// </summary>
    public readonly ref struct FEventListenHelper
    {
        public readonly int EvtId;
        public readonly FEvent Evt;
        public readonly Delegate Handler;

        public FEventListenHelper(FEvent evt, int evtId, Delegate handler)
        {
            Evt = evt;
            EvtId = evtId;
            Handler = handler;
        }
    }

    public static class FEventExtension
    {
        /// <summary>
        /// 立即执行监听处理。
        /// </summary>
        /// <remarks>仅适用于通过 <see cref="FEvent.PostEventHandler{T}"/> 注册的后处理监听。</remarks>
        public static FEventListenHelper Immediate<T>(this in FEventListenHelper helper, in T evtData = default, object dispatcher = null)
        {
            ((FEvent.PostEventHandler<T>)helper.Handler)(dispatcher ?? helper.Evt, evtData);
            return helper;
        }

        /// <summary>
        /// 注册事件监听生命周期管理。
        /// </summary>
        public static FEventListenHelper Managed(this in FEventListenHelper helper, ref FEventListenManaged managed)
        {
            managed.Add(helper.Evt, helper.EvtId, helper.Handler);
            return helper;
        }
    }
}
