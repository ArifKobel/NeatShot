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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var enabled = _startup.IsEnabled;
        if (_settings.Current.LaunchAtStartup != enabled)
        {
            await _settings.UpdateAsync(settings => settings with { LaunchAtStartup = enabled }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
