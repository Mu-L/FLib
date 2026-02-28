// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        public Mng<T> GetStaMng<T>(Entity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return eti.Chunk.GetRef<Mng<T>>(eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetStaMng<T>(Entity et, in T val)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            eti.Chunk.GetRef<Mng<T>>(eti.IndexInChunk).Set(val);
        }

        /// <summary>
        /// 
        /// </summary>
        public unsafe Ref<T> GetSta<T>(Entity et) where T : unmanaged
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return new Ref<T>(eti.Chunk.Get<T>(eti.IndexInChunk));
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetSta(Entity et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return eti.Chunk.GetObj(eti.IndexInChunk, ComponentRegistry.GetMeta(componentType));
        }

        /// <summary>
        /// 
        /// </summary>
        public ref T GetStaRef<T>(Entity et) where T : unmanaged
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return ref eti.Chunk.GetRef<T>(eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSta<T>(Entity et, in T val) where T : unmanaged
        {
            ref readonly var eti = ref GetEntityInfo(et);
            eti.Chunk.GetRef<T>(eti.IndexInChunk) = val;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetShared<T>(Entity et, in T val) where T : ISharedComponent
        {
            ref readonly var eti = ref GetEntityInfo(et);
            var compId = ComponentRegistry.GetId<T>();
            var oldHash = eti.Chunk.SparseComponentMeta[compId];
            var newHash = val.GetHashCode();
            if (oldHash == newHash) return;

            var sharedGroup = (SharedComponentGroup<T>)Soa.GetGroup<T>();
            sharedGroup.Alloc(et, val, newHash);
            eti.GetArchetype(this).SetSharedComponent(eti, new QuerySharedComponent(compId, newHash));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasSta<T>(Entity et) where T : unmanaged
        {
            return BitArrayOperator.GetBit(ArchetypeGroup[GetEntityInfo(et).ArchetypeIndex].ComponentMask, ComponentRegistry.GetMeta<T>().Id);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasStaMng<T>(Entity et)
        {
            return BitArrayOperator.GetBit(ArchetypeGroup[GetEntityInfo(et).ArchetypeIndex].ComponentMask, ComponentRegistry.GetMeta<Mng<T>>().Id);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasSta(Entity et, Type componentType)
        {
            return BitArrayOperator.GetBit(ArchetypeGroup[GetEntityInfo(et).ArchetypeIndex].ComponentMask, ComponentRegistry.GetMeta(componentType).Id);
        }
    }
}