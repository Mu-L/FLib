// ==================== qcbf@qq.com | 2026-02-26 ====================

#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;

namespace FLib.WorldCores
{
    /// <summary>
    /// 
    /// </summary>
    public delegate void LifecycleDelegate(ref byte ptr, WorldCore world, WorldEntityId eId);

    /// <summary>
    /// 组件自身作为system, 组件自身收到awake的调用
    /// </summary>
    public interface IWorldAwake
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void Awake<T>(ref byte ptr, WorldCore world, WorldEntityId entityId)
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                WorldComponentEvents.OnAwake?.Invoke(world, entityId, typeof(T), ref ptr);
                WorldComponentEvents<T>.OnAwake?.Invoke(world, entityId, ref comp);
            }
            catch (Exception e)
            {
                world.ThrowException(typeof(T), entityId, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static void AwakeWithComponentSelf<T>(ref byte ptr, WorldCore world, WorldEntityId entityId) where T : IWorldAwake
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                WorldComponentEvents.OnAwake?.Invoke(world, entityId, typeof(T), ref ptr);
                WorldComponentEvents<T>.OnAwake?.Invoke(world, entityId, ref comp);
                comp.OnComponentAwake(world, entityId);
            }
            catch (Exception e)
            {
                world.ThrowException(typeof(T), entityId, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static LifecycleDelegate CreateLifecycleDelegate(Type interfaceType, Type type, string name, out bool isWithComponentSelf)
        {
            isWithComponentSelf = interfaceType.IsAssignableFrom(type);
            if (isWithComponentSelf)
                name += "WithComponentSelf";
            return CreateLifecycleDelegate(interfaceType, type, name);
        }

        /// <summary>
        /// 
        /// </summary>
        internal static LifecycleDelegate CreateLifecycleDelegate(Type interfaceType, Type type, string name)
        {
            var mi = interfaceType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return (LifecycleDelegate)mi.CreateDelegate(typeof(LifecycleDelegate));
        }


        /// <summary>
        /// 
        /// </summary>
        void OnComponentAwake(WorldCore world, WorldEntityId entityId);
    }
}