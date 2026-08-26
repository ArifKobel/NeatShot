using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NeatShot.Core.Annotations;
using NeatShot.Core.Settings;
using NeatShot.Export;
using NeatShot.Imaging;

namespace NeatShot.Editor;

public sealed class EditorService
{
    private readonly ImageFileWriter _fileWriter;
    private readonly SettingsManager _settings;

    public EditorService(ImageFileWriter fileWriter, SettingsManager settings)
    {
        _fileWriter = fileWriter;
        _settings = settings;
    }

    public void Open(string imagePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
        bitmap.EndInit();

        var image = bitmap.ToCapturedImage();
        Open(new Core.Capture.Capture(image, new Core.Capture.PixelRect(0, 0, image.Width, image.Height), Core.Capture.CaptureMode.Fullscreen, File.GetLastWriteTime(imagePath)));
    }

    public void Open(Core.Capture.Capture capture)
    {
        ArgumentNullException.ThrowIfNull(capture);

        var document = new AnnotationDocument(capture.Image);
        var viewModel = new EditorViewModel(
            document,
            bitmap => _fileWriter.Save(bitmap, capture.CapturedAt),
            SaveAs);

        var window = new EditorWindow(viewModel);
        window.Show();
        window.Activate();
    }

    private string? SaveAs(BitmapSource bitmap)
    {
        var settings = _settings.Current;
        var dialog = new SaveFileDialog
        {
            Title = "Save capture",
            InitialDirectory = settings.SaveDirectory,
            FileName = Path.ChangeExtension(Core.Export.FileNameFormatter.Format(settings.FileNamePattern, DateTimeOffset.Now), null),
            DefaultExt = ImageFileWriter.Extension(settings.ImageFormat),
            Filter = "PNG image (*.png)|*.png|JPEG image (*.jpg)|*.jpg",
            FilterIndex = settings.ImageFormat == ImageFormat.Jpeg ? 2 : 1,
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var format = Path.GetExtension(dialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            ? ImageFormat.Jpeg
            : ImageFormat.Png;
        ImageFileWriter.Write(bitmap, dialog.FileName, format);
        return dialog.FileName;
    }
}
