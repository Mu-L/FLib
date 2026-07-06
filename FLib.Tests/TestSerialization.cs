// ==================== qcbf@qq.com | 2026-03-26 ====================

using FLib.WorldCores.TimeLogics;

namespace FLib.Tests;

public class TestSerialization
{
    private sealed class Json5ExistingObjectData
    {
        public int Id;
        public string Name = string.Empty;
        public Json5ExistingNestedData Nested = new();
        public List<int> Items = [9];
        public int[] Array = [8];
        public object Dynamic = 7;
    }

    private sealed class Json5ExistingNestedData
    {
        public int Value;
    }

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

    [Fact]
    public void Json5DeserializeToExistingObject()
    {
        var data = new Json5ExistingObjectData
        {
            Id = 1,
            Name = "old",
            Nested = new Json5ExistingNestedData { Value = 2 },
            Items = [9],
            Array = [8],
            Dynamic = 7
        };
        var nested = data.Nested;
        var items = data.Items;
        var oldArray = data.Array;

        var result = Json5.Deserialize("{ Id: 3, Name: 'new', Nested: { Value: 4 }, Items: [1, 2, 3], Array: [5, 6], Dynamic: { A: 1 } }", data);

        Assert.Same(data, result);
        Assert.Equal(3, data.Id);
        Assert.Equal("new", data.Name);
        Assert.Same(nested, data.Nested);
        Assert.Equal(4, data.Nested.Value);
        Assert.Same(items, data.Items);
        Assert.Equal([1, 2, 3], data.Items);
        Assert.NotSame(oldArray, data.Array);
        Assert.Equal([5, 6], data.Array);
        var dynamicDict = Assert.IsType<Dictionary<string, object>>(data.Dynamic);
        Assert.Equal(1L, dynamicDict["A"]);
    }

    [Fact]
    public void Json5DeserializeToExistingDictionary()
    {
        var dict = new Dictionary<string, int> { ["Old"] = 1 };

        var result = Json5.Deserialize("{ New: 2 }", dict);

        Assert.Same(dict, result);
        Assert.Equal(1, dict["Old"]);
        Assert.Equal(2, dict["New"]);
    }

    [Fact]
    public void Json5DeserializeToExistingList()
    {
        var list = new List<int> { 9 };

        var result = Json5.Deserialize("[1, 2, 3]", list);

        Assert.Same(list, result);
        Assert.Equal([1, 2, 3], list);
    }
}
