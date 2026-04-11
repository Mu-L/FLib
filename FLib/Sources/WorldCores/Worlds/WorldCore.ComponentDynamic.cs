// ==================== qcbf@qq.com | 2026-01-14 ====================

#nullable enable
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
        /// 获取实体的动态组件。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <returns>返回对该实体的动态组件的引用</returns>
        public ref T GetDyn<T>(WorldEntityId et)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[WorldComponentRegistry.GetId<T>()];
            return ref Soa.GetGroup<T>()[compIdx];
        }
        
        /// <summary>
        /// 获取实体的指定类型的动态组件。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="type">组件的类型</param>
        /// <returns>返回该实体的动态组件实例</returns>
        public object GetDyn(WorldEntityId et, Type type)
        {
            var dynIdx = GetEntityInfo(et).DynamicComponentSparseIndex;
            Assert(dynIdx >= 0);
            var compIdx = DynamicComponentSparse.GetRef(dynIdx)[WorldComponentRegistry.GetId(type)];
            return Soa.GetGroup(type).Components.GetValue(compIdx)!;
        }
        
        /// <summary>
        /// 设置实体的动态组件。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <param name="component">要设置的组件值</param>
        /// <returns>返回组件在动态组件组中的索引</returns>
        public int SetDyn<T>(WorldEntityId et, in T component)
        {
            return SetDyn(et, component, ref GetEntityInfo(et));
        }
        
        /// <summary>
        /// 设置实体的动态组件（使用实体信息引用）。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <param name="component">要设置的组件值</param>
        /// <param name="eti">实体信息的引用</param>
        /// <returns>返回组件在动态组件组中的索引</returns>
        internal int SetDyn<T>(WorldEntityId et, in T component, ref WorldEntityInfo eti)
        {
            Assert(!WorldComponentRegistry.GetInfo(typeof(T)).IsShared, et);
            var id = WorldComponentRegistry.GetId<T>();
            ref var slot = ref EnsureDynamicComponentIndex(id, ref eti);
            var group = Soa.GetGroup<T>();
            if (slot < 0)
            {
                Assert(!eti.IsDestroying, et, "entity is destroying");
                TryAddRequiredComponents(et, ref eti, WorldComponentRegistry.GetInfo(typeof(T)));
                slot = group.Alloc(et, component);
            }
            else
            {
                ref readonly var info = ref WorldComponentRegistry.GetInfo<T>();
                if (info.Awake != null || (info.Destroy != null && info.Op(EComponentOption.AlwaysReceiveDestroy)))
                {
                    group.Free(et, slot, false);
                    slot = group.Alloc(et, component);
                }
                else
                {
                    group[slot] = component;
                }
            }
            
            return slot;
        }
        
        /// <summary>
        /// 设置实体的指定类型的动态组件。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="componentType">组件的类型（若为 null 则使用 component 的实际类型）</param>
        /// <param name="component">要设置的组件值</param>
        /// <returns>返回组件在动态组件组中的索引</returns>
        public int SetDyn(WorldEntityId et, Type? componentType, object? component)
        {
            componentType ??= component!.GetType();
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
            
            return slot;
        }
        
        /// <summary>
        /// 设置实体的动态组件（使用脚本包）。
        /// </summary>
        public void SetDyn(WorldEntityId et, in ScriptPackBytes script)
        {
            var type = TypeAssistant.GetType(script.ScriptTypeName);
            var index = SetDyn(et, type, TypeAssistant.New(type));
            WorldComponentRegistry.GetInfo(type).BytesPackWrapper!.Deserialize(ref Soa.GetGroup(type).GetPointer(index), script.InstanceBytes.Span);
        }
        
        /// <summary>
        /// 设置实体的动态组件（使用脚本包）。
        /// </summary>
        public void SetDyn(WorldEntityId et, in ReadOnlySpan<ScriptPackBytes> scripts)
        {
            if (scripts.IsEmpty)
                return;
            foreach (var item in scripts)
                SetDyn(et, item);
        }
        
        /// <summary>
        /// 移除实体的指定类型的动态组件。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        public void RemoveDyn<T>(WorldEntityId et)
        {
            RemoveDyn(et, typeof(T));
        }
        
        /// <summary>
        /// 移除实体的指定类型的动态组件。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="type">要移除的组件类型</param>
        public void RemoveDyn(WorldEntityId et, Type type)
        {
            Assert(!GetEntityInfo(et).Chunk.Has(WorldComponentRegistry.GetId(type)), et, "cannot remove static component");
            // Assert(!GetEntityInfo(et).IsDestroying, et, "entity is destroying");
            ref readonly var eti = ref GetEntityInfo(et);
            ref var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            var id = WorldComponentRegistry.GetId(type).Id;
            var compIdx = sparse[id];
            sparse[id] = -1;
            Soa.GetGroup(type).Free(et, compIdx, false);
        }
        
        /// <summary>
        /// 检查实体是否拥有指定类型的动态组件。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <returns>如果实体拥有该动态组件返回 true，否则返回 false</returns>
        public bool HasDyn<T>(WorldEntityId et)
        {
            return HasDyn(et, typeof(T));
        }
        
        /// <summary>
        /// 检查实体是否拥有指定类型的动态组件。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="componentType">要检查的组件类型</param>
        /// <returns>如果实体拥有该动态组件返回 true，否则返回 false</returns>
        public bool HasDyn(WorldEntityId et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (!eti.HasDynamicComponent) return false;
            var compId = WorldComponentRegistry.GetId(componentType).Id;
            ref readonly var sparse = ref DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex);
            return compId < sparse.Count && sparse[compId] >= 0;
        }
        
        /// <summary>
        /// 确保实体有动态组件索引，不存在则创建。
        /// </summary>
        /// <param name="componentId">组件的 ID</param>
        /// <param name="eti">实体信息的引用</param>
        /// <returns>返回对组件索引的引用</returns>
#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
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
        /// 尝试添加该组件的所有必需组件。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="eti">实体信息的引用</param>
        /// <param name="info">组件信息</param>
        private void TryAddRequiredComponents(WorldEntityId et, ref WorldEntityInfo eti, in WorldComponentInfo info)
        {
            if (info.Options?.RequiredComponents == null)
                return;
            foreach (var reqComp in info.Options.RequiredComponents)
                SetDyn(et, reqComp, TypeAssistant.New(reqComp));
        }
    }
}