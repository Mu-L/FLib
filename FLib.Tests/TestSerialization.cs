// ==================== qcbf@qq.com | 2026-03-26 ====================

using FLib.WorldCores.TimeLogics;

namespace FLib.Tests;

public class TestSerialization
{
    [Fact]
    public void TimeLogic()
    {
        var tl = new TimeLogic
        {
            Name = "tl",
            EndFrame = 10,
            Tracks = [new ScriptPackInstance(new TimeLogicTrack() { Name = "tlt1", Clips = [new ScriptPackInstance(new TimeLogicClip() { Name = "tlc11", BeginFrame = 0, EndFrame = 10 })] })]
        };

        var json = Json5.Serialize(tl);
        Assert.Equal(json, Json5.Serialize(Json5.Deserialize<TimeLogic>(json)));

        var bytes = BytesPack.Pack(tl);
        Assert.Equal(bytes, BytesPack.Pack(BytesPack.Unpack<TimeLogic>(bytes)));
    }


    [Fact]
    public void BytesPacks()
    {
        TypeAssistant.AddAssemblies(typeof(Team).Assembly);
        var instance = new Team { Value = 123 };
        var pack1 = new ScriptPackInstance(instance);
        Assert.Equal(pack1, Json5.Deserialize<ScriptPackInstance>(Json5.Serialize(pack1)));
        Assert.Equal(pack1, BytesPack.Unpack<ScriptPackInstance>(BytesPack.Pack(pack1)));

        var pack2 = new ScriptPackBytes(instance);
        var scriptPackBytes = Json5.Deserialize<ScriptPackBytes>(Json5.Serialize(pack2));
        Assert.Equal(pack2.Bytes, scriptPackBytes.Bytes);
        Assert.Equal(pack2.Bytes, BytesPack.Unpack<ScriptPackBytes>(BytesPack.Pack(pack2)).Bytes);
    }
}