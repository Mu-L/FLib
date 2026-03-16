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
        ref var fxSys = ref et.GetStaRef<WorldEffectSystem>();
        var fx = fxSys.Add(typeof(AEffect), default, 1);
        Assert.NotNull(fx);
        fx.Data.Flags.Add(1);
        Assert.True(fxSys.HasEffect(1));
        Assert.True(fxSys.HasFlags(1));
        for (var i = 0; i < WorldGlobalSetting.FrameRate / 2; i++)
            world.Update();
        Assert.Equal((FNum)0.5, fxSys.Get(1)!.Time.Remaining);
        for (var i = 0; i < WorldGlobalSetting.FrameRate / 2; i++)
            world.Update();
    }
}