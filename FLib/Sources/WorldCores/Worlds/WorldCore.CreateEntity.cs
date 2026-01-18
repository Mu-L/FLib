// ==================== qcbf@qq.com | 2026-01-18 ====================

using System;

namespace FLib.WorldCores
{
    public partial class WorldCore
    {
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1>(in T1 v1 = default) where T1 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(1);
                builder.Add<T1>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2>(in T1 v1 = default, in T2 v2 = default) where T1 : unmanaged where T2 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(2);
                builder.Add<T1>();
                builder.Add<T2>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2, T3>(in T1 v1 = default, in T2 v2 = default, in T3 v3 = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T3>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(3);
                builder.Add<T1>();
                builder.Add<T2>();
                builder.Add<T3>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            *chunk.Get<T3>(eti.IndexInChunk) = v3;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T3>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T3>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2, T3, T4>(in T1 v1 = default, in T2 v2 = default, in T3 v3 = default, in T4 v4 = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T3>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T4>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(4);
                builder.Add<T1>();
                builder.Add<T2>();
                builder.Add<T3>();
                builder.Add<T4>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            *chunk.Get<T3>(eti.IndexInChunk) = v3;
            *chunk.Get<T4>(eti.IndexInChunk) = v4;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T3>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T3>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T4>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T4>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2, T3, T4, T5>(in T1 v1 = default, in T2 v2 = default, in T3 v3 = default, in T4 v4 = default, in T5 v5 = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T3>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T4>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T5>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(5);
                builder.Add<T1>();
                builder.Add<T2>();
                builder.Add<T3>();
                builder.Add<T4>();
                builder.Add<T5>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            *chunk.Get<T3>(eti.IndexInChunk) = v3;
            *chunk.Get<T4>(eti.IndexInChunk) = v4;
            *chunk.Get<T5>(eti.IndexInChunk) = v5;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T3>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T3>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T4>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T4>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T5>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T5>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2, T3, T4, T5, T6>(in T1 v1 = default, in T2 v2 = default, in T3 v3 = default, in T4 v4 = default, in T5 v5 = default, in T6 v6 = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T3>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T4>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T5>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T6>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(6);
                builder.Add<T1>();
                builder.Add<T2>();
                builder.Add<T3>();
                builder.Add<T4>();
                builder.Add<T5>();
                builder.Add<T6>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            *chunk.Get<T3>(eti.IndexInChunk) = v3;
            *chunk.Get<T4>(eti.IndexInChunk) = v4;
            *chunk.Get<T5>(eti.IndexInChunk) = v5;
            *chunk.Get<T6>(eti.IndexInChunk) = v6;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T3>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T3>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T4>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T4>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T5>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T5>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T6>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T6>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2, T3, T4, T5, T6, T7>(in T1 v1 = default, in T2 v2 = default, in T3 v3 = default, in T4 v4 = default, in T5 v5 = default, in T6 v6 = default, in T7 v7 = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T3>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T4>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T5>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T6>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T7>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(7);
                builder.Add<T1>();
                builder.Add<T2>();
                builder.Add<T3>();
                builder.Add<T4>();
                builder.Add<T5>();
                builder.Add<T6>();
                builder.Add<T7>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            *chunk.Get<T3>(eti.IndexInChunk) = v3;
            *chunk.Get<T4>(eti.IndexInChunk) = v4;
            *chunk.Get<T5>(eti.IndexInChunk) = v5;
            *chunk.Get<T6>(eti.IndexInChunk) = v6;
            *chunk.Get<T7>(eti.IndexInChunk) = v7;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T3>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T3>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T4>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T4>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T5>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T5>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T6>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T6>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T7>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T7>(eti.IndexInChunk), this, et);
            return et;
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        public unsafe Entity CreateEntity<T1, T2, T3, T4, T5, T6, T7, T8>(in T1 v1 = default, in T2 v2 = default, in T3 v3 = default, in T4 v4 = default, in T5 v5 = default, in T6 v6 = default, in T7 v7 = default, in T8 v8 = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            Array.Clear(ComponentRegistry.ComponentTypeMaskBuffer);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T1>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T2>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T3>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T4>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T5>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T6>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T7>(), true);
            BitArrayOperator.SetBit(ComponentRegistry.ComponentTypeMaskBuffer, ComponentRegistry.GetId<T8>(), true);
            var hash = ComponentRegistry.GetHash(ComponentRegistry.ComponentTypeMaskBuffer);
            if (!ArchetypeGroup.ArchetypeMap.TryGetValue(hash, out var archetype))
            {
                using var builder = new ArchetypeBuilder(8);
                builder.Add<T1>();
                builder.Add<T2>();
                builder.Add<T3>();
                builder.Add<T4>();
                builder.Add<T5>();
                builder.Add<T6>();
                builder.Add<T7>();
                builder.Add<T8>();
                archetype = ArchetypeGroup.Create(hash, builder);
            }
            var et = archetype.CreateEntity(out var eti);
            var chunk = eti.Chunk;
            *chunk.Get<T1>(eti.IndexInChunk) = v1;
            *chunk.Get<T2>(eti.IndexInChunk) = v2;
            *chunk.Get<T3>(eti.IndexInChunk) = v3;
            *chunk.Get<T4>(eti.IndexInChunk) = v4;
            *chunk.Get<T5>(eti.IndexInChunk) = v5;
            *chunk.Get<T6>(eti.IndexInChunk) = v6;
            *chunk.Get<T7>(eti.IndexInChunk) = v7;
            *chunk.Get<T8>(eti.IndexInChunk) = v8;
            ComponentGenericMap<T1>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T1>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T2>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T2>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T3>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T3>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T4>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T4>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T5>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T5>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T6>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T6>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T7>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T7>(eti.IndexInChunk), this, et);
            ComponentGenericMap<T8>.Info.ComponentAwake?.Invoke(ref *(byte*)chunk.Get<T8>(eti.IndexInChunk), this, et);
            return et;
        }
    }
}