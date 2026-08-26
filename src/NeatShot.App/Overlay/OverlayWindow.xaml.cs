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
    private const int FramesBeforeReveal = 2;

    private readonly nint _handle;
    private OverlayViewModel? _viewModel;
    private int _framesUntilReveal;

    public OverlayWindow(ScreenInfo screen)
    {
        InitializeComponent();
        Screen = screen;
        Left = screen.Bounds.X / screen.ScaleFactor;
        Top = screen.Bounds.Y / screen.ScaleFactor;
        Width = screen.Bounds.Width / screen.ScaleFactor;
        Height = screen.Bounds.Height / screen.ScaleFactor;

        _handle = new WindowInteropHelper(this).EnsureHandle();
        WindowPlacement.DisableTransitions(_handle);
        WindowPlacement.HideFromSwitcher(_handle);
    }

    public ScreenInfo Screen { get; }

    public void Present(OverlayViewModel viewModel, BitmapSource backdrop)
    {
        _viewModel = viewModel;
        Backdrop.Source = backdrop;
        Canvas.Attach(viewModel, Screen.Bounds, VisualTreeHelper.GetDpi(this).DpiScaleX);

        WindowPlacement.SetCloaked(_handle, true);
        WindowPlacement.MoveToPixels(_handle, Screen.Bounds, topmost: true);
        Show();
        RevealOncePainted();
    }

    public void Dismiss()
    {
        CompositionTarget.Rendering -= OnFrameRendered;
        Hide();
        Backdrop.Source = null;
    }

    private void RevealOncePainted()
    {
        _framesUntilReveal = FramesBeforeReveal;
        CompositionTarget.Rendering += OnFrameRendered;
    }

    private void OnFrameRendered(object? sender, EventArgs e)
    {
        if (--_framesUntilReveal > 0)
        {
            return;
        }

        CompositionTarget.Rendering -= OnFrameRendered;
        WindowPlacement.SetCloaked(_handle, false);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _viewModel?.MoveCursor(CursorPixel(e));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        CaptureMouse();
        _viewModel?.BeginDrag(CursorPixel(e));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
        _viewModel?.EndDrag(CursorPixel(e));
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        _viewModel?.Cancel();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Escape:
                _viewModel?.Cancel();
                break;
            case Key.Enter:
            case Key.Space:
                _viewModel?.Confirm();
                break;
        }
    }

    private PixelPoint CursorPixel(MouseEventArgs e)
    {
        var screenPoint = PointToScreen(e.GetPosition(this));
        return new PixelPoint((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
    }
}
