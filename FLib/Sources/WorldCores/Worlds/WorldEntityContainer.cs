// ==================== qcbf@qq.com | 2026-03-04 ====================

#nullable enable
using System;
using System.Collections.Generic;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    public struct WorldEntityContainer
    {
        public WorldEntityInfo[] EntityInfos;
        public FEvent?[] Events;
        public int Count;
        public Stack<ushort> Frees;

        public readonly ref WorldEntityInfo this[ushort index] => ref EntityInfos[index];
        public readonly ref WorldEntityInfo this[int index] => ref EntityInfos[index];

        public WorldEntityContainer(int entityCapacity)
        {
            Count = 0;
            EntityInfos = new WorldEntityInfo[entityCapacity];
            Events = new FEvent[entityCapacity];
            Frees = new Stack<ushort>(entityCapacity >> 1);
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
            if (!Frees.TryPop(out var id))
            {
                EnsureCapacity(MathEx.GetNextPowerOfTwo(Count + 1));
                id = checked((ushort)Count++);
            }

            Events[id] ??= new FEvent();
            EntityInfos[id] = entityInfo;
            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Remove(ushort id)
        {
            EntityInfos[id] = default;
            if (id < --Count)
                Frees.Push(id);
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly FEvent? DispatchEvent(ushort id) => Events[id];

        /// <summary>
        /// 
        /// </summary>
        public readonly FEvent Event(ushort id) => Events[id] ??= new FEvent();
    }
}