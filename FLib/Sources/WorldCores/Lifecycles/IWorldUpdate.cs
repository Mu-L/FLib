// ==================== qcbf@qq.com | 2026-02-26 ====================

using FLib.WorldCores.Entities;

namespace FLib.WorldCores
{
    public interface IWorldUpdate
    {
        void Update(WorldCore world, WorldEntity entity);
    }
}