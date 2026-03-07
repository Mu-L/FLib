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
            AddComponent(ComponentRegistry.GetInfo<T>(), false);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithMng<T>()
        {
            AddComponent(ComponentRegistry.GetInfo<Mng<T>>(), false);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public EntityBuilder WithShared<T>() where T : ISharedComponent
        {
            AddComponent(ComponentRegistry.GetInfo<T>(), true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void AddComponent(in ComponentInfo info, bool isShared)
        {
            AssertNewComponent(World, info, isShared);
            Components.Add(info);
            StaticComponentMask.Set(info.Meta, true);
            if (info.Options?.RequiredComponents != null)
            {
                foreach (var t in info.Options.RequiredComponents)
                {
                    ref readonly var reqInfo = ref ComponentRegistry.GetInfo(t);
                    AddComponent(reqInfo, reqInfo.IsShared);
                }
            }
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
        internal static void AssertNewComponent(WorldCore world, in ComponentInfo info, bool isShared)
        {
            world.Assert(!StaticComponentMask.Get(info.Meta), msg: "already exist");
            world.Assert(!info.Op(EComponentOption.RejectChunk));
            if (isShared)
                world.Assert(!info.HasLifecycle, msg: "nonsupport life invoker");
            else
                world.Assert(!info.IsShared);
            world.Assert(!typeof(ILifecycleUpdate).IsAssignableFrom(info.Type));
            world.Assert(!typeof(ILifecycleStart).IsAssignableFrom(info.Type));
        }
    }
}