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
    private readonly CaptureCache _cache;
    private readonly Action<Core.Capture.Capture> _openEditor;
    private string? _cachedPath;

    public QuickAccessViewModel(
        Core.Capture.Capture capture,
        ImageFileWriter fileWriter,
        CaptureCache cache,
        Action<Core.Capture.Capture> openEditor)
    {
        Capture = capture;
        Bitmap = capture.Image.ToBitmapSource();
        _fileWriter = fileWriter;
        _cache = cache;
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

    public string FileForDrag() => FilePath ?? (_cachedPath ??= _cache.Store(Bitmap, Capture.CapturedAt));

    private string EnsureSaved() => FilePath ??= _fileWriter.Save(Bitmap, Capture.CapturedAt);

    [RelayCommand]
    private Task CopyAsync()
    {
        ClipboardImageService.SetImage(Bitmap);
        return ConfirmAndDismissAsync("Copied");
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync()
    {
        EnsureSaved();
        return ConfirmAndDismissAsync("Saved");
    }

    private bool CanSave() => !IsSaved;

    [RelayCommand]
    private void RevealInExplorer()
    {
        var path = EnsureSaved();
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
