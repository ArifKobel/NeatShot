namespace NeatShot.Core.Settings;

public interface IStartupRegistrar
{
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
