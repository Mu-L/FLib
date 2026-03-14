// ==================== qcbf@qq.com | 2025-07-25 ====================

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using FLib;
using FLib.Unity.Utilities;
using UnityEngine;

namespace FLib.Unity
{
    public static class FEventExtension
    {
        public static FEventListenHelper<T> UnlistenManaged<T>(this in FEventListenHelper<T> helper, ref FEventListenManaged managed)
        {
            managed.Push(helper.Evt, helper.EvtId, helper.Handler);
            return helper;
        }

        public static FEventListenHelper<T> UnlistenOnDestroy<T>(this in FEventListenHelper<T> helper, GameObject target)
        {
            target.GetOrAddComponent<FEventUnlistenOnDestroyAndDisable>().OnDestoryManaged.Push(helper.Evt, helper.EvtId, helper.Handler);
            return helper;
        }

        public static FEventListenHelper<T> UnlistenOnDestroy<T>(this in FEventListenHelper<T> helper, Component target)
        {
            target.GetOrAddComponent<FEventUnlistenOnDestroyAndDisable>().OnDestoryManaged.Push(helper.Evt, helper.EvtId, helper.Handler);
            return helper;
        }

        public static FEventListenHelper<T> UnlistenOnDisable<T>(this in FEventListenHelper<T> helper, GameObject target)
        {
            target.GetOrAddComponent<FEventUnlistenOnDestroyAndDisable>().OnDisableManaged.Push(helper.Evt, helper.EvtId, helper.Handler);
            return helper;
        }

        public static FEventListenHelper<T> UnlistenOnDisable<T>(this in FEventListenHelper<T> helper, Component target)
        {
            target.GetOrAddComponent<FEventUnlistenOnDestroyAndDisable>().OnDisableManaged.Push(helper.Evt, helper.EvtId, helper.Handler);
            return helper;
        }
    }
}
