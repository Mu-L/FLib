using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FLib;

namespace FLib.Tests;

public class TestCollections2
{
    [Fact]
    public void Dictionary2RefApisWork()
    {
        var dictionary = new Dictionary2<string, int>(3);
        dictionary.GetValueRefOrAdd("value") = 1;

        ref var value = ref dictionary.GetValueRef("value");
        value++;
        Assert.Equal(2, dictionary.GetValueOrDefault("value"));

        ref var existing = ref dictionary.GetValueRefOrAdd("value", out var exists);
        Assert.True(exists);
        existing = 3;

        ref var missing = ref dictionary.GetValueRefOrNullRef("missing");
        Assert.True(Unsafe.IsNullRef(ref missing));
        Assert.Equal(3, dictionary["value"]);
    }

    [Fact]
    public void Dictionary2AllowsRemovalDuringEnumeration()
    {
        var dictionary = new Dictionary2<int, int>(16);
        for (var i = 0; i < 8; i++)
            dictionary.Add(i, i);

        var enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
            enumerator.RemoveSelf();

        Assert.True(dictionary.Count == 0);
    }

    [Fact]
    public void Dictionary2MaintainsCollisionChainsAfterRemovalAndReuse()
    {
        var dictionary = new Dictionary2<int, int>(3, new ConstantHashComparer());
        dictionary.Add(1, 10);
        dictionary.Add(2, 20);
        dictionary.Add(3, 30);

        Assert.True(dictionary.Remove(2));
        Assert.True(dictionary.TryGetValue(1, out var first) && first == 10);
        Assert.True(dictionary.TryGetValue(3, out var third) && third == 30);

        var capacity = dictionary.Capacity;
        dictionary.Add(4, 40);

        Assert.Equal(capacity, dictionary.Capacity);
        Assert.Equal(40, dictionary.GetValueOrDefault(4));
        Assert.Equal(10, dictionary.GetValueOrDefault(1));
        Assert.Equal(30, dictionary.GetValueOrDefault(3));
    }

    [Fact]
    public void Dictionary2AllowsInsertionWithoutResizeDuringEnumeration()
    {
        var dictionary = new Dictionary2<int, int>(16);
        dictionary.Add(1, 1);
        dictionary.Add(2, 2);

        var enumerator = dictionary.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        dictionary.Add(3, 3);
        while (enumerator.MoveNext())
        {
        }

        Assert.Equal(3, dictionary.Count);
    }

    [Fact]
    public void Dictionary2RejectsResizeDuringEnumeration()
    {
        var dictionary = new Dictionary2<int, int>(3);
        dictionary.Add(1, 1);
        dictionary.Add(2, 2);
        dictionary.Add(3, 3);
        var enumerator = dictionary.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        dictionary.Add(4, 4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void HashSet2AllowsRemovalAndRefAccess()
    {
        var set = new HashSet2<int>(16);
        set.Add(1);
        set.Add(2);
        ref readonly var existing = ref set.GetValueOrAdd(2);

        Assert.Equal(2, existing);
        Assert.True(set.Contains(2));

        var enumerator = set.GetEnumerator();
        while (enumerator.MoveNext())
            enumerator.RemoveSelf();

        Assert.True(set.Count == 0);
    }

    [Fact]
    public void HashSet2MaintainsCollisionChainsAfterRemovalAndReuse()
    {
        var set = new HashSet2<int>(3, new ConstantHashComparer());
        set.Add(1);
        set.Add(2);
        set.Add(3);

        Assert.True(set.Remove(2));
        Assert.True(set.Contains(1));
        Assert.True(set.Contains(3));

        var capacity = set.Capacity;
        set.Add(4);

        Assert.Equal(capacity, set.Capacity);
        Assert.True(set.Contains(4));
        Assert.Equal(3, set.Count);
    }

    [Fact]
    public void Collections2AllowDirectRemovalDuringForeach()
    {
        var dictionary = new Dictionary2<int, int>(16);
        var set = new HashSet2<int>(16);
        for (var i = 0; i < 8; i++)
        {
            dictionary.Add(i, i);
            set.Add(i);
        }

        foreach (var item in dictionary)
            dictionary.Remove(item.Key);
        foreach (var item in set)
            set.Remove(item);

        Assert.Empty(dictionary);
        Assert.Empty(set);
    }

    [Fact]
    public void HashSet2RejectsResizeDuringEnumeration()
    {
        var set = new HashSet2<int>(3);
        set.Add(1);
        set.Add(2);
        set.Add(3);
        var enumerator = set.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        set.Add(4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    private sealed class ConstantHashComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj) => 0;
    }
}
