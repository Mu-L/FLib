// ==================== qcbf@qq.com | 2026-02-26 ====================

using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    public interface IWorldStart
    {
        void OnComponentStart(WorldCore world, WorldEntityId eId);
    }
}