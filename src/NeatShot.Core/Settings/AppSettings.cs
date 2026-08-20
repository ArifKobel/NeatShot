namespace NeatShot.Core.Settings;

public sealed record AppSettings
{
    public static IReadOnlyDictionary<HotkeyAction, Hotkey> DefaultHotkeys { get; } = new Dictionary<HotkeyAction, Hotkey>
    {
        [HotkeyAction.CaptureFullscreen] = Hotkey.Parse("Alt+Shift+1"),
        [HotkeyAction.CaptureWindow] = Hotkey.Parse("Alt+Shift+2"),
        [HotkeyAction.CaptureRegion] = Hotkey.Parse("Alt+Shift+3"),
        [HotkeyAction.OpenLastCapture] = Hotkey.Parse("Alt+Shift+E"),
    };

    public string SaveDirectory { get; init; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "NeatShot");

    public string FileNamePattern { get; init; } = "NeatShot {date} at {time}";

    public ImageFormat ImageFormat { get; init; } = ImageFormat.Png;

    public bool CopyToClipboard { get; init; } = true;

    public bool SaveToDisk { get; init; } = true;

    public bool LaunchAtStartup { get; init; }

    public TimeSpan QuickAccessTimeout { get; init; } = TimeSpan.FromSeconds(8);

    public IReadOnlyDictionary<HotkeyAction, Hotkey> Hotkeys { get; init; } = DefaultHotkeys;
}
