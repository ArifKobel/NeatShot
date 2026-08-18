using Microsoft.Extensions.Hosting;
using NeatShot.Core.Settings;

namespace NeatShot.Settings;

public sealed class StartupSync : IHostedService
{
    private readonly SettingsManager _settings;
    private readonly IStartupRegistrar _startup;

    public StartupSync(SettingsManager settings, IStartupRegistrar startup)
    {
        _settings = settings;
        _startup = startup;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var wanted = _settings.Current.LaunchAtStartup;
        if (_startup.IsEnabled != wanted)
        {
            _startup.SetEnabled(wanted);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
