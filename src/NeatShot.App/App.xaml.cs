using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeatShot.Composition;
using NeatShot.Core.Settings;
using NeatShot.Tray;

namespace NeatShot;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddNeatShot())
            .Build();

        await _host.Services.GetRequiredService<SettingsManager>().InitializeAsync();
        await _host.StartAsync();

        _host.Services.GetRequiredService<TrayController>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        base.OnExit(e);
    }
}
