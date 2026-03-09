// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        public ref T GetDyn<T>(WorldEntity et)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[WorldComponentRegistry.GetId<T>()];
            return ref Soa.GetGroup<T>()[compIdx];
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetDyn(WorldEntity et, Type type)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[WorldComponentRegistry.GetId(type)];
            return Soa.GetGroup(type).Components.GetValue(compIdx);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn<T>(WorldEntity et, in T component)
        {
            SetDyn(et, component, ref GetEntityInfo(et));
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn<T>(WorldEntity et, in T component, ref WorldEntityInfo eti)
        {
            Assert(!eti.IsDestroying, et, "entity is destroying");
            Assert(!WorldComponentRegistry.GetInfo(typeof(T)).IsShared, et);
            var id = WorldComponentRegistry.GetId<T>();
            ref var slot = ref EnsureDynamicComponentIndex(id, ref eti);
            var group = Soa.GetGroup<T>();
            if (slot < 0)
            {
                TryAddRequiredComponents(et, ref eti, WorldComponentRegistry.GetInfo(typeof(T)));
                slot = group.Alloc(et, component);
            }
            else
            {
                group[slot] = component;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDyn(WorldEntity et, Type componentType, object component)
        {
            componentType ??= component.GetType();
            Assert(!WorldComponentRegistry.GetInfo(componentType).IsShared, et);
            var id = WorldComponentRegistry.GetId(componentType);
            ref var eti = ref GetEntityInfo(et);
            ref var slot = ref EnsureDynamicComponentIndex(id, ref eti);
            var group = Soa.GetGroup(componentType);
            if (slot < 0)
            {
                TryAddRequiredComponents(et, ref eti, WorldComponentRegistry.GetInfo(componentType));
                slot = group.Alloc(et, component);
            }
            else
            {
                group.Components.SetValue(component, slot);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveDyn<T>(WorldEntity et)
        {
            RemoveDyn(et, typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveDyn(WorldEntity et, Type type)
        {
            Assert(!GetEntityInfo(et).Chunk.Has(WorldComponentRegistry.GetId(type)), et, "cannot remove static component");
            Assert(!GetEntityInfo(et).IsDestroying, et, "entity is destroying");
            ref readonly var eti = ref GetEntityInfo(et);
            ref var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            var id = WorldComponentRegistry.GetId(type).Id;
            var compIdx = sparse[id];
            sparse[id] = -1;
            Soa.GetGroup(type).Free(et, compIdx, false);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasDyn<T>(WorldEntity et)
        {
            return HasDyn(et, typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasDyn(WorldEntity et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (!eti.HasDynamicComponent) return false;
            var compId = WorldComponentRegistry.GetId(componentType).Id;
            ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            return compId < sparse.Count && sparse[compId] >= 0;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private ref int EnsureDynamicComponentIndex(WorldIncrementId componentId, ref WorldEntityInfo eti)
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

        /// <summary>
        /// 
        /// </summary>
        private void TryAddRequiredComponents(WorldEntity et, ref WorldEntityInfo eti, in WorldComponentInfo info)
        {
            if (info.Options?.RequiredComponents == null)
                return;
            foreach (var reqComp in info.Options.RequiredComponents)
                SetDyn(et, reqComp, TypeAssistant.New(reqComp));
        }
    }
}