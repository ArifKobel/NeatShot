using NeatShot.Core.Settings;

namespace NeatShot.Core.Tests.Settings;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "NeatShot.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task LoadAsync_ReturnsDefaultsWhenFileIsMissing()
    {
        var store = new JsonSettingsStore(Path.Combine(_directory, "settings.json"));

        var settings = await store.LoadAsync();

        Assert.Equal(new AppSettings(), settings with { Hotkeys = AppSettings.DefaultHotkeys });
    }

    [Fact]
    public async Task SaveAsync_RoundTripsAllProperties()
    {
        var store = new JsonSettingsStore(Path.Combine(_directory, "nested", "settings.json"));
        var settings = new AppSettings
        {
            SaveDirectory = @"D:\Shots",
            FileNamePattern = "shot-{n}",
            ImageFormat = ImageFormat.Jpeg,
            CopyToClipboard = false,
            SaveToDisk = false,
            LaunchAtStartup = true,
            Hotkeys = new Dictionary<HotkeyAction, Hotkey>
            {
                [HotkeyAction.CaptureRegion] = Hotkey.Parse("Alt+F9"),
            },
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(settings.SaveDirectory, loaded.SaveDirectory);
        Assert.Equal(settings.FileNamePattern, loaded.FileNamePattern);
        Assert.Equal(settings.ImageFormat, loaded.ImageFormat);
        Assert.Equal(settings.CopyToClipboard, loaded.CopyToClipboard);
        Assert.Equal(settings.SaveToDisk, loaded.SaveToDisk);
        Assert.Equal(settings.LaunchAtStartup, loaded.LaunchAtStartup);
        Assert.Equal(Hotkey.Parse("Alt+F9"), loaded.Hotkeys[HotkeyAction.CaptureRegion]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
