// ==================== qcbf@qq.com | 2026-03-04 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace FLib.WorldCores.Behaviors
{
    public static class BehaviorPool
    {
        public static Behavior[] Behaviors = new Behavior[256];
        public static ConcurrentDictionary<Type, ConcurrentStack<int>> Frees = new();
        public static int Count;

        /// <summary>
        /// 
        /// </summary>
        public static Behavior Rent(Type behaviorType)
        {
            if (Frees.TryGetValue(behaviorType, out var frees) && frees.TryPop(out var index))
                return Behaviors[index];

            var behaviorArrayLength = Behaviors.Length;
            if (behaviorArrayLength >= Count)
            {
                lock (Frees)
                {
                    if (behaviorArrayLength >= Count)
                        Array.Resize(ref Behaviors, Behaviors.Length * 2);
                }
            }

            index = Interlocked.Increment(ref Count) - 1;
            var behavior = Behaviors[index] = (Behavior)TypeAssistant.New(behaviorType);
            behavior.Id = index;
            return behavior;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Free(Behavior behavior)
        {
            var frees = Frees.GetOrAdd(behavior.GetType(), _ => new ConcurrentStack<int>());
            frees.Push(behavior.Id);
            behavior.Id = -1;
        }
    }
}