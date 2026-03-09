// ==================== qcbf@qq.com | 2026-03-02 ====================

using FLib.WorldCores.Entities;
using FLib.WorldCores;

#pragma warning disable CA2211
namespace FLib.WorldCores.Components
{
    public static class WorldComponentEvents<T>
    {
        public delegate void Delegate(WorldCore world, WorldEntity entity, ref T component);

        public static Delegate OnAwake;
        public static Delegate OnDestroy;
        public static Delegate OnStart;
        // public static Delegate OnUpdate;
    }
}