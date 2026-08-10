using Microsoft.Extensions.Logging;
using NeatShot.Core.Capture;
using NeatShot.Core.Settings;
using NeatShot.Export;
using NeatShot.Imaging;
using NeatShot.Overlay;

namespace NeatShot.Capture;

public sealed partial class CaptureCoordinator
{
    private readonly IScreenProvider _screens;
    private readonly IScreenCapture _screenCapture;
    private readonly OverlayService _overlay;
    private readonly ImageFileWriter _fileWriter;
    private readonly SettingsManager _settings;
    private readonly ILogger<CaptureCoordinator> _logger;
    private bool _busy;

    public CaptureCoordinator(
        IScreenProvider screens,
        IScreenCapture screenCapture,
        OverlayService overlay,
        ImageFileWriter fileWriter,
        SettingsManager settings,
        ILogger<CaptureCoordinator> logger)
    {
        _screens = screens;
        _screenCapture = screenCapture;
        _overlay = overlay;
        _fileWriter = fileWriter;
        _settings = settings;
        _logger = logger;
    }

    public Core.Capture.Capture? LastCapture { get; private set; }

    public Task HandleAsync(HotkeyAction action) => action switch
    {
        HotkeyAction.CaptureFullscreen => StartAsync(CaptureMode.Fullscreen),
        HotkeyAction.CaptureWindow => StartAsync(CaptureMode.Window),
        HotkeyAction.CaptureRegion => StartAsync(CaptureMode.Region),
        HotkeyAction.OpenLastCapture => OpenLastAsync(),
        _ => Task.CompletedTask,
    };

    public async Task StartAsync(CaptureMode mode)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            var desktopBounds = _screens.GetVirtualScreenBounds();
            var desktop = _screenCapture.Capture(desktopBounds);
            var region = mode == CaptureMode.Fullscreen
                ? desktopBounds
                : await _overlay.SelectRegionAsync(desktop, desktopBounds, mode);

            if (region is not { IsEmpty: false } selected)
            {
                LogCaptureCancelled(mode);
                return;
            }

            var image = desktop.Crop(selected.Offset(-desktopBounds.X, -desktopBounds.Y));
            Publish(new Core.Capture.Capture(image, selected, mode, DateTimeOffset.Now));
        }
        finally
        {
            _busy = false;
        }
    }

    public Task OpenLastAsync()
    {
        LogOpenLastRequested();
        return Task.CompletedTask;
    }

    private void Publish(Core.Capture.Capture capture)
    {
        LastCapture = capture;
        var bitmap = capture.Image.ToBitmapSource();

        if (_settings.Current.CopyToClipboard)
        {
            ClipboardImageService.SetImage(bitmap);
        }

        var path = _fileWriter.Save(bitmap, capture.CapturedAt);
        LogCaptureSaved(capture.Mode, capture.Image.Width, capture.Image.Height, path);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Mode} capture cancelled")]
    private partial void LogCaptureCancelled(CaptureMode mode);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Mode} capture {Width}x{Height} saved to {Path}")]
    private partial void LogCaptureSaved(CaptureMode mode, int width, int height, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Open last capture requested")]
    private partial void LogOpenLastRequested();
}
