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
        public readonly WorldEffectBase Effect;
        public FNum EndTime;

        public readonly FNum Remaining => EndTime - Effect.World.Time;

        public WorldEffectTime(WorldEffectBase effect)
        {
            Effect = effect;
            EndTime = Effect.World.Time + Effect.Duration;
        }

        public void OnComponentUpdate(WorldCore world, WorldEntityId entityId)
        {
            if (world.Time >= EndTime)
                Effect.RemoveSelf();
        }

        public void ResetTime(in FNum time)
        {
            EndTime = time + Effect.Duration;
        }
    }
}