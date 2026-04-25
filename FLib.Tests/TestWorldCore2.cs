// ==================== qcbf@qq.com | 2026-03-01 ====================

using FLib.WorldCores.Entities;
using FLib.WorldCores;

namespace FLib.Tests;

public class TestWorldCore2
{
    [WorldComponentOption(options: EComponentOption.AlwaysReceiveDestroy)]
    public struct Comp1 : IWorldDestroy
    {
        public int Value;

        public void OnComponentDestroy(WorldCore world, WorldEntityId entityId)
        {
            world.Set(entityId, new Comp2() { Value = Value * 10 });
        }
    }

    [WorldComponentOption(options: EComponentOption.AlwaysReceiveDestroy)]
    public struct Comp2 : IWorldDestroy
    {
        public int Value;

        public void OnComponentDestroy(WorldCore world, WorldEntityId entityId)
        {
            Value *= 10;
            world.Get<Comp2>(entityId);
            world.Set(entityId, new Comp1() { Value = Value * 10 });
        }
    }


    [Fact]
    public void Test()
    {
        using var world = new WorldCore();
        var et = world.CreateEntityBuilder().Build();
        world.Set(et, new Comp1() { Value = 123 });
        world.Remove<Comp1>(et);
        Assert.Equal(123 * 10, ((Comp2)world.GetAll(et)[0]!).Value);

        Assert.ThrowsAny<Exception>(() => world.RemoveEntity(et));
    }
}