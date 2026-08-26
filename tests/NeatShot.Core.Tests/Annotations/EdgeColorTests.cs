using NeatShot.Core.Annotations;
using NeatShot.Core.Capture;

namespace NeatShot.Core.Tests.Annotations;

public class EdgeColorTests
{
    [Fact]
    public void Dominant_ReturnsUniformBorderColor()
    {
        var image = Solid(40, 30, new Rgba(0x1E, 0x1E, 0x24));

        Assert.Equal(new Rgba(0x1E, 0x1E, 0x24), EdgeColor.Dominant(image));
    }

    [Fact]
    public void Dominant_IgnoresTheInterior()
    {
        var image = Solid(40, 30, new Rgba(0x20, 0x20, 0x20));
        Fill(image, 1, 1, 38, 28, new Rgba(0xFF, 0xFF, 0xFF));

        Assert.Equal(new Rgba(0x20, 0x20, 0x20), EdgeColor.Dominant(image));
    }

    [Fact]
    public void Dominant_PicksTheMostCommonEdgeColorOverOutliers()
    {
        var image = Solid(40, 30, new Rgba(0xF0, 0xF0, 0xF0));
        Fill(image, 0, 0, 6, 6, new Rgba(0xFF, 0x00, 0x00));

        Assert.Equal(new Rgba(0xF0, 0xF0, 0xF0), EdgeColor.Dominant(image));
    }

    [Fact]
    public void Dominant_AveragesSimilarShades()
    {
        var image = Solid(40, 30, new Rgba(0x30, 0x30, 0x30));
        Fill(image, 0, 0, 40, 1, new Rgba(0x34, 0x34, 0x34));

        var color = EdgeColor.Dominant(image);

        Assert.InRange(color.R, 0x30, 0x34);
        Assert.Equal(color.R, color.G);
        Assert.Equal(color.R, color.B);
    }

    private static CapturedImage Solid(int width, int height, Rgba color)
    {
        var image = new CapturedImage(width, height, new byte[width * height * CapturedImage.BytesPerPixel]);
        Fill(image, 0, 0, width, height, color);
        return image;
    }

    private static void Fill(CapturedImage image, int x, int y, int width, int height, Rgba color)
    {
        for (var row = y; row < y + height; row++)
        {
            for (var column = x; column < x + width; column++)
            {
                var offset = row * image.Stride + column * CapturedImage.BytesPerPixel;
                image.Pixels[offset] = color.B;
                image.Pixels[offset + 1] = color.G;
                image.Pixels[offset + 2] = color.R;
                image.Pixels[offset + 3] = color.A;
            }
        }
    }
}
