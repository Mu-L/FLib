// ==================== qcbf@qq.com | 2026-02-26 ====================

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    /// <summary>
    /// 组件自身作为system, 组件自身收到awake的调用
    /// </summary>
    public interface IAwakeSystem
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void AwakeOneself<T>(ref byte ptr, WorldCore world, Entity entity) where T : IAwakeSystem
        {
            try
            {
                Unsafe.As<byte, T>(ref ptr).Awake(world, entity);
            }
            catch (Exception e)
            {
                world.ThrowException(typeof(T), entity, e);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        void Awake(WorldCore world, Entity entity);
    }

    /// <summary>
    /// 通用system, 当指定的组件被添加时会执行
    /// </summary>
    public interface IAwakeSystem<T>
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void AwakeExtension(object[] extensionSystems, ref byte component, WorldCore world, Entity entity)
        {
            ref var comp = ref Unsafe.As<byte, T>(ref component);
            foreach (IAwakeSystem<T> exSys in extensionSystems)
            {
                try
                {
                    exSys.Awake(world, entity, ref comp);
                }
                catch (Exception e)
                {
                    world.ThrowException(exSys.GetType(), entity, e);
                }
            }
        }

        void Awake(WorldCore world, Entity entity, ref T value);
    }
}