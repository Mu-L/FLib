// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public interface IAwakeSystem
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void Awake<T>(ref byte ptr, WorldCore world, Entity entity) where T : IAwakeSystem
        {
            try
            {
                Unsafe.As<byte, T>(ref ptr).Awake(world, entity);
            }
            catch (Exception e)
            {
                throw new Exception($"{entity} {typeof(T)} {e}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Awake(WorldCore world, Entity entity);
    }
}