// ==================== qcbf@qq.com |2025-12-28 ====================

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores
{
    public sealed unsafe class Archetype : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly WorldCore World;

        /// <summary>
        /// 
        /// </summary>
        public readonly ulong[] ComponentMask;

        /// <summary>
        /// 
        /// </summary>
        public readonly ComponentMeta[] ComponentTypes;

        /// <summary>
        /// 
        /// </summary>
        public readonly ComponentSparseList Sparse;

        /// <summary>
        /// 
        /// </summary>
        public readonly int EntitiesPerChunk;

        /// <summary>
        /// 
        /// </summary>
        public readonly ushort Index;

        /// <summary>
        /// 
        /// </summary>
        public readonly IncrementId MaxComponentId;

        /// <summary>
        /// 
        /// </summary>
        public readonly HashSet<Chunk> AllChunks = new();

        /// <summary>
        /// 
        /// </summary>
        public readonly Dictionary<int, Chunk> SharedChunks = new();


        public Archetype(WorldCore world, in ArchetypeBuilder builder, ushort index)
        {
            World = world;
            Index = index;
            MaxComponentId = builder.MaxComponentId;
            ComponentTypes = builder.ComponentTypes.ToArray();
            ComponentMask = new ulong[BitArrayOperator.GetBitsLength(MaxComponentId.Raw)];
            EntitiesPerChunk = (int)(GlobalSetting.ChunkAllocator.ChunkSize / (builder.ComponentsSize + sizeof(Entity)));
            Sparse = new ComponentSparseList(MaxComponentId, false);
            var offset = MathEx.AlignUp(EntitiesPerChunk * sizeof(Entity), GlobalSetting.ComponentAlign);
            for (ushort i = 0; i < ComponentTypes.Length; i++)
            {
                ref readonly var meta = ref ComponentTypes[i];
                if (!typeof(ISharedComponent).IsAssignableFrom(meta.Type))
                {
                    Sparse[meta.Id] = offset;
                    BitArrayOperator.SetBit(ComponentMask, meta.Id, true);
                    offset += MathEx.AlignUp(meta.Size * EntitiesPerChunk, GlobalSetting.ComponentAlign);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Entity CreateEntity(out EntityInfo entityInfo, int sharedComponentsKey = 0)
        {
            if (!SharedChunks.TryGetValue(sharedComponentsKey, out var chunk) || chunk.Count >= EntitiesPerChunk)
            {
                var newChunk = GlobalObjectPool<Chunk>.Create();
                newChunk.Previous = chunk;
                newChunk.Sparse = Sparse;
                chunk = SharedChunks[sharedComponentsKey] = newChunk;
                var result = AllChunks.Add(newChunk);
                Debug.Assert(result);
            }

            var chunkEntityIndex = chunk.Count++;
            entityInfo = new EntityInfo(World.GenVersion(), Index, chunkEntityIndex, chunk);
            var id = checked((ushort)World.EntityInfos.Add(entityInfo));
            return *chunk.GetEntity(entityInfo.IndexInChunk) = new Entity(id, entityInfo.Version);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveEntity(in EntityInfo eti)
        {
            var chunk = eti.Chunk;
            if (chunk.Count <= 1)
            {
                if (chunk.Previous != null)
                    SharedChunks[chunk.SharedComponentsKey] = chunk.Previous;
                else
                    SharedChunks.Remove(chunk.SharedComponentsKey);
                AllChunks.Remove(chunk);
                GlobalObjectPool<Chunk>.Release(chunk);
                return;
            }

            if (eti.IndexInChunk + 1 == chunk.Count)
            {
                --chunk.Count;
                return;
            }

            var et = *chunk.GetEntity(eti.IndexInChunk);
            for (var i = 0; i < ComponentTypes.Length; i++)
            {
                var meta = ComponentTypes[i];
                ComponentRegistry.GetInfo(meta).ComponentDestroy?.Invoke(ref *(byte*)chunk.Get(eti.IndexInChunk, meta), World, et);
            }

            var srcIndex = (ushort)(chunk.Count - 1);
            var dstIndex = eti.IndexInChunk;
            for (var i = 0; i < ComponentTypes.Length; i++)
            {
                var meta = ComponentTypes[i];
                Unsafe.CopyBlock(chunk.Get(dstIndex, meta), chunk.Get(srcIndex, meta), meta.Size);
            }

            var srcEt = chunk.GetEntity(srcIndex);
            World.GetEntityInfo(*srcEt).IndexInChunk = dstIndex;
            *chunk.GetEntity(dstIndex) = *srcEt;
            --chunk.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSharedComponent<T>(in EntityInfo eti, in T value) where T : ISharedComponent
        {
            // World.SoaComponent.GetGroup<T>().
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            foreach (var chunk in AllChunks)
                GlobalObjectPool<Chunk>.Release(chunk);
            AllChunks.Clear();
            SharedChunks.Clear();
        }
    }
}