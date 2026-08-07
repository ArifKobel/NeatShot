using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeatShot.Core.Capture;

namespace NeatShot.Imaging;

public static class BitmapConversion
{
    private const double ScreenDpi = 96.0;

    public static BitmapSource ToBitmapSource(this CapturedImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var bitmap = BitmapSource.Create(
            image.Width,
            image.Height,
            ScreenDpi,
            ScreenDpi,
            PixelFormats.Bgra32,
            null,
            image.Pixels,
            image.Stride);
        bitmap.Freeze();
        return bitmap;
    }

    public static CapturedImage ToCapturedImage(this BitmapSource bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var source = bitmap.Format == PixelFormats.Bgra32
            ? bitmap
            : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);

        var stride = source.PixelWidth * CapturedImage.BytesPerPixel;
        var pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);
        return new CapturedImage(source.PixelWidth, source.PixelHeight, pixels);
    }
}
