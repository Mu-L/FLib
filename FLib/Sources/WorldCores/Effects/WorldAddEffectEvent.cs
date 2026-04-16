// ==================== qcbf@qq.com | 2026-03-10 ====================

using FLib.WorldCores.Entities;

namespace FLib.WorldCores.Effects
{
    public struct WorldAddEffectEvent
    {
        public WorldEffectBase Effect;
        public uint Id;
        public ushort AddCount;
        public WorldEntityId AddedBy;
    }
}