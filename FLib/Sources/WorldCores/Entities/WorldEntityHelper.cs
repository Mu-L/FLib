// ==================== qcbf@qq.com | 2026-03-02 ====================

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using FLib.WorldCores;
using FLib.WorldCores.Components;

namespace FLib.WorldCores.Entities
{
    /// <summary>
    /// 实体辅助类，提供对实体的快速访问。
    /// </summary>
    public readonly struct WorldEntityHelper
    {
        public readonly WorldHandle WorldHandle;
        public readonly WorldEntity Entity;
        public WorldCore World => WorldHandle.World;
        public bool IsEmpty => Entity.IsEmpty || WorldHandle.IsEmpty;
        public override string ToString() => Entity.ToString();

        public WorldEntityHelper(WorldHandle world, WorldEntity entity)
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
        public void SetShared<T>(in T val) where T : IWorldSharedComponent => World.SetShared(Entity, val);

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
        public int SetDyn<T>(in T component) => World.SetDyn(Entity, component);

        /// <summary>
        /// 设置实体的动态组件（通过类型和值）。
        /// </summary>
        public int SetDyn(object component, Type type) => World.SetDyn(Entity, type, component);

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

        #region Events

        /// <summary>
        /// 分发事件（后处理）。
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void DispatchEvent<T>(in T evtData, object dispatcher = null)
            => World.Entities.DispatchEvent(Entity.Id)?.DispatchEvent(evtData, dispatcher);

        /// <summary>
        /// 按 ID 分发事件（后处理）。
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void DispatchEventById<T>(int evtId, in T evtData, object dispatcher = null)
            => World.Entities.DispatchEvent(Entity.Id)?.DispatchEventById(evtId, evtData, dispatcher);

        /// <summary>
        /// 按 ID 分发空事件（后处理）。
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public void DispatchEventById(int evtId, object dispatcher = null)
            => World.Entities.DispatchEvent(Entity.Id)?.DispatchEventById(evtId, dispatcher);

        /// <summary>
        /// 分发前置事件，返回是否继续执行。
        /// </summary>
        /// <returns>is continuing run</returns>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public bool DispatchPreEvent<T>(ref T evtData, object dispatcher = null)
            => World.Entities.DispatchEvent(Entity.Id)?.DispatchPreEvent(ref evtData, dispatcher) != false;

        /// <summary>
        /// 按 ID 分发前置事件，返回是否继续执行。
        /// </summary>
        /// <returns>is continuing run</returns>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public bool DispatchPreEventById<T>(int evtId, ref T evtData, object dispatcher = null)
            => World.Entities.DispatchEvent(Entity.Id)?.DispatchPreEventById(evtId, ref evtData, dispatcher) != false;

        /// <summary>
        /// 按 ID 分发前置空事件，返回是否继续执行。
        /// </summary>
        /// <returns>is continuing run</returns>
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        public bool DispatchPreEventById(int evtId, object dispatcher = null)
            => World.Entities.DispatchEvent(Entity.Id)?.DispatchPreEventById(evtId, dispatcher) != false;

        /// <summary>
        /// 监听事件（后处理）。
        /// </summary>
        public FEventListenHelper<T> ListenEvent<T>(FEvent.PostEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
            => World.Entities.Event(Entity.Id).ListenEvent(handler, priority, isListenOnce);

        /// <summary>
        /// 监听事件（后处理），指定事件 ID。
        /// </summary>
        public FEventListenHelper<T> ListenEvent<T>(int evtId, FEvent.PostEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
            => World.Entities.Event(Entity.Id).ListenEvent(evtId, handler, priority, isListenOnce);

        /// <summary>
        /// 监听空事件（后处理）。
        /// </summary>
        public FEventListenHelper<object> ListenEvent(int evtId, FEvent.PostEventHandler<object> handler, short priority = 0, bool isListenOnce = false)
            => World.Entities.Event(Entity.Id).ListenEvent(evtId, handler, priority, isListenOnce);

        /// <summary>
        /// 监听前置事件。
        /// </summary>
        public void ListenPreEvent<T>(FEvent.PreEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
            => World.Entities.Event(Entity.Id).ListenPreEvent(handler, priority, isListenOnce);

        /// <summary>
        /// 监听前置事件，指定事件 ID。
        /// </summary>
        public void ListenPreEvent<T>(int evtId, FEvent.PreEventHandler<T> handler, short priority = 0, bool isListenOnce = false)
            => World.Entities.Event(Entity.Id).ListenPreEvent(evtId, handler, priority, isListenOnce);

        /// <summary>
        /// 监听空前置事件。
        /// </summary>
        public void ListenPreEvent(int evtId, FEvent.PreEventHandler<object> handler, short priority = 0, bool isListenOnce = false)
            => World.Entities.Event(Entity.Id).ListenPreEvent(evtId, handler, priority, isListenOnce);

        /// <summary>
        /// 取消监听事件。
        /// </summary>
        public void UnlistenEvent<T>(FEvent.PostEventHandler<T> handler)
            => World.Entities.Event(Entity.Id).UnlistenEvent(handler);

        /// <summary>
        /// 取消监听前置事件。
        /// </summary>
        public void UnlistenEvent<T>(FEvent.PreEventHandler<T> handler)
            => World.Entities.Event(Entity.Id).UnlistenEvent(handler);

        /// <summary>
        /// 取消监听指定 ID 的事件。
        /// </summary>
        public void UnlistenEvent<T>(int evtId, FEvent.PostEventHandler<T> handler)
            => World.Entities.Event(Entity.Id).UnlistenEvent(evtId, handler);

        /// <summary>
        /// 取消监听指定 ID 的前置事件。
        /// </summary>
        public void UnlistenEvent<T>(int evtId, FEvent.PreEventHandler<T> handler)
            => World.Entities.Event(Entity.Id).UnlistenEvent(evtId, handler);

        /// <summary>
        /// 检查是否已监听事件。
        /// </summary>
        public bool IsListenEvent<T>(FEvent.PostEventHandler<T> handler)
            => World.Entities.DispatchEvent(Entity.Id)?.IsListenEvent(handler) == true;

        /// <summary>
        /// 检查是否已监听前置事件。
        /// </summary>
        public bool IsListenEvent<T>(FEvent.PreEventHandler<T> handler)
            => World.Entities.DispatchEvent(Entity.Id)?.IsListenEvent(handler) == true;

        /// <summary>
        /// 检查是否已监听指定 ID 的事件。
        /// </summary>
        public bool IsListenEvent<T>(int evtId, FEvent.PostEventHandler<T> handler)
            => World.Entities.DispatchEvent(Entity.Id)?.IsListenEvent(evtId, handler) == true;

        /// <summary>
        /// 检查是否已监听指定 ID 的前置事件。
        /// </summary>
        public bool IsListenEvent<T>(int evtId, FEvent.PreEventHandler<T> handler)
            => World.Entities.DispatchEvent(Entity.Id)?.IsListenEvent(evtId, handler) == true;

        /// <summary>
        /// 清空所有事件监听。
        /// </summary>
        public void ClearListenEvents()
            => World.Entities.Event(Entity.Id).ClearListenEvents();

        #endregion


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveSelf()
        {
            World.RemoveEntity(Entity);
        }

        public static implicit operator WorldEntity(in WorldEntityHelper helper) => helper.Entity;
        public static implicit operator WorldCore(in WorldEntityHelper helper) => helper.World;
    }
}