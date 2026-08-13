using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeatShot.Core.Annotations;
using NeatShot.Core.Capture;
using NeatShot.Imaging;

namespace NeatShot.Editor;

public sealed class AnnotationRenderer
{
    private const int PixelateBlockSize = 12;
    private const int BlurRadius = 6;
    private const int CacheLimit = 64;
    private const double ArrowHeadLength = 4;
    private const double ArrowHeadWidth = 2.2;
    private const byte HighlightAlpha = 0x70;

    private static readonly Typeface TextTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Pen SelectionPen = Frozen(new Pen(new SolidColorBrush(Color.FromRgb(0x0A, 0x84, 0xFF)), 1) { DashStyle = DashStyles.Dash });

    private readonly CapturedImage _image;
    private readonly Dictionary<ObscureAnnotation, BitmapSource> _obscureCache = [];

    public AnnotationRenderer(CapturedImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    public double PixelsPerDip { get; set; } = 1;

    public void Draw(DrawingContext context, IEnumerable<Annotation> annotations, Annotation? selected = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(annotations);

        foreach (var annotation in annotations)
        {
            Draw(context, annotation);
        }

        if (selected is not null)
        {
            context.DrawRectangle(null, SelectionPen, ToRect(selected.Bounds.Inflate(4)));
        }
    }

    public static Size MeasureText(string text, double fontSize, double pixelsPerDip) =>
        new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TextTypeface, fontSize, Brushes.Black, pixelsPerDip) is var formatted
            ? new Size(formatted.WidthIncludingTrailingWhitespace, formatted.Height)
            : Size.Empty;

    private void Draw(DrawingContext context, Annotation annotation)
    {
        switch (annotation)
        {
            case RectangleAnnotation rectangle:
                context.DrawRoundedRectangle(null, CreatePen(rectangle.Style), ToRect(rectangle.Rect), 2, 2);
                break;
            case EllipseAnnotation ellipse:
                var rect = ToRect(ellipse.Rect);
                context.DrawEllipse(null, CreatePen(ellipse.Style), rect.Center(), rect.Width / 2, rect.Height / 2);
                break;
            case ArrowAnnotation arrow:
                DrawArrow(context, arrow);
                break;
            case FreehandAnnotation freehand:
                DrawFreehand(context, freehand);
                break;
            case TextAnnotation text:
                DrawText(context, text);
                break;
            case CounterAnnotation counter:
                DrawCounter(context, counter);
                break;
            case HighlightAnnotation highlight:
                context.DrawRectangle(new SolidColorBrush(ToColor(highlight.Color with { A = HighlightAlpha })), null, ToRect(highlight.Rect));
                break;
            case ObscureAnnotation obscure:
                DrawObscure(context, obscure);
                break;
        }
    }

    private static void DrawArrow(DrawingContext context, ArrowAnnotation arrow)
    {
        var start = ToPoint(arrow.Start);
        var end = ToPoint(arrow.End);
        var direction = end - start;
        if (direction.Length < 0.5)
        {
            return;
        }

        direction.Normalize();
        var normal = new Vector(-direction.Y, direction.X);
        var width = arrow.Style.StrokeWidth;
        var headLength = Math.Min(width * ArrowHeadLength, (end - start).Length);
        var headBase = end - direction * headLength;
        var brush = new SolidColorBrush(ToColor(arrow.Style.Color));

        var pen = new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Flat };
        context.DrawLine(pen, start, headBase);

        var head = new StreamGeometry();
        using (var geometry = head.Open())
        {
            geometry.BeginFigure(end, isFilled: true, isClosed: true);
            geometry.LineTo(headBase + normal * width * ArrowHeadWidth, isStroked: false, isSmoothJoin: false);
            geometry.LineTo(headBase - normal * width * ArrowHeadWidth, isStroked: false, isSmoothJoin: false);
        }

        context.DrawGeometry(brush, null, head);
    }

    private static void DrawFreehand(DrawingContext context, FreehandAnnotation freehand)
    {
        if (freehand.Points.Count == 0)
        {
            return;
        }

        var pen = CreatePen(freehand.Style);
        pen.StartLineCap = PenLineCap.Round;
        pen.EndLineCap = PenLineCap.Round;
        pen.LineJoin = PenLineJoin.Round;

        var path = new StreamGeometry();
        using (var geometry = path.Open())
        {
            geometry.BeginFigure(ToPoint(freehand.Points[0]), isFilled: false, isClosed: false);
            geometry.PolyLineTo(freehand.Points.Skip(1).Select(ToPoint).ToList(), isStroked: true, isSmoothJoin: true);
        }

        context.DrawGeometry(null, pen, path);
    }

    private void DrawText(DrawingContext context, TextAnnotation text)
    {
        var formatted = new FormattedText(
            text.Text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            TextTypeface,
            text.FontSize,
            new SolidColorBrush(ToColor(text.Style.Color)),
            PixelsPerDip);

        var origin = ToPoint(text.Position);
        var outline = formatted.BuildGeometry(origin);
        context.DrawGeometry(null, new Pen(Brushes.Black, Math.Max(1, text.FontSize / 12)) { LineJoin = PenLineJoin.Round, Brush = new SolidColorBrush(Color.FromArgb(0x90, 0, 0, 0)) }, outline);
        context.DrawText(formatted, origin);
    }

    private void DrawCounter(DrawingContext context, CounterAnnotation counter)
    {
        var center = ToPoint(counter.Center);
        var fill = new SolidColorBrush(ToColor(counter.Style.Color));
        context.DrawEllipse(fill, new Pen(Brushes.White, 2), center, CounterAnnotation.Radius, CounterAnnotation.Radius);

        var label = new FormattedText(
            counter.Number.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            TextTypeface,
            CounterAnnotation.Radius * 1.1,
            Brushes.White,
            PixelsPerDip);
        context.DrawText(label, new Point(center.X - label.Width / 2, center.Y - label.Height / 2));
    }

    private void DrawObscure(DrawingContext context, ObscureAnnotation obscure)
    {
        var region = ToPixelRect(obscure.Rect).Intersect(new PixelRect(0, 0, _image.Width, _image.Height));
        if (region.IsEmpty)
        {
            return;
        }

        if (!_obscureCache.TryGetValue(obscure, out var bitmap))
        {
            if (_obscureCache.Count >= CacheLimit)
            {
                _obscureCache.Clear();
            }

            var cropped = _image.Crop(region);
            var filtered = obscure.Kind == ObscureKind.Pixelate
                ? ImageFilters.Pixelate(cropped, PixelateBlockSize)
                : ImageFilters.BoxBlur(cropped, BlurRadius);
            bitmap = filtered.ToBitmapSource();
            _obscureCache[obscure] = bitmap;
        }

        context.DrawImage(bitmap, new Rect(region.X, region.Y, region.Width, region.Height));
    }

    private static Pen CreatePen(AnnotationStyle style) =>
        new(new SolidColorBrush(ToColor(style.Color)), style.StrokeWidth) { LineJoin = PenLineJoin.Round };

    private static Color ToColor(Rgba color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    private static Point ToPoint(ImagePoint point) => new(point.X, point.Y);

    private static Rect ToRect(ImageRect rect) => new(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height));

    private static PixelRect ToPixelRect(ImageRect rect) => PixelRect.FromEdges(
        (int)Math.Floor(rect.Left),
        (int)Math.Floor(rect.Top),
        (int)Math.Ceiling(rect.Right),
        (int)Math.Ceiling(rect.Bottom));

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}

internal static class RectExtensions
{
    public static Point Center(this Rect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
}
