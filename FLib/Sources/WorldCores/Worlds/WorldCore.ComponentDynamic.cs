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
            Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[ComponentRegistry.GetId<T>()];
            return ref Soa.GetGroup<T>()[compIdx];
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetDyn(Entity et, Type type)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[ComponentRegistry.GetId(type)];
            return Soa.GetGroup(type).Components.GetValue(compIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn<T>(Entity et, in T component)
        {
            SetDyn(et, ref GetEntityInfo(et), component);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn<T>(Entity et, ref EntityInfo eti, in T component)
        {
            Assert(!eti.IsDestroying, et, "entity is destroying");
            Assert(!ComponentRegistry.GetInfo(typeof(T)).IsShared, et);
            var id = ComponentRegistry.GetId<T>();
            ref var slot = ref EnsureDynamicComponentIndex(id, ref eti);
            var group = Soa.GetGroup<T>();
            if (slot < 0)
                slot = group.Alloc(et, component);
            else
                group[slot] = component;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn(Entity et, object component, Type componentType)
        {
            componentType ??= component.GetType();
            Assert(!ComponentRegistry.GetInfo(componentType).IsShared, et);
            var id = ComponentRegistry.GetId(componentType);
            ref var eti = ref GetEntityInfo(et);
            ref var slot = ref EnsureDynamicComponentIndex(id, ref eti);
            var group = Soa.GetGroup(componentType);
            if (slot < 0)
                slot = group.Alloc(et, component);
            else
                group.Components.SetValue(component, slot);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveDyn<T>(Entity et)
        {
            Assert(HasDyn<T>(et), et);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int EnsureDynamicComponentIndex(IncrementId componentId, ref EntityInfo eti)
        {
            var compId = componentId.Id;
            if (eti.HasDynamicComponent)
            {
                ref var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
                if (sparse.Count <= compId)
                {
                    var oldSize = sparse.Buffer.Length;
                    if (sparse.Allocate(componentId.Raw))
                        sparse.Buffer.AsSpan(oldSize).Fill(-1);
                    sparse.Count = componentId.Raw;
                }

                return ref sparse[compId];
            }
            else
            {
                var sparse = new PooledList<int>(componentId.Raw);
                sparse.Count = componentId.Raw;
                Array.Fill(sparse.Buffer, -1);
                eti.DynamicComponentSparseIndex = DynamicComponentSparse.Add(sparse);
                return ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex)[compId];
            }
        }
    }
}