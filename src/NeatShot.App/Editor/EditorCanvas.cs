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

    private const double HandleTolerance = 8;
    private const double WheelZoomStep = 1.1;
    private const double FitPadding = 24;

    private const int ShadowLayers = 6;

    private static readonly Brush Background = Frozen(new SolidColorBrush(Color.FromRgb(0x15, 0x15, 0x19)));
    private static readonly Brush ImageShadow = Frozen(new SolidColorBrush(Color.FromArgb(0x30, 0, 0, 0)));

    private double _scale = 1;
    private Vector _pan;
    private Point _panOrigin;
    private Vector _panStart;
    private bool _panning;
    private bool _spaceHeld;

    public EditorCanvas()
    {
        ClipToBounds = true;
    }

    public EditorViewModel? ViewModel
    {
        get => (EditorViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public double Scale => _scale;

    public Point ImageToCanvas(ImagePoint point)
    {
        var offset = Offset();
        return new Point(offset.X + point.X * _scale, offset.Y + point.Y * _scale);
    }

    public ImagePoint CanvasToImage(Point point)
    {
        var offset = Offset();
        return new ImagePoint((point.X - offset.X) / _scale, (point.Y - offset.Y) / _scale);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (ViewModel is null)
        {
            return;
        }

        var image = ViewModel.Document.Image;
        UpdateScale(image.Width, image.Height);
        var offset = Offset();
        var canvas = ViewModel.Canvas;
        var canvasRect = new Rect(offset.X + canvas.X * _scale, offset.Y + canvas.Y * _scale, canvas.Width * _scale, canvas.Height * _scale);
        var background = ViewModel.CanvasBackground;

        drawingContext.DrawRectangle(Background, null, new Rect(RenderSize));
        DrawShadow(drawingContext, canvasRect);
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(background.A, background.R, background.G, background.B)), null, canvasRect);
        drawingContext.PushTransform(new MatrixTransform(_scale, 0, 0, _scale, offset.X, offset.Y));

        drawingContext.DrawImage(ViewModel.Bitmap, new Rect(0, 0, image.Width, image.Height));
        ViewModel.Renderer.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var previews = ViewModel.Previews;
        ViewModel.Renderer.Draw(drawingContext, ViewModel.VisibleAnnotations);

        if (ViewModel.Selection.Count > 0)
        {
            var selection = ViewModel.Selection.Select(a => previews.TryGetValue(a.Id, out var p) ? p : a).ToArray();
            AnnotationRenderer.DrawSelection(drawingContext, selection, _scale);
        }

        if (ViewModel.Marquee is { } marquee)
        {
            AnnotationRenderer.DrawMarquee(drawingContext, marquee, _scale);
        }

        drawingContext.Pop();
    }

    private static void DrawShadow(DrawingContext drawingContext, Rect image)
    {
        for (var layer = ShadowLayers; layer >= 1; layer--)
        {
            var spread = layer * 2;
            var rect = new Rect(image.X - spread, image.Y - spread + layer, image.Width + spread * 2, image.Height + spread * 2);
            drawingContext.DrawRoundedRectangle(ImageShadow, null, rect, spread, spread);
        }
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

        if (_spaceHeld)
        {
            BeginPan(e.GetPosition(this));
            return;
        }

        if (e.ClickCount == 2 && ViewModel?.ActiveTool == EditorTool.Select && ViewModel.BeginTextEdit(CanvasToImage(e.GetPosition(this))))
        {
            ReleaseMouseCapture();
            return;
        }

        var extend = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        ViewModel?.PointerDown(CanvasToImage(e.GetPosition(this)), HandleTolerance / _scale, extend);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);

        if (_panning)
        {
            _pan = _panStart + (position - _panOrigin);
            InvalidateVisual();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
        {
            ViewModel?.PointerMove(CanvasToImage(position));
        }
        else
        {
            UpdateCursor(position);
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsMouseCaptured)
        {
            return;
        }

        ReleaseMouseCapture();
        if (_panning)
        {
            _panning = false;
            return;
        }

        ViewModel?.PointerUp(CanvasToImage(e.GetPosition(this)), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        Focus();
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.ActiveTool = EditorTool.Select;
        ViewModel.SelectAt(CanvasToImage(e.GetPosition(this)));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton == MouseButton.Middle)
        {
            CaptureMouse();
            BeginPan(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton == MouseButton.Middle && _panning)
        {
            _panning = false;
            ReleaseMouseCapture();
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (ViewModel is null)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var anchor = CanvasToImage(e.GetPosition(this));
            ViewModel.ZoomBy(e.Delta > 0 ? WheelZoomStep : 1 / WheelZoomStep);
            UpdateScale(ViewModel.Document.Image.Width, ViewModel.Document.Image.Height);
            var moved = ImageToCanvas(anchor);
            _pan += e.GetPosition(this) - moved;
        }
        else
        {
            _pan += Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? new Vector(e.Delta, 0) : new Vector(0, e.Delta);
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Space)
        {
            _spaceHeld = true;
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == Key.Space)
        {
            _spaceHeld = false;
            UpdateCursor(Mouse.GetPosition(this));
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.FitToWindow) && ViewModel?.FitToWindow == true)
        {
            _pan = default;
        }

        if (e.PropertyName is nameof(EditorViewModel.ActiveTool))
        {
            UpdateCursor(Mouse.GetPosition(this));
        }

        InvalidateVisual();
    }

    private void BeginPan(Point position)
    {
        _panning = true;
        _panOrigin = position;
        _panStart = _pan;
    }

    private void UpdateScale(int imageWidth, int imageHeight)
    {
        if (ViewModel is null || RenderSize.Width <= 0 || RenderSize.Height <= 0)
        {
            return;
        }

        if (ViewModel.FitToWindow)
        {
            _scale = Math.Min(1, Math.Min((RenderSize.Width - FitPadding * 2) / imageWidth, (RenderSize.Height - FitPadding * 2) / imageHeight));
            ViewModel.Zoom = _scale;
        }
        else
        {
            _scale = ViewModel.Zoom;
        }
    }

    private Point Offset()
    {
        var image = ViewModel!.Document.Image;
        var canvas = ViewModel.Document.Canvas;
        _pan = ClampPan(_pan, canvas.Width * _scale, canvas.Height * _scale);
        return new Point(
            Math.Round((RenderSize.Width - image.Width * _scale) / 2 + _pan.X),
            Math.Round((RenderSize.Height - image.Height * _scale) / 2 + _pan.Y));
    }

    private Vector ClampPan(Vector pan, double imageWidth, double imageHeight)
    {
        var slackX = Math.Max(0, (imageWidth - RenderSize.Width) / 2 + FitPadding);
        var slackY = Math.Max(0, (imageHeight - RenderSize.Height) / 2 + FitPadding);
        return new Vector(Math.Clamp(pan.X, -slackX, slackX), Math.Clamp(pan.Y, -slackY, slackY));
    }

    private void UpdateCursor(Point position)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (_spaceHeld)
        {
            Cursor = Cursors.ScrollAll;
            return;
        }

        if (ViewModel.ActiveTool != EditorTool.Select)
        {
            Cursor = ViewModel.ActiveTool == EditorTool.Text ? Cursors.IBeam : Cursors.Cross;
            return;
        }

        Cursor = ViewModel.HitHandle(CanvasToImage(position), HandleTolerance / _scale) switch
        {
            Handle.TopLeft or Handle.BottomRight => Cursors.SizeNWSE,
            Handle.TopRight or Handle.BottomLeft => Cursors.SizeNESW,
            Handle.Top or Handle.Bottom => Cursors.SizeNS,
            Handle.Left or Handle.Right => Cursors.SizeWE,
            Handle.ArrowStart or Handle.ArrowEnd => Cursors.Cross,
            Handle.Body => Cursors.SizeAll,
            _ => Cursors.Arrow,
        };
    }

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
