// ==================== qcbf@qq.com | 2026-03-13 ====================

using FLib.WorldCores;
using FLib.WorldCores.Effects;

namespace FLib.Tests;

public class TestWorldEffect
{
    public class AEffect : WorldEffect
    {
        public int Value;
    }

    [Fact]
    public void Basic()
    {
        using var world = new WorldCore();
        var et = world.BuildEntity().With<WorldEffectSystem>().BuildAsEntityHelper();
        ref var effect = ref et.GetStaRef<WorldEffectSystem>();
        
    }
}