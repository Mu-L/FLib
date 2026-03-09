// ==================== qcbf@qq.com | 2026-01-10 ====================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FLib.WorldCores;
using FLib.WorldCores.Components;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.SoaComponents
{
    public class WorldSoaComponentGroup<T> : IWorldSoaComponentGroupable
    {
        public Stack<int> Frees = new();
        public int Count;
        internal T[] Components = Array.Empty<T>();

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
        public virtual void EnsureCapacity(int capacity)
        {
            if (Components.Length >= capacity) return;
            Array.Resize(ref Components, capacity);
            Frees.EnsureCapacity(capacity >> 2);
        }

        /// <summary>
        /// 
        /// </summary>
        int IWorldSoaComponentGroupable.Alloc(in WorldEntity et, object component) => Alloc(et, (T)component);

        /// <summary>
        /// 
        /// </summary>
        public virtual int Alloc(in WorldEntity et, in T component)
        {
            if (!Frees.TryPop(out var index))
            {
                if (Count >= Components.Length)
                    EnsureCapacity(MathEx.GetNextPowerOfTwo(Count + 1));
                index = Count;
            }

            ++Count;
            Components[index] = component;
            ref var first = ref MemoryMarshal.GetArrayDataReference(Components);
            first = ref Unsafe.Add(ref first, index);
            WorldComponentRegistry.GetInfo<T>().Awake?.Invoke(ref Unsafe.As<T, byte>(ref first), World, et);
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Free(in WorldEntity et, int index, bool onEntityDestroyed)
        {
            try
            {
                ref readonly var info = ref WorldComponentRegistry.GetInfo<T>();
                if (!onEntityDestroyed || info.Op(EComponentOption.AlwaysReceiveDestroy))
                    info.Destroy?.Invoke(ref Unsafe.As<T, byte>(ref Components[index]), World, et);
            }
            finally
            {
                if (!WorldComponentGenericMap<T>.Info.Op(EComponentOption.DoNotResetMemory))
                    Components[index] = default;
                --Count;
                if (index < Count)
                    Frees.Push(index);
            }
        }
    }
}