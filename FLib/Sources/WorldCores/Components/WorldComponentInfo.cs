// ==================== qcbf@qq.com | 2026-01-10 ====================

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace FLib.WorldCores
{
    public readonly struct WorldComponentInfo
    {
        public WorldComponentMeta Meta { get; }
        public readonly Type Type;
        public readonly LifecycleDelegate? Awake;
        public readonly LifecycleDelegate? Destroy;
        public readonly WorldComponentOptionAttribute? Options;
        public readonly bool IsShared;

        public bool HasLifecycle => Awake != null || Destroy != null;

        public WorldComponentInfo(WorldComponentMeta meta, Type type)
        {
            IsShared = typeof(IWorldSharedComponent).IsAssignableFrom(type);
            Type = type;
            Meta = meta;
            Options = type.GetCustomAttribute<WorldComponentOptionAttribute>();
            Awake = IWorldLifecycleAwake.CreateLifecycleDelegate(typeof(IWorldLifecycleAwake), type, nameof(IWorldLifecycleAwake.Awake));
            Destroy = IWorldLifecycleAwake.CreateLifecycleDelegate(typeof(IWorldLifecycleDestroy), type, nameof(IWorldLifecycleDestroy.Destroy));
        }

        public bool Op(EComponentOption option) => Options != null && (Options.Options & option) == option;

        public static implicit operator WorldComponentMeta(in WorldComponentInfo info) => info.Meta;
    }
}