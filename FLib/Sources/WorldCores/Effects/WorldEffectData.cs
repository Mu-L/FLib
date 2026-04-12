// ==================== qcbf@qq.com | 2026-03-09 ====================

namespace FLib.WorldCores.Effects
{
    public struct WorldEffectData
    {
        public FNum Duration;
        public ushort MaxStackCount;
        public ushort StackCount;
        public EWorldEffectAddOption AddOption;
        [BitFlagsType()] public BitFlags Flags;
        
        public override string ToString() => Json5.Serialize(this);
    }
}