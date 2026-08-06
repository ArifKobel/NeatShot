using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeatShot.Core.Input;
using NeatShot.Core.Settings;

namespace NeatShot.Capture;

public sealed class HotkeyBinder : IHostedService
{
    private readonly IHotkeyService _hotkeys;
    private readonly SettingsManager _settings;
    private readonly CaptureCoordinator _coordinator;
    private readonly ILogger<HotkeyBinder> _logger;

    public HotkeyBinder(
        IHotkeyService hotkeys,
        SettingsManager settings,
        CaptureCoordinator coordinator,
        ILogger<HotkeyBinder> logger)
    {
        _hotkeys = hotkeys;
        _settings = settings;
        _coordinator = coordinator;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        _settings.Changed += OnSettingsChanged;
        Bind(_settings.Current);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hotkeys.HotkeyPressed -= OnHotkeyPressed;
        _settings.Changed -= OnSettingsChanged;
        return Task.CompletedTask;
    }

    private void Bind(AppSettings settings)
    {
        foreach (var action in _hotkeys.Apply(settings.Hotkeys))
        {
            _logger.LogWarning("Hotkey {Hotkey} for {Action} is already in use", settings.Hotkeys[action], action);
        }
    }

    private void OnSettingsChanged(object? sender, AppSettings settings) => Bind(settings);

    private async void OnHotkeyPressed(object? sender, HotkeyAction action)
    {
        try
        {
            await _coordinator.HandleAsync(action);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Hotkey action {Action} failed", action);
        }
    }
}
