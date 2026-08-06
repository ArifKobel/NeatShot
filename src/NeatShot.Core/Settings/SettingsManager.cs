namespace NeatShot.Core.Settings;

public sealed class SettingsManager
{
    private readonly ISettingsStore _store;
    private AppSettings _current = new();

    public SettingsManager(ISettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public event EventHandler<AppSettings>? Changed;

    public AppSettings Current => _current;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _current = await _store.LoadAsync(cancellationToken);
    }

    public async Task UpdateAsync(Func<AppSettings, AppSettings> change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        var updated = change(_current);
        if (updated == _current)
        {
            return;
        }

        await _store.SaveAsync(updated, cancellationToken);
        _current = updated;
        Changed?.Invoke(this, updated);
    }
}
