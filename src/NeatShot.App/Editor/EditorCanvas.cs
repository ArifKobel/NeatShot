using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NeatShot.Core.Annotations;

namespace NeatShot.Editor;

public sealed class EditorCanvas : FrameworkElement
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(EditorViewModel),
        typeof(EditorCanvas),
        new PropertyMetadata(null, OnViewModelChanged));

    private static readonly DrawingBrush CheckerBrush = CreateCheckerBrush();

    private double _scale = 1;
    private Point _offset;

    public EditorViewModel? ViewModel
    {
        get => (EditorViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public double Scale => _scale;

    public Point ImageToCanvas(ImagePoint point) => new(_offset.X + point.X * _scale, _offset.Y + point.Y * _scale);

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (ViewModel is null)
        {
            return;
        }

        var image = ViewModel.Document.Image;
        UpdateLayoutMetrics(image.Width, image.Height);

        drawingContext.DrawRectangle(CheckerBrush, null, new Rect(RenderSize));
        drawingContext.PushTransform(new MatrixTransform(_scale, 0, 0, _scale, _offset.X, _offset.Y));

        drawingContext.DrawImage(ViewModel.Bitmap, new Rect(0, 0, image.Width, image.Height));
        ViewModel.Renderer.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var annotations = ViewModel.Preview is { } preview
            ? ViewModel.Annotations.Where(a => a.Id != preview.Id).Append(preview)
            : ViewModel.Annotations;
        ViewModel.Renderer.Draw(drawingContext, annotations, ViewModel.Selected);

        drawingContext.Pop();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        CaptureMouse();
        ViewModel?.PointerDown(ToImage(e.GetPosition(this)));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
        {
            ViewModel?.PointerMove(ToImage(e.GetPosition(this)));
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
            ViewModel?.PointerUp(ToImage(e.GetPosition(this)));
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (EditorCanvas)d;
        if (e.OldValue is EditorViewModel old)
        {
            old.PropertyChanged -= canvas.OnViewModelPropertyChanged;
        }

        if (e.NewValue is EditorViewModel next)
        {
            next.PropertyChanged += canvas.OnViewModelPropertyChanged;
        }

        canvas.InvalidateVisual();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    private void UpdateLayoutMetrics(int imageWidth, int imageHeight)
    {
        var available = RenderSize;
        if (available.Width <= 0 || available.Height <= 0)
        {
            return;
        }

        _scale = Math.Min(1, Math.Min(available.Width / imageWidth, available.Height / imageHeight));
        _offset = new Point(
            Math.Floor((available.Width - imageWidth * _scale) / 2),
            Math.Floor((available.Height - imageHeight * _scale) / 2));
    }

    private ImagePoint ToImage(Point point) => new((point.X - _offset.X) / _scale, (point.Y - _offset.Y) / _scale);

    private static DrawingBrush CreateCheckerBrush()
    {
        var light = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x30));
        var dark = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x27));
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(dark, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
        group.Children.Add(new GeometryDrawing(light, null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
        group.Children.Add(new GeometryDrawing(light, null, new RectangleGeometry(new Rect(8, 8, 8, 8))));

        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 16, 16),
            ViewportUnits = BrushMappingMode.Absolute,
        };
        brush.Freeze();
        return brush;
    }
}
