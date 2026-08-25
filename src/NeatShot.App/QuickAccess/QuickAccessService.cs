using System.Windows.Threading;
using NeatShot.Core.Capture;
using NeatShot.Core.Settings;
using NeatShot.Export;

namespace NeatShot.QuickAccess;

public sealed class QuickAccessService
{
    private const int MaxStacked = 4;
    private static readonly TimeSpan FollowInterval = TimeSpan.FromMilliseconds(250);

    private readonly IScreenProvider _screens;
    private readonly ICursorLocator _cursor;
    private readonly SettingsManager _settings;
    private readonly ImageFileWriter _fileWriter;
    private readonly List<QuickAccessWindow> _windows = [];
    private readonly DispatcherTimer _follow;
    private ScreenInfo? _screen;

    public QuickAccessService(IScreenProvider screens, ICursorLocator cursor, SettingsManager settings, ImageFileWriter fileWriter)
    {
        _screens = screens;
        _cursor = cursor;
        _settings = settings;
        _fileWriter = fileWriter;
        _follow = new DispatcherTimer { Interval = FollowInterval };
        _follow.Tick += (_, _) => FollowCursor();
    }

    public event EventHandler<Core.Capture.Capture>? EditRequested;

    public void Show(Core.Capture.Capture capture, string? filePath)
    {
        ArgumentNullException.ThrowIfNull(capture);

        while (_windows.Count >= MaxStacked)
        {
            _windows[0].Close();
        }

        _screen = ActiveScreen();
        var viewModel = new QuickAccessViewModel(capture, filePath, _fileWriter, c => EditRequested?.Invoke(this, c));
        var window = new QuickAccessWindow(viewModel, _screen, _settings.Current.QuickAccessTimeout);
        window.Closed += (sender, _) =>
        {
            _windows.Remove((QuickAccessWindow)sender!);
            Restack();
        };

        _windows.Add(window);
        window.Show();
        Restack();
        _follow.Start();
    }

    private void FollowCursor()
    {
        if (_windows.Count == 0)
        {
            _follow.Stop();
            return;
        }

        var active = ActiveScreen();
        if (active != _screen)
        {
            _screen = active;
            Restack();
        }
    }

    private ScreenInfo ActiveScreen()
    {
        var screens = _screens.GetScreens();
        var cursor = _cursor.GetPosition();
        return screens.FirstOrDefault(s => s.Bounds.Contains(cursor))
            ?? screens.FirstOrDefault(s => s.IsPrimary)
            ?? screens[0];
    }

    private void Restack()
    {
        for (var i = 0; i < _windows.Count; i++)
        {
            _windows[i].Place(_screen!, _windows.Count - 1 - i);
        }
    }
}
