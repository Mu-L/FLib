// ==================== qcbf@qq.com | 2026-03-13 ====================

using System.Runtime.CompilerServices;
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

        public readonly FNum Remaining => EndTime - Effect.World.Time;

        public WorldEffectTime(WorldEffect effect)
        {
            Effect = effect;
            EndTime = default;
        }

        public void OnUpdate(WorldCore world, WorldEntity entity)
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