namespace NeatShot.Core.Annotations;

public sealed record AnnotationStyle(Rgba Color, double StrokeWidth)
{
    public static AnnotationStyle Default => new(Rgba.Red, 4);
}

public interface IStyledAnnotation
{
    AnnotationStyle Style { get; }
}

public abstract record Annotation
{
    private const double HitTolerance = 6;

    public Guid Id { get; init; } = Guid.NewGuid();

    public abstract ImageRect Bounds { get; }

    public virtual bool CanResize => true;

    public abstract Annotation Translate(double dx, double dy);

    public abstract Annotation WithBounds(ImageRect bounds);

    public virtual Annotation WithColor(Rgba color) => this;

    public virtual Annotation WithStrokeWidth(double width) => this;

    public Annotation Duplicate() => this with { Id = Guid.NewGuid() };

    public virtual bool HitTest(ImagePoint point) => Bounds.Inflate(HitTolerance).Contains(point);

    protected static double Tolerance => HitTolerance;
}

public sealed record RectangleAnnotation(ImageRect Rect, AnnotationStyle Style) : Annotation, IStyledAnnotation
{
    public override ImageRect Bounds => Rect;

    public override Annotation Translate(double dx, double dy) => this with { Rect = Rect.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds) => this with { Rect = bounds };

    public override Annotation WithColor(Rgba color) => this with { Style = Style with { Color = color } };

    public override Annotation WithStrokeWidth(double width) => this with { Style = Style with { StrokeWidth = width } };
}

public sealed record EllipseAnnotation(ImageRect Rect, AnnotationStyle Style) : Annotation, IStyledAnnotation
{
    public override ImageRect Bounds => Rect;

    public override Annotation Translate(double dx, double dy) => this with { Rect = Rect.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds) => this with { Rect = bounds };

    public override Annotation WithColor(Rgba color) => this with { Style = Style with { Color = color } };

    public override Annotation WithStrokeWidth(double width) => this with { Style = Style with { StrokeWidth = width } };

    public override bool HitTest(ImagePoint point)
    {
        var rx = Rect.Width / 2 + Tolerance;
        var ry = Rect.Height / 2 + Tolerance;
        if (rx <= 0 || ry <= 0)
        {
            return false;
        }

        var nx = (point.X - Rect.Center.X) / rx;
        var ny = (point.Y - Rect.Center.Y) / ry;
        return nx * nx + ny * ny <= 1;
    }
}

public sealed record ArrowAnnotation(ImagePoint Start, ImagePoint End, AnnotationStyle Style) : Annotation, IStyledAnnotation
{
    public override ImageRect Bounds => ImageRect.FromPoints(Start, End);

    public override bool CanResize => false;

    public override Annotation Translate(double dx, double dy) =>
        this with { Start = Start.Translate(dx, dy), End = End.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds) => this;

    public override Annotation WithColor(Rgba color) => this with { Style = Style with { Color = color } };

    public override Annotation WithStrokeWidth(double width) => this with { Style = Style with { StrokeWidth = width } };

    public override bool HitTest(ImagePoint point) =>
        point.DistanceToSegment(Start, End) <= Style.StrokeWidth / 2 + Tolerance;
}

public sealed record FreehandAnnotation(IReadOnlyList<ImagePoint> Points, AnnotationStyle Style) : Annotation, IStyledAnnotation
{
    public override ImageRect Bounds => Points.Count == 0
        ? default
        : Points.Skip(1).Aggregate(
            ImageRect.FromPoints(Points[0], Points[0]),
            (bounds, point) => bounds.Union(ImageRect.FromPoints(point, point)));

    public override Annotation Translate(double dx, double dy) =>
        this with { Points = Points.Select(p => p.Translate(dx, dy)).ToArray() };

    public override Annotation WithBounds(ImageRect bounds)
    {
        var current = Bounds;
        var scaleX = current.Width > 0 ? bounds.Width / current.Width : 1;
        var scaleY = current.Height > 0 ? bounds.Height / current.Height : 1;
        return this with
        {
            Points = Points
                .Select(p => new ImagePoint(bounds.X + (p.X - current.X) * scaleX, bounds.Y + (p.Y - current.Y) * scaleY))
                .ToArray(),
        };
    }

    public override Annotation WithColor(Rgba color) => this with { Style = Style with { Color = color } };

    public override Annotation WithStrokeWidth(double width) => this with { Style = Style with { StrokeWidth = width } };

    public override bool HitTest(ImagePoint point)
    {
        var reach = Style.StrokeWidth / 2 + Tolerance;
        for (var i = 1; i < Points.Count; i++)
        {
            if (point.DistanceToSegment(Points[i - 1], Points[i]) <= reach)
            {
                return true;
            }
        }

        return Points.Count == 1 && point.DistanceTo(Points[0]) <= reach;
    }
}

public sealed record TextAnnotation(ImagePoint Position, string Text, AnnotationStyle Style, double FontSize, ImageRect Extent) : Annotation, IStyledAnnotation
{
    public override ImageRect Bounds => Extent;

    public override Annotation Translate(double dx, double dy) =>
        this with { Position = Position.Translate(dx, dy), Extent = Extent.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds)
    {
        var factor = Extent.Height > 0 ? bounds.Height / Extent.Height : 1;
        return this with
        {
            Position = new ImagePoint(bounds.X, bounds.Y),
            FontSize = Math.Max(6, FontSize * factor),
            Extent = new ImageRect(bounds.X, bounds.Y, Extent.Width * factor, bounds.Height),
        };
    }

    public override Annotation WithColor(Rgba color) => this with { Style = Style with { Color = color } };
}

public sealed record CounterAnnotation(ImagePoint Center, int Number, AnnotationStyle Style) : Annotation, IStyledAnnotation
{
    public const double Radius = 14;

    public override ImageRect Bounds => ImageRect.FromCenter(Center, Radius);

    public override bool CanResize => false;

    public override Annotation Translate(double dx, double dy) => this with { Center = Center.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds) => this with { Center = bounds.Center };

    public override Annotation WithColor(Rgba color) => this with { Style = Style with { Color = color } };

    public override bool HitTest(ImagePoint point) => point.DistanceTo(Center) <= Radius + Tolerance;
}

public enum ObscureKind
{
    Blur,
    Pixelate,
}

public sealed record ObscureAnnotation(ImageRect Rect, ObscureKind Kind, int Strength = ObscureAnnotation.DefaultStrength) : Annotation
{
    public const int MinStrength = 1;
    public const int MaxStrength = 10;
    public const int DefaultStrength = 4;

    public override ImageRect Bounds => Rect;

    public ObscureAnnotation WithStrength(int strength) => this with { Strength = Math.Clamp(strength, MinStrength, MaxStrength) };

    public override Annotation Translate(double dx, double dy) => this with { Rect = Rect.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds) => this with { Rect = bounds };
}

public sealed record HighlightAnnotation(ImageRect Rect, Rgba Color) : Annotation
{
    public override ImageRect Bounds => Rect;

    public override Annotation Translate(double dx, double dy) => this with { Rect = Rect.Translate(dx, dy) };

    public override Annotation WithBounds(ImageRect bounds) => this with { Rect = bounds };

    public override Annotation WithColor(Rgba color) => this with { Color = color };
}
