// ==================== qcbf@qq.com | 2026-03-01 ====================

using FLib.WorldCores;

namespace FLib.Tests;

public class TestWorldCore2
{
    public struct Comp : IDestroySystem
    {
        public int Value;

        public void Destroy(WorldCore world, Entity entity)
        {
            world.Set(entity, new Comp());
        }
    }


    [Fact]
    public void Test()
    {
        using var world = new WorldCore();
        var et = world.CreateEntity().Build();
        world.Set(et, new Comp() { Value = 123 });

        world.Remove<Comp>(et);

        world.Set(et, new Comp());
    }
}