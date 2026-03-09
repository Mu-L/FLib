// ==================={By Qcbf|qcbf@qq.com|9/18/2023 3:36:10 PM}===================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.Behaviors
{
    public readonly unsafe struct WorldStopBehaviorEvent
    {
        internal readonly WorldBehaviorSystem* SystemPtr;
        public readonly WorldBehavior Behavior;
        public readonly bool IsPrimary;

        public ref WorldBehaviorSystem System => ref *SystemPtr;
        public ref WorldEntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;

        public WorldStopBehaviorEvent(ref WorldBehaviorSystem system, WorldBehavior behavior, bool isPrimary)
        {
            SystemPtr = (WorldBehaviorSystem*)Unsafe.AsPointer(ref system);
            Behavior = behavior;
            IsPrimary = isPrimary;
        }
    }
}