// ==================== qcbf@qq.com | 2026-01-14 ====================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityInfo GetEntityInfoOrEmpty(in Entity et)
        {
            ref var eti = ref Entities[et.Id];
            if (eti.Version != et.Version)
                return ref EntityInfo.Empty;
            return ref eti;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref EntityInfo GetEntityInfo(in Entity et)
        {
            ref var eti = ref Entities[et.Id];
            Assert(eti.Version == et.Version, msg: "version error");
            return ref eti;
        }

        /// <summary>
        /// 
        /// </summary>
        public unsafe Entity CreateEntity(in EntityBuilder builder, int hash, bool initMemory = true)
        {
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var archetypeBuilder = new ArchetypeBuilder(1);
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
                ref readonly var info = ref ComponentRegistry.GetInfo(meta);
                if (!info.IsShared)
                    info.Awake?.Invoke(ref *(byte*)chunk.Get(indexInChunk, meta), this, et);
            }

            return et;
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveEntity(Entity et)
        {
            ref var eti = ref GetEntityInfo(et);
            eti.SetDestroying();
            if (eti.HasDynamicComponent)
            {
                var sparse = DynamicComponentSparse[eti.DynamicComponentSparseIndex];
                for (var i = 0; i < sparse.Count; i++)
                {
                    var denseIndex = sparse[i];
                    if (denseIndex < 0) continue;
                    var type = ComponentRegistry.GetType(new IncrementId(i + 1));
                    Soa.GetGroup(type).Free(et, denseIndex, true);
                }
            }

            ArchetypeGroup[eti.ArchetypeIndex].RemoveEntity(eti);
            Entities.Remove(et.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasEntity(Entity et)
        {
            return !et.IsEmpty && Entities.Count > et.Id && Entities[et.Id].Version == et.Version;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool HasEntityAndNotDestroying(Entity et)
        {
            if (et.IsEmpty) return false;
            if (Entities.Count <= et.Id) return false;
            ref readonly var eti = ref Entities[et.Id];
            return eti.Version == et.Version && !eti.IsDestroying;
        }

        /// <summary>
        /// 
        /// </summary>
        public IList<object> GetAllEntities(Entity et, IList<object> list = null)
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
                    var meta = ComponentRegistry.GetInfo(new IncrementId(i + 1)).Meta;
                    var compIdx = DynamicComponentSparse.GetRef(denseIndex)[meta.Id];
                    var val = Soa.GetGroup(meta.Type).Components.GetValue(compIdx);
                    list.Add(val);
                }
            }

            return list;
        }
    }
}