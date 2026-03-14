// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 获取实体的组件（优先返回动态组件，如果不存在则返回静态组件）。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <returns>返回该实体的组件引用</returns>
        public unsafe ref T Get<T>(WorldEntity et)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            if (eti.HasDynamicComponent && !eti.Chunk.Has<T>())
            {
                var compIdx = DynamicComponentSparse.GetRef(eti.DynamicComponentSparseIndex)[WorldComponentRegistry.GetId<T>()];
                return ref Soa.GetGroup<T>()[compIdx];
            }

            return ref eti.Chunk.GetRef<T>(eti.IndexInChunk);
        }

        /// <summary>
        /// 获取实体的指定类型的组件。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="componentType">组件的类型</param>
        /// <returns>返回该实体的组件实例</returns>
        public object Get(WorldEntity et, Type componentType)
        {
            ref readonly var eti = ref GetEntityInfo(et);
            return eti.HasDynamicComponent && !eti.Chunk.Has(WorldComponentRegistry.GetId(componentType)) ? GetDyn(et, componentType) : GetSta(et, componentType);
        }

        /// <summary>
        /// 设置实体的组件值（如果组件不存在则添加为动态组件）。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <param name="component">要设置的组件值</param>
        public void Set<T>(WorldEntity et, in T component)
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
        /// 移除实体的指定类型的组件。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        public void Remove<T>(WorldEntity et) => RemoveDyn<T>(et);

        /// <summary>
        /// 检查实体是否拥有指定类型的组件。
        /// </summary>
        /// <typeparam name="T">组件的类型</typeparam>
        /// <param name="et">目标实体</param>
        /// <returns>如果实体拥有该组件返回 true，否则返回 false</returns>
        public bool Has<T>(WorldEntity et)
        {
            ref var eti = ref GetEntityInfo(et);
            var compId = WorldComponentGenericMap<T>.Id;
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
        /// 获取实体的所有组件实例。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="result">用于存储组件的列表，为 null 时会创建新的列表</param>
        /// <returns>包含该实体所有组件的列表</returns>
        public IList GetAll(WorldEntity et, IList result = null)
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
                    result.Add(GetDyn(et, WorldComponentRegistry.GetType(new WorldIncrementId(i + 1))));
                }
            }

            return result;
        }
    }
}