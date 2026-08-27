using NeatShot.Core.Annotations;

namespace NeatShot.Core.Tests.Annotations;

public class PathSimplifierTests
{
    [Fact]
    public void Simplify_KeepsShortPathsUntouched()
    {
        var points = new[] { new ImagePoint(0, 0), new ImagePoint(5, 5) };

        Assert.Same(points, PathSimplifier.Simplify(points, 1));
    }

    [Fact]
    public void Simplify_DropsPointsOnAStraightLine()
    {
        var points = Enumerable.Range(0, 20).Select(i => new ImagePoint(i, i * 0.5)).ToArray();

        var simplified = PathSimplifier.Simplify(points, 0.5);

        Assert.Equal([points[0], points[^1]], simplified);
    }

    [Fact]
    public void Simplify_KeepsCorners()
    {
        var points = new[]
        {
            new ImagePoint(0, 0), new ImagePoint(10, 0.2), new ImagePoint(20, 0), new ImagePoint(20, 10), new ImagePoint(20, 20),
        };

        var simplified = PathSimplifier.Simplify(points, 1);

        Assert.Equal([points[0], points[2], points[4]], simplified);
    }

    [Fact]
    public void Simplify_KeepsEndpoints()
    {
        var points = new[] { new ImagePoint(0, 0), new ImagePoint(1, 30), new ImagePoint(2, 0), new ImagePoint(3, 30) };

        var simplified = PathSimplifier.Simplify(points, 1);

        Assert.Equal(points[0], simplified[0]);
        Assert.Equal(points[^1], simplified[^1]);
    }

    [Fact]
    public void Smooth_AveragesNeighboursAndKeepsEndpoints()
    {
        var points = new[] { new ImagePoint(0, 0), new ImagePoint(3, 9), new ImagePoint(6, 0), new ImagePoint(9, 9) };

        var smoothed = PathSimplifier.Smooth(points);

        Assert.Equal(points[0], smoothed[0]);
        Assert.Equal(new ImagePoint(3, 3), smoothed[1]);
        Assert.Equal(new ImagePoint(6, 6), smoothed[2]);
        Assert.Equal(points[^1], smoothed[^1]);
    }
}
