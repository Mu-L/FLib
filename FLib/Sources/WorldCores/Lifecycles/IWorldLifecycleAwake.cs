// ==================== qcbf@qq.com | 2026-02-26 ====================

#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    /// <summary>
    /// 
    /// </summary>
    public delegate void LifecycleDelegate(ref byte ptr, WorldCore world, WorldEntity entity);

    /// <summary>
    /// 组件自身作为system, 组件自身收到awake的调用
    /// </summary>
    public interface IWorldLifecycleAwake
    {
        /// <summary>
        /// 
        /// </summary>
        internal static void Awake<T>(ref byte ptr, WorldCore world, WorldEntity entity) where T : IWorldLifecycleAwake
        {
            try
            {
                ref var comp = ref Unsafe.As<byte, T>(ref ptr);
                comp.Awake(world, entity);
                WorldComponentEvents<T>.OnAwake?.Invoke(world, entity, ref comp);
            }
            catch (Exception e)
            {
                world.ThrowException(typeof(T), entity, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal static LifecycleDelegate? CreateLifecycleDelegate(Type interfaceType, Type type, string name)
        {
            if (!interfaceType.IsAssignableFrom(type))
                return null;
            var mi = interfaceType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return mi.CreateDelegate<LifecycleDelegate>();
        }


        /// <summary>
        /// 
        /// </summary>
        void Awake(WorldCore world, WorldEntity entity);
    }
}