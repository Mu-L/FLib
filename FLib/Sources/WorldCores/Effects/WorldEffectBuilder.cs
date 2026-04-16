// ==================== qcbf@qq.com | 2026-03-16 ====================

#nullable enable
namespace FLib.WorldCores.Effects
{
    public readonly ref struct WorldEffectBuilder
    {
        public readonly WorldEffectBase? Effect;

        public WorldEffectBuilder(WorldEffectBase? effect)
        {
            Effect = effect;
        }
        
        
    }
}