// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;

namespace FLib.WorldCores.Behaviors
{
    public readonly struct SwapBehaviorEvent
    {
        public readonly Type OldPrimaryType;
        public readonly Behavior NewPrimary;

        public SwapBehaviorEvent(Type oldPrimaryType, Behavior newPrimary)
        {
            OldPrimaryType = oldPrimaryType;
            NewPrimary = newPrimary;
        }
    }
}