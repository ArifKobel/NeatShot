using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeatShot.Export;
using NeatShot.Imaging;

namespace NeatShot.QuickAccess;

public sealed partial class QuickAccessViewModel : ObservableObject
{
    private static readonly TimeSpan ConfirmationDuration = TimeSpan.FromMilliseconds(650);

    private readonly ImageFileWriter _fileWriter;
    private readonly Action<Core.Capture.Capture> _openEditor;

    public QuickAccessViewModel(
        Core.Capture.Capture capture,
        string? filePath,
        ImageFileWriter fileWriter,
        Action<Core.Capture.Capture> openEditor)
    {
        Capture = capture;
        Bitmap = capture.Image.ToBitmapSource();
        FilePath = filePath;
        _fileWriter = fileWriter;
        _openEditor = openEditor;
    }

    public event EventHandler? Dismissed;

    public Core.Capture.Capture Capture { get; }

    public BitmapSource Bitmap { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSaved))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? FilePath { get; private set; }

    [ObservableProperty]
    public partial string? Confirmation { get; private set; }

    public bool IsSaved => FilePath is not null;

    public string EnsureFile()
    {
        if (FilePath is null)
        {
            FilePath = _fileWriter.Save(Bitmap, Capture.CapturedAt);
        }

        return FilePath;
    }

    [RelayCommand]
    private Task CopyAsync()
    {
        ClipboardImageService.SetImage(Bitmap);
        return ConfirmAndDismissAsync("Copied");
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        EnsureFile();
        return ConfirmAndDismissAsync("Saved");
    }

    private bool CanSave() => !IsSaved;

    [RelayCommand]
    private void RevealInExplorer()
    {
        var path = EnsureFile();
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{Path.GetFullPath(path)}\"");
    }

    [RelayCommand]
    private void Edit()
    {
        _openEditor(Capture);
        Dismiss();
    }

    [RelayCommand]
    private void Dismiss() => Dismissed?.Invoke(this, EventArgs.Empty);

    private async Task ConfirmAndDismissAsync(string message)
    {
        Confirmation = message;
        await Task.Delay(ConfirmationDuration);
        Dismiss();
    }
}
