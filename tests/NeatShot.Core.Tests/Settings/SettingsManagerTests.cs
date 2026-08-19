using NeatShot.Core.Settings;

namespace NeatShot.Core.Tests.Settings;

public class SettingsManagerTests
{
    [Fact]
    public async Task InitializeAsync_LoadsFromStore()
    {
        var store = new InMemoryStore(new AppSettings { FileNamePattern = "custom" });
        var manager = new SettingsManager(store);

        await manager.InitializeAsync();

        Assert.Equal("custom", manager.Current.FileNamePattern);
    }

    [Fact]
    public async Task UpdateAsync_PersistsAndRaisesChanged()
    {
        var store = new InMemoryStore(new AppSettings());
        var manager = new SettingsManager(store);
        await manager.InitializeAsync();
        AppSettings? observed = null;
        manager.Changed += (_, settings) => observed = settings;

        await manager.UpdateAsync(settings => settings with { CopyToClipboard = false });

        Assert.False(manager.Current.CopyToClipboard);
        Assert.False(store.Saved!.CopyToClipboard);
        Assert.Same(manager.Current, observed);
    }

    [Fact]
    public async Task UpdateAsync_SkipsSaveWhenNothingChanged()
    {
        var store = new InMemoryStore(new AppSettings());
        var manager = new SettingsManager(store);
        await manager.InitializeAsync();
        var raised = false;
        manager.Changed += (_, _) => raised = true;

        await manager.UpdateAsync(settings => settings);

        Assert.Null(store.Saved);
        Assert.False(raised);
    }

    private sealed class InMemoryStore : ISettingsStore
    {
        private readonly AppSettings _initial;

        public InMemoryStore(AppSettings initial)
        {
            _initial = initial;
        }

        public AppSettings? Saved { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_initial);

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            Saved = settings;
            return Task.CompletedTask;
        }
    }
}
