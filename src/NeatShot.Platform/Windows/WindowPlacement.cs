using NeatShot.Core.Capture;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeatShot.Platform.Windows;

public static class WindowPlacement
{
    public static void MoveToPixels(nint handle, PixelRect bounds, bool topmost)
    {
        var insertAfter = topmost ? HWND.HWND_TOPMOST : HWND.Null;
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
            | (topmost ? 0 : SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

        _ = PInvoke.SetWindowPos(new HWND(handle), insertAfter, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags);
    }
}
