// ==================== qcbf@qq.com | 2026-03-21 ====================

using FLib.WorldCores;

namespace FLib.Sources.WorldCores.Components
{
    public readonly struct ComponentHandle
    {
        public readonly int Index;
        public readonly WorldIncrementId TypeId;

        public ComponentHandle(int index, WorldIncrementId typeId)
        {
            Index = index;
            TypeId = typeId;
        }
    }
}