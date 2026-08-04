using NeatShot.Core.Capture;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeatShot.Platform.Screens;

public sealed class ScreenProvider : IScreenProvider
{
    private const uint PrimaryMonitorFlag = 0x1;
    private const double BaselineDpi = 96.0;

    public unsafe IReadOnlyList<ScreenInfo> GetScreens()
    {
        var screens = new List<ScreenInfo>();

        PInvoke.EnumDisplayMonitors(
            HDC.Null,
            null,
            (monitor, _, _, _) =>
            {
                if (TryDescribe(monitor, out var screen))
                {
                    screens.Add(screen);
                }

                return true;
            },
            default);

        return screens;
    }

    public PixelRect GetVirtualScreenBounds() => new(
        PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_XVIRTUALSCREEN),
        PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_YVIRTUALSCREEN),
        PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN),
        PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN));

    private static unsafe bool TryDescribe(HMONITOR monitor, out ScreenInfo screen)
    {
        var info = new MONITORINFOEXW();
        info.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);
        if (!PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info))
        {
            screen = null!;
            return false;
        }

        PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _);
        screen = new ScreenInfo(
            info.szDevice.ToString(),
            ToPixelRect(info.monitorInfo.rcMonitor),
            ToPixelRect(info.monitorInfo.rcWork),
            dpiX / BaselineDpi,
            (info.monitorInfo.dwFlags & PrimaryMonitorFlag) != 0);
        return true;
    }

    private static PixelRect ToPixelRect(RECT rect) =>
        PixelRect.FromEdges(rect.left, rect.top, rect.right, rect.bottom);
}
