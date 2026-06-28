// ==================== qcbf@qq.com |2025-12-28 ====================

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FLib.WorldCores.Queries;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;
using FLib.WorldCores;

namespace FLib.WorldCores.Archetypes
{
    public sealed unsafe class WorldArchetype : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly WorldCore World;

        /// <summary>
        /// 包含静态组件和shared组件
        /// </summary>
        public readonly ulong[] ComponentMask;

        /// <summary>
        /// 只有静态组件, 不包含shared组件
        /// </summary>
        public readonly WorldComponentMeta[] ComponentTypes;

        /// <summary>
        /// 
        /// </summary>
        public readonly int[] SparseComponentOffset;

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
        public readonly WorldIncrementId MaxComponentId;

        /// <summary>  </summary>
        public readonly Dictionary<int, List<WorldChunk>> SharedChunks = new();

        public override string ToString() => $"{Index}, {string.Join(',', ComponentTypes.Select(v => v.Type.ToString()))}";

        public WorldArchetype(WorldCore world, in WorldArchetypeBuilder builder, ushort index)
        {
            World = world;
            Index = index;
            MaxComponentId = builder.MaxComponentId;
            ComponentMask = new ulong[BitArrayOperator.GetBitsLength(MaxComponentId.Raw)];
            EntitiesPerChunk = (int)(WorldGlobalSetting.ChunkAllocator.ChunkSize / (builder.ComponentsSize + sizeof(WorldEntityId)));
            SparseComponentOffset = new int[MaxComponentId.Raw];
            var offset = EntitiesPerChunk * sizeof(WorldEntityId);
            using var tempComponents = new PooledList<WorldComponentMeta>();
            for (ushort i = 0; i < builder.ComponentTypes.Count; i++)
            {
                ref readonly var meta = ref builder.ComponentTypes[i];
                BitArrayOperator.SetBit(ComponentMask, meta.Id, true);
                if (!WorldComponentRegistry.GetInfo(meta).IsShared)
                {
                    SparseComponentOffset[meta.Id] = offset;
                    offset += meta.Size * EntitiesPerChunk;
                    tempComponents.Add(meta);
                }
            }

            ComponentTypes = tempComponents.ToArray();
#if DEBUG
            for (var i = 0; i < SparseComponentOffset.Length; i++)
            {
                if (SparseComponentOffset[i] != 0
                    && SparseComponentOffset[i] + WorldComponentRegistry.GetInfo(new WorldIncrementId(i + 1)).Meta.Size * EntitiesPerChunk > WorldGlobalSetting.ChunkAllocator.ChunkSize)
                    throw new OverflowException($"memory overflow {i}");
            }
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public WorldEntityId CreateEntity(out WorldEntityInfo entityInfo, in ReadOnlySpan<WorldQuerySharedComponent> sharedComponents = default)
        {
            var chunk = GetChunk(sharedComponents);
            var chunkEntityIndex = chunk.Count++;
            entityInfo = new WorldEntityInfo(World.GenVersion(), Index, chunkEntityIndex, chunk);
            return *chunk.GetEntity(entityInfo.IndexInChunk) = new WorldEntityId(World.Entities.Add(entityInfo), entityInfo.Version);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RemoveEntity(in WorldEntityInfo eti)
        {
            var chunk = eti.Chunk;
            var et = *chunk.GetEntity(eti.IndexInChunk);
            for (var i = 0; i < ComponentTypes.Length; i++)
            {
                ref readonly var info = ref WorldComponentRegistry.GetInfo(ComponentTypes[i]);
                (info.Flags(EComponentFlag.AlwaysReceiveDestroy) ? info.DestroyWithComponentSelf : info.Destroy).Invoke(ref *(byte*)chunk.Get(eti.IndexInChunk, info.Meta), World, et);
            }

            RemoveEntity(chunk, eti.IndexInChunk);
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetSharedComponent(in WorldEntityInfo eti, in WorldQuerySharedComponent sharedComponent)
        {
            var chunk = eti.Chunk;
            Span<WorldQuerySharedComponent> sharedComponents = stackalloc WorldQuerySharedComponent[chunk.AllSharedComponents.Length + 1];
            chunk.AllSharedComponents.CopyTo(sharedComponents);
            if (chunk.Has(sharedComponent.ComponentId))
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

                World.ThrowException($"not found exist component id {WorldComponentRegistry.GetType(sharedComponent.ComponentId)}");
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
            foreach (var sharedChunk in SharedChunks)
            {
                foreach (var chunk in sharedChunk.Value)
                    GlobalObjectPool<WorldChunk>.Release(chunk);
            }

            SharedChunks.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        private void MoveEntity(WorldChunk fromChunk, ushort fromIndex, WorldChunk newChunk)
        {
            CopyEntity(fromChunk, fromIndex, newChunk, newChunk.Count++);
            RemoveEntity(fromChunk, fromIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        private void RemoveEntity(WorldChunk chunk, ushort index)
        {
            if (chunk.Count == 1)
            {
                var list = SharedChunks[chunk.AllSharedComponentsHash];
                var theLastChunk = list[chunk.Index] = list[^1];
                theLastChunk.Index = chunk.Index;
                list.RemoveAt(list.Count - 1);
                GlobalObjectPool<WorldChunk>.Release(chunk);
            }
            else
            {
                var newCount = (ushort)(chunk.Count - 1);
                if (index < newCount)
                    CopyEntity(chunk, newCount, chunk, index); // 后面在考虑是否跨chunk copy保持chunk的紧凑
                chunk.Count = newCount;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CopyEntity(WorldChunk fromChunk, ushort fromIndex, WorldChunk toChunk, ushort toIndex)
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
        private WorldChunk GetChunk(in ReadOnlySpan<WorldQuerySharedComponent> sharedComponents = default)
        {
            var sharedHash = 0;
            if (!sharedComponents.IsEmpty)
            {
                var hashCode = new HashCode();
                hashCode.AddBytes(MemoryMarshal.AsBytes(sharedComponents));
                sharedHash = hashCode.ToHashCode();
            }

            WorldChunk chunk;
            if (!SharedChunks.TryGetValue(sharedHash, out var chunkList))
                SharedChunks.Add(sharedHash, chunkList = new List<WorldChunk>());
            if (chunkList.Count == 0)
            {
                chunk = AppendNewChunk(sharedComponents);
            }
            else
            {
                chunk = chunkList[^1];
                if (chunk.Count >= EntitiesPerChunk)
                    chunk = AppendNewChunk(sharedComponents);
            }

            return chunk;

            WorldChunk AppendNewChunk(in ReadOnlySpan<WorldQuerySharedComponent> sharedComponents)
            {
                var newChunk = GlobalObjectPool<WorldChunk>.Create();
                newChunk.SparseComponentMeta = ArrayPool<int>.Shared.Rent(SparseComponentOffset.Length);
                SparseComponentOffset.CopyTo(newChunk.SparseComponentMeta, 0);
                Array.Clear(newChunk.SparseComponentMeta, SparseComponentOffset.Length, newChunk.SparseComponentMeta.Length - SparseComponentOffset.Length);
                newChunk.AllSharedComponentsHash = sharedHash;
                newChunk.Index = (short)chunkList.Count;
                if (sharedComponents.IsEmpty)
                {
                    newChunk.AllSharedComponents = Array.Empty<WorldQuerySharedComponent>();
                }
                else
                {
                    newChunk.AllSharedComponents = sharedComponents.ToArray();
                    foreach (var sharedComponent in sharedComponents)
                        newChunk.SparseComponentMeta[sharedComponent.ComponentId] = sharedComponent.Hash;
                }

                chunkList.Add(newChunk);
                return newChunk;
            }
        }
    }
}