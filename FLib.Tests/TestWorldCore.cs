// ==================== qcbf@qq.com | 2026-01-03 ====================

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;
using System.Numerics;
using System.Reflection;
using FLib.WorldCores.Components;
using FLib.WorldCores;
using FLib.WorldCores.Entities;

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

    public void ComponentUpdate(WorldCore world, WorldEntityId eId)
    {
        ++world.Get<Actor>(eId).Id;
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

[WorldCores.WorldComponentOption(options: EComponentOption.DoNotResetMemory | EComponentOption.AlwaysReceiveDestroy)]
public struct Managed : IWorldAwake, IWorldDestroy, IWorldUpdate, IWorldStart
{
    public List<string> Values;
    public uint AwakeFrame;
    public uint StartFrame;
    public uint UpdateFrame;

    public void OnAwake(WorldCore world, WorldEntityId entityId)
    {
        Values = [nameof(OnAwake)];
        AwakeFrame = world.Frame;
    }

    public void OnStart(WorldCore world, WorldEntityId eId)
    {
        Values.Add(nameof(OnStart));
        StartFrame = world.Frame;
    }

    public void OnDestroy(WorldCore world, WorldEntityId entityId) => Values.Add(nameof(OnDestroy));

    public void OnUpdate(WorldCore world, WorldEntityId entityId)
    {
        Values.Add(nameof(OnUpdate));
        UpdateFrame = world.Frame;
    }
}

public record struct Shared(int Value) : IWorldSharedComponent;

public class TestWorldCore
{
    [Fact]
    [SuppressMessage("Usage", "CA2263:类型已知时首选泛型重载")]
    public void BasicAll()
    {
        var world = new WorldCore();

        var et = world.BuildEntity().Build();
        Assert.False(world.Has<Player>(et));
        world.RemoveEntity(et);
        
        WorldComponentRegistry.GetMeta<Buff>();
        var player1 = world.BuildEntity().WithMng<Player>().With<Team>().With<Actor>().Build();
        world.SetSta(player1, new Team { Value = 5 });
        world.SetStaMng(player1, new Player { Name = "p1" });

        var player2 = world.BuildEntity().With<Team>().With<Actor>().WithMng<Player>().Build();
        world.Set(player2, new Team { Value = 10 });

        var enemy1 = world.BuildEntity().With<Enemy>().With<Team>().With<Actor>().Build();
        world.Set(enemy1, new Team { Value = 100 });

        Assert.Equal(world.Entities[player1.Id].ArchetypeIndex, world.Entities[player2.Id].ArchetypeIndex);
        Assert.False(world.HasSta<Enemy>(player1));
        Assert.False(world.HasStaMng<Player>(enemy1));
        Assert.NotEqual(world.Entities[player1.Id].ArchetypeIndex, world.Entities[enemy1.Id].ArchetypeIndex);
        Assert.ThrowsAny<Exception>(() => world.SetSta(player1, new Enemy()));
        Assert.Equal(5, world.GetSta<Team>(player1).Val.Value);
        Assert.Equal(10, world.GetStaRef<Team>(player2).Value);
        Assert.Equal(100, world.Get<Team>(enemy1).Value);
        Assert.Equal("p1", world.GetStaMng<Player>(player1).Val.Name);
        Assert.Null(world.GetStaMng<Player>(player2).Val.Name);

        var v = world.Query<Team>().Select(v => v.Item2.Val.Value).ToArray();
        Assert.Equal([5, 10, 100], world.Query<Team>().Select(v => v.Item2.Val.Value));
        Assert.Equal([5, 10], world.Query<Team>(world.BuildQuery().WithAll<Team>().WithNone<Enemy>()).Select(v => v.Item2.Val.Value));

        // entity
        Assert.Equal(["FLib.Tests.Player", "5", "FLib.Tests.Actor"], world.GetAllEntities(player1).Select(v1 => v1.ToString()));
        world.RemoveEntity(player1);
        Assert.False(world.HasEntity(player1));
        Assert.ThrowsAny<Exception>(() => world.GetSta<Team>(player1));
        Assert.Equal(10, world.GetSta<Team>(player2).Val.Value);
        Assert.Equal(1, world.GetEntityInfo(player2).Chunk.Count);

        // managed
        player1 = world.BuildEntity().WithMng<Player>().With<Team>().With<Actor>().Build();
        world.SetSta(player1, new Team { Value = 6 });
        Assert.Equal(6, world.GetSta<Team>(player1).Val.Value);
        Assert.Equal(10, world.Get<Team>(player2).Value);
        Assert.Equal(100, world.GetSta<Team>(enemy1).Val.Value);

        // dynamic
        Assert.False(world.HasDyn<Buff>(player1));
        world.SetDyn(player1, new Buff { Name = "abc" });
        Assert.Equal([player2, player1], world.BuildQuery().WithNone<Enemy>().Query());

        Assert.True(world.Has<Buff>(player1));
        Assert.Equal("abc", world.GetDyn<Buff>(player1).Name);
        Assert.Equal("abc", ((Buff)world.GetDyn(player1, typeof(Buff))).Name);
        world.RemoveDyn<Buff>(player1);
        Assert.False(world.HasDyn<Buff>(player1));
        Assert.ThrowsAny<Exception>(() => world.GetDyn<Buff>(player1));
        world.SetDyn(player1, null!, new Buff { Name = "aaa" });
        Assert.Equal("aaa", world.GetDyn<Buff>(player1).Name);
        world.Remove<Buff>(player1);
        Assert.False(world.Has<Buff>(player1));
        Assert.Equal(0, world.Soa.GetGroup<Buff>().Count);
        world.SetDyn(player1, new Buff { Name = "abc2" });
        Assert.Equal(1, world.Soa.GetGroup<Buff>().Count);

        // get all
        Assert.Equal([typeof(Mng<Player>), typeof(Team), typeof(Actor), typeof(Buff)], ((List<object>)world.GetAll(player1)).Select(v => v.GetType()));

        // dispose
        world.RemoveEntity(player1);
        Assert.Equal(0, world.Soa.GetGroup<Buff>().Count);
        world.Dispose();
        // Assert.True(WorldGlobalSetting.ChunkAllocator.FreePagesCount >= 2);
        // Assert.Empty((IEnumerable)typeof(Mng<Player>).GetField("_objects", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!);
    }


    [Fact]
    public void SharedComponent()
    {
        using var world = new WorldCore();
        var et1 = world.BuildEntity().With<Team>().With<Actor>().WithMng<Player>().WithShared<Shared>().Build();
        Assert.Equal(0, world.GetEntityInfo(et1).Chunk.AllSharedComponentsHash);

        world.SetSta(et1, new Team { Value = 10 });
        world.SetShared(et1, new Shared(1));

        Assert.NotEqual(0, world.GetEntityInfo(et1).Chunk.AllSharedComponentsHash);
        Assert.Equal(10, world.Get<Team>(et1).Value);

        var et2 = world.BuildEntity().With<Team>().With<Actor>().WithMng<Player>().WithShared<Shared>().Build();
        world.SetSta(et2, new Team { Value = 10 });
        world.SetShared(et2, new Shared(10));

        Assert.Equal([typeof(Team), typeof(Actor), typeof(Mng<Player>), typeof(Shared)], ((List<object>)world.GetAll(et1)).Select(v => v.GetType()));

        Assert.Equal([et1], world.BuildQuery().WithShared(new Shared(1)).Query());
        Assert.Equal([et2], world.BuildQuery().WithShared(new Shared(10)).Query());
        Assert.Equal([et1, et2], world.BuildQuery().WithAll<Team>().Query());
    }


    [Fact]
    public void ComponentSystem()
    {
        using var world = new WorldCore();
        world.Update();
        var et = world.BuildEntity().With<Team>().With<Actor>().WithMng<Player>().WithShared<Shared>().Build();
        world.Set(et, new Managed());
        world.Set(et, new Player { Name = "abc" });

        Assert.Equal([nameof(IWorldAwake.OnAwake)], world.Get<Managed>(et).Values);
        Assert.Equal(1u, world.Get<Managed>(et).AwakeFrame);

        world.Update();
        Assert.Equal([nameof(IWorldAwake.OnAwake), nameof(IWorldStart.OnStart), nameof(IWorldUpdate.OnUpdate)], world.Get<Managed>(et).Values);
        Assert.Equal(2u, world.Get<Managed>(et).StartFrame);

        world.Update();
        Assert.Equal([nameof(IWorldAwake.OnAwake), nameof(IWorldStart.OnStart), nameof(IWorldUpdate.OnUpdate), nameof(IWorldUpdate.OnUpdate)], world.Get<Managed>(et).Values);
        Assert.Equal(2u, world.Get<Managed>(et).StartFrame);
        Assert.Equal(3u, world.Get<Managed>(et).UpdateFrame);

        Assert.Equal("abc", world.Soa.GetGroup<Player>()[0].Name);
        world.RemoveEntity(et);

        Assert.Equal([nameof(IWorldAwake.OnAwake), nameof(IWorldStart.OnStart), nameof(IWorldUpdate.OnUpdate), nameof(IWorldUpdate.OnUpdate), nameof(IWorldDestroy.OnDestroy)],
            world.Soa.GetGroup<Managed>()[0].Values);

        Assert.Null(world.Soa.GetGroup<Player>()[0].Name);
    }
}