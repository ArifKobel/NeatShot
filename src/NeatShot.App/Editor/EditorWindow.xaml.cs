using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using NeatShot.Platform.Windows;

namespace NeatShot.Editor;

public partial class EditorWindow : Window
{
    private const uint TitleBarColor = 0x00241E1E;

    private readonly EditorViewModel _viewModel;

    public EditorWindow(EditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => Close();
        SourceInitialized += (_, _) => WindowPlacement.UseDarkTitleBar(new WindowInteropHelper(this).Handle, TitleBarColor);
        Loaded += (_, _) => Surface.Focus();
        Surface.ContextMenuOpening += (_, e) => e.Handled = _viewModel.PendingTextPosition is not null;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Surface.ViewChanged += (_, _) => PlaceTextInput();
        TextEntry.KeyDown += OnTextInputKeyDown;
        TextEntry.LostFocus += (_, _) => CommitText();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.PendingTextPosition))
        {
            ShowTextInput();
        }
    }

    private void ShowTextInput()
    {
        if (_viewModel.PendingTextPosition is not { } position)
        {
            TextEntry.Visibility = Visibility.Collapsed;
            return;
        }

        var editing = _viewModel.EditingText;
        var color = editing?.Style.Color ?? _viewModel.Color;
        TextEntry.Text = editing?.Text ?? string.Empty;
        var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        TextEntry.Foreground = brush;
        TextEntry.CaretBrush = brush;
        PlaceTextInput();
        TextEntry.Visibility = Visibility.Visible;
        TextEntry.Focus();
        TextEntry.CaretIndex = TextEntry.Text.Length;
    }

    private void PlaceTextInput()
    {
        if (_viewModel.PendingTextPosition is not { } position)
        {
            return;
        }

        var origin = Surface.ImageToCanvas(position);
        TextEntry.FontSize = (_viewModel.EditingText?.FontSize ?? _viewModel.FontSize) * Surface.Scale;
        Canvas.SetLeft(TextEntry, origin.X);
        Canvas.SetTop(TextEntry, origin.Y);
    }

    private void OnTextInputKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitText();
                e.Handled = true;
                break;
            case Key.Escape:
                _viewModel.CancelText();
                Surface.Focus();
                e.Handled = true;
                break;
        }
    }

    private void CommitText()
    {
        if (TextEntry.Visibility != Visibility.Visible)
        {
            return;
        }

        var fontSize = _viewModel.EditingText?.FontSize ?? _viewModel.FontSize;
        var size = AnnotationRenderer.MeasureText(TextEntry.Text, fontSize, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        _viewModel.CommitText(TextEntry.Text, size.Width, size.Height);
        Surface.Focus();
    }
}
