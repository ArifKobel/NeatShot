using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NeatShot.Core.Settings;

namespace NeatShot.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsManager _settings;
    private readonly IStartupRegistrar _startup;

    public SettingsViewModel(SettingsManager settings, IStartupRegistrar startup)
    {
        _settings = settings;
        _startup = startup;

        var current = settings.Current;
        SaveDirectory = current.SaveDirectory;
        FileNamePattern = current.FileNamePattern;
        ImageFormat = current.ImageFormat;
        CopyToClipboard = current.CopyToClipboard;
        SaveToDisk = current.SaveToDisk;
        LaunchAtStartup = current.LaunchAtStartup;
        Hotkeys =
        [
            new HotkeyEntry(HotkeyAction.CaptureRegion, "Capture region", current.Hotkeys[HotkeyAction.CaptureRegion]),
            new HotkeyEntry(HotkeyAction.CaptureWindow, "Capture window", current.Hotkeys[HotkeyAction.CaptureWindow]),
            new HotkeyEntry(HotkeyAction.CaptureFullscreen, "Capture fullscreen", current.Hotkeys[HotkeyAction.CaptureFullscreen]),
            new HotkeyEntry(HotkeyAction.OpenLastCapture, "Open last capture", current.Hotkeys[HotkeyAction.OpenLastCapture]),
        ];
    }

    public event EventHandler? CloseRequested;

    public IReadOnlyList<HotkeyEntry> Hotkeys { get; }

    public IReadOnlyList<ImageFormat> ImageFormats { get; } = Enum.GetValues<ImageFormat>();

    public string FileNamePreview => Core.Export.FileNameFormatter.Format(FileNamePattern, DateTimeOffset.Now);

    [ObservableProperty]
    public partial string SaveDirectory { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileNamePreview))]
    public partial string FileNamePattern { get; set; }

    [ObservableProperty]
    public partial ImageFormat ImageFormat { get; set; }

    [ObservableProperty]
    public partial bool CopyToClipboard { get; set; }

    [ObservableProperty]
    public partial bool SaveToDisk { get; set; }

    [ObservableProperty]
    public partial bool LaunchAtStartup { get; set; }

    [ObservableProperty]
    public partial string? Error { get; private set; }

    [RelayCommand]
    private void BrowseDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose where captures are saved",
            InitialDirectory = Directory.Exists(SaveDirectory) ? SaveDirectory : null,
        };

        if (dialog.ShowDialog() == true)
        {
            SaveDirectory = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Error = Validate();
        if (Error is not null)
        {
            return;
        }

        var hotkeys = Hotkeys.ToDictionary(entry => entry.Action, entry => entry.Hotkey!.Value);
        await _settings.UpdateAsync(settings => settings with
        {
            SaveDirectory = SaveDirectory.Trim(),
            FileNamePattern = FileNamePattern.Trim(),
            ImageFormat = ImageFormat,
            CopyToClipboard = CopyToClipboard,
            SaveToDisk = SaveToDisk,
            LaunchAtStartup = LaunchAtStartup,
            Hotkeys = hotkeys,
        });

        _startup.SetEnabled(LaunchAtStartup);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(SaveDirectory))
        {
            return "Choose a folder for captures.";
        }

        if (string.IsNullOrWhiteSpace(FileNamePattern))
        {
            return "The file name pattern must not be empty.";
        }

        var missing = Hotkeys.FirstOrDefault(entry => entry.Hotkey is null);
        if (missing is not null)
        {
            return $"Assign a shortcut for \"{missing.Label}\".";
        }

        var duplicate = Hotkeys.GroupBy(entry => entry.Hotkey).FirstOrDefault(group => group.Count() > 1);
        return duplicate is null ? null : $"{duplicate.Key} is assigned more than once.";
    }
}

public sealed partial class HotkeyEntry : ObservableObject
{
    public HotkeyEntry(HotkeyAction action, string label, Hotkey hotkey)
    {
        Action = action;
        Label = label;
        Hotkey = hotkey;
    }

    public HotkeyAction Action { get; }

    public string Label { get; }

    [ObservableProperty]
    public partial Hotkey? Hotkey { get; set; }
}
