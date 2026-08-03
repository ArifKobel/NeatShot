namespace NeatShot.Core.Capture;

public sealed record WindowInfo(nint Handle, string Title, PixelRect Bounds);
