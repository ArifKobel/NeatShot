using System.Windows.Threading;
using NeatShot.Core.Capture;
using NeatShot.Export;

namespace NeatShot.QuickAccess;

public sealed class QuickAccessService
{
    private const int MaxStacked = 4;
    private const int StackGap = 2;
    private const int EdgeMargin = 6;
    private static readonly TimeSpan FollowInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FollowDelay = TimeSpan.FromMilliseconds(1500);

    private readonly IScreenProvider _screens;
    private readonly ICursorLocator _cursor;
    private readonly ImageFileWriter _fileWriter;
    private readonly List<QuickAccessWindow> _windows = [];
    private readonly DispatcherTimer _follow;
    private ScreenInfo? _screen;
    private ScreenInfo? _candidate;
    private DateTime _candidateSince;

    public QuickAccessService(IScreenProvider screens, ICursorLocator cursor, ImageFileWriter fileWriter)
    {
        _screens = screens;
        _cursor = cursor;
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
        var window = new QuickAccessWindow(viewModel, _screen, _cursor);
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
        if (active == _screen)
        {
            _candidate = null;
            return;
        }

        if (active != _candidate)
        {
            _candidate = active;
            _candidateSince = DateTime.UtcNow;
            return;
        }

        if (DateTime.UtcNow - _candidateSince >= FollowDelay)
        {
            _screen = active;
            _candidate = null;
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
        if (_screen is null)
        {
            return;
        }

        var bottom = _screen.WorkArea.Bottom - (int)Math.Round(EdgeMargin * _screen.ScaleFactor);
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            _windows[i].Place(_screen, bottom);
            bottom -= _windows[i].PixelHeight + StackGap;
        }
    }
}
