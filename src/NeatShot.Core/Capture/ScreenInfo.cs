namespace NeatShot.Core.Capture;

public sealed record ScreenInfo(
    string DeviceName,
    PixelRect Bounds,
    PixelRect WorkArea,
    double ScaleFactor,
    bool IsPrimary);
