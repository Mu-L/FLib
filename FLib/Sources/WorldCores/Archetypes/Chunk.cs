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
        /// 
        /// </summary>
        public int[] SparseComponentOffset;

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
            ArrayPool<int>.Shared.Return(SparseComponentOffset);
            SparseComponentOffset = null;
            AllSharedComponents = null;
            AllSharedComponentsHash = Count = 0;
            Previous = null;
        }

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
            return (T*)(Buffer + SparseComponentOffset[ComponentRegistry.GetId<T>()]) + entityIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        internal void* Get(ushort entityIndex, in ComponentMeta meta)
        {
            Debug.Assert(entityIndex < Count);
            return Buffer + SparseComponentOffset[meta.Id] + meta.Size * entityIndex;
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
            Unsafe.InitBlock(Buffer + SparseComponentOffset[meta.Id] + meta.Size * entityIndex, 0, meta.Size);
        }

        /// <summary>
        /// 
        /// </summary>
        public Span<T> GetAll<T>() where T : unmanaged
        {
            return new Span<T>(Buffer + SparseComponentOffset[ComponentGenericMap<T>.Id], Count);
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
        /// 这里默认外部传的组件id都是sharedComponent的
        /// </summary>
        public int GetSharedComponent(IncrementId componentId)
        {
            Debug.Assert(HasSharedComponent(componentId));
            return SparseComponentOffset[componentId];
        }

        /// <summary>
        /// 这里默认外部传的组件id都是sharedComponent的
        /// </summary>
        /// <returns></returns>
        public bool HasSharedComponent(IncrementId componentId, int hash)
        {
            return componentId.Raw <= SparseComponentOffset.Length && SparseComponentOffset[componentId] == hash;
        }

        /// <summary>
        /// 这里默认外部传的组件id都是sharedComponent的
        /// </summary>
        /// <returns></returns>
        public bool HasSharedComponent(IncrementId componentId)
        {
            return componentId.Raw <= SparseComponentOffset.Length && SparseComponentOffset[componentId] != 0;
        }
    }
}