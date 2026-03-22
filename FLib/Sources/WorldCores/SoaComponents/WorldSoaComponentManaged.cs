// ==================== qcbf@qq.com | 2026-03-21 ====================

using System;
using FLib.Sources.WorldCores.Components;
using FLib.WorldCores.Components;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    /// <summary>
    /// 动态组件托管
    /// </summary>
    public struct WorldSoaComponentManaged : IDisposable
    {
        public WorldEntity Entity;
        public PooledList<ComponentHandle> Components;

        public WorldCore World => Entity.World;
        public bool IsEmpty => !Components.IsInitialized;

        /// <summary>
        ///  
        /// </summary>
        public WorldSoaComponentManaged(WorldEntity entity)
        {
            Entity = entity;
            Components = default;
        }

        /// <summary>
        /// 添加一个动态组件
        /// </summary>
        public int Add<T>(in T component)
        {
            var idx = World.Soa.GetGroup<T>().Alloc(Entity.EntityId, component);
            Components.Add(new ComponentHandle(idx, WorldComponentRegistry.GetId<T>()));
            return idx;
        }

        /// <summary>
        /// 
        /// </summary>
        public ref T Get<T>()
        {
            var id = WorldComponentRegistry.GetId<T>();
            foreach (var item in Components)
            {
                if (item.TypeId == id)
                    return ref World.Soa.GetGroup<T>()[item.Index];
            }

            throw new WorldCoreException(Entity, $"{Entity} has no component {typeof(T)}");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            try
            {
                foreach (var item in Components)
                    World.Soa.GetGroup(item.TypeId).Free(Entity, item.Index, false);
            }
            finally
            {
                Components.Dispose();
            }
        }
    }
}