// ==================== qcbf@qq.com | 2026-01-03 ====================

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using FLib.WorldCores;

namespace FLib.Tests;

public struct Player
{
    public string Name;
}

public struct Team
{
    public byte Value;
    public byte TestAlign1;
    public byte TestAlign2;
    public override string ToString() => Value.ToString();

    public void ComponentUpdate(WorldCore world, Entity entity)
    {
        ++world.Get<Actor>(entity).Id;
        ++Value;
    }
}

public struct Actor
{
    public uint Id;
}

public struct Enemy
{
    public uint Ai;
}

public struct Buff
{
    public string Name;
}

public struct ManagedSystem : IAwakeSystem, IDestroySystem, IUpdateSystem
{
    public string[] Values;
    public void Awake(WorldCore world, Entity entity) => Values = [nameof(Awake), string.Empty, string.Empty];
    public void Destroy(WorldCore world, Entity entity) => Values[2] = nameof(Destroy);
    public void Update(WorldCore world, Entity entity) => Values[1] = nameof(Update);
}

public struct UnmanagedSystem : IAwakeSystem, IDestroySystem, IUpdateSystem
{
    public int Value;
    public void Awake(WorldCore world, Entity entity) => ++Value;
    public void Destroy(WorldCore world, Entity entity) => ++Value;
    public void Update(WorldCore world, Entity entity) => ++Value;
}

public record struct Shared(int Value) : ISharedComponent;

public class TestWorldCore
{
    [Fact]
    [SuppressMessage("Usage", "CA2263:类型已知时首选泛型重载")]
    public void BasicAll()
    {
        var world = new WorldCore();
        ComponentRegistry.GetMeta<Buff>();
        var player1 = world.CreateEntity().WithMng<Player>().With<Team>().With<Actor>().Build();
        world.SetSta(player1, new Team { Value = 5 });
        world.SetStaMng(player1, new Player { Name = "p1" });

        var player2 = world.CreateEntity().With<Team>().With<Actor>().WithMng<Player>().Build();
        world.Set(player2, new Team { Value = 10 });

        var enemy1 = world.CreateEntity().With<Enemy>().With<Team>().With<Actor>().Build();
        world.Set(enemy1, new Team { Value = 100 });

        Assert.Equal(world.EntityInfos[player1.Id].ArchetypeIndex, world.EntityInfos[player2.Id].ArchetypeIndex);
        Assert.False(world.HasSta<Enemy>(player1));
        Assert.False(world.HasStaMng<Player>(enemy1));
        Assert.NotEqual(world.EntityInfos[player1.Id].ArchetypeIndex, world.EntityInfos[enemy1.Id].ArchetypeIndex);
        Assert.ThrowsAny<Exception>(() => world.SetSta(player1, new Enemy()));
        Assert.Equal(5, world.GetSta<Team>(player1).Val.Value);
        Assert.Equal(10, world.GetStaRef<Team>(player2).Value);
        Assert.Equal(100, world.Get<Team>(enemy1).Value);
        Assert.Equal("p1", world.GetStaMng<Player>(player1).Val.Name);
        Assert.Null(world.GetStaMng<Player>(player2).Val.Name);

        Assert.Equal([5, 10, 100], world.Query<Team>().Select(v => v.Item2.Val.Value));
        Assert.Equal([5, 10], world.Query<Team>(world.CreateQuery().WithAll<Team>().WithNone<Enemy>()).Select(v => v.Item2.Val.Value));

        // entity
        Assert.Equal(["FLib.Tests.Player", "5", "FLib.Tests.Actor"], world.GetAllEntities(player1).Select(v => v.ToString()));
        world.RemoveEntity(player1);
        Assert.False(world.HasEntity(player1));
        Assert.ThrowsAny<Exception>(() => world.GetSta<Team>(player1));
        Assert.Equal(10, world.GetSta<Team>(player2).Val.Value);
        Assert.Equal(1, world.GetEntityInfo(player2).Chunk.Count);

        // managed
        player1 = world.CreateEntity().WithMng<Player>().With<Team>().With<Actor>().Build();
        world.SetSta(player1, new Team { Value = 6 });
        Assert.Equal(6, world.GetSta<Team>(player1).Val.Value);
        Assert.Equal(10, world.Get<Team>(player2).Value);
        Assert.Equal(100, world.GetSta<Team>(enemy1).Val.Value);

        // dynamic
        Assert.False(world.HasDyn<Buff>(player1));
        world.SetDyn(player1, new Buff { Name = "abc" });
        Assert.Equal([player2, player1], world.CreateQuery().WithNone<Enemy>().Query());

        Assert.True(world.Has<Buff>(player1));
        Assert.Equal("abc", world.GetDyn<Buff>(player1).Name);
        Assert.Equal("abc", ((Buff)world.GetDyn(player1, typeof(Buff))).Name);
        world.RemoveDyn<Buff>(player1);
        Assert.False(world.HasDyn<Buff>(player1));
        Assert.ThrowsAny<Exception>(() => world.GetDyn<Buff>(player1));
        world.SetDyn(player1, new Buff { Name = "aaa" }, null);
        Assert.Equal("aaa", world.GetDyn<Buff>(player1).Name);
        world.Remove<Buff>(player1);
        Assert.False(world.Has<Buff>(player1));
        Assert.Equal(0, world.Soa.GetGroup<Buff>().Count);
        world.SetDyn(player1, new Buff { Name = "abc2" });
        Assert.Equal(1, world.Soa.GetGroup<Buff>().Count);
        world.RemoveEntity(player1);
        Assert.Equal(0, world.Soa.GetGroup<Buff>().Count);

        // dispose
        world.Dispose();
        Assert.True(GlobalSetting.ChunkAllocator.FreePagesCount >= 2);
        Assert.Empty((IEnumerable)typeof(Mng<Player>).GetField("_objects", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!);
    }


    [Fact]
    public void SharedComponent()
    {
        using var world = new WorldCore();
        var et1 = world.CreateEntity().With<Team>().With<Actor>().WithMng<Player>().WithShared<Shared>().Build();
        Assert.Equal(0, world.GetEntityInfo(et1).Chunk.AllSharedComponentsHash);

        world.SetSta(et1, new Team { Value = 10 });
        world.SetShared(et1, new Shared(1));

        Assert.NotEqual(0, world.GetEntityInfo(et1).Chunk.AllSharedComponentsHash);
        Assert.Equal(10, world.Get<Team>(et1).Value);

        var et2 = world.CreateEntity().With<Team>().With<Actor>().WithMng<Player>().WithShared<Shared>().Build();
        world.SetSta(et2, new Team { Value = 10 });
        world.SetShared(et2, new Shared(10));

        Assert.Equal([et1], world.CreateQuery().WithShared(new Shared(1)).Query());
        Assert.Equal([et2], world.CreateQuery().WithShared(new Shared(10)).Query());
        Assert.Equal([et1, et2], world.CreateQuery().WithAll<Team>().Query());
    }


    [Fact]
    public void ComponentSystem()
    {
        using var world = new WorldCore();
        var et = world.CreateEntity().With<Team>().With<Actor>().WithMng<Player>().WithShared<Shared>().With<UnmanagedSystem>().Build();
        world.Set(et, new ManagedSystem());

        Assert.Equal([nameof(IAwakeSystem.Awake), string.Empty, string.Empty], world.Get<ManagedSystem>(et).Values);

        world.Update();
        Assert.Equal([nameof(IAwakeSystem.Awake), nameof(IUpdateSystem.Update), string.Empty], world.Get<ManagedSystem>(et).Values);
    }
}