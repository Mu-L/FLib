// ==================== qcbf@qq.com | 2026-02-13 ====================

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public interface IAwakeInvokable
    {
        void Awake(WorldCore world, Entity entity);
    }

    public interface IDestroyInvokable
    {
        void Destroy(WorldCore world, Entity entity);
    }

    public static class LifeInvokers
    {
        public delegate void Delegate(ref byte ptr, WorldCore world, Entity entity);

        internal static void Awake<T>(ref byte ptr, WorldCore world, Entity entity) where T : IAwakeInvokable
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

        internal static void Destroy<T>(ref byte ptr, WorldCore world, Entity entity) where T : IDestroyInvokable
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

        // internal static void ComponentEnable<T>(ref byte ptr, WorldCore world, Entity entity) where T : IComponentEnable
        // {
        //     try
        //     {
        //         Unsafe.As<byte, T>(ref ptr).ComponentEnable(world, entity);
        //     }
        //     catch (Exception e)
        //     {
        //         throw new Exception($"{entity} {typeof(T))} {e}");
        //     }
        // }
        // 
        // internal static void ComponentDisable<T>(ref byte ptr, WorldCore world, Entity entity) where T : IComponentDisable
        // {
        //     try
        //     {
        //         Unsafe.As<byte, T>(ref ptr).ComponentDisable(world, entity);
        //     }
        //     catch (Exception e)
        //     {
        //         throw new Exception($"{entity} {typeof(T))} {e}");
        //     }
        // }


        /// <summary>
        /// 
        /// </summary>
        public static void Get(Type type, out Delegate awake, out Delegate destroy)
        {
            awake = typeof(IAwakeInvokable).IsAssignableFrom(type) ? CreateDelegate(type, nameof(IAwakeInvokable.Awake)) : null;
            destroy = typeof(IDestroyInvokable).IsAssignableFrom(type) ? CreateDelegate(type, nameof(IDestroyInvokable.Destroy)) : null;
        }

        /// <summary>
        /// 
        /// </summary>
        internal static Delegate CreateDelegate(Type type, string name)
        {
            var mi = typeof(LifeInvokers).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
            return mi.CreateDelegate<Delegate>();
        }
    }
}