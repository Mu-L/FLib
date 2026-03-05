// ==================={By Qcbf|qcbf@qq.com|9/18/2023 3:35:18 PM}===================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FLib.WorldCores.Behaviors
{
    [StructLayout(LayoutKind.Auto)]
    public unsafe struct DoBehaviorEvent
    {
        public bool IsFirst;
        internal BehaviorSystem* SystemPtr;
        public Behavior Behavior;

        public ref BehaviorSystem System => ref *SystemPtr;
        public ref EntityHelper Entity => ref SystemPtr->Self;
        public WorldCore World => SystemPtr->Self.World;
        public bool IsPrimary => System.PrimaryId == Behavior.Id;
        public bool IsEmpty => SystemPtr == null;

        public readonly ref readonly T GetParam<T>()
        {
            Debug.Assert(Behavior is Behavior<T>);
            return ref Behavior<T>.NewParam;
        }
    }
}