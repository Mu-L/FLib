// ==================== qcbf@qq.com | 2026-03-13 ====================

using FLib.WorldCores;
using FLib.WorldCores.Effects;
using FLib.WorldCores.Entities;

namespace FLib.Tests;

public class TestWorldEffect
{
    public class AEffect : WorldEffectBase
    {
        public int Value;
        public uint Flags;
        
        public override uint FlagsMask => Flags;
        
        public override void OnDestroy()
        {
            RemoveSelf();
        }
    }
    
    public TestWorldEffect()
    {
        WorldGlobalSetting.CreateEffectHandler = (in _, in _, _, _) => new AEffect { MaxStackCount = 1, Duration = 1, Flags = 1 };
    }
    
    [Fact]
    public void Basic()
    {
        using var world = new WorldCore();
        var et = world.CreateEntityBuilder().With<WorldEffectSystem>().BuildAsEntity();
        ref var fxSys = ref et.GetStaRef<WorldEffectSystem>();
        var fx = fxSys.Add(default, 1);
        Assert.NotNull(fx);
        Assert.True(fxSys.HasEffect(1));
        Assert.True(fxSys.HasFlags(1));
        for (var i = 0; i < WorldGlobalSetting.FrameRate / 2; i++)
            world.Update();
        Assert.Equal(FNum.Round((FNum)0.5 * 100), FNum.Round(fxSys.Get(1)!.Time.Remaining * 100));
        for (var i = 0; i < WorldGlobalSetting.FrameRate / 2; i++)
            world.Update();
        
        Assert.False(fxSys.HasEffect(1));
        Assert.False(fxSys.HasFlags(1));
        
        fxSys.Add(default, 1);
        et.RemoveSelf();
        
        Assert.Null(WorldEffectPool.Containers.Frees);
        Assert.Empty(WorldEffectPool.Containers);
    }
}