using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NeatShot.Core.Capture;
using NeatShot.Platform.Windows;

namespace NeatShot.QuickAccess;

public partial class QuickAccessWindow : Window
{
    public static readonly DependencyProperty MoveProgressProperty = DependencyProperty.Register(
        nameof(MoveProgress),
        typeof(double),
        typeof(QuickAccessWindow),
        new PropertyMetadata(1.0, (d, _) => ((QuickAccessWindow)d).ApplyMove()));

    private const int EdgeMargin = 6;
    private const int SwipeAwayZone = 40;
    private static readonly Duration SlideDuration = new(TimeSpan.FromMilliseconds(220));
    private static readonly Duration MoveDuration = new(TimeSpan.FromMilliseconds(260));

    private readonly QuickAccessViewModel _viewModel;
    private readonly ICursorLocator _cursor;
    private ScreenInfo _screen;
    private nint _handle;
    private Point _dragOrigin;
    private PixelRect _from;
    private PixelRect _to;

    public QuickAccessWindow(QuickAccessViewModel viewModel, ScreenInfo screen, ICursorLocator cursor)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _screen = screen;
        _cursor = cursor;

        viewModel.Dismissed += (_, _) => Close();
        Card.SizeChanged += (_, e) => CardClip.Rect = new Rect(e.NewSize);
        Card.PreviewMouseLeftButtonDown += OnCardMouseDown;
        Card.PreviewMouseMove += OnCardMouseMove;
    }

    public int PixelHeight => (int)Math.Round((Card.Height + Root.Margin.Top + Root.Margin.Bottom) * _screen.ScaleFactor);

    public void Place(ScreenInfo screen, int bottom)
    {
        _screen = screen;
        var scale = screen.ScaleFactor;
        var width = (int)Math.Round((Card.Width + Root.Margin.Left + Root.Margin.Right) * scale);
        var height = PixelHeight;
        var left = screen.WorkArea.Left + (int)Math.Round((EdgeMargin - Root.Margin.Left) * scale);
        var target = new PixelRect(left, bottom - height, width, height);

        if (_handle == 0 || _to.IsEmpty)
        {
            _from = _to = target;
            ApplyMove();
            return;
        }

        if (target == _to)
        {
            return;
        }

        _from = _to;
        _to = target;
        BeginAnimation(MoveProgressProperty, null);
        SetValue(MoveProgressProperty, 0.0);
        BeginAnimation(
            MoveProgressProperty,
            new DoubleAnimation(1, MoveDuration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private double MoveProgress => (double)GetValue(MoveProgressProperty);

    private void ApplyMove()
    {
        if (_handle == 0)
        {
            return;
        }

        var t = MoveProgress;
        var bounds = new PixelRect(
            (int)Math.Round(_from.X + (_to.X - _from.X) * t),
            (int)Math.Round(_from.Y + (_to.Y - _from.Y) * t),
            _to.Width,
            _to.Height);
        WindowPlacement.MoveToPixels(_handle, bounds, topmost: true);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _handle = new WindowInteropHelper(this).Handle;
        WindowPlacement.DisableTransitions(_handle);
        ApplyMove();
        SlideIn();
    }

    private void SlideIn()
    {
        var offset = new TranslateTransform(-Card.Width, 0);
        Root.RenderTransform = offset;
        offset.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(0, SlideDuration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragOrigin = e.GetPosition(this);
        if (e.ClickCount == 2)
        {
            _viewModel.EditCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnCardMouseMove(object sender, MouseEventArgs e)
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
        DragDrop.DoDragDrop(Card, data, DragDropEffects.Copy);

        if (_cursor.GetPosition().X < _screen.WorkArea.Left + SwipeAwayZone * _screen.ScaleFactor)
        {
            Close();
        }
    }
}
