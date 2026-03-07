// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Collections;
using System.Collections.Generic;
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
        public object Get(Entity et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return eti.HasDynamicComponent && !eti.Chunk.Has(ComponentRegistry.GetId(componentType)) ? GetDyn(et, componentType) : GetSta(et, componentType);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Set<T>(Entity et, in T component)
        {
            ref var eti = ref GetEntityInfo(et);
            if (!eti.Chunk.Has<T>())
            {
                SetDyn(et, component, ref eti);
            }
            else
            {
                Assert(!eti.IsDestroying, et, "entity is destroying");
                eti.Chunk.GetRef<T>(eti.IndexInChunk) = component;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Remove<T>(Entity et) => RemoveDyn<T>(et);

        /// <summary>
        /// 
        /// </summary>
        public bool Has<T>(Entity et)
        {
            ref var eti = ref GetEntityInfo(et);
            var compId = ComponentGenericMap<T>.Id;
            if (compId.IsEmpty)
                return false;

            if (eti.HasDynamicComponent && !eti.Chunk.Has<T>())
            {
                ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
                return compId < sparse.Count && sparse[compId] >= 0;
            }

            return BitArrayOperator.GetBit(ArchetypeGroup[eti.ArchetypeIndex].ComponentMask, compId);
        }

        /// <summary>
        /// 
        /// </summary>
        public IList GetAll(Entity et, IList result = null)
        {
            result ??= new List<object>();
            ref readonly var eti = ref GetEntityInfo(et);
            eti.Chunk.GetAll(eti.IndexInChunk, eti.GetArchetype(this), result);

            eti.Chunk.GetAllShared(this, result);

            if (eti.HasDynamicComponent)
            {
                var sparse = DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
                for (var i = 0; i < sparse.Count; i++)
                {
                    var denseIndex = sparse[i];
                    if (denseIndex < 0) continue;
                    result.Add(GetDyn(et, ComponentRegistry.GetType(new IncrementId(i + 1))));
                }
            }

            return result;
        }
    }
}