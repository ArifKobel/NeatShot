using System.IO;
using Microsoft.Extensions.DependencyInjection;
using NeatShot.Capture;
using NeatShot.Core.Capture;
using NeatShot.Core.Input;
using NeatShot.Core.Settings;
using NeatShot.Editor;
using NeatShot.Export;
using NeatShot.Overlay;
using NeatShot.Platform.Capture;
using NeatShot.Platform.Input;
using NeatShot.Platform.Interop;
using NeatShot.Platform.Screens;
using NeatShot.Platform.Tray;
using NeatShot.Platform.Windows;
using NeatShot.QuickAccess;
using NeatShot.Tray;

namespace NeatShot.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNeatShot(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsStore>(_ => JsonSettingsStore.InUserProfile());
        services.AddSingleton<SettingsManager>();

        services.AddSingleton<MessageWindow>();
        services.AddSingleton<IScreenProvider, ScreenProvider>();
        services.AddSingleton<IScreenCapture, GdiScreenCapture>();
        services.AddSingleton<IWindowEnumerator, WindowEnumerator>();
        services.AddSingleton<IHotkeyService, GlobalHotkeyService>();
        services.AddSingleton(provider => new TrayIcon(
            provider.GetRequiredService<MessageWindow>(),
            Path.Combine(AppContext.BaseDirectory, "Assets", "neatshot.ico"),
            "NeatShot"));

        services.AddSingleton<OverlayService>();
        services.AddSingleton<ImageFileWriter>();
        services.AddSingleton<QuickAccessService>();
        services.AddSingleton<EditorService>();
        services.AddSingleton<CaptureCoordinator>();
        services.AddSingleton<TrayMenuViewModel>();
        services.AddSingleton<TrayController>();
        services.AddHostedService<HotkeyBinder>();

        return services;
    }
}
