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
        public readonly LifecycleSystemHelper Lifecycle;
        public readonly ComponentOptionAttribute? Options;
        public readonly bool IsShared;

        public ComponentInfo(ComponentMeta meta, Type type)
        {
            IsShared = typeof(ISharedComponent).IsAssignableFrom(type);
            Type = type;
            Meta = meta;
            Options = type.GetCustomAttribute<ComponentOptionAttribute>();
            Lifecycle = new LifecycleSystemHelper(type);
        }

        public static implicit operator ComponentMeta(in ComponentInfo info) => info.Meta;
    }
}