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
        public Stack<int> Frees = new();
        public int Count;
        internal T[] Components = Array.Empty<T>();
        internal WorldEntityId[] ComponentEntities = Array.Empty<WorldEntityId>();


        public WorldCore World { get; set; }
        Array IWorldSoaComponentGroupable.Components => Components;

        public virtual ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Components[index];
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
            Frees.EnsureCapacity(capacity >> 2);
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
            if (!Frees.TryPop(out var index))
            {
                if (Count >= Components.Length)
                    EnsureCapacity(MathEx.GetNextPowerOfTwo(Count + 1));
                index = Count;
            }

            ++Count;
            Components[index] = component;
            ComponentEntities[index] = et;
            WorldComponentRegistry.GetInfo<T>().Awake?.Invoke(ref Unsafe.As<T, byte>(ref Components[index]), World, et, true); // 目前主要是Destroy需要info判断是否调用组件的生命周期事件，Awake不需要，所以这里直接传入default
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
                info.Destroy?.Invoke(ref Unsafe.As<T, byte>(ref Components[index]), World, et, !onEntityDestroyed || info.Op(EComponentOption.AlwaysReceiveDestroy));
            }
            finally
            {
                ComponentEntities[index] = default;
                if (!WorldComponentGenericMap<T>.Info.Op(EComponentOption.DoNotResetMemory))
                    Components[index] = default;
                --Count;
                if (index < Count)
                    Frees.Push(index);
            }
        }

        public WorldComponentEnumerator<T> GetEnumerator() => new(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    }
}