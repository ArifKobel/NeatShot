using System.Globalization;
using System.Windows;
using System.Windows.Media;
using NeatShot.Core.Capture;

namespace NeatShot.Overlay;

public sealed class SelectionCanvas : FrameworkElement
{
    private static readonly Brush DimBrush = Freeze(new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)));
    private static readonly Brush LabelBackground = Freeze(new SolidColorBrush(Color.FromArgb(0xD0, 0x1E, 0x1E, 0x24)));
    private static readonly Pen SelectionPen = Freeze(new Pen(Brushes.White, 1));
    private static readonly Pen HoverPen = Freeze(new Pen(new SolidColorBrush(Color.FromRgb(0x4C, 0x9F, 0xFF)), 2));
    private static readonly Typeface LabelTypeface = new("Segoe UI");

    private OverlayViewModel? _viewModel;
    private PixelRect _screenBounds;
    private double _scale = 1;

    public void Attach(OverlayViewModel viewModel, PixelRect screenBounds, double scale)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelChanged;
        }

        _viewModel = viewModel;
        _screenBounds = screenBounds;
        _scale = scale;
        viewModel.PropertyChanged += OnViewModelChanged;
        InvalidateVisual();
    }

    private void OnViewModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_viewModel is null)
        {
            return;
        }

        var surface = new Rect(RenderSize);
        var selection = _viewModel.Selection is { IsEmpty: false } region ? ToLocal(region) : (Rect?)null;

        DrawDim(drawingContext, surface, selection);

        if (selection is { } rect)
        {
            drawingContext.DrawRectangle(null, SelectionPen, Snap(rect));
            DrawSizeLabel(drawingContext, rect, _viewModel.Selection!.Value.Size);
        }
        else if (_viewModel.HoveredWindow is { } window && _viewModel.Mode != CaptureMode.Fullscreen)
        {
            drawingContext.DrawRectangle(null, HoverPen, Snap(ToLocal(window.Bounds)));
        }
    }

    private static void DrawDim(DrawingContext drawingContext, Rect surface, Rect? hole)
    {
        if (hole is null)
        {
            drawingContext.DrawRectangle(DimBrush, null, surface);
            return;
        }

        var geometry = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(surface),
            new RectangleGeometry(hole.Value));
        drawingContext.DrawGeometry(DimBrush, null, geometry);
    }

    private void DrawSizeLabel(DrawingContext drawingContext, Rect selection, PixelSize size)
    {
        var text = new FormattedText(
            string.Create(CultureInfo.InvariantCulture, $"{size.Width} × {size.Height}"),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            12,
            Brushes.White,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        const double padding = 5;
        var width = text.Width + padding * 2;
        var height = text.Height + padding;
        var x = Math.Clamp(selection.Left, 0, Math.Max(0, RenderSize.Width - width));
        var y = selection.Bottom + 6 + height <= RenderSize.Height ? selection.Bottom + 6 : selection.Top - height - 6;
        if (y < 0)
        {
            y = selection.Top + 6;
        }

        var box = new Rect(x, y, width, height);
        drawingContext.DrawRoundedRectangle(LabelBackground, null, box, 4, 4);
        drawingContext.DrawText(text, new Point(x + padding, y + padding / 2));
    }

    private Rect ToLocal(PixelRect rect) => new(
        (rect.X - _screenBounds.X) / _scale,
        (rect.Y - _screenBounds.Y) / _scale,
        rect.Width / _scale,
        rect.Height / _scale);

    private static Rect Snap(Rect rect) => new(
        Math.Floor(rect.X) + 0.5,
        Math.Floor(rect.Y) + 0.5,
        Math.Max(0, Math.Round(rect.Width) - 1),
        Math.Max(0, Math.Round(rect.Height) - 1));

    private static T Freeze<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
