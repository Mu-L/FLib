// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using FLib.WorldCores;
using FLib.WorldCores.Components;

namespace FLib.WorldCores.SoaComponents
{
    /// <summary>
    /// 动态组件组 管理器
    /// </summary>
    public class WorldSoaComponentGroupManager
    {
        public WorldCore World;
        public IWorldSoaComponentGroupable[] Groups = Array.Empty<IWorldSoaComponentGroupable>();

        public IWorldSoaComponentGroupable this[WorldIncrementId componentId] => Groups[componentId];

        public unsafe WorldSoaComponentGroupManager(WorldCore world)
        {
            World = world;
        }

        /// <summary>
        /// 获取动态组件组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public WorldSoaComponentGroup<T> GetGroup<T>()
        {
            var id = WorldComponentRegistry.GetId<T>().Id;
            if (id < Groups.Length && Groups[id] != null)
                return (WorldSoaComponentGroup<T>)Groups[id];
            return (WorldSoaComponentGroup<T>)GetGroup(typeof(T));
        }

        /// <summary>
        /// 获取动态组件组
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public IWorldSoaComponentGroupable GetGroup(Type componentType)
        {
            var id = WorldComponentRegistry.GetId(componentType);
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
        public IWorldSoaComponentGroupable CreateGroup(Type componentType)
        {
            ref readonly var info = ref WorldComponentRegistry.GetInfo(componentType);
            World.Assert(!info.Op(EComponentOption.RejectSoa));
            if (info.IsShared)
                return (IWorldSoaComponentGroupable)TypeAssistant.New(typeof(WorldSharedComponentGroup<>).MakeGenericType(componentType), World);
            if (typeof(IWorldUpdate).IsAssignableFrom(componentType) || typeof(IWorldStart).IsAssignableFrom(componentType))
                return (IWorldSoaComponentGroupable)TypeAssistant.New(typeof(WorldUpdateSoaComponentGroup<>).MakeGenericType(componentType), World);
            return (IWorldSoaComponentGroupable)TypeAssistant.New(typeof(WorldSoaComponentGroup<>).MakeGenericType(componentType!), World);
        }
    }
}