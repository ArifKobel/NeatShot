using System.Windows;
using System.Windows.Media.Imaging;
using NeatShot.Core.Capture;
using NeatShot.Imaging;

namespace NeatShot.Overlay;

public sealed class OverlayService
{
    private readonly IScreenProvider _screens;
    private readonly IWindowEnumerator _windowEnumerator;
    private readonly Dictionary<string, OverlayWindow> _overlays = [];

    public OverlayService(IScreenProvider screens, IWindowEnumerator windows)
    {
        _screens = screens;
        _windowEnumerator = windows;
    }

    public void Prepare()
    {
        foreach (var screen in _screens.GetScreens())
        {
            OverlayFor(screen);
        }
    }

    public async Task<PixelRect?> SelectRegionAsync(CapturedImage frozenDesktop, PixelRect desktopBounds, CaptureMode mode)
    {
        ArgumentNullException.ThrowIfNull(frozenDesktop);

        var windows = mode == CaptureMode.Fullscreen ? [] : _windowEnumerator.GetVisibleWindows();
        var viewModel = new OverlayViewModel(mode, windows);
        var desktop = frozenDesktop.ToBitmapSource();
        var overlays = _screens.GetScreens().Select(OverlayFor).ToList();

        foreach (var overlay in overlays)
        {
            overlay.Present(viewModel, Slice(desktop, overlay.Screen.Bounds.Offset(-desktopBounds.X, -desktopBounds.Y)));
        }

        overlays[0].Activate();

        try
        {
            return await viewModel.Result;
        }
        finally
        {
            foreach (var overlay in overlays)
            {
                overlay.Dismiss();
            }
        }
    }

    private OverlayWindow OverlayFor(ScreenInfo screen)
    {
        if (_overlays.TryGetValue(screen.DeviceName, out var existing) && existing.Screen == screen)
        {
            return existing;
        }

        existing?.Close();
        var overlay = new OverlayWindow(screen);
        _overlays[screen.DeviceName] = overlay;
        return overlay;
    }

    private static CroppedBitmap Slice(BitmapSource desktop, PixelRect region)
    {
        var clipped = region.Intersect(new PixelRect(0, 0, desktop.PixelWidth, desktop.PixelHeight));
        var slice = new CroppedBitmap(desktop, new Int32Rect(clipped.X, clipped.Y, clipped.Width, clipped.Height));
        slice.Freeze();
        return slice;
    }
}
