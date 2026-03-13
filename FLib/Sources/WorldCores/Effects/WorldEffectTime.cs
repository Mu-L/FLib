// ==================== qcbf@qq.com | 2026-03-13 ====================

using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    /// <summary>
    /// 
    /// </summary>
    public struct WorldEffectTime : IWorldUpdate
    {
        public readonly WorldEffect Effect;
        public FNum EndTime;

        public WorldEffectTime(WorldEffect effect)
        {
            Effect = effect;
            EndTime = effect.World.Time + effect.Data.Duration;
        }

        public void Update(WorldCore world, WorldEntity entity)
        {
            if (world.Time >= EndTime)
                Effect.RemoveSelf();
        }

        public void RefreshTime(in FNum time)
        {
            EndTime = time + Effect.Data.Duration;
        }
    }
}