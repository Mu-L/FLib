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
        public readonly LifecycleDelegate? Awake;
        public readonly LifecycleDelegate? Destroy;
        public readonly ComponentOptionAttribute? Options;
        public readonly bool IsShared;

        public bool HasLifecycle => Awake != null || Destroy != null;

        public ComponentInfo(ComponentMeta meta, Type type)
        {
            IsShared = typeof(ISharedComponent).IsAssignableFrom(type);
            Type = type;
            Meta = meta;
            Options = type.GetCustomAttribute<ComponentOptionAttribute>();
            Awake = ILifecycleAwake.CreateLifecycleDelegate(typeof(ILifecycleAwake), type, nameof(ILifecycleAwake.Awake));
            Destroy = ILifecycleAwake.CreateLifecycleDelegate(typeof(ILifecycleDestroy), type, nameof(ILifecycleDestroy.Destroy));
        }

        public bool Op(EComponentOption option) => Options != null && (Options.Options & option) == option;

        public static implicit operator ComponentMeta(in ComponentInfo info) => info.Meta;
    }
}