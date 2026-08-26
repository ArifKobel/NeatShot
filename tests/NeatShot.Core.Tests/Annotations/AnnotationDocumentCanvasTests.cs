using NeatShot.Core.Annotations;
using NeatShot.Core.Capture;

namespace NeatShot.Core.Tests.Annotations;

public class AnnotationDocumentCanvasTests
{
    private static readonly AnnotationStyle Style = new(Rgba.Red, 2);

    private static AnnotationDocument CreateDocument() =>
        new(new CapturedImage(100, 80, new byte[100 * 80 * CapturedImage.BytesPerPixel]));

    [Fact]
    public void Canvas_MatchesImageWhileAnnotationsStayInside()
    {
        var document = CreateDocument();
        document.Execute(new AddAnnotationCommand(new RectangleAnnotation(new ImageRect(10, 10, 20, 20), Style)));

        Assert.Equal(new ImageRect(0, 0, 100, 80), document.Canvas);
    }

    [Fact]
    public void Canvas_GrowsAroundAnnotationsPastTheEdge()
    {
        var document = CreateDocument();
        document.Execute(new AddAnnotationCommand(new RectangleAnnotation(new ImageRect(-20.5, 60, 30, 40), Style)));

        var canvas = document.Canvas;

        Assert.Equal(-29, canvas.Left);
        Assert.Equal(0, canvas.Top);
        Assert.Equal(100, canvas.Right);
        Assert.Equal(108, canvas.Bottom);
    }

    [Fact]
    public void Canvas_ShrinksBackOnUndo()
    {
        var document = CreateDocument();
        document.Execute(new AddAnnotationCommand(new RectangleAnnotation(new ImageRect(90, 10, 40, 10), Style)));
        document.Undo();

        Assert.Equal(new ImageRect(0, 0, 100, 80), document.Canvas);
    }
}
