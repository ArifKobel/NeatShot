using NeatShot.Core.Capture;
using NeatShot.Imaging;

namespace NeatShot.Overlay;

public sealed class OverlayService
{
    private readonly IScreenProvider _screens;
    private readonly IWindowEnumerator _windows;

    public OverlayService(IScreenProvider screens, IWindowEnumerator windows)
    {
        _screens = screens;
        _windows = windows;
    }

    public async Task<PixelRect?> SelectRegionAsync(CapturedImage frozenDesktop, PixelRect desktopBounds, CaptureMode mode)
    {
        ArgumentNullException.ThrowIfNull(frozenDesktop);

        var windows = mode == CaptureMode.Fullscreen ? [] : _windows.GetVisibleWindows();
        var viewModel = new OverlayViewModel(mode, windows);
        var overlays = _screens.GetScreens()
            .Select(screen => CreateWindow(viewModel, screen, frozenDesktop, desktopBounds))
            .ToList();

        foreach (var overlay in overlays)
        {
            overlay.Show();
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
                overlay.Close();
            }
        }
    }

    private static OverlayWindow CreateWindow(OverlayViewModel viewModel, ScreenInfo screen, CapturedImage desktop, PixelRect desktopBounds)
    {
        var backdrop = desktop.Crop(screen.Bounds.Offset(-desktopBounds.X, -desktopBounds.Y)).ToBitmapSource();
        return new OverlayWindow(viewModel, screen, backdrop);
    }
}
