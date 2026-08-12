using NeatShot.Core.Annotations;

namespace NeatShot.Core.Tests.Annotations;

public class AnnotationHitTests
{
    private static readonly AnnotationStyle Style = new(Rgba.Red, 2);

    [Fact]
    public void Rectangle_HitsInsideAndNearEdge()
    {
        var rectangle = new RectangleAnnotation(new ImageRect(10, 10, 20, 20), Style);

        Assert.True(rectangle.HitTest(new ImagePoint(20, 20)));
        Assert.True(rectangle.HitTest(new ImagePoint(33, 20)));
        Assert.False(rectangle.HitTest(new ImagePoint(50, 50)));
    }

    [Fact]
    public void Ellipse_MissesCornersOfBoundingBox()
    {
        var ellipse = new EllipseAnnotation(new ImageRect(0, 0, 100, 100), Style);

        Assert.True(ellipse.HitTest(new ImagePoint(50, 50)));
        Assert.False(ellipse.HitTest(new ImagePoint(2, 2)));
    }

    [Fact]
    public void Arrow_HitsAlongSegmentOnly()
    {
        var arrow = new ArrowAnnotation(new ImagePoint(0, 0), new ImagePoint(100, 0), Style);

        Assert.True(arrow.HitTest(new ImagePoint(50, 3)));
        Assert.False(arrow.HitTest(new ImagePoint(50, 30)));
        Assert.False(arrow.HitTest(new ImagePoint(130, 0)));
    }

    [Fact]
    public void Freehand_BoundsSpanAllPoints()
    {
        var stroke = new FreehandAnnotation([new ImagePoint(5, 5), new ImagePoint(50, 10), new ImagePoint(20, 40)], Style);

        Assert.Equal(new ImageRect(5, 5, 45, 35), stroke.Bounds);
        Assert.True(stroke.HitTest(new ImagePoint(27, 7)));
    }

    [Fact]
    public void Counter_HitsWithinRadius()
    {
        var counter = new CounterAnnotation(new ImagePoint(50, 50), 1, Style);

        Assert.True(counter.HitTest(new ImagePoint(60, 50)));
        Assert.False(counter.HitTest(new ImagePoint(80, 50)));
    }

    [Fact]
    public void Translate_MovesEveryPointAndKeepsId()
    {
        var arrow = new ArrowAnnotation(new ImagePoint(0, 0), new ImagePoint(10, 10), Style);

        var moved = (ArrowAnnotation)arrow.Translate(5, -5);

        Assert.Equal(arrow.Id, moved.Id);
        Assert.Equal(new ImagePoint(5, -5), moved.Start);
        Assert.Equal(new ImagePoint(15, 5), moved.End);
    }
}
