using NeatShot.Core.Capture;
using Windows.Win32;

namespace NeatShot.Platform.Input;

public sealed class CursorLocator : ICursorLocator
{
    public PixelPoint GetPosition() =>
        PInvoke.GetCursorPos(out var point) ? new PixelPoint(point.X, point.Y) : PixelPoint.Zero;
}
