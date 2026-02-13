// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using System.Diagnostics;
using System.Reflection;

namespace FLib.WorldCores
{
    public readonly struct ComponentInfo
    {
        public ComponentMeta Meta { get; }
        public readonly Type Type;
        public readonly LifeInvoker.Delegate ComponentAwake;
        public readonly LifeInvoker.Delegate ComponentDestroy;

        // public readonly LifeInvoker.Delegate ComponentEnable; // working
        // public readonly LifeInvoker.Delegate ComponentDisable;

        public ComponentInfo(ComponentMeta meta, Type type)
        {
            Type = type;
            Meta = meta;
            ComponentAwake = typeof(IAwakeInvokable).IsAssignableFrom(type) ? LifeInvoker.Make(type, nameof(IAwakeInvokable.Awake)) : null;
            ComponentDestroy = typeof(IDestroyInvokable).IsAssignableFrom(type) ? LifeInvoker.Make(type, nameof(IDestroyInvokable.Destroy)) : null;
        }
    }
}