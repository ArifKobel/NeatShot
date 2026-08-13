using NeatShot.Core.Capture;

namespace NeatShot.Core.Annotations;

public static class ImageFilters
{
    private const int Channels = CapturedImage.BytesPerPixel;

    public static CapturedImage Pixelate(CapturedImage source, int blockSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(blockSize, 1);

        var result = new byte[source.Pixels.Length];
        var sums = new long[Channels];

        for (var blockY = 0; blockY < source.Height; blockY += blockSize)
        {
            var blockHeight = Math.Min(blockSize, source.Height - blockY);
            for (var blockX = 0; blockX < source.Width; blockX += blockSize)
            {
                var blockWidth = Math.Min(blockSize, source.Width - blockX);
                Array.Clear(sums);

                for (var y = blockY; y < blockY + blockHeight; y++)
                {
                    var offset = y * source.Stride + blockX * Channels;
                    for (var x = 0; x < blockWidth; x++, offset += Channels)
                    {
                        for (var c = 0; c < Channels; c++)
                        {
                            sums[c] += source.Pixels[offset + c];
                        }
                    }
                }

                var count = blockWidth * blockHeight;
                for (var y = blockY; y < blockY + blockHeight; y++)
                {
                    var offset = y * source.Stride + blockX * Channels;
                    for (var x = 0; x < blockWidth; x++, offset += Channels)
                    {
                        for (var c = 0; c < Channels; c++)
                        {
                            result[offset + c] = (byte)(sums[c] / count);
                        }
                    }
                }
            }
        }

        return new CapturedImage(source.Width, source.Height, result);
    }

    public static CapturedImage BoxBlur(CapturedImage source, int radius, int passes = 3)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(radius, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(passes, 1);

        var current = (byte[])source.Pixels.Clone();
        var scratch = new byte[current.Length];

        for (var pass = 0; pass < passes; pass++)
        {
            BlurHorizontal(current, scratch, source.Width, source.Height, radius);
            BlurVertical(scratch, current, source.Width, source.Height, radius);
        }

        return new CapturedImage(source.Width, source.Height, current);
    }

    private static void BlurHorizontal(byte[] input, byte[] output, int width, int height, int radius)
    {
        var stride = width * Channels;
        var sums = new int[Channels];

        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            Array.Clear(sums);
            var count = 0;

            for (var x = 0; x <= Math.Min(radius, width - 1); x++)
            {
                Accumulate(input, row + x * Channels, sums, 1);
                count++;
            }

            for (var x = 0; x < width; x++)
            {
                var target = row + x * Channels;
                for (var c = 0; c < Channels; c++)
                {
                    output[target + c] = (byte)(sums[c] / count);
                }

                var leaving = x - radius;
                if (leaving >= 0)
                {
                    Accumulate(input, row + leaving * Channels, sums, -1);
                    count--;
                }

                var entering = x + radius + 1;
                if (entering < width)
                {
                    Accumulate(input, row + entering * Channels, sums, 1);
                    count++;
                }
            }
        }
    }

    private static void BlurVertical(byte[] input, byte[] output, int width, int height, int radius)
    {
        var stride = width * Channels;
        var sums = new int[Channels];

        for (var x = 0; x < width; x++)
        {
            var column = x * Channels;
            Array.Clear(sums);
            var count = 0;

            for (var y = 0; y <= Math.Min(radius, height - 1); y++)
            {
                Accumulate(input, column + y * stride, sums, 1);
                count++;
            }

            for (var y = 0; y < height; y++)
            {
                var target = column + y * stride;
                for (var c = 0; c < Channels; c++)
                {
                    output[target + c] = (byte)(sums[c] / count);
                }

                var leaving = y - radius;
                if (leaving >= 0)
                {
                    Accumulate(input, column + leaving * stride, sums, -1);
                    count--;
                }

                var entering = y + radius + 1;
                if (entering < height)
                {
                    Accumulate(input, column + entering * stride, sums, 1);
                    count++;
                }
            }
        }
    }

    private static void Accumulate(byte[] pixels, int offset, int[] sums, int sign)
    {
        for (var c = 0; c < Channels; c++)
        {
            sums[c] += sign * pixels[offset + c];
        }
    }
}
