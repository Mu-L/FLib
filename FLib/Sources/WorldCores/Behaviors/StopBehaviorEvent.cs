// ==================={By Qcbf|qcbf@qq.com|9/18/2023 3:36:10 PM}===================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FLib.WorldCores.Behaviors
{
    public readonly unsafe struct StopBehaviorEvent
    {
        internal readonly BehaviorSystem* SystemPtr;
        public readonly Behavior Behavior;
        public readonly bool IsPrimary;

        public ref BehaviorSystem System => ref *SystemPtr;
        public ref EntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsEmpty => SystemPtr == null;

        public StopBehaviorEvent(ref BehaviorSystem system, Behavior behavior, bool isPrimary)
        {
            SystemPtr = (BehaviorSystem*)Unsafe.AsPointer(ref system);
            Behavior = behavior;
            IsPrimary = isPrimary;
        }
    }
}