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
        internal static void Destroy<T>(ref byte ptr, WorldCore world, WorldEntityId eId) where T : IWorldDestroy
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                comp.OnDestroy(world, eId);
                WorldComponentEvents<T>.OnDestroy?.Invoke(world, eId, ref comp);
            }
            catch (Exception e)
            {
                world.ThrowException($"{typeof(T)}", eId, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void OnDestroy(WorldCore world, WorldEntityId eId);
    }
}