// ==================== qcbf@qq.com | 2026-03-04 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CA2211
namespace FLib.WorldCores.Behaviors
{
    public static class WorldBehaviorPool
    {
        public static WorldBehavior[] Behaviors = new WorldBehavior[256];
        public static readonly ConcurrentDictionary<Type, ConcurrentStack<int>> Frees = new();

        private static readonly object SyncLock = new();
        private static int _count;

        public static int Count => _count;

        /// <summary>
        /// 
        /// </summary>
        public static WorldBehavior Rent(Type behaviorType)
        {
            if (Frees.TryGetValue(behaviorType, out var frees) && frees.TryPop(out var index))
                return Behaviors[index];

            if (_count >= Behaviors.Length)
            {
                lock (SyncLock)
                {
                    if (_count >= Behaviors.Length)
                        Array.Resize(ref Behaviors, Behaviors.Length * 2);
                }
            }

            index = Interlocked.Increment(ref _count) - 1;
            var behavior = Behaviors[index] = (WorldBehavior)TypeAssistant.New(behaviorType);
            behavior.Id = index;
            return behavior;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Free(WorldBehavior behavior)
        {
            var frees = Frees.GetOrAdd(behavior.GetType(), _ => new ConcurrentStack<int>());
            frees.Push(behavior.Id);
            unsafe
            {
                behavior.SystemPtr = null;
            }
        }
    }
}