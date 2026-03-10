// ==================== qcbf@qq.com | 2026-03-10 ====================

using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public struct WorldAddEffectEvent
    {
        public uint Id;
        public WorldEffect Effect;
        public WorldEntity AddedBy;
        public ushort AddCount;
    }
}