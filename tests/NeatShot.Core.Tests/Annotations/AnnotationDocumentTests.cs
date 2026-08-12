using NeatShot.Core.Annotations;
using NeatShot.Core.Capture;

namespace NeatShot.Core.Tests.Annotations;

public class AnnotationDocumentTests
{
    private static readonly AnnotationStyle Style = AnnotationStyle.Default;

    [Fact]
    public void Execute_AddsAnnotationAndEnablesUndo()
    {
        var document = CreateDocument();
        var rectangle = new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style);

        document.Execute(new AddAnnotationCommand(rectangle));

        Assert.Single(document.Annotations, rectangle);
        Assert.True(document.CanUndo);
        Assert.False(document.CanRedo);
    }

    [Fact]
    public void Undo_ThenRedo_RestoresAnnotation()
    {
        var document = CreateDocument();
        var rectangle = new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style);
        document.Execute(new AddAnnotationCommand(rectangle));

        Assert.True(document.Undo());
        Assert.Empty(document.Annotations);
        Assert.True(document.CanRedo);

        Assert.True(document.Redo());
        Assert.Single(document.Annotations, rectangle);
    }

    [Fact]
    public void Execute_AfterUndo_DiscardsRedoHistory()
    {
        var document = CreateDocument();
        document.Execute(new AddAnnotationCommand(new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style)));
        document.Undo();

        document.Execute(new AddAnnotationCommand(new EllipseAnnotation(new ImageRect(0, 0, 10, 10), Style)));

        Assert.False(document.CanRedo);
        Assert.IsType<EllipseAnnotation>(Assert.Single(document.Annotations));
    }

    [Fact]
    public void RemoveCommand_UndoRestoresOriginalPosition()
    {
        var document = CreateDocument();
        var first = new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style);
        var second = new RectangleAnnotation(new ImageRect(20, 20, 10, 10), Style);
        var third = new RectangleAnnotation(new ImageRect(40, 40, 10, 10), Style);
        document.Execute(new AddAnnotationCommand(first));
        document.Execute(new AddAnnotationCommand(second));
        document.Execute(new AddAnnotationCommand(third));

        document.Execute(new RemoveAnnotationCommand(second));
        document.Undo();

        Assert.Equal([first, second, third], document.Annotations);
    }

    [Fact]
    public void ReplaceCommand_SwapsAnnotationInPlace()
    {
        var document = CreateDocument();
        var original = new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style);
        document.Execute(new AddAnnotationCommand(original));
        var moved = (RectangleAnnotation)original.Translate(5, 5);

        document.Execute(new ReplaceAnnotationCommand(original, moved));

        Assert.Single(document.Annotations, moved);
        document.Undo();
        Assert.Single(document.Annotations, original);
    }

    [Fact]
    public void ReplaceCommand_RejectsDifferentIds()
    {
        var a = new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style);
        var b = new RectangleAnnotation(new ImageRect(0, 0, 10, 10), Style);

        Assert.Throws<ArgumentException>(() => new ReplaceAnnotationCommand(a, b));
    }

    [Fact]
    public void HitTest_PrefersTopmostAnnotation()
    {
        var document = CreateDocument();
        var bottom = new RectangleAnnotation(new ImageRect(0, 0, 50, 50), Style);
        var top = new RectangleAnnotation(new ImageRect(10, 10, 20, 20), Style);
        document.Execute(new AddAnnotationCommand(bottom));
        document.Execute(new AddAnnotationCommand(top));

        Assert.Same(top, document.HitTest(new ImagePoint(15, 15)));
        Assert.Same(bottom, document.HitTest(new ImagePoint(45, 45)));
        Assert.Null(document.HitTest(new ImagePoint(200, 200)));
    }

    [Fact]
    public void NextCounterNumber_ContinuesFromHighestExisting()
    {
        var document = CreateDocument();
        Assert.Equal(1, document.NextCounterNumber);

        document.Execute(new AddAnnotationCommand(new CounterAnnotation(new ImagePoint(0, 0), 3, Style)));

        Assert.Equal(4, document.NextCounterNumber);
    }

    private static AnnotationDocument CreateDocument() =>
        new(new CapturedImage(100, 100, new byte[100 * 100 * CapturedImage.BytesPerPixel]));
}
