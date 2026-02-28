// ==================== qcbf@qq.com |2026-01-02 ====================

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    public sealed unsafe class Chunk : IObjectPoolActivatable, IObjectPoolDeactivatable
    {
        /// <summary>
        /// head: [entity...]
        /// body: [comp1..., comp2...]
        /// </summary>
        public byte* Buffer;

        /// <summary>
        /// 
        /// </summary>
        public ushort Count;

        /// <summary>
        /// 稀疏集的组件元数据，如果是静态组件就是内存偏移，如果是shared就是组件hash
        /// </summary>
        public int[] SparseComponentMeta;

        /// <summary>
        /// 
        /// </summary>
        public Chunk Previous;

        /// <summary>
        /// 
        /// </summary>
        public int AllSharedComponentsHash;

        /// <summary>
        /// 
        /// </summary>
        public QuerySharedComponent[] AllSharedComponents;

        void IObjectPoolActivatable.ObjectPoolActivate()
        {
            Buffer = GlobalSetting.ChunkAllocator.Alloc();
        }

        void IObjectPoolDeactivatable.ObjectPoolDeactivatable()
        {
            GlobalSetting.ChunkAllocator.Free(ref Buffer);
            ArrayPool<int>.Shared.Return(SparseComponentMeta);
            SparseComponentMeta = null;
            AllSharedComponents = null;
            AllSharedComponentsHash = Count = 0;
            Previous = null;
        }

        public override string ToString() => $"{Count}, {((IntPtr)Buffer).ToString("X")}";

        /// <summary>
        /// 
        /// </summary>
        public Entity* GetEntity(int entityIndex)
        {
            Debug.Assert(entityIndex < Count);
            return (Entity*)Buffer + entityIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        public IList<Entity> GetAllEntities(IList<Entity> array = null)
        {
            array ??= new Entity[Count];
            var count = Math.Min(array.Count, Count);
            for (var i = 0; i < count; i++)
                array[i] = *GetEntity(i);
            return array;
        }

        /// <summary>
        /// 
        /// </summary>
        internal ref T GetRef<T>(ushort entityIndex)
        {
            return ref *Get<T>(entityIndex);
        }

#pragma warning disable CS8500
        /// <summary>
        /// 
        /// </summary>
        internal T* Get<T>(ushort entityIndex)
        {
            Debug.Assert(entityIndex < Count);
            Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            Debug.Assert(Has(ComponentRegistry.GetId<T>()));
            return (T*)(Buffer + SparseComponentMeta[ComponentRegistry.GetId<T>()]) + entityIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void* Get(ushort entityIndex, in ComponentMeta meta)
        {
            Debug.Assert(entityIndex < Count);
            Debug.Assert(Has(meta.Id));
            return Buffer + SparseComponentMeta[meta.Id] + meta.Size * entityIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        public object GetObj(ushort entityIndex, in ComponentMeta meta)
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
        /// 
        /// </summary>
        public void ClearMemory(ushort entityIndex, in ComponentMeta meta)
        {
            Debug.Assert(entityIndex < Count);
            Debug.Assert(Has(meta.Id));
            Unsafe.InitBlock(Buffer + SparseComponentMeta[meta.Id] + meta.Size * entityIndex, 0, meta.Size);
        }

        /// <summary>
        /// 
        /// </summary>
        public Span<T> GetAll<T>() where T : unmanaged
        {
            Debug.Assert(Has(ComponentGenericMap<T>.Id));
            return new Span<T>(Buffer + SparseComponentMeta[ComponentGenericMap<T>.Id], Count);
        }

        /// <summary>
        /// 
        /// </summary>
        public IList GetAll(ushort entityIndex, Archetype archetype, IList list = null)
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
        public IList GetAll(in ComponentMeta meta, IList list = null)
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
                var group = world.Soa[sharedComponent.ComponentId];
                var index = ((ISharedComponentGroupable)group).GetIndexFromHash(sharedComponent.Hash);
                result.Add(group.Components.GetValue(index));
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public int GetMeta(IncrementId componentId)
        {
            Debug.Assert(Has(componentId));
            return SparseComponentMeta[componentId];
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has<T>()
        {
            return Has(ComponentRegistry.GetId<T>());
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has(IncrementId componentId)
        {
            return componentId.Raw <= SparseComponentMeta.Length && SparseComponentMeta[componentId] != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Has(IncrementId componentId, int meta)
        {
            return componentId.Raw <= SparseComponentMeta.Length && SparseComponentMeta[componentId] == meta;
        }
    }
}