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
    private const int PixelateBlockPerStrength = 3;
    private const int BlurRadiusPerStrength = 2;
    private const int CacheLimit = 64;
    private const double ArrowHeadLength = 4;
    private const double ArrowHeadWidth = 2.2;
    private const double HandleSize = 8;
    private const double SplineTension = 6;
    private const byte HighlightAlpha = 0x70;

    private static readonly Color AccentColor = Color.FromRgb(0x0A, 0x84, 0xFF);
    private static readonly Typeface TextTypeface = new(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Brush HandleFill = Frozen(Brushes.White);
    private static readonly Brush MarqueeFill = Frozen(new SolidColorBrush(Color.FromArgb(0x22, 0x0A, 0x84, 0xFF)));

    private readonly CapturedImage _image;
    private readonly Dictionary<ObscureAnnotation, BitmapSource> _obscureCache = [];

    public AnnotationRenderer(CapturedImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
    }

    public double PixelsPerDip { get; set; } = 1;

    public void Draw(DrawingContext context, IEnumerable<Annotation> annotations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(annotations);

        foreach (var annotation in annotations)
        {
            Draw(context, annotation);
        }
    }

    public static void DrawSelection(DrawingContext context, IReadOnlyList<Annotation> selection, double scale)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selection);

        var outline = new Pen(new SolidColorBrush(AccentColor), 1 / scale) { DashStyle = DashStyles.Dash };
        var handlePen = new Pen(new SolidColorBrush(AccentColor), 1.5 / scale);
        var handleRadius = HandleSize / 2 / scale;

        foreach (var annotation in selection)
        {
            if (annotation is ArrowAnnotation arrow)
            {
                context.DrawEllipse(HandleFill, handlePen, ToPoint(arrow.Start), handleRadius, handleRadius);
                context.DrawEllipse(HandleFill, handlePen, ToPoint(arrow.End), handleRadius, handleRadius);
                continue;
            }

            context.DrawRectangle(null, outline, ToRect(annotation.Bounds.Inflate(2 / scale)));
            if (selection.Count == 1 && annotation.CanResize)
            {
                foreach (var (_, position) in EditorViewModel.HandlePositions(annotation.Bounds))
                {
                    var p = ToPoint(position);
                    context.DrawRectangle(HandleFill, handlePen, new Rect(p.X - handleRadius, p.Y - handleRadius, handleRadius * 2, handleRadius * 2));
                }
            }
        }
    }

    public static void DrawMarquee(DrawingContext context, ImageRect marquee, double scale)
    {
        ArgumentNullException.ThrowIfNull(context);
        var pen = new Pen(new SolidColorBrush(AccentColor), 1 / scale) { DashStyle = DashStyles.Dash };
        context.DrawRectangle(MarqueeFill, pen, ToRect(marquee));
    }

    public static Size MeasureText(string text, double fontSize, double pixelsPerDip)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TextTypeface, fontSize, Brushes.Black, pixelsPerDip);
        return new Size(formatted.WidthIncludingTrailingWhitespace, formatted.Height);
    }

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
        var points = freehand.Points;
        if (points.Count == 0)
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
            geometry.BeginFigure(ToPoint(points[0]), isFilled: false, isClosed: false);
            for (var i = 0; i < points.Count - 1; i++)
            {
                var previous = ToPoint(points[Math.Max(i - 1, 0)]);
                var current = ToPoint(points[i]);
                var next = ToPoint(points[i + 1]);
                var after = ToPoint(points[Math.Min(i + 2, points.Count - 1)]);
                geometry.BezierTo(
                    current + (next - previous) / SplineTension,
                    next - (after - current) / SplineTension,
                    next,
                    isStroked: true,
                    isSmoothJoin: true);
            }
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

        context.DrawText(formatted, ToPoint(text.Position));
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
                ? ImageFilters.Pixelate(cropped, obscure.Strength * PixelateBlockPerStrength)
                : ImageFilters.BoxBlur(cropped, obscure.Strength * BlurRadiusPerStrength);
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
