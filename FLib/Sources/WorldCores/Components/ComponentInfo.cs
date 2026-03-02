// ==================== qcbf@qq.com | 2026-01-10 ====================

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace FLib.WorldCores
{
    public readonly struct ComponentInfo
    {
        public ComponentMeta Meta { get; }
        public readonly Type Type;
        public readonly LifecycleSystemDelegate? Awake;
        public readonly LifecycleSystemDelegate? Destroy;
        public readonly ComponentOptionAttribute? Options;
        public readonly bool IsShared;

        public bool HasLifecycle => Awake != null || Destroy != null;

        public ComponentInfo(ComponentMeta meta, Type type)
        {
            IsShared = typeof(ISharedComponent).IsAssignableFrom(type);
            Type = type;
            Meta = meta;
            Options = type.GetCustomAttribute<ComponentOptionAttribute>();
            Awake = IAwakeSystem.CreateLifecycleSystemDelegate(typeof(IAwakeSystem), type, nameof(IAwakeSystem.Awake));
            Destroy = IAwakeSystem.CreateLifecycleSystemDelegate(typeof(IDestroySystem), type, nameof(IDestroySystem.Destroy));
        }

        public static implicit operator ComponentMeta(in ComponentInfo info) => info.Meta;
    }
}