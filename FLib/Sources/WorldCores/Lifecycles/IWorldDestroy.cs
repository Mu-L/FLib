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
        internal static void Destroy<T>(ref byte ptr, WorldCore world, WorldEntity entity) where T : IWorldDestroy
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                comp.Destroy(world, entity);
                WorldComponentEvents<T>.OnDestroy?.Invoke(world, entity, ref comp);
            }
            catch (Exception e)
            {
                world.ThrowException($"{typeof(T)}", entity, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Destroy(WorldCore world, WorldEntity entity);
    }
}