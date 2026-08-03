namespace NeatShot.Core.Capture;

public sealed class CapturedImage
{
    public const int BytesPerPixel = 4;

    public CapturedImage(int width, int height, byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(pixels);

        if (pixels.Length != width * height * BytesPerPixel)
        {
            throw new ArgumentException("Pixel buffer does not match the given dimensions.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride => Width * BytesPerPixel;

    public PixelSize Size => new(Width, Height);

    public byte[] Pixels { get; }

    public CapturedImage Crop(PixelRect region)
    {
        var clipped = region.Intersect(new PixelRect(0, 0, Width, Height));
        if (clipped.IsEmpty)
        {
            throw new ArgumentException("Region does not overlap the image.", nameof(region));
        }

        var result = new byte[clipped.Width * clipped.Height * BytesPerPixel];
        var rowLength = clipped.Width * BytesPerPixel;

        for (var row = 0; row < clipped.Height; row++)
        {
            var sourceOffset = (clipped.Y + row) * Stride + clipped.X * BytesPerPixel;
            Buffer.BlockCopy(Pixels, sourceOffset, result, row * rowLength, rowLength);
        }

        return new CapturedImage(clipped.Width, clipped.Height, result);
    }
}
