// ==================== qcbf@qq.com |2025-12-28 ====================

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        public readonly uint[] SparseComponentOffset;

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
            ComponentMask = new ulong[BitArrayOperator.GetBitsLength(MaxComponentId.Raw)];
            EntitiesPerChunk = (int)(GlobalSetting.ChunkAllocator.ChunkSize / (builder.ComponentsSize + sizeof(Entity)));
            SparseComponentOffset = new uint[MaxComponentId.Raw];
            var offset = (uint)MathEx.AlignUp(EntitiesPerChunk * sizeof(Entity), GlobalSetting.ComponentAlign);
            using var tempComponents = new PooledList<ComponentMeta>();
            for (ushort i = 0; i < builder.ComponentTypes.Count; i++)
            {
                ref readonly var meta = ref builder.ComponentTypes[i];
                BitArrayOperator.SetBit(ComponentMask, meta.Id, true);
                if (!typeof(ISharedComponent).IsAssignableFrom(meta.Type))
                {
                    SparseComponentOffset[meta.Id] = offset;
                    offset += (uint)MathEx.AlignUp(meta.Size * EntitiesPerChunk, GlobalSetting.ComponentAlign);
                    tempComponents.Add(meta);
                }
            }

            ComponentTypes = tempComponents.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        public Entity CreateEntity(out EntityInfo entityInfo, in ReadOnlySpan<QuerySharedComponent> sharedComponents = default)
        {
            var chunk = GetChunk(sharedComponents);
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
            var et = *chunk.GetEntity(eti.IndexInChunk);
            for (var i = 0; i < ComponentTypes.Length; i++)
            {
                var meta = ComponentTypes[i];
                ComponentRegistry.GetInfo(meta).ComponentDestroy?.Invoke(ref *(byte*)chunk.Get(eti.IndexInChunk, meta), World, et);
            }

            RemoveEntity(chunk, eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSharedComponent(in EntityInfo eti, in QuerySharedComponent sharedComponent)
        {
            var chunk = eti.Chunk;
            Span<QuerySharedComponent> sharedComponents = stackalloc QuerySharedComponent[chunk.AllSharedComponents.Length + 1];
            chunk.AllSharedComponents.CopyTo(sharedComponents);
            if (chunk.HasSharedComponent(sharedComponent.ComponentId))
            {
                sharedComponents = sharedComponents[..^1];
                for (var i = 0; i < sharedComponents.Length; i++)
                {
                    if (sharedComponents[i].ComponentId == sharedComponent.ComponentId)
                    {
                        sharedComponents[i] = sharedComponent;
                        goto Found;
                    }
                }

                throw new Exception($"not found exist component id {ComponentRegistry.GetType(sharedComponent.ComponentId)}");
                Found: ;
            }
            else
            {
                sharedComponents[^1] = sharedComponent;
            }

            MoveEntity(chunk, eti.IndexInChunk, GetChunk(sharedComponents));
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

        /// <summary>
        /// 
        /// </summary>
        private void MoveEntity(Chunk fromChunk, ushort fromIndex, Chunk newChunk)
        {
            CopyEntity(fromChunk, fromIndex, newChunk, newChunk.Count++);
            RemoveEntity(fromChunk, fromIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        private void RemoveEntity(Chunk chunk, ushort index)
        {
            if (--chunk.Count == 0)
            {
                if (chunk.Previous != null)
                    SharedChunks[chunk.AllSharedComponentsHash] = chunk.Previous;
                else
                    SharedChunks.Remove(chunk.AllSharedComponentsHash);
                AllChunks.Remove(chunk);
                GlobalObjectPool<Chunk>.Release(chunk);
            }
            else
            {
                if (index < chunk.Count - 1)
                    CopyEntity(chunk, (ushort)(chunk.Count - 1), chunk, index); // 后面在考虑是否跨chunk copy保持chunk的紧凑
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CopyEntity(Chunk fromChunk, ushort fromIndex, Chunk toChunk, ushort toIndex)
        {
            for (var i = 0; i < ComponentTypes.Length; i++)
            {
                var meta = ComponentTypes[i];
                Unsafe.CopyBlock(toChunk.Get(toIndex, meta), fromChunk.Get(fromIndex, meta), meta.Size);
            }

            var fromEntity = *fromChunk.GetEntity(fromIndex);
            *toChunk.GetEntity(toIndex) = fromEntity;
            ref var eti = ref World.GetEntityInfo(fromEntity);
            eti.IndexInChunk = toIndex;
            eti.Chunk = toChunk;
        }

        /// <summary>
        /// 
        /// </summary>
        private Chunk GetChunk(in ReadOnlySpan<QuerySharedComponent> sharedComponents = default)
        {
            var sharedHash = 0;
            if (!sharedComponents.IsEmpty)
            {
                var hashCode = new HashCode();
                hashCode.AddBytes(MemoryMarshal.AsBytes(sharedComponents));
                sharedHash = hashCode.ToHashCode();
            }

            if (!SharedChunks.TryGetValue(sharedHash, out var chunk) || chunk.Count >= EntitiesPerChunk)
            {
                var newChunk = GlobalObjectPool<Chunk>.Create();
                newChunk.Previous = chunk;
                newChunk.SparseComponentOffset = ArrayPool<int>.Shared.Rent(SparseComponentOffset.Length);
                SparseComponentOffset.CopyTo(newChunk.SparseComponentOffset, 0);
                newChunk.AllSharedComponentsHash = sharedHash;
                newChunk.AllSharedComponents = sharedComponents.ToArray();
                foreach (var sharedComponent in sharedComponents)
                    newChunk.SparseComponentOffset[sharedComponent.ComponentId] = sharedComponent.Hash;

                chunk = SharedChunks[sharedHash] = newChunk;
                var result = AllChunks.Add(newChunk);
                Debug.Assert(result);
            }

            return chunk;
        }
    }
}