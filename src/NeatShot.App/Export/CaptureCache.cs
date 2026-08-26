using System.IO;
using System.Windows.Media.Imaging;
using NeatShot.Core.Export;
using NeatShot.Core.Settings;

namespace NeatShot.Export;

public sealed class CaptureCache
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(1);

    private readonly string _directory;

    public CaptureCache(string directory)
    {
        _directory = directory;
    }

    public static CaptureCache InLocalAppData() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NeatShot",
        "Cache"));

    public string Store(BitmapSource image, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(image);

        Directory.CreateDirectory(_directory);
        Prune();

        var path = Path.Combine(_directory, FileNameFormatter.Format(AppSettings.DefaultFileNamePattern, timestamp) + ".png");
        ImageFileWriter.Write(image, path, ImageFormat.Png);
        return path;
    }

    private void Prune()
    {
        var cutoff = DateTime.UtcNow - Retention;
        foreach (var file in Directory.EnumerateFiles(_directory, "*.png"))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff)
            {
                File.Delete(file);
            }
        }
    }
}
