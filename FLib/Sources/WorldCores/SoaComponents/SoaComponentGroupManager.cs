// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using System.Diagnostics;

namespace FLib.WorldCores
{
    /// <summary>
    /// 动态组件组 管理器
    /// </summary>
    public class SoaComponentGroupManager
    {
        public WorldCore World;
        public ISoaComponentGroupable[] Groups = Array.Empty<ISoaComponentGroupable>();

        public ISoaComponentGroupable this[IncrementId componentId] => Groups[componentId];

        public unsafe SoaComponentGroupManager(WorldCore world)
        {
            World = world;
        }

        /// <summary>
        /// 获取动态组件组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public SoaComponentGroup<T> GetGroup<T>()
        {
            var id = ComponentRegistry.GetId<T>().Id;
            if (id < Groups.Length && Groups[id] != null)
                return (SoaComponentGroup<T>)Groups[id];
            return (SoaComponentGroup<T>)GetGroup(typeof(T));
        }

        /// <summary>
        /// 获取动态组件组
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public ISoaComponentGroupable GetGroup(Type componentType)
        {
            var id = ComponentRegistry.GetId(componentType);
            if (id >= Groups.Length)
            {
                Array.Resize(ref Groups, id + 1);
                return Groups[id] = CreateGroup(componentType);
            }

            return Groups[id] ??= CreateGroup(componentType);
        }

        /// <summary>
        /// 
        /// </summary>
        public ISoaComponentGroupable CreateGroup(Type componentType)
        {
            ref readonly var info = ref ComponentRegistry.GetInfo(componentType);
            World.Assert(!info.Op(EComponentOption.RejectSoa));
            if (info.IsShared)
                return (ISoaComponentGroupable)TypeAssistant.New(typeof(SharedComponentGroup<>).MakeGenericType(componentType), World);
            if (typeof(ILifecycleUpdate).IsAssignableFrom(componentType) || typeof(ILifecycleStart).IsAssignableFrom(componentType))
                return (ISoaComponentGroupable)TypeAssistant.New(typeof(UpdateSoaComponentGroup<>).MakeGenericType(componentType), World);
            return (ISoaComponentGroupable)TypeAssistant.New(typeof(SoaComponentGroup<>).MakeGenericType(componentType!), World);
        }
    }
}