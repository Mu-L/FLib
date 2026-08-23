using FLib;

namespace FLib.Tests;

[BytesPackGen]
public partial class BytesPackCollectionData
{
    [BytesPackGenField] public List2<int> Values = new();
    [BytesPackGenField] public Dictionary2<int, int> Lookup = new();
}

public class TestBytesPackCollections
{
    [Fact]
    public void List2AndDictionary2RoundTrip()
    {
        var data = new BytesPackCollectionData();
        data.Values.Add(1);
        data.Values.Add(-2);
        data.Values.Add(300);
        data.Lookup.Add(7, -8);
        data.Lookup.Add(9, 10);

        var result = BytesPack.Unpack<BytesPackCollectionData>(BytesPack.Pack(data));

        Assert.Equal([1, -2, 300], result.Values);
        Assert.Equal(-8, result.Lookup[7]);
        Assert.Equal(10, result.Lookup[9]);
    }
}
