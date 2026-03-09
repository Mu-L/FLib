// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;

namespace FLib.WorldCores.Behaviors
{
    public readonly struct WorldSwapBehaviorEvent
    {
        public readonly Type OldPrimaryType;
        public readonly WorldBehavior NewPrimary;

        public WorldSwapBehaviorEvent(Type oldPrimaryType, WorldBehavior newPrimary)
        {
            OldPrimaryType = oldPrimaryType;
            NewPrimary = newPrimary;
        }
    }
}