using Microsoft.Extensions.Logging;
using NeatShot.Core.Capture;
using NeatShot.Core.Settings;
using NeatShot.Editor;
using NeatShot.Export;
using NeatShot.Imaging;
using NeatShot.Overlay;
using NeatShot.QuickAccess;

namespace NeatShot.Capture;

public sealed partial class CaptureCoordinator
{
    private readonly IScreenProvider _screens;
    private readonly IScreenCapture _screenCapture;
    private readonly OverlayService _overlay;
    private readonly QuickAccessService _quickAccess;
    private readonly EditorService _editor;
    private readonly SettingsManager _settings;
    private readonly ILogger<CaptureCoordinator> _logger;
    private bool _busy;

    public CaptureCoordinator(
        IScreenProvider screens,
        IScreenCapture screenCapture,
        OverlayService overlay,
        QuickAccessService quickAccess,
        EditorService editor,
        SettingsManager settings,
        ILogger<CaptureCoordinator> logger)
    {
        _screens = screens;
        _screenCapture = screenCapture;
        _overlay = overlay;
        _quickAccess = quickAccess;
        _editor = editor;
        _quickAccess.EditRequested += (_, capture) => _editor.Open(capture);
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
        if (LastCapture is { } capture)
        {
            _editor.Open(capture);
        }

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

        LogCaptured(capture.Mode, capture.Image.Width, capture.Image.Height);
        _quickAccess.Show(capture);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Mode} capture cancelled")]
    private partial void LogCaptureCancelled(CaptureMode mode);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Mode} capture {Width}x{Height}")]
    private partial void LogCaptured(CaptureMode mode, int width, int height);
}
