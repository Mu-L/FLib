// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FLib.WorldCores
{
    public class SoaComponentGroup<T> : ISoaComponentGroupable
    {
        public Stack<int> Frees = new();
        public int Count;
        internal T[] Components = Array.Empty<T>();

        public WorldCore World { get; set; }
        Array ISoaComponentGroupable.Components => Components;

        public virtual ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Components[index];
        }

        public SoaComponentGroup(WorldCore world)
        {
            World = world;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void EnsureCapacity(int capacity)
        {
            if (Components.Length >= capacity) return;
            Array.Resize(ref Components, capacity);
            Frees.EnsureCapacity(capacity >> 2);
        }

        /// <summary>
        /// 
        /// </summary>
        int ISoaComponentGroupable.Alloc(in Entity et, object component) => Alloc(et, (T)component);

        /// <summary>
        /// 
        /// </summary>
        public virtual int Alloc(in Entity et, in T component)
        {
            if (!Frees.TryPop(out var index))
            {
                if (Count >= Components.Length)
                    EnsureCapacity(MathEx.GetNextPowerOfTwo(Count + 1));
                index = Count;
            }

            ++Count;
            Components[index] = component;
            InvokeAwake(et, index);
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Free(in Entity et, int index)
        {
            try
            {
                InvokeDestroy(et, index);
            }
            finally
            {
                if (!ComponentGenericMap<T>.Info.Op(EComponentOption.DoNotResetMemory))
                    Components[index] = default;
                --Count;
                if (index < Count)
                    Frees.Push(index);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void InvokeDestroy(in Entity et, int index)
        {
            ComponentRegistry.GetInfo<T>().Destroy?.Invoke(ref Unsafe.As<T, byte>(ref Components[index]), World, et);
        }

        /// <summary>
        /// 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void InvokeAwake(in Entity et, int index)
        {
            ref var first = ref MemoryMarshal.GetArrayDataReference(Components);
            first = ref Unsafe.Add(ref first, index);
            ComponentRegistry.GetInfo<T>().Awake?.Invoke(ref Unsafe.As<T, byte>(ref first), World, et);
        }
    }
}