// ==================={By Qcbf|qcbf@qq.com|9/18/2023 3:35:18 PM}===================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Behaviors
{
    [StructLayout(LayoutKind.Auto)]
    public unsafe struct WorldDoBehaviorEvent
    {
        public bool IsFirst;
        internal WorldBehaviorSystem* SystemPtr;
        public WorldBehavior Behavior;

        public ref WorldBehaviorSystem System => ref *SystemPtr;
        public ref WorldEntity Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsPrimary => System.PrimaryId == Behavior.Id;
        public bool IsEmpty => SystemPtr == null;


        public WorldDoBehaviorEvent(ref WorldBehaviorSystem bhvSys) : this() => SystemPtr = (WorldBehaviorSystem*)Unsafe.AsPointer(ref bhvSys);

        public readonly ref readonly T GetParam<T>()
        {
            Debug.Assert(Behavior is WorldBehavior<T>);
            return ref WorldBehavior<T>.NewParam;
        }
    }
}