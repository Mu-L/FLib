// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    [WorldComponentOption(options: EComponentOption.RejectSoa)]
    public struct WorldEffectSystem : IWorldAwake, IWorldDestroy
    {
        public uint Mask;
        public WorldEntityHelper Self;
        

        public void Awake(WorldCore world, WorldEntity entity)
        {
            world.SetDyn(entity, new WorldEffectContainer());
        }

        public void Destroy(WorldCore world, WorldEntity entity)
        {
            world.RemoveDyn<WorldEffectContainer>(entity);
        }
        
        public void Add()
        {
        }
    }
}