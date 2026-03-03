// ==================== qcbf@qq.com | 2026-03-03 ====================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FLib.WorldCores.Behaviors
{
    public struct BehaviorSystem : ILifecycleAwake
    {
        public static BehaviorScript[] AllScripts;
        public Dictionary<Type, ConcurrentStack<BehaviorScript>> FreeScripts;

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
        }


        public void Stop(int index)
        {
        }
    }
}