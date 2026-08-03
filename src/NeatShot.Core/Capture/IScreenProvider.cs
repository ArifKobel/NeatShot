namespace NeatShot.Core.Capture;

public interface IScreenProvider
{
    IReadOnlyList<ScreenInfo> GetScreens();

    PixelRect GetVirtualScreenBounds();
}
