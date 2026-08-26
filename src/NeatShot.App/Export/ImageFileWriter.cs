using System.IO;
using System.Windows.Media.Imaging;
using NeatShot.Core.Export;
using NeatShot.Core.Settings;

namespace NeatShot.Export;

public sealed class ImageFileWriter
{
    private const int JpegQuality = 92;

    private readonly SettingsManager _settings;

    public ImageFileWriter(SettingsManager settings)
    {
        _settings = settings;
    }

    public string Save(BitmapSource image, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(image);

        var settings = _settings.Current;
        var path = UniquePath(settings.SaveDirectory, FileNameFormatter.Format(settings.FileNamePattern, timestamp), Extension(settings.ImageFormat));
        Directory.CreateDirectory(settings.SaveDirectory);
        Write(image, path, settings.ImageFormat);
        return path;
    }

    public static void Write(BitmapSource image, string path, ImageFormat format)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var encoder = CreateEncoder(format);
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    public static string Extension(ImageFormat format) => format switch
    {
        ImageFormat.Jpeg => ".jpg",
        _ => ".png",
    };

    private static BitmapEncoder CreateEncoder(ImageFormat format) => format switch
    {
        ImageFormat.Jpeg => new JpegBitmapEncoder { QualityLevel = JpegQuality },
        _ => new PngBitmapEncoder(),
    };

    private static string UniquePath(string directory, string name, string extension)
    {
        var path = Path.Combine(directory, name + extension);
        for (var attempt = 2; File.Exists(path); attempt++)
        {
            path = Path.Combine(directory, $"{name} ({attempt}){extension}");
        }

        return path;
    }
}
