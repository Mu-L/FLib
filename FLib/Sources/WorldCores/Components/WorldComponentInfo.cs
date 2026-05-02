// ==================== qcbf@qq.com | 2026-01-10 ====================

#nullable enable
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using FLib.WorldCores;

namespace FLib.WorldCores.Components
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct WorldComponentInfo
    {
        public WorldComponentMeta Meta { get; }
        public readonly Type Type;
        public readonly LifecycleDelegate Awake;
        public readonly LifecycleDelegate Destroy;
        public readonly LifecycleDelegate DestroyWithComponentSelf;
        public readonly WorldComponentOptionAttribute? Options;
        public readonly IBytesPackGenericWrapper? BytesPackWrapper;
        public readonly EComponentState States;

        public bool IsShared => (States & EComponentState.Shared) != 0;
        public bool IsAwakeComponent => (States & EComponentState.AwakeComponent) != 0;
        public bool IsDestroyComponent => (States & EComponentState.DestroyComponent) != 0;
        public bool HasLifecycle => IsAwakeComponent || IsDestroyComponent;


        public WorldComponentInfo(WorldComponentMeta meta, Type type)
        {
            var flags = EComponentState.None;
            if (typeof(IWorldSharedComponent).IsAssignableFrom(type))
                flags |= EComponentState.Shared;
            Type = type;
            Meta = meta;
            Options = type.GetCustomAttribute<WorldComponentOptionAttribute>();

            Awake = IWorldAwake.CreateLifecycleDelegate(typeof(IWorldAwake), type, nameof(IWorldAwake.Awake), out var isWithComponentSelf);
            if (isWithComponentSelf)
                flags |= EComponentState.AwakeComponent;

            // destroy会同时保存 只调用全局生命周期事件和包含调用组件自身 的两个接口方法. 因为支持因为entity destroy导致的组件被移除可以选择是否还需要接收组件自身的destroy.
            Destroy = IWorldAwake.CreateLifecycleDelegate(typeof(IWorldDestroy), type, nameof(IWorldDestroy.Destroy), out isWithComponentSelf);
            if (isWithComponentSelf)
            {
                flags |= EComponentState.DestroyComponent;
                DestroyWithComponentSelf = Destroy;
                Destroy = IWorldAwake.CreateLifecycleDelegate(typeof(IWorldDestroy), type, nameof(IWorldDestroy.Destroy));
            }
            else
            {
                // 如果组件没有实现IWorldDestroy接口 就无所谓了, 两个都指向同一个方法就行了
                DestroyWithComponentSelf = Destroy;
            }

            States = flags;
            BytesPackWrapper = typeof(IBytesPackable).IsAssignableFrom(type)
                ? (IBytesPackGenericWrapper?)TypeAssistant.New(typeof(BytesPackGenericWrapper<>).MakeGenericType(type))
                : null;
        }

        public bool Op(EComponentOption option) => Options != null && (Options.Options & option) == option;

        public static implicit operator WorldComponentMeta(in WorldComponentInfo info) => info.Meta;
    }

    [Flags]
    public enum EComponentState : byte
    {
        None = 0,
        Shared = 1 << 0,
        AwakeComponent = 1 << 1,
        DestroyComponent = 1 << 2,
    }
}