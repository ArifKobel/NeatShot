namespace NeatShot.Core.Capture;

public interface IScreenCapture
{
    CapturedImage Capture(PixelRect bounds);
}
