using NeatShot.Core.Capture;

namespace NeatShot.Core.Tests.Capture;

public class PixelRectTests
{
    [Fact]
    public void FromPoints_NormalizesRegardlessOfDragDirection()
    {
        var rect = PixelRect.FromPoints(new PixelPoint(50, 40), new PixelPoint(10, 20));

        Assert.Equal(new PixelRect(10, 20, 40, 20), rect);
    }

    [Fact]
    public void Intersect_ReturnsOverlappingArea()
    {
        var a = new PixelRect(0, 0, 100, 100);
        var b = new PixelRect(50, 50, 100, 100);

        Assert.Equal(new PixelRect(50, 50, 50, 50), a.Intersect(b));
    }

    [Fact]
    public void Intersect_ReturnsEmptyWhenDisjoint()
    {
        var a = new PixelRect(0, 0, 10, 10);
        var b = new PixelRect(20, 20, 10, 10);

        Assert.True(a.Intersect(b).IsEmpty);
    }

    [Fact]
    public void Union_SpansBothRectangles()
    {
        var a = new PixelRect(0, 0, 10, 10);
        var b = new PixelRect(20, 20, 10, 10);

        Assert.Equal(new PixelRect(0, 0, 30, 30), a.Union(b));
    }

    [Fact]
    public void Union_WithEmptyReturnsOther()
    {
        var a = new PixelRect(5, 5, 10, 10);

        Assert.Equal(a, PixelRect.Empty.Union(a));
        Assert.Equal(a, a.Union(PixelRect.Empty));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(10, 10, false)]
    [InlineData(-1, 5, false)]
    public void Contains_TreatsRightAndBottomEdgeAsExclusive(int x, int y, bool expected)
    {
        var rect = new PixelRect(0, 0, 10, 10);

        Assert.Equal(expected, rect.Contains(new PixelPoint(x, y)));
    }
}
