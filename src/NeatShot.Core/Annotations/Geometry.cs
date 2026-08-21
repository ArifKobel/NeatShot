namespace NeatShot.Core.Annotations;

public readonly record struct ImagePoint(double X, double Y)
{
    public ImagePoint Translate(double dx, double dy) => new(X + dx, Y + dy);

    public double DistanceTo(ImagePoint other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public double DistanceToSegment(ImagePoint a, ImagePoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0)
        {
            return DistanceTo(a);
        }

        var t = Math.Clamp(((X - a.X) * dx + (Y - a.Y) * dy) / lengthSquared, 0, 1);
        return DistanceTo(new ImagePoint(a.X + t * dx, a.Y + t * dy));
    }
}

public readonly record struct ImageRect(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public ImagePoint Center => new(X + Width / 2, Y + Height / 2);
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static ImageRect FromPoints(ImagePoint a, ImagePoint b) => new(
        Math.Min(a.X, b.X),
        Math.Min(a.Y, b.Y),
        Math.Abs(a.X - b.X),
        Math.Abs(a.Y - b.Y));

    public static ImageRect FromCenter(ImagePoint center, double radius) =>
        new(center.X - radius, center.Y - radius, radius * 2, radius * 2);

    public bool Contains(ImagePoint point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    public bool IntersectsWith(ImageRect other) =>
        other.Left <= Right && Left <= other.Right && other.Top <= Bottom && Top <= other.Bottom;

    public ImageRect Translate(double dx, double dy) => this with { X = X + dx, Y = Y + dy };

    public ImageRect Inflate(double amount) =>
        new(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);

    public ImageRect Union(ImageRect other) => FromPoints(
        new ImagePoint(Math.Min(Left, other.Left), Math.Min(Top, other.Top)),
        new ImagePoint(Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom)));
}

public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255)
{
    public static Rgba Red => new(0xFF, 0x3B, 0x30);
    public static Rgba Orange => new(0xFF, 0x9F, 0x0A);
    public static Rgba Yellow => new(0xFF, 0xD6, 0x0A);
    public static Rgba Green => new(0x30, 0xD1, 0x58);
    public static Rgba Blue => new(0x0A, 0x84, 0xFF);
    public static Rgba Purple => new(0xBF, 0x5A, 0xF2);
    public static Rgba White => new(0xFF, 0xFF, 0xFF);
    public static Rgba Black => new(0, 0, 0);
}
