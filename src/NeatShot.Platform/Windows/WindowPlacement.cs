using NeatShot.Core.Capture;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeatShot.Platform.Windows;

public static class WindowPlacement
{
    public static unsafe void DisableTransitions(nint handle)
    {
        var disabled = 1;
        _ = PInvoke.DwmSetWindowAttribute(new HWND(handle), DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, &disabled, sizeof(int));
    }

    public static unsafe void SetCloaked(nint handle, bool cloaked)
    {
        var value = cloaked ? 1 : 0;
        _ = PInvoke.DwmSetWindowAttribute(new HWND(handle), DWMWINDOWATTRIBUTE.DWMWA_CLOAK, &value, sizeof(int));
    }

    public static unsafe void UseDarkTitleBar(nint handle, uint captionColor)
    {
        var enabled = 1;
        _ = PInvoke.DwmSetWindowAttribute(new HWND(handle), DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &enabled, sizeof(int));
        var color = new COLORREF(captionColor);
        _ = PInvoke.DwmSetWindowAttribute(new HWND(handle), DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, &color, (uint)sizeof(COLORREF));
    }

    public static void HideFromSwitcher(nint handle)
    {
        var window = new HWND(handle);
        var style = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        _ = PInvoke.SetWindowLong(window, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)(style | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW));
    }

    public static void MoveToPixels(nint handle, PixelRect bounds, bool topmost)
    {
        var insertAfter = topmost ? HWND.HWND_TOPMOST : HWND.Null;
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
            | (topmost ? 0 : SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

        _ = PInvoke.SetWindowPos(new HWND(handle), insertAfter, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags);
    }
}
