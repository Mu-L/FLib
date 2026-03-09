// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores.SoaComponents;
using FLib.WorldCores.Queries;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        public Mng<T> GetStaMng<T>(WorldEntity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return eti.Chunk.GetRef<Mng<T>>(eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetStaMng<T>(WorldEntity et, in T val)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            eti.Chunk.GetRef<Mng<T>>(eti.IndexInChunk).Set(val);
        }

        /// <summary>
        /// 
        /// </summary>
        public unsafe Ref<T> GetSta<T>(WorldEntity et) where T : unmanaged
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return new Ref<T>(eti.Chunk.Get<T>(eti.IndexInChunk));
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetSta(WorldEntity et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return eti.Chunk.GetObj(eti.IndexInChunk, WorldComponentRegistry.GetMeta(componentType));
        }

        /// <summary>
        /// 
        /// </summary>
        public ref T GetStaRef<T>(WorldEntity et) where T : unmanaged
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return ref eti.Chunk.GetRef<T>(eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSta<T>(WorldEntity et, in T val) where T : unmanaged
        {
            ref readonly var eti = ref GetEntityInfo(et);
            eti.Chunk.GetRef<T>(eti.IndexInChunk) = val;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetShared<T>(WorldEntity et, in T val) where T : IWorldSharedComponent
        {
            ref readonly var eti = ref GetEntityInfo(et);
            var compId = WorldComponentRegistry.GetId<T>();
            var oldHash = eti.Chunk.SparseComponentMeta[compId];
            var newHash = val.GetHashCode();
            if (oldHash == newHash) return;

            var sharedGroup = (WorldSharedComponentGroup<T>)Soa.GetGroup<T>();
            sharedGroup.Alloc(et, val, newHash);
            eti.GetArchetype(this).SetSharedComponent(eti, new WorldQuerySharedComponent(compId, newHash));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasSta<T>(WorldEntity et) where T : unmanaged
        {
            return BitArrayOperator.GetBit(ArchetypeGroup[GetEntityInfo(et).ArchetypeIndex].ComponentMask, WorldComponentRegistry.GetMeta<T>().Id);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasStaMng<T>(WorldEntity et)
        {
            return BitArrayOperator.GetBit(ArchetypeGroup[GetEntityInfo(et).ArchetypeIndex].ComponentMask, WorldComponentRegistry.GetMeta<Mng<T>>().Id);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasSta(WorldEntity et, Type componentType)
        {
            return BitArrayOperator.GetBit(ArchetypeGroup[GetEntityInfo(et).ArchetypeIndex].ComponentMask, WorldComponentRegistry.GetMeta(componentType).Id);
        }
    }
}