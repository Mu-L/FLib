// ==================== qcbf@qq.com | 2026-03-02 ====================

using System;
using System.Collections;

namespace FLib.WorldCores
{
    /// <summary>
    /// 实体辅助类，提供对实体的快速访问。
    /// </summary>
    public readonly struct EntityHelper
    {
        public readonly WorldHandle WorldHandle;
        public readonly Entity Entity;
        public WorldCore World => WorldHandle.World;
        public bool IsEmpty => Entity.IsEmpty || WorldHandle.IsEmpty;
        public override string ToString() => Entity.ToString();

        public EntityHelper(WorldHandle world, Entity entity)
        {
            WorldHandle = world;
            Entity = entity;
        }

        #region Simple

        /// <summary>
        /// 获取实体的组件（静态或动态）。
        /// </summary>
        public ref T Get<T>() => ref World.Get<T>(Entity);

        /// <summary>
        /// 获取实体的组件（静态或动态），通过类型参数。
        /// </summary>
        public object Get(Type componentType) => World.Get(Entity, componentType);

        /// <summary>
        /// 设置实体的组件（如果存在静态则写入静态，否则写入动态）。
        /// </summary>
        public void Set<T>(in T component) => World.Set(Entity, component);

        /// <summary>
        /// 移除实体上的组件（只能用于动态组件）。
        /// </summary>
        public void Remove<T>() => World.Remove<T>(Entity);

        /// <summary>
        /// 判断实体是否包含某组件（静态或动态）。
        /// </summary>
        public bool Has<T>() => World.Has<T>(Entity);

        /// <summary>
        /// 获得实体上所有组件的列表。
        /// </summary>
        public IList GetAll(IList result = null) => World.GetAll(Entity, result);

        #endregion

        #region Static

        /// <summary>
        /// 获取实体的静态组件并返回一个 <see cref="Ref{T}"/> 包装。
        /// </summary>
        public Ref<T> GetSta<T>() where T : unmanaged => World.GetSta<T>(Entity);

        /// <summary>
        /// 获取实体的静态组件并返回对组件的引用。
        /// </summary>
        public ref T GetStaRef<T>() where T : unmanaged => ref World.GetStaRef<T>(Entity);

        /// <summary>
        /// 获取实体的静态管理组件（<see cref="Mng{T}"/>）。
        /// </summary>
        public Mng<T> GetStaMng<T>() => World.GetStaMng<T>(Entity);

        /// <summary>
        /// 设置实体的静态组件值。
        /// </summary>
        public void SetSta<T>(in T val) where T : unmanaged => World.SetSta(Entity, val);

        /// <summary>
        /// 设置实体的静态管理组件值。
        /// </summary>
        public void SetStaMng<T>(in T val) => World.SetStaMng(Entity, val);

        /// <summary>
        /// 设置实体的共享组件值。
        /// </summary>
        public void SetShared<T>(in T val) where T : ISharedComponent => World.SetShared(Entity, val);

        /// <summary>
        /// 检查实体是否包含指定的静态组件。
        /// </summary>
        public bool HasSta<T>() where T : unmanaged => World.HasSta<T>(Entity);

        /// <summary>
        /// 检查实体是否包含指定的静态管理组件。
        /// </summary>
        public bool HasStaMng<T>() => World.HasStaMng<T>(Entity);

        /// <summary>
        /// 检查实体是否包含指定类型的静态组件。
        /// </summary>
        public bool HasSta(Type componentType) => World.HasSta(Entity, componentType);

        /// <summary>
        /// 获取实体的指定类型静态组件（非泛型）。
        /// </summary>
        public object GetSta(Type componentType) => World.GetSta(Entity, componentType);

        #endregion

        #region Dynamic

        /// <summary>
        /// 获取实体的动态组件并返回引用。
        /// </summary>
        public ref T GetDyn<T>() => ref World.GetDyn<T>(Entity);

        /// <summary>
        /// 获取实体的动态组件（非泛型）。
        /// </summary>
        public object GetDyn(Type type) => World.GetDyn(Entity, type);

        /// <summary>
        /// 设置实体的动态组件。
        /// </summary>
        public void SetDyn<T>(in T component) => World.SetDyn(Entity, component);

        /// <summary>
        /// 设置实体的动态组件（通过类型和值）。
        /// </summary>
        public void SetDyn(object component, Type type) => World.SetDyn(Entity, component, type);

        /// <summary>
        /// 移除实体的动态组件。
        /// </summary>
        public void RemoveDyn<T>() => World.RemoveDyn<T>(Entity);

        /// <summary>
        /// 移除实体的动态组件（通过类型）。
        /// </summary>
        public void RemoveDyn(Type type) => World.RemoveDyn(Entity, type);

        /// <summary>
        /// 检查实体是否包含指定的动态组件。
        /// </summary>
        public bool HasDyn<T>() => World.HasDyn<T>(Entity);

        /// <summary>
        /// 检查实体是否包含指定类型的动态组件。
        /// </summary>
        public bool HasDyn(Type type) => World.HasDyn(Entity, type);

        #endregion


        public static implicit operator Entity(in EntityHelper helper) => helper.Entity;
        public static implicit operator WorldCore(in EntityHelper helper) => helper.World;
    }
}