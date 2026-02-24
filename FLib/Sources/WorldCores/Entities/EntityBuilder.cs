// ==================== qcbf@qq.com | 2026-01-09 ====================

using System;
using System.Diagnostics;

namespace FLib.WorldCores
{
    public ref struct EntityBuilder
    {
        public WorldCore World;
        internal PooledList<ComponentData> ComponentDatas;

        public readonly struct ComponentData
        {
            public readonly ComponentMeta Meta;
            public readonly bool IsShared;
            public readonly LifeInvokers.Delegate Invoker;

            public ComponentData(ComponentMeta meta, bool isShared, LifeInvokers.Delegate invoker)
            {
                Meta = meta;
                IsShared = isShared;
                Invoker = invoker;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder(WorldCore world) : this()
        {
            World = world;
            StaticComponentMask.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder With<T>() where T : unmanaged
        {
            var meta = ComponentRegistry.GetMeta<T>();
            Debug.Assert(!StaticComponentMask.Get(meta), "already exist");
            ComponentDatas.Add(new ComponentData(meta, false, ComponentGenericMap<T>.Info.ComponentAwake));
            StaticComponentMask.Set(meta, true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithMng<T>()
        {
            var meta = ComponentRegistry.GetMeta<Mng<T>>();
            Debug.Assert(!StaticComponentMask.Get(meta), "already exist");
            ComponentDatas.Add(new ComponentData(meta, false, ComponentGenericMap<Mng<T>>.Info.ComponentAwake));
            StaticComponentMask.Set(meta, true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithShared<T>() where T : ISharedComponent
        {
            var meta = ComponentRegistry.GetMeta<T>();
            Debug.Assert(!StaticComponentMask.Get(meta), "already exist");
            ComponentDatas.Add(new ComponentData(meta, true, ComponentGenericMap<T>.Info.ComponentAwake));
            StaticComponentMask.Set(meta, true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initMemory">是否初始化内存, false:性能会更高,但会导致字段不是默认值</param>
        public Entity Build(bool initMemory = true)
        {
            var et = World.CreateEntity(this, StaticComponentMask.HashCode(), initMemory);
            ComponentDatas.Dispose();
            return et;
        }
    }
}