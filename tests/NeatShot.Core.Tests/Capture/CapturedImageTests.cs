using NeatShot.Core.Capture;

namespace NeatShot.Core.Tests.Capture;

public class CapturedImageTests
{
    [Fact]
    public void Constructor_RejectsMismatchedBuffer()
    {
        Assert.Throws<ArgumentException>(() => new CapturedImage(2, 2, new byte[3]));
    }

    [Fact]
    public void Crop_CopiesOnlyRequestedPixels()
    {
        var image = CreateGradient(4, 4);

        var cropped = image.Crop(new PixelRect(1, 1, 2, 2));

        Assert.Equal(new PixelSize(2, 2), cropped.Size);
        Assert.Equal(PixelAt(image, 1, 1), PixelAt(cropped, 0, 0));
        Assert.Equal(PixelAt(image, 2, 2), PixelAt(cropped, 1, 1));
    }

    [Fact]
    public void Crop_ClipsRegionToImageBounds()
    {
        var image = CreateGradient(4, 4);

        var cropped = image.Crop(new PixelRect(2, 2, 10, 10));

        Assert.Equal(new PixelSize(2, 2), cropped.Size);
    }

    [Fact]
    public void Crop_ThrowsWhenRegionIsOutside()
    {
        var image = CreateGradient(4, 4);

        Assert.Throws<ArgumentException>(() => image.Crop(new PixelRect(10, 10, 2, 2)));
    }

    private static CapturedImage CreateGradient(int width, int height)
    {
        var pixels = new byte[width * height * CapturedImage.BytesPerPixel];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)i;
        }

        return new CapturedImage(width, height, pixels);
    }

    private static byte[] PixelAt(CapturedImage image, int x, int y)
    {
        var offset = y * image.Stride + x * CapturedImage.BytesPerPixel;
        return image.Pixels[offset..(offset + CapturedImage.BytesPerPixel)];
    }
}
