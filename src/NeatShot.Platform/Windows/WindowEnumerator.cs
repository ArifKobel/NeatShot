using NeatShot.Core.Capture;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeatShot.Platform.Windows;

public sealed class WindowEnumerator : IWindowEnumerator
{
    public IReadOnlyList<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        var shell = PInvoke.GetShellWindow();

        PInvoke.EnumWindows(
            (hwnd, _) =>
            {
                if (hwnd != shell && IsCandidate(hwnd) && TryGetBounds(hwnd, out var bounds))
                {
                    windows.Add(new WindowInfo(hwnd, GetTitle(hwnd), bounds));
                }

                return true;
            },
            default);

        return windows;
    }

    private static bool IsCandidate(HWND hwnd)
    {
        if (!PInvoke.IsWindowVisible(hwnd) || PInvoke.IsIconic(hwnd))
        {
            return false;
        }

        if (PInvoke.GetAncestor(hwnd, GET_ANCESTOR_FLAGS.GA_ROOTOWNER) != hwnd)
        {
            return false;
        }

        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        if (exStyle.HasFlag(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW))
        {
            return false;
        }

        return !IsCloaked(hwnd) && PInvoke.GetWindowTextLength(hwnd) > 0;
    }

    private static unsafe bool IsCloaked(HWND hwnd)
    {
        uint cloaked;
        var result = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(uint));
        return result.Succeeded && cloaked != 0;
    }

    private static unsafe bool TryGetBounds(HWND hwnd, out PixelRect bounds)
    {
        RECT rect;
        var result = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, &rect, (uint)sizeof(RECT));
        bounds = result.Succeeded
            ? PixelRect.FromEdges(rect.left, rect.top, rect.right, rect.bottom)
            : PixelRect.Empty;
        return !bounds.IsEmpty;
    }

    private static unsafe string GetTitle(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);
        Span<char> buffer = stackalloc char[length + 1];
        fixed (char* pointer = buffer)
        {
            var written = PInvoke.GetWindowText(hwnd, pointer, buffer.Length);
            return new string(buffer[..written]);
        }
    }
}
