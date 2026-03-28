// ==================== qcbf@qq.com | 2026-03-21 ====================

namespace FLib.WorldCores.SoaComponents
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct WorldSoaComponentHandle
    {
        public readonly int Index;
        public readonly WorldIncrementId TypeId;
        
        public WorldSoaComponentHandle(int index, WorldIncrementId typeId)
        {
            Index = index;
            TypeId = typeId;
        }
    }
}