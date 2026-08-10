using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeatShot.Core.Capture;
using NeatShot.Platform.Windows;

namespace NeatShot.Overlay;

public partial class OverlayWindow : Window
{
    private readonly OverlayViewModel _viewModel;
    private readonly ScreenInfo _screen;

    public OverlayWindow(OverlayViewModel viewModel, ScreenInfo screen, BitmapSource backdrop)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _screen = screen;
        Backdrop.Source = backdrop;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        WindowPlacement.MoveToPixels(handle, _screen.Bounds, topmost: true);
        Canvas.Attach(_viewModel, _screen.Bounds, VisualTreeHelper.GetDpi(this).DpiScaleX);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _viewModel.MoveCursor(CursorPixel(e));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        CaptureMouse();
        _viewModel.BeginDrag(CursorPixel(e));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        _viewModel.EndDrag(CursorPixel(e));
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        _viewModel.Cancel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Escape:
                _viewModel.Cancel();
                break;
            case Key.Enter:
            case Key.Space:
                _viewModel.Confirm();
                break;
        }
    }

    private PixelPoint CursorPixel(MouseEventArgs e)
    {
        var screenPoint = PointToScreen(e.GetPosition(this));
        return new PixelPoint((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
    }
}
