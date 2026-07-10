// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FLib.WorldCores;
using FLib.WorldCores.Components;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    public class WorldSoaComponentGroup<T> : IWorldSoaComponentGroupable, IEnumerable<T>
    {
        public StableIndexAllocator IndexAllocator;
        public WorldEntityId[] ComponentEntities = Array.Empty<WorldEntityId>();
        internal T[] Components = Array.Empty<T>();


        public WorldCore World { get; set; }
        public int Count => IndexAllocator.Count;
        Array IWorldSoaComponentGroupable.Components => Components;

        public virtual ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Components[index];
        }

        public WorldSoaComponentGroup(WorldCore world)
        {
            World = world;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool EnsureCapacity(int capacity)
        {
            if (Components.Length >= capacity) return false;
            Array.Resize(ref Components, capacity);
#if NET6_0_OR_GREATER
            IndexAllocator.Frees?.EnsureCapacity(capacity >> 2);
#endif
            Array.Resize(ref ComponentEntities, Components.Length);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public ref byte GetPointer(int index)
        {
            return ref Unsafe.As<T, byte>(ref Components[index]);
        }

        /// <summary>
        /// 
        /// </summary>
        int IWorldSoaComponentGroupable.Alloc(in WorldEntityId et, object component) => Alloc(et, (T)component);

        /// <summary>
        /// 
        /// </summary>
        public virtual int Alloc(in WorldEntityId et, in T component)
        {
            var index = AllocWithoutAwake(et, component);
            InvokeAwake(et, index);
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeAwake(WorldEntityId et, int componentIndex)
        {
            WorldComponentRegistry.GetInfo<T>().Awake.Invoke(ref Unsafe.As<T, byte>(ref Components[componentIndex]), World, et);
        }

        /// <summary>
        /// 
        /// </summary>
        internal int AllocWithoutAwake(in WorldEntityId et, in T component)
        {
            var index = IndexAllocator.Alloc();
            if (index >= Components.Length)
                EnsureCapacity(MathEx.GetNextCapacityLength(index));
            Components[index] = component;
            ComponentEntities[index] = et;
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Free(in WorldEntityId et, int index, bool onEntityDestroyed)
        {
            try
            {
                ref readonly var info = ref WorldComponentRegistry.GetInfo<T>();
                (!onEntityDestroyed || info.Flags(EComponentFlag.AlwaysReceiveDestroy) ? info.DestroyWithComponentSelf : info.Destroy)
                    .Invoke(ref Unsafe.As<T, byte>(ref Components[index]), World, et);
            }
            finally
            {
                ComponentEntities[index] = default;
                if (!WorldComponentGenericMap<T>.Info.Flags(EComponentFlag.DoNotResetMemory))
                    Components[index] = default;
                IndexAllocator.Free(index);
            }
        }

        public WorldComponentEnumerator<T> GetEnumerator() => new(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    }
}