namespace FLib.Tests;

public class TestQuadMap
{
    [Fact]
    public void ResizePreservesIntersectingBitsAcrossUnalignedRows()
    {
        var map = new QuadMap().SetSize(new FVector2Int(70, 3));
        for (var y = 0; y < map.TerrainSize.Y; y++)
            for (var x = 0; x < map.TerrainSize.X; x++)
                map[0, x, y] = (x + y * 3) % 7 == 0;

        map.SetSize(new FVector2Int(65, 4));

        for (var y = 0; y < 3; y++)
            for (var x = 0; x < 65; x++)
                Assert.Equal((x + y * 3) % 7 == 0, map[0, x, y]);
        for (var x = 0; x < 65; x++)
            Assert.False(map[0, x, 3]);
    }

    [Fact]
    public void RectangleCheckHandlesWordAndRowBoundaries()
    {
        var map = new QuadMap().SetSize(new FVector2Int(130, 3));
        for (var y = 0; y < 2; y++)
            for (var x = 60; x < 80; x++)
                map[0, x, y] = true;

        Assert.True(map.CheckTile(new FVector2Int(60, 0), new FVector2Int(20, 2), true));
        Assert.False(map.CheckTile(new FVector2Int(59, 0), new FVector2Int(21, 2), true));
        Assert.False(map.CheckTile(new FVector2Int(60, 0), new FVector2Int(20, 4), true));
        Assert.False(map.CheckTile(new FVector2Int(60, 0), FVector2Int.Zero, true));
    }

    [Theory]
    [InlineData(0, 0, 1, 0)]
    [InlineData(10, 0, 1, 0)]
    [InlineData(3, 1, 1, 0)]
    [InlineData(2, 1, 1, 1)]
    [InlineData(1, 2, 1, 1)]
    [InlineData(1, 3, 0, 1)]
    [InlineData(-2, 1, -1, 1)]
    [InlineData(-3, -1, -1, 0)]
    [InlineData(1, -2, 1, -1)]
    public void NextStepUsesNearestOctant(int dx, int dy, int expectedX, int expectedY)
    {
        var map = new QuadMap().SetSize(new FVector2Int(21, 21));
        var from = new FVector2Int(10, 10);

        var next = map.FindNearNextStepPos(from, new FVector2Int(from.X + dx, from.Y + dy));

        Assert.Equal(new FVector2Int(from.X + expectedX, from.Y + expectedY), next);
    }

    [Fact]
    public void IntegerOctantsMatchAngularReference()
    {
        FVector2Int[] offsets = [new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)];
        var map = new QuadMap().SetSize(new FVector2Int(205, 205));
        var from = new FVector2Int(102, 102);
        for (var y = -100; y <= 100; y++)
        {
            for (var x = -100; x <= 100; x++)
            {
                var direction = new FVector2Int(x, y);
                var angle = FVector2.Angle360(FVector2.Right, direction);
                var expectedIndex = (int)((angle + (FNum)22.5) / 45) & 7;

                Assert.Equal(from + offsets[expectedIndex], map.FindNearNextStepPos(from, from + direction));
            }
        }
    }
}
