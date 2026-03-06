// ==================== qcbf@qq.com | 2026-03-04 ====================

#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CA2211
namespace FLib.WorldCores.Behaviors
{
    public static class BehaviorPool
    {
        public static Behavior[] Behaviors = new Behavior[256];

        // public static Type[] BehaviorIdTypes = new Type[8];
        // public static Dictionary<Type, byte> BehaviorTypeIds = new(8);
        public static readonly ConcurrentDictionary<Type, ConcurrentStack<int>> Frees = new();

        private static readonly object SyncLock = new();
        private static int _count;

        public static int Count => _count;

        /// <summary>
        /// 
        /// </summary>
        public static Behavior Rent(Type behaviorType)
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
            var behavior = Behaviors[index] = (Behavior)TypeAssistant.New(behaviorType);
            behavior.Id = index;
            // if (!BehaviorTypeIds.TryGetValue(behaviorType, out var id))
            // {
            //     lock (SyncLock)
            //     {
            //         id = checked((byte)(BehaviorTypeIds.Count + 1));
            //         BehaviorTypeIds.Add(behaviorType, id);
            //         if (BehaviorIdTypes.Length <= id)
            //             Array.Resize(ref BehaviorIdTypes, id * 2);
            //     }
            // 
            //     BehaviorIdTypes[id] = behaviorType;
            // }
            // behavior.TypeId = id;
            return behavior;
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Free(Behavior behavior)
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