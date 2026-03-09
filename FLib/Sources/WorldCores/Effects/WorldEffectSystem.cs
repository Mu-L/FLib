// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;

namespace FLib.WorldCores.Effects
{
    [WorldComponentOption(options: EComponentOption.RejectSoa)]
    public struct WorldEffectSystem : IWorldLifecycleAwake
    {
        public uint Mask;

        public void Awake(WorldCore world, WorldEntity entity)
        {
            world.SetDyn(entity, new WorldEffectContainer());
        }
    }
}