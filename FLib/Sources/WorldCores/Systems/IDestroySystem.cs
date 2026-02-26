// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public interface IDestroySystem
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void Destroy<T>(ref byte ptr, WorldCore world, Entity entity) where T : IDestroySystem
        {
            try
            {
                Unsafe.As<byte, T>(ref ptr).Destroy(world, entity);
            }
            catch (Exception e)
            {
                throw new Exception($"{entity} {typeof(T)} {e}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Destroy(WorldCore world, Entity entity);
    }
}