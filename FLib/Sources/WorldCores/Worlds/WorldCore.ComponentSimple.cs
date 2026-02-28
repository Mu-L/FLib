// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Diagnostics;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        public unsafe ref T Get<T>(Entity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (eti.HasDynamicComponent && !eti.Chunk.Has<T>())
            {
                var compIdx = DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex)[ComponentRegistry.GetId<T>()];
                return ref Soa.GetGroup<T>()[compIdx];
            }

            return ref *eti.Chunk.Get<T>(eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Set<T>(Entity et, in T component)
        {
            ref var eti = ref GetEntityInfo(et);
            if (!eti.Chunk.Has<T>())
            {
                SetDyn(et, ref eti, component);
            }
            else
            {
                Debug.Assert(!eti.IsDestroying, "entity is destroying");
                eti.Chunk.GetRef<T>(eti.IndexInChunk) = component;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Remove<T>(Entity et)
        {
            Debug.Assert(!GetEntityInfo(et).Chunk.Has<T>(), "cannot remove static component");
            Debug.Assert(!GetEntityInfo(et).IsDestroying, "entity is destroying");
            RemoveDyn<T>(et);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has<T>(Entity et)
        {
            ref var eti = ref GetEntityInfo(et);
            var compId = ComponentGenericMap<T>.Id;
            if (eti.HasDynamicComponent && !eti.Chunk.Has<T>())
            {
                ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
                return compId < sparse.Count && sparse[compId] >= 0;
            }

            return BitArrayOperator.GetBit(ArchetypeGroup[eti.ArchetypeIndex].ComponentMask, compId);
        }
    }
}