using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NeatShot.Editor;

public partial class EditorWindow : Window
{
    private readonly EditorViewModel _viewModel;

    public EditorWindow(EditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CloseRequested += (_, _) => Close();
        Loaded += (_, _) => Surface.Focus();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

        var color = _viewModel.Color;
        var origin = Surface.ImageToCanvas(position);
        TextEntry.Text = string.Empty;
        TextEntry.FontSize = _viewModel.FontSize * Surface.Scale;
        TextEntry.Foreground = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        Canvas.SetLeft(TextEntry, origin.X);
        Canvas.SetTop(TextEntry, origin.Y);
        TextEntry.Visibility = Visibility.Visible;
        TextEntry.Focus();
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

        var size = AnnotationRenderer.MeasureText(TextEntry.Text, _viewModel.FontSize, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        _viewModel.CommitText(TextEntry.Text, size.Width, size.Height);
        Surface.Focus();
    }
}
