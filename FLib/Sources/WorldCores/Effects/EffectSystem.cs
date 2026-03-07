// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;

namespace FLib.WorldCores.Effects
{
    [ComponentOption(options: EComponentOption.RejectSoa)]
    public struct EffectSystem : ILifecycleAwake
    {
        public uint Mask;

        public void Awake(WorldCore world, Entity entity)
        {
            world.SetDyn(entity, new EffectContainer());
        }
    }
}