using System.ComponentModel;
using System.Runtime.InteropServices;
using NeatShot.Platform.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeatShot.Platform.Tray;

public sealed class TrayIcon : IDisposable
{
    private const uint IconId = 1;
    private const uint CallbackMessage = PInvoke.WM_USER + 1;

    private readonly MessageWindow _window;
    private readonly HICON _icon;
    private bool _disposed;

    public TrayIcon(MessageWindow window, string iconPath, string tooltip)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);

        _window = window;
        _icon = LoadIcon(iconPath);
        _window.AddHandler(HandleMessage);

        var data = CreateData();
        data.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = _icon;
        data.szTip = tooltip;

        if (!PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, data))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public event EventHandler? LeftClick;

    public event EventHandler? RightClick;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, CreateData());
        _ = PInvoke.DestroyIcon(_icon);
    }

    private static unsafe HICON LoadIcon(string path)
    {
        HANDLE icon;
        fixed (char* pathPointer = path)
        {
            icon = PInvoke.LoadImage(
                HINSTANCE.Null,
                new PCWSTR(pathPointer),
                GDI_IMAGE_TYPE.IMAGE_ICON,
                0,
                0,
                IMAGE_FLAGS.LR_LOADFROMFILE | IMAGE_FLAGS.LR_DEFAULTSIZE);
        }

        if (icon.IsNull)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new HICON(icon.Value);
    }

    private unsafe NOTIFYICONDATAW CreateData() => new()
    {
        cbSize = (uint)sizeof(NOTIFYICONDATAW),
        hWnd = _window.Handle,
        uID = IconId,
    };

    private bool HandleMessage(uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message != CallbackMessage)
        {
            return false;
        }

        switch ((uint)lParam.Value)
        {
            case PInvoke.WM_LBUTTONUP:
                LeftClick?.Invoke(this, EventArgs.Empty);
                return true;
            case PInvoke.WM_RBUTTONUP:
            case PInvoke.WM_CONTEXTMENU:
                _ = PInvoke.SetForegroundWindow(_window.Handle);
                RightClick?.Invoke(this, EventArgs.Empty);
                return true;
            default:
                return false;
        }
    }
}
