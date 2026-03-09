// ==================== qcbf@qq.com | 2026-03-09 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.Effects
{
    public static class WorldEffectPool
    {
        public static readonly ConcurrentDictionary<Type, ConcurrentStack<WorldEffect>> AllFrees = new();
        [ThreadStatic] public static FixedIndexList<WorldEffectContainer> Containers;

        /// <summary>
        /// 
        /// </summary>
        public static unsafe WorldEffect Rent(Type type, ref WorldEffectSystem system)
        {
            if (!AllFrees.TryGetValue(type, out var frees) || !frees.TryPop(out var effect))
                effect = (WorldEffect)Activator.CreateInstance(type)!;
            effect.SystemPtr = (WorldEffectSystem*)Unsafe.AsPointer(ref system);
            return effect;
        }

        /// <summary>
        /// 
        /// </summary>
        public static unsafe void Free(WorldEffect effect)
        {
            effect.SystemPtr = null;
            AllFrees.GetOrAdd(effect.GetType(), _ => new ConcurrentStack<WorldEffect>()).Push(effect);
        }
    }
}