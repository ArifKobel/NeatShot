using NeatShot.Core.Capture;

namespace NeatShot.Core.Annotations;

public static class EdgeColor
{
    private const int BucketShift = 4;
    private const int MaxSamplesPerSide = 512;

    public static Rgba Dominant(CapturedImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var buckets = new Dictionary<int, Bucket>();
        foreach (var (x, y) in EdgePixels(image))
        {
            var offset = y * image.Stride + x * CapturedImage.BytesPerPixel;
            var b = image.Pixels[offset];
            var g = image.Pixels[offset + 1];
            var r = image.Pixels[offset + 2];
            var key = (r >> BucketShift) << 8 | (g >> BucketShift) << 4 | (b >> BucketShift);

            buckets[key] = buckets.TryGetValue(key, out var bucket)
                ? bucket.Add(r, g, b)
                : new Bucket(1, r, g, b);
        }

        var dominant = buckets.Values.MaxBy(bucket => bucket.Count);
        return new Rgba(
            (byte)(dominant.R / dominant.Count),
            (byte)(dominant.G / dominant.Count),
            (byte)(dominant.B / dominant.Count));
    }

    private static IEnumerable<(int X, int Y)> EdgePixels(CapturedImage image)
    {
        var stepX = Math.Max(1, image.Width / MaxSamplesPerSide);
        var stepY = Math.Max(1, image.Height / MaxSamplesPerSide);

        for (var x = 0; x < image.Width; x += stepX)
        {
            yield return (x, 0);
            yield return (x, image.Height - 1);
        }

        for (var y = stepY; y < image.Height - 1; y += stepY)
        {
            yield return (0, y);
            yield return (image.Width - 1, y);
        }
    }

    private readonly record struct Bucket(int Count, long R, long G, long B)
    {
        public Bucket Add(byte r, byte g, byte b) => new(Count + 1, R + r, G + g, B + b);
    }
}
