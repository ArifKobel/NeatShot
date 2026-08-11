using NeatShot.Core.Capture;
using NeatShot.Core.Settings;
using NeatShot.Export;

namespace NeatShot.QuickAccess;

public sealed class QuickAccessService
{
    private readonly IScreenProvider _screens;
    private readonly SettingsManager _settings;
    private readonly ImageFileWriter _fileWriter;
    private QuickAccessWindow? _window;

    public QuickAccessService(IScreenProvider screens, SettingsManager settings, ImageFileWriter fileWriter)
    {
        _screens = screens;
        _settings = settings;
        _fileWriter = fileWriter;
    }

    public event EventHandler<Core.Capture.Capture>? EditRequested;

    public void Show(Core.Capture.Capture capture, string? filePath)
    {
        ArgumentNullException.ThrowIfNull(capture);

        _window?.Close();

        var screens = _screens.GetScreens();
        var screen = screens.FirstOrDefault(s => s.IsPrimary) ?? screens[0];
        var viewModel = new QuickAccessViewModel(capture, filePath, _fileWriter, c => EditRequested?.Invoke(this, c));

        _window = new QuickAccessWindow(viewModel, screen, _settings.Current.QuickAccessTimeout);
        _window.Closed += (sender, _) =>
        {
            if (ReferenceEquals(_window, sender))
            {
                _window = null;
            }
        };
        _window.Show();
    }
}
