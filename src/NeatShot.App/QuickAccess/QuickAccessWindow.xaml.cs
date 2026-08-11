using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NeatShot.Core.Capture;
using NeatShot.Platform.Windows;

namespace NeatShot.QuickAccess;

public partial class QuickAccessWindow : Window
{
    private const int EdgeMargin = 8;
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(220));

    private readonly QuickAccessViewModel _viewModel;
    private readonly ScreenInfo _screen;
    private readonly DispatcherTimer _dismissTimer;
    private Point _dragOrigin;

    public QuickAccessWindow(QuickAccessViewModel viewModel, ScreenInfo screen, TimeSpan timeout)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _screen = screen;
        _dismissTimer = new DispatcherTimer { Interval = timeout };
        _dismissTimer.Tick += (_, _) => Close();

        viewModel.Dismissed += (_, _) => Close();
        MouseEnter += (_, _) => _dismissTimer.Stop();
        MouseLeave += (_, _) => _dismissTimer.Start();
        Thumbnail.PreviewMouseLeftButtonDown += (_, e) => _dragOrigin = e.GetPosition(this);
        Thumbnail.PreviewMouseMove += OnThumbnailMouseMove;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var scale = _screen.ScaleFactor;
        var width = (int)Math.Round(Width * scale);
        var height = (int)Math.Round(ActualHeight * scale);
        var margin = (int)Math.Round(EdgeMargin * scale);
        var bounds = new PixelRect(
            _screen.WorkArea.Right - width - margin,
            _screen.WorkArea.Bottom - height - margin,
            width,
            height);

        WindowPlacement.MoveToPixels(new WindowInteropHelper(this).Handle, bounds, topmost: true);
        SlideIn();
        _dismissTimer.Start();
    }

    private void SlideIn()
    {
        var offset = new System.Windows.Media.TranslateTransform(Width, 0);
        Card.RenderTransform = offset;
        offset.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty,
            new DoubleAnimation(0, SlideDuration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, SlideDuration));
    }

    private void OnThumbnailMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var delta = e.GetPosition(this) - _dragOrigin;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject(DataFormats.FileDrop, new[] { _viewModel.EnsureFile() });
        data.SetImage(_viewModel.Bitmap);
        DragDrop.DoDragDrop(Thumbnail, data, DragDropEffects.Copy);
    }
}
