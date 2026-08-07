using System.Windows;
using System.Windows.Media.Imaging;

namespace NeatShot.Export;

public static class ClipboardImageService
{
    private const int RetryCount = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    public static void SetImage(BitmapSource image)
    {
        ArgumentNullException.ThrowIfNull(image);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return;
            }
            catch (System.Runtime.InteropServices.COMException) when (attempt < RetryCount)
            {
                Thread.Sleep(RetryDelay);
            }
        }
    }
}
