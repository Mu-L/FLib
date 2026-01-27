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

        public SoaComponentGroupManager(WorldCore world)
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
            var id = ComponentRegistry.GetId<T>();
            if (id >= Groups.Length)
                Groups = new ISoaComponentGroupable[id + 1];
            return (SoaComponentGroup<T>)(Groups[id] ??= new SoaComponentGroup<T>() { World = World });
        }

        /// <summary>
        /// 获取动态组件组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public SharedComponentGroup<T> GetSharedGroup<T>() where T : ISharedComponent
        {
            var id = ComponentRegistry.GetId<T>();
            if (id >= Groups.Length)
                Groups = new ISoaComponentGroupable[id + 1];
            return (SharedComponentGroup<T>)(Groups[id] ??= new SharedComponentGroup<T>() { World = World });
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
                Groups = new ISoaComponentGroupable[id + 1];
            ref var group = ref Groups[id];
            if (group == null)
            {
                group = (ISoaComponentGroupable)TypeAssistant.New(typeof(SoaComponentGroup<>).MakeGenericType(componentType));
                group.World = World;
            }

            return group;
        }

        /// <summary>
        /// 获取动态组件组
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public ISharedComponentGroupable GetSharedGroup(Type componentType)
        {
            var id = ComponentRegistry.GetId(componentType);
            if (id >= Groups.Length)
                Groups = new ISoaComponentGroupable[id + 1];
            ref var group = ref Groups[id];
            if (group == null)
            {
                group = (ISoaComponentGroupable)TypeAssistant.New(typeof(SharedComponentGroup<>).MakeGenericType(componentType));
                group.World = World;
            }

            return (ISharedComponentGroupable)group;
        }
    }
}