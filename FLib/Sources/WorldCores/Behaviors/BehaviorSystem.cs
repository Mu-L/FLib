// ==================== qcbf@qq.com | 2026-03-03 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FLib.WorldCores.Behaviors
{
    public struct BehaviorSystem : ILifecycleAwake
    {
        public static Behavior[] AllScripts;
        public static Dictionary<Type, ConcurrentStack<Behavior>> FreeScripts;

        public uint Mask;
        public int PrimaryIndex;
        public int SecondaryIndex;

        public EntityHelper Self;

        void ILifecycleAwake.Awake(WorldCore world, Entity entity)
        {
            Self = new EntityHelper(world, entity);
        }

        public void Do(Type behaviorType)
        {
            Behavior bhv;
            if (FreeScripts?.TryGetValue(behaviorType, out var frees) != true || !frees.TryPop(out bhv))
                bhv = (Behavior)TypeAssistant.New(behaviorType);
        }

        public void Stop(int index)
        {
        }

        public readonly bool IsRunning(uint mask)
        {
            return true;
        }

        public readonly bool IsRunning(Type behaviorType)
        {
            return true;
        }
    }
}