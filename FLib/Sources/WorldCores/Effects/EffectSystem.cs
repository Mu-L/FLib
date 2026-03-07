// ==================== qcbf@qq.com | 2026-03-06 ====================

using System;

namespace FLib.WorldCores.Effects
{
    [ComponentOption(options: EComponentOption.RejectSoa, requiredComponents: new[] { typeof(Mng<EffectContainer>) })]
    public struct EffectSystem
    {
        public uint Mask;
    }
}