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
            With(ComponentRegistry.GetMeta<T>(), false);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithMng<T>()
        {
            With(ComponentRegistry.GetMeta<Mng<T>>(), false);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithShared<T>() where T : ISharedComponent
        {
            With(ComponentRegistry.GetMeta<T>(), true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        private void With(ComponentMeta meta, bool isShared)
        {
            AssertNewComponent(World, meta, isShared);
            Components.Add(meta);
            StaticComponentMask.Set(meta, true);
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
        internal static void AssertNewComponent(WorldCore world, ComponentMeta meta, bool isShared)
        {
            var type = meta.Type;
            ref readonly var info = ref ComponentRegistry.GetInfo(type);
            world.Assert(!StaticComponentMask.Get(info.Meta), msg: "already exist");
            world.Assert(!info.Op(EComponentOption.RejectChunk));
            if (isShared)
                world.Assert(!info.HasLifecycle, msg: "nonsupport life invoker");
            else
                world.Assert(!info.IsShared);
            world.Assert(!typeof(ILifecycleUpdate).IsAssignableFrom(type));
            world.Assert(!typeof(ILifecycleStart).IsAssignableFrom(type));
        }
    }
}