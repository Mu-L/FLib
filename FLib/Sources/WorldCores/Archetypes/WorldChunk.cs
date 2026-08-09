// ==================== qcbf@qq.com |2026-01-02 ====================

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FLib.WorldCores.SoaComponents;
using FLib.WorldCores.Queries;
using FLib.WorldCores.Entities;
using FLib.WorldCores.Components;
using FLib.WorldCores;

namespace FLib.WorldCores.Archetypes
{
    public sealed unsafe class WorldChunk : IObjectPoolActivatable, IObjectPoolDeactivatable
    {
        /// <summary>
        /// head: [entity...]
        /// body: [comp1..., comp2...]
        /// </summary>
        public byte* Buffer;

        /// <summary>
        /// 数据块中的实体数量。
        /// </summary>
        public ushort Count;

        /// <summary>
        /// 稀疏集的组件元数据，如果是静态组件就是内存偏移，如果是shared就是组件hash
        /// </summary>
        public int[] SparseComponentMeta;

        /// <summary>
        /// 前一个输出所有项模组件的较验掌。
        /// </summary>
        public int AllSharedComponentsHash;

        /// <summary> archetype所在的index </summary>
        internal short Index;

        /// <summary>
        /// 
        /// </summary>
        public WorldQuerySharedComponent[] AllSharedComponents;

        void IObjectPoolActivatable.ObjectPoolActivate()
        {
            Buffer = WorldSetting.ChunkAllocator.Alloc();
        }

        void IObjectPoolDeactivatable.ObjectPoolDeactivatable()
        {
            WorldSetting.ChunkAllocator.Free(ref Buffer);
            ArrayPool<int>.Shared.Return(SparseComponentMeta);
            SparseComponentMeta = null;
            AllSharedComponents = null;
            AllSharedComponentsHash = Count = 0;
        }

        public override string ToString() => $"{Count}, {((IntPtr)Buffer).ToString("X")}";

        /// <summary>
        /// 执一个数据块中的实体指针。
        /// </summary>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <returns>实体指针</returns>
        public WorldEntityId* GetEntity(int entityIndex)
        {
            Debug.Assert(entityIndex < Count);
            return (WorldEntityId*)Buffer + entityIndex;
        }

        /// <summary>
        /// 获取数据块中所有实体。
        /// </summary>
        /// <param name="array">用于存储实体的数组，为 null 时会创建新数组</param>
        /// <returns>包含数据块中所有实体的数组</returns>
        public IList<WorldEntityId> GetAllEntities(IList<WorldEntityId> array = null)
        {
            array ??= new WorldEntityId[Count];
            var count = Math.Min(array.Count, Count);
            for (var i = 0; i < count; i++)
                array[i] = *GetEntity(i);
            return array;
        }

        /// <summary>
        /// 获取数据块中指定活会索引的特定类型组件的引用。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <returns>组件的引用</returns>
        internal ref T GetRef<T>(ushort entityIndex)
        {
            return ref Unsafe.AsRef<T>(Get(entityIndex, WorldComponentRegistry.GetMeta<T>()));
        }

        /// <summary>
        /// 获取数据块中指定实体的指季类型组件指针。
        /// </summary>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <returns>组件指针</returns>
        internal void* Get<T>(ushort entityIndex)
        {
            return Get(entityIndex, WorldComponentRegistry.GetMeta<T>());
        }

        /// <summary>
        /// 获取数据块中指定实体的指季类型组件指针。
        /// </summary>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <param name="meta">组件的元数据信息</param>
        /// <returns>组件指针</returns>
        internal void* Get(ushort entityIndex, in WorldComponentMeta meta)
        {
            Debug.Assert(entityIndex < Count);
            Debug.Assert(Has(meta.Id));
            return Buffer + SparseComponentMeta[meta.Id] + meta.Size * entityIndex;
        }

        /// <summary>
        /// 获取数据块中指定实体的特定类型组件的对象表示。
        /// </summary>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <param name="meta">组件的元数据信息</param>
        /// <returns>空返回视影中的特定类型组件实例</returns>
        public object GetObj(ushort entityIndex, in WorldComponentMeta meta)
        {
            var ptr = Get(entityIndex, meta);
            object obj;
            if (meta.Type.IsGenericType)
            {
                obj = Activator.CreateInstance(meta.Type);
                var gch = GCHandle.Alloc(obj, GCHandleType.Pinned);
                try
                {
                    Unsafe.CopyBlockUnaligned((void*)gch.AddrOfPinnedObject(), ptr, meta.Size);
                }
                finally
                {
                    gch.Free();
                }
            }
            else
            {
                obj = Marshal.PtrToStructure((IntPtr)ptr, meta.Type);
            }

            return obj;
        }

        /// <summary>
        /// 清除数据块中指定实体的特定类型组件的内存。
        /// </summary>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <param name="meta">组件的元数据信息</param>
        public void ClearMemory(ushort entityIndex, in WorldComponentMeta meta)
        {
            Debug.Assert(entityIndex < Count);
            Debug.Assert(Has(meta.Id));
            Unsafe.InitBlock(Buffer + SparseComponentMeta[meta.Id] + meta.Size * entityIndex, 0, meta.Size);
        }

        /// <summary>
        /// 获取数据块中所有指定类型的组件的距離前缀型伏。
        /// </summary>
        /// <typeparam name="T">组件类型，必须是非托管类型</typeparam>
        /// <returns>整个数据块中该类型组件的距離前缀</returns>
        public Span<T> GetAll<T>() where T : unmanaged
        {
            Debug.Assert(Has(WorldComponentGenericMap<T>.Id));
            return new Span<T>(Buffer + SparseComponentMeta[WorldComponentGenericMap<T>.Id], Count);
        }

        /// <summary>
        /// 获取数据块中指定实体的所有组件。
        /// </summary>
        /// <param name="entityIndex">实体的索引位置</param>
        /// <param name="archetype">原型信息</param>
        /// <param name="list">用于存储组件的列表，为 null 时会创建新列表</param>
        /// <returns>包含实体所有组件的列表</returns>
        public IList GetAll(ushort entityIndex, WorldArchetype archetype, IList list = null)
        {
            list ??= new List<object>(Count);
            foreach (var meta in archetype.ComponentTypes)
            {
                var obj = GetObj(entityIndex, meta);
                list.Add(obj);
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        public IList GetAll(in WorldComponentMeta meta, IList list = null)
        {
            list ??= new List<object>(Count);
            for (ushort i = 0; i < Count; i++)
                list.Add(GetObj(i, meta));
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        public IList GetAllShared(WorldCore world, IList result = null)
        {
            result ??= new List<object>();
            foreach (var sharedComponent in AllSharedComponents)
            {
                var group = world.DynComponentGroups[sharedComponent.ComponentId];
                var index = ((IWorldSharedComponentGroupable)group).GetIndexFromHash(sharedComponent.Hash);
                result.Add(group.Components.GetValue(index));
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public int GetMeta(WorldIncrementId componentId)
        {
            Debug.Assert(Has(componentId));
            return SparseComponentMeta[componentId];
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has<T>()
        {
            return Has(WorldComponentRegistry.GetId<T>());
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has(WorldIncrementId componentId)
        {
            return componentId.Raw <= SparseComponentMeta.Length && SparseComponentMeta[componentId] != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has(WorldIncrementId componentId, int v)
        {
            return componentId.Raw <= SparseComponentMeta.Length && SparseComponentMeta[componentId] == v;
        }
    }
}