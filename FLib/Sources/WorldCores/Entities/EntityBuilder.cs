// ==================== qcbf@qq.com | 2026-01-09 ====================

using System;
using System.Diagnostics;

namespace FLib.WorldCores
{
    public ref struct EntityBuilder
    {
        public WorldCore World;
        internal PooledList<ComponentMeta> Components;

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
            AssertNewComponent(World, typeof(T));
            var meta = ComponentRegistry.GetMeta<T>();
            World.Assert(!StaticComponentMask.Get(meta), msg: "already exist");
            Components.Add(meta);
            StaticComponentMask.Set(meta, true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithMng<T>()
        {
            AssertNewComponent(World, typeof(T));
            var meta = ComponentRegistry.GetMeta<Mng<T>>();
            World.Assert(!StaticComponentMask.Get(meta), msg: "already exist");
            Components.Add(meta);
            StaticComponentMask.Set(meta, true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithShared<T>() where T : ISharedComponent
        {
            var meta = ComponentRegistry.GetMeta<T>();
            World.Assert(!StaticComponentMask.Get(meta), msg: "already exist");
            World.Assert(ComponentRegistry.GetInfo<T>().Lifecycle.IsEmpty, msg: "nonsupport life invoker");
            Components.Add(meta);
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
            Components.Dispose();
            return et;
        }

        [Conditional("DEBUG")]
        private static void AssertNewComponent(WorldCore world, Type type)
        {
            world.Assert(!ComponentRegistry.GetInfo(type).IsShared);
            world.Assert(!typeof(IUpdateSystem).IsAssignableFrom(type));
            world.Assert(!typeof(IUpdateStartSystem).IsAssignableFrom(type));
        }
    }
}