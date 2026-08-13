using NeatShot.Core.Annotations;
using NeatShot.Core.Capture;

namespace NeatShot.Core.Tests.Annotations;

public class ImageFiltersTests
{
    [Fact]
    public void Pixelate_AveragesEachBlock()
    {
        var image = Checkerboard(4, 4);

        var result = ImageFilters.Pixelate(image, 2);

        Assert.All(result.Pixels, value => Assert.Equal(127, value));
    }

    [Fact]
    public void Pixelate_WithBlockLargerThanImageProducesFlatColor()
    {
        var image = Checkerboard(3, 3);

        var result = ImageFilters.Pixelate(image, 10);

        Assert.Single(result.Pixels.Distinct());
    }

    [Fact]
    public void BoxBlur_KeepsUniformImageUnchanged()
    {
        var pixels = Enumerable.Repeat((byte)200, 5 * 5 * CapturedImage.BytesPerPixel).ToArray();
        var image = new CapturedImage(5, 5, pixels);

        var result = ImageFilters.BoxBlur(image, 2);

        Assert.Equal(pixels, result.Pixels);
    }

    [Fact]
    public void BoxBlur_SoftensSharpEdge()
    {
        var pixels = new byte[8 * 1 * CapturedImage.BytesPerPixel];
        for (var x = 4; x < 8; x++)
        {
            Array.Fill(pixels, (byte)255, x * CapturedImage.BytesPerPixel, CapturedImage.BytesPerPixel);
        }

        var result = ImageFilters.BoxBlur(new CapturedImage(8, 1, pixels), 1, passes: 1);

        Assert.Equal(0, result.Pixels[0]);
        Assert.InRange(result.Pixels[3 * CapturedImage.BytesPerPixel], 1, 254);
        Assert.InRange(result.Pixels[4 * CapturedImage.BytesPerPixel], 1, 254);
        Assert.Equal(255, result.Pixels[7 * CapturedImage.BytesPerPixel]);
    }

    private static CapturedImage Checkerboard(int width, int height)
    {
        var pixels = new byte[width * height * CapturedImage.BytesPerPixel];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value = (byte)((x + y) % 2 == 0 ? 255 : 0);
                Array.Fill(pixels, value, (y * width + x) * CapturedImage.BytesPerPixel, CapturedImage.BytesPerPixel);
            }
        }

        return new CapturedImage(width, height, pixels);
    }
}
