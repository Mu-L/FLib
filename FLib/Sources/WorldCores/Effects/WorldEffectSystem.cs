// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;
using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public struct WorldEffectSystem : IWorldAwake, IWorldDestroy
    {
        public uint Mask;
        public WorldEntityHelper Self;

        private int _containerIndex;


        public void Awake(WorldCore world, WorldEntity entity)
        {
            _containerIndex = world.SetDyn(entity, new WorldEffectContainer());
        }

        public void Destroy(WorldCore world, WorldEntity entity)
        {
            _containerIndex = -1;
            world.RemoveDyn<WorldEffectContainer>(entity);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Add(WorldEffect effect)
        {
        }
    }
}