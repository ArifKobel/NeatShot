using Microsoft.Extensions.Logging;
using NeatShot.Core.Capture;
using NeatShot.Core.Settings;

namespace NeatShot.Capture;

public sealed partial class CaptureCoordinator
{
    private readonly ILogger<CaptureCoordinator> _logger;

    public CaptureCoordinator(ILogger<CaptureCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task HandleAsync(HotkeyAction action) => action switch
    {
        HotkeyAction.CaptureFullscreen => StartAsync(CaptureMode.Fullscreen),
        HotkeyAction.CaptureWindow => StartAsync(CaptureMode.Window),
        HotkeyAction.CaptureRegion => StartAsync(CaptureMode.Region),
        HotkeyAction.OpenLastCapture => OpenLastAsync(),
        _ => Task.CompletedTask,
    };

    public Task StartAsync(CaptureMode mode)
    {
        LogCaptureRequested(mode);
        return Task.CompletedTask;
    }

    public Task OpenLastAsync()
    {
        LogOpenLastRequested();
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Capture requested: {Mode}")]
    private partial void LogCaptureRequested(CaptureMode mode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Open last capture requested")]
    private partial void LogOpenLastRequested();
}
