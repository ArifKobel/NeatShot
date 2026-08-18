using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeatShot.Capture;
using NeatShot.Core.Capture;
using NeatShot.Core.Settings;
using NeatShot.Settings;

namespace NeatShot.Tray;

public sealed partial class TrayMenuViewModel : ObservableObject
{
    private readonly CaptureCoordinator _coordinator;
    private readonly SettingsManager _settings;
    private readonly SettingsWindowService _settingsWindow;

    public TrayMenuViewModel(CaptureCoordinator coordinator, SettingsManager settings, SettingsWindowService settingsWindow)
    {
        _coordinator = coordinator;
        _settings = settings;
        _settingsWindow = settingsWindow;
        settings.Changed += (_, _) => OnPropertyChanged(string.Empty);
    }

    public string CaptureRegionShortcut => Shortcut(HotkeyAction.CaptureRegion);

    public string CaptureWindowShortcut => Shortcut(HotkeyAction.CaptureWindow);

    public string CaptureFullscreenShortcut => Shortcut(HotkeyAction.CaptureFullscreen);

    [RelayCommand]
    private Task CaptureRegion() => _coordinator.StartAsync(CaptureMode.Region);

    [RelayCommand]
    private Task CaptureWindow() => _coordinator.StartAsync(CaptureMode.Window);

    [RelayCommand]
    private Task CaptureFullscreen() => _coordinator.StartAsync(CaptureMode.Fullscreen);

    [RelayCommand]
    private void OpenSettings() => _settingsWindow.Open();

    [RelayCommand]
    private static void Exit() => Application.Current.Shutdown();

    private string Shortcut(HotkeyAction action) =>
        _settings.Current.Hotkeys.TryGetValue(action, out var hotkey) ? hotkey.ToString() : string.Empty;
}
