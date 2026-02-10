// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        public unsafe ref T GetSimple<T>(Entity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (eti.HasDynamicComponent)
            {
                ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
                var compId = ComponentRegistry.GetId<T>().Id;
                if (compId < sparse.Count && sparse[compId] >= 0)
                    return ref Soa.GetGroup<T>().Components[sparse[compId]];
            }

            return ref *eti.Chunk.Get<T>(eti.IndexInChunk);
        }
    }
}