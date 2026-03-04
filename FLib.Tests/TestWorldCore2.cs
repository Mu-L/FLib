// ==================== qcbf@qq.com | 2026-03-01 ====================

using FLib.WorldCores;

namespace FLib.Tests;

public class TestWorldCore2
{
    public struct Comp1 : ILifecycleDestroy
    {
        public int Value;

        public void Destroy(WorldCore world, Entity entity)
        {
            world.Set(entity, new Comp2() { Value = Value * 10 });
        }
    }

    public struct Comp2 : ILifecycleDestroy
    {
        public int Value;

        public void Destroy(WorldCore world, Entity entity)
        {
            Value *= 10;
            world.Get<Comp2>(entity);
            world.Set(entity, new Comp1() { Value = Value * 10 });
        }
    }


    [Fact]
    public void Test()
    {
        using var world = new WorldCore();
        var et = world.BuildEntity().Build();
        world.Set(et, new Comp1() { Value = 123 });
        world.Remove<Comp1>(et);
        Assert.Equal(123 * 10, ((Comp2)world.GetAll(et)[0]!).Value);

        Assert.ThrowsAny<Exception>(() => world.RemoveEntity(et));
    }
}