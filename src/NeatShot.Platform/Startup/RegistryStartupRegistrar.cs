using Microsoft.Win32;
using NeatShot.Core.Settings;

namespace NeatShot.Platform.Startup;

public sealed class RegistryStartupRegistrar : IStartupRegistrar
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NeatShot";

    private readonly string _executablePath;

    public RegistryStartupRegistrar(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _executablePath = executablePath;
    }

    public static RegistryStartupRegistrar ForCurrentProcess() =>
        new(Environment.ProcessPath ?? throw new InvalidOperationException("Process path is unavailable."));

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value && File.Exists(value.Trim('"'));
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            key.SetValue(ValueName, $"\"{_executablePath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
