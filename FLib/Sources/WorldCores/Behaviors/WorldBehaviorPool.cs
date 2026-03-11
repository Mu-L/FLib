// ==================== qcbf@qq.com | 2026-03-04 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#pragma warning disable CA2211
namespace FLib.WorldCores.Behaviors
{
    public static class WorldBehaviorPool
    {
        public static WorldBehavior[] Behaviors = new WorldBehavior[256];
        public static ConcurrentDictionary<Type, ConcurrentStack<int>> AllFrees = new(WorldGlobalSetting.ThreadConcurrencyLevel, 256);

        private static readonly object SyncLock = new();
        private static int _count;

        public static int Count => _count;

        /// <summary>
        /// 
        /// </summary>
        public static WorldBehavior Rent(Type behaviorType)
        {
            if (AllFrees.TryGetValue(behaviorType, out var frees) && frees.TryPop(out var index))
                return Behaviors[index];

            if (_count >= Behaviors.Length)
            {
                lock (SyncLock)
                {
                    if (_count >= Behaviors.Length)
                        Array.Resize(ref Behaviors, Behaviors.Length * 2);
                }
            }

            return NewBehavior(behaviorType);
        }

        /// <summary>
        /// 
        /// </summary>
        public static unsafe void Free(WorldBehavior behavior)
        {
            behavior.SystemPtr = null;
            AllFrees.GetOrAdd(behavior.GetType(), _ => new ConcurrentStack<int>()).Push(behavior.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void EnsureCapacity((Type, int)[] capacities)
        {
            var allCount = capacities.Sum(v => v.Item2);
            Behaviors = new WorldBehavior[allCount];
            AllFrees = new ConcurrentDictionary<Type, ConcurrentStack<int>>(WorldGlobalSetting.ThreadConcurrencyLevel, allCount);
            for (var i = 0; i < capacities.Length; i++)
            {
                var type = capacities[i].Item1;
                var count = capacities[i].Item2;
                var stack = AllFrees[type] = new ConcurrentStack<int>();
                for (var j = 0; j < count; j++)
                    stack.Push(NewBehavior(type).Id);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static WorldBehavior NewBehavior(Type behaviorType)
        {
            var index = Interlocked.Increment(ref _count) - 1;
            var behavior = Behaviors[index] = (WorldBehavior)TypeAssistant.New(behaviorType);
            behavior.Id = index;
            return behavior;
        }
    }
}