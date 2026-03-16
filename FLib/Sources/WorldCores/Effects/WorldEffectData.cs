// ==================== qcbf@qq.com | 2026-03-09 ====================

namespace FLib.WorldCores.Effects
{
    public struct WorldEffectData
    {
        public static readonly WorldEffectData Default = new() { MaxStackCount = 1, Duration = FNum.One };
        public uint Id;
        public ushort MaxStackCount;
        public ushort StackCount;
        public FNum Duration;
        public BitFlags Flags;
        public EWorldEffectAddOption AddOption;
    }
}