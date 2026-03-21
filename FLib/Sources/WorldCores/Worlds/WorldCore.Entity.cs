// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;
using FLib.WorldCores.Archetypes;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 获取实体信息或空实体信息。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <returns>实体信息的引用，如果实体无效则返回空信息</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref WorldEntityInfo GetEntityInfoOrEmpty(in WorldEntity et)
        {
            ref var eti = ref Entities[et.Id];
            if (eti.Version != et.Version)
                return ref WorldEntityInfo.Empty;
            return ref eti;
        }

        /// <summary>
        /// 获取实体信息。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <returns>实体信息的引用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref WorldEntityInfo GetEntityInfo(in WorldEntity et)
        {
            ref var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, msg: "version error");
            return ref eti;
        }

        /// <summary>
        /// 使用提供的构件信息创建一个新实体。
        /// </summary>
        /// <param name="builder">实体构建器，包含要添加的组件信息</param>
        /// <param name="hash">组件绔合的散列值</param>
        /// <param name="initMemory">是否初始化内存（默认为 true）</param>
        /// <returns>新创建的实体</returns>
        public unsafe WorldEntity CreateEntity(in WorldEntityBuilder builder, int hash, bool initMemory = true)
        {
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var archetypeBuilder = new WorldArchetypeBuilder(1);
                for (var i = 0; i < builder.Components.Count; i++)
                    archetypeBuilder.With(builder.Components[i]);
                archetype = ArchetypeGroup.Create(hash, archetypeBuilder);
            }

            var et = archetype.CreateEntity(out var entityInfo);
            var chunk = entityInfo.Chunk;
            var indexInChunk = entityInfo.IndexInChunk;
            if (initMemory)
            {
                for (var i = 0; i < archetype.ComponentTypes.Length; i++)
                {
                    chunk.ClearMemory(indexInChunk, archetype.ComponentTypes[i]);
                }
            }

            for (var i = 0; i < builder.Components.Count; i++)
            {
                var meta = builder.Components[i];
                ref readonly var info = ref WorldComponentRegistry.GetInfo(meta);
                if (!info.IsShared)
                    info.Awake?.Invoke(ref *(byte*)chunk.Get(indexInChunk, meta), this, et);
            }

            return et;
        }

        /// <summary>
        /// 从世界中移除指定的实体。
        /// </summary>
        /// <param name="et">要移除的实体</param>
        public void RemoveEntity(WorldEntity et)
        {
            ref var eti = ref GetEntityInfo(et);
            eti.SetDestroying();
            if (eti.HasDynamicComponent)
            {
                var sparse = DynamicComponentSparse[eti.DynamicComponentSparseIndex];
                for (var i = 0; i < sparse.Count; i++)
                {
                    ref var denseIndex = ref sparse[i];
                    if (denseIndex < 0) continue;
                    var type = WorldComponentRegistry.GetType(new WorldIncrementId(i + 1));
                    Soa.GetGroup(type).Free(et, denseIndex, true);
                    denseIndex = -1;
                }
            }

            ArchetypeGroup[eti.ArchetypeIndex].RemoveEntity(eti);
            Entities.Remove(et.Id);
        }

        /// <summary>
        /// 检查实体是否存在于世界中。
        /// </summary>
        /// <param name="et">要检查的实体</param>
        /// <returns>如果实体存在返回 true，否则返回 false</returns>
        public bool HasEntity(WorldEntity et)
        {
            return !et.IsEmpty && Entities.Count > et.Id && Entities[et.Id].Version == et.Version;
        }

        /// <summary>
        /// 检查实体是否存在且未处于销毁中。
        /// </summary>
        /// <param name="et">要检查的实体</param>
        /// <returns>如果实体存在且未销毁返回 true，否则返回 false</returns>
        public bool HasEntityAndNotDestroying(WorldEntity et)
        {
            if (et.IsEmpty) return false;
            if (Entities.Count <= et.Id) return false;
            ref readonly var eti = ref Entities[et.Id];
            return eti.Version == et.Version && !eti.IsDestroying;
        }

        /// <summary>
        /// 获取实体的所有组件对象。
        /// </summary>
        /// <param name="et">目标实体</param>
        /// <param name="list">用于存储组件的列表，为 null 时会创建新列表</param>
        /// <returns>包含实体所有组件对象的列表</returns>
        public IList<object> GetAllEntities(WorldEntity et, IList<object> list = null)
        {
            list ??= new List<object>();
            var eti = GetEntityInfo(et);
            var chunk = eti.Chunk;
            foreach (var meta in ArchetypeGroup[eti.ArchetypeIndex].ComponentTypes)
                list.Add(chunk.GetObj(eti.IndexInChunk, meta));

            if (eti.HasDynamicComponent)
            {
                var sparse = DynamicComponentSparse[eti.DynamicComponentSparseIndex];
                var denseIndexes = sparse;
                for (var i = 0; i < denseIndexes.Count; i++)
                {
                    var denseIndex = denseIndexes[i];
                    if (denseIndex < 0) continue;
                    var meta = WorldComponentRegistry.GetInfo(new WorldIncrementId(i + 1)).Meta;
                    var compIdx = DynamicComponentSparse.GetRef(denseIndex)[meta.Id];
                    var val = Soa.GetGroup(meta.Type).Components.GetValue(compIdx);
                    list.Add(val);
                }
            }

            return list;
        }
    }
}