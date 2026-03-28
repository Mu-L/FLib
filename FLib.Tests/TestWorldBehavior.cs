// ==================== qcbf@qq.com | 2026-03-11 ====================

using FLib.WorldCores;
using FLib.WorldCores.Behaviors;
using FLib.WorldCores.Effects;
using Xunit;

namespace FLib.Tests;

[Flags]
public enum EBehaviors
{
    None,
    Idle = 1 << 0,
    Move = 1 << 1,
}

public class IdleBehavior : WorldBehavior
{
    public override uint Mask => (uint)EBehaviors.Idle;
}

public class MoveBehavior : WorldBehavior<MoveBehavior.ParamData>
{
    public struct ParamData
    {
        public byte Priority;
    }

    public override bool CheckFriend(WorldBehavior targetBehavior, bool isFirst) => targetBehavior is IdleBehavior;

    public override byte InitialPriority => Param.Priority;

    public override uint Mask => (uint)EBehaviors.Move;
}

public class ZBehavior : WorldBehavior
{
    public override byte InitialPriority => byte.MaxValue;

    public override uint Mask => (uint)EBehaviors.Idle;
}

public class TestWorldBehavior
{
    public TestWorldBehavior()
    {
        WorldBehaviorPool.EnsureCapacity([(typeof(IdleBehavior), 1), (typeof(MoveBehavior), 1)]);
    }

    [Fact]
    public void TestBasic()
    {
        using var world = new WorldCore();
        var et = world.CreateEntity().With<WorldBehaviorSystem>().BuildAsEntity();
        ref var bSys = ref et.Get<WorldBehaviorSystem>();
        Assert.Equal(typeof(IdleBehavior), bSys.Primary?.GetType());
        Assert.True(bSys.HasPrimary);
        Assert.True(bSys.Do(typeof(MoveBehavior), new MoveBehavior.ParamData { Priority = 10 }));
        Assert.Equal(typeof(MoveBehavior), bSys.Primary?.GetType());
        Assert.Equal(typeof(IdleBehavior), bSys.Secondary?.GetType());
        Assert.Equal(10, ((MoveBehavior)bSys.Primary!).Priority);
        Assert.ThrowsAny<Exception>(() => et.Get<WorldBehaviorSystem>().Do(typeof(IdleBehavior), new MoveBehavior.ParamData { Priority = 10 }));
        Assert.True(bSys.Do<ZBehavior>());
        Assert.False(bSys.Do<IdleBehavior>());
        Assert.True(bSys.Stop<ZBehavior>());
        Assert.Equal(typeof(IdleBehavior), bSys.Primary?.GetType());
        Assert.False(bSys.HasSecondary);

        bSys.StopAll(isDoDefault: false);
        Assert.False(bSys.HasSecondary);
        Assert.False(bSys.HasPrimary);

        Assert.All(WorldBehaviorPool.Behaviors.Take(WorldBehaviorPool.Count), bhv => Assert.True(bhv.IsEmpty));
        Assert.Single(WorldBehaviorPool.AllFrees[typeof(IdleBehavior)]);
        Assert.Single(WorldBehaviorPool.AllFrees[typeof(MoveBehavior)]);
        Assert.Single(WorldBehaviorPool.AllFrees[typeof(ZBehavior)]);
    }
}