// ==================== qcbf@qq.com | 2026-03-04 ====================

#nullable enable
using System;
using System.Collections.Generic;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    /// <summary>  </summary>
    public struct WorldEntityContainer
    {
        public readonly WorldCore World;
        public WorldEntityInfo[] EntityInfos;
        public EntityEvent?[] Events;
        public StableIndexAllocator IndexAllocator;

        public int Count => IndexAllocator.Count;
        public readonly ref WorldEntityInfo this[ushort index] => ref EntityInfos[index];
        public readonly ref WorldEntityInfo this[int index] => ref EntityInfos[index];

        public WorldEntityContainer(WorldCore world, int entityCapacity)
        {
            World = world;
            EntityInfos = new WorldEntityInfo[entityCapacity];
            Events = new EntityEvent[entityCapacity];
            IndexAllocator = new StableIndexAllocator() { Frees = new Stack<int>(entityCapacity >> 1) };
        }

        /// <summary>
        /// 
        /// </summary>
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= EntityInfos.Length) return;
            Array.Resize(ref EntityInfos, capacity);
            Array.Resize(ref Events, capacity);
#if NET6_0_OR_GREATER
            Frees.EnsureCapacity(capacity >> 1);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public ushort Add(in WorldEntityInfo entityInfo)
        {
            var id = (ushort)IndexAllocator.Alloc();
            if (EntityInfos.Length <= id)
                EnsureCapacity(MathEx.GetNextCapacityLength(Count));
            EntityInfos[id] = entityInfo;
            if (Events[id] != null)
                Events[id]!.Entity = new WorldEntity(World, new WorldEntityId(id, entityInfo.Version));
            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Remove(ushort id)
        {
            IndexAllocator.Free(id);
            EntityInfos[id] = default;
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly EntityEvent? GetDispatchEvent(ushort id) => Events[id];

        /// <summary>
        /// 
        /// </summary>
        public readonly EntityEvent GetEvent(ushort id) => Events[id] ??= new EntityEvent { Entity = new WorldEntity(World, new WorldEntityId(id, EntityInfos[id].Version)) };
    }
}