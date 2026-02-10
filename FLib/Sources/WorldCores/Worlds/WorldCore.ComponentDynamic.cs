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
        public ref T GetDyn<T>(Entity et)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Debug.Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[ComponentRegistry.GetId<T>()];
            return ref Soa.GetGroup<T>().Components[compIdx];
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetDyn(Entity et, Type type)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Debug.Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[ComponentRegistry.GetId(type)];
            return Soa.GetGroup(type).Components.GetValue(compIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn<T>(Entity et, in T component)
        {
            var group = Soa.GetGroup<T>();
            var compIdx = DynamicComponentIndex(et, group, ComponentRegistry.GetId<T>());
            group.Components[compIdx] = component;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn(Entity et, object component, Type componentType)
        {
            componentType ??= component.GetType();
            var group = Soa.GetGroup(componentType);
            var compIdx = DynamicComponentIndex(et, group, ComponentRegistry.GetId(componentType));
            group.Components.SetValue(component, compIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveDyn<T>(Entity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            ref var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            var id = ComponentRegistry.GetId<T>().Id;
            var compIdx = sparse[id];
            sparse[id] = -1;
            Soa.GetGroup<T>().Free(et, compIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveDyn(Entity et, Type type)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            ref var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            var id = ComponentRegistry.GetId(type).Id;
            var compIdx = sparse[id];
            sparse[id] = -1;
            Soa.GetGroup(type).Free(et, compIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasDyn<T>(Entity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (!eti.HasDynamicComponent) return false;
            var compId = ComponentRegistry.GetId<T>().Id;
            ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            return compId < sparse.Count && sparse[compId] >= 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasDyn(Entity et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (!eti.HasDynamicComponent) return false;
            var compId = ComponentRegistry.GetId(componentType).Id;
            ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            return compId < sparse.Count && sparse[compId] >= 0;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private int DynamicComponentIndex(Entity et, ISoaComponentGroupable group, IncrementId componentId)
        {
            ref var eti = ref GetEntityInfo(et);
            int denseIdx;
            var compId = componentId.Id;
            if (eti.HasDynamicComponent)
            {
                ref var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
                if (sparse.Count >= compId || sparse[compId] < 0)
                {
                    sparse.Allocate(componentId.Raw);
                    denseIdx = sparse[componentId] = group.Alloc(et);
                }
                else
                {
                    denseIdx = sparse[compId];
                }
            }
            else
            {
                denseIdx = group.Alloc(et);
                var sparse = new PooledList<int>(componentId.Raw);
                sparse.Span.Fill(-1);
                sparse[compId] = denseIdx;
                eti.DynamicComponentSparseIndex = DynamicComponentSparse.Add(sparse);
            }

            return denseIdx;
        }
    }
}