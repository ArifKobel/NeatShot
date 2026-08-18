using NeatShot.Core.Settings;

namespace NeatShot.Settings;

public sealed class SettingsWindowService
{
    private readonly SettingsManager _settings;
    private readonly IStartupRegistrar _startup;
    private SettingsWindow? _window;

    public SettingsWindowService(SettingsManager settings, IStartupRegistrar startup)
    {
        _settings = settings;
        _startup = startup;
    }

    public void Open()
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        _window = new SettingsWindow(new SettingsViewModel(_settings, _startup));
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }
}
