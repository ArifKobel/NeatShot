namespace NeatShot.Core.Capture;

public readonly record struct PixelPoint(int X, int Y)
{
    public static PixelPoint Zero => default;

    public PixelPoint Offset(int dx, int dy) => new(X + dx, Y + dy);
}
