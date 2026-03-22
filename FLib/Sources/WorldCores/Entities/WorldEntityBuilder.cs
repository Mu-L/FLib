// ==================== qcbf@qq.com | 2026-01-09 ====================

using System.Diagnostics;
using FLib.WorldCores;
using FLib.WorldCores.Components;

namespace FLib.WorldCores.Entities
{
    public ref struct WorldEntityBuilder
    {
        public WorldCore World;
        internal PooledList<WorldComponentMeta> Components;

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityBuilder(WorldCore world) : this()
        {
            World = world;
            WorldStaticComponentMask.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityBuilder With<T>() where T : unmanaged
        {
            AddComponent(WorldComponentRegistry.GetInfo<T>(), false);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityBuilder WithMng<T>()
        {
            AddComponent(WorldComponentRegistry.GetInfo<Mng<T>>(), false);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityBuilder WithShared<T>() where T : IWorldSharedComponent
        {
            AddComponent(WorldComponentRegistry.GetInfo<T>(), true);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void AddComponent(in WorldComponentInfo info, bool isShared)
        {
            AssertNewComponent(World, info, isShared);
            Components.Add(info);
            WorldStaticComponentMask.Set(info.Meta, true);
            if (info.Options?.RequiredComponents != null)
            {
                foreach (var t in info.Options.RequiredComponents)
                {
                    ref readonly var reqInfo = ref WorldComponentRegistry.GetInfo(t);
                    AddComponent(reqInfo, reqInfo.IsShared);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initMemory">是否初始化内存, false:性能会更高,但会导致字段不是默认值</param>
        public WorldEntityId Build(bool initMemory = true)
        {
            var et = World.CreateEntity(this, WorldStaticComponentMask.HashCode(), initMemory);
            Components.Dispose();
            return et;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initMemory">是否初始化内存, false:性能会更高,但会导致字段不是默认值</param>
        public WorldEntity BuildAsEntityHelper(bool initMemory = true)
        {
            var et = World.CreateEntity(this, WorldStaticComponentMask.HashCode(), initMemory);
            Components.Dispose();
            return new WorldEntity(World, et);
        }

        [Conditional("DEBUG")]
        internal static void AssertNewComponent(WorldCore world, in WorldComponentInfo info, bool isShared)
        {
            world.Assert(!WorldStaticComponentMask.Get(info.Meta), msg: "already exist");
            world.Assert(!info.Op(EComponentOption.RejectChunk));
            if (isShared)
                world.Assert(!info.HasLifecycle, msg: "nonsupport life invoker");
            else
                world.Assert(!info.IsShared);
            world.Assert(!typeof(IWorldUpdate).IsAssignableFrom(info.Type), msg: "static component not support update");
            world.Assert(!typeof(IWorldStart).IsAssignableFrom(info.Type), msg: "static component not support start");
        }
    }
}