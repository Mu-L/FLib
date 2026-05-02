// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;

namespace FLib.WorldCores
{
    public interface IWorldDestroy
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void Destroy<T>(ref byte ptr, WorldCore world, WorldEntityId entityId)
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                WorldComponentEvents.OnDestroy?.Invoke(world, entityId, typeof(T), ref ptr);
                WorldComponentEvents<T>.OnDestroy?.Invoke(world, entityId, ref comp);
            }
            catch (Exception e)
            {
                world.ThrowException($"{typeof(T)}", entityId, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static void DestroyWithComponentSelf<T>(ref byte ptr, WorldCore world, WorldEntityId entityId) where T : IWorldDestroy
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                WorldComponentEvents.OnDestroy?.Invoke(world, entityId, typeof(T), ref ptr);
                WorldComponentEvents<T>.OnDestroy?.Invoke(world, entityId, ref comp);
                comp.OnComponentDestroy(world, entityId);
            }
            catch (Exception e)
            {
                world.ThrowException($"{typeof(T)}", entityId, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void OnComponentDestroy(WorldCore world, WorldEntityId entityId);
    }
}