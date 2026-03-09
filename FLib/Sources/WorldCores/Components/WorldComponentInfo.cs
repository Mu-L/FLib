// ==================== qcbf@qq.com | 2026-01-10 ====================

#nullable enable
using System;
using System.Reflection;
using FLib.WorldCores;

namespace FLib.WorldCores.Components
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
            Awake = IWorldAwake.CreateLifecycleDelegate(typeof(IWorldAwake), type, nameof(IWorldAwake.Awake));
            Destroy = IWorldAwake.CreateLifecycleDelegate(typeof(IWorldDestroy), type, nameof(IWorldDestroy.Destroy));
        }

        public bool Op(EComponentOption option) => Options != null && (Options.Options & option) == option;

        public static implicit operator WorldComponentMeta(in WorldComponentInfo info) => info.Meta;
    }
}