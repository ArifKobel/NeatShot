using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace NeatShot.Platform.Interop;

public sealed class MessageWindow : IDisposable
{
    private const string ClassName = "NeatShot.MessageWindow";

    private readonly WNDPROC _windowProcedure;
    private readonly List<Func<uint, WPARAM, LPARAM, bool>> _handlers = [];
    private bool _disposed;

    public unsafe MessageWindow()
    {
        _windowProcedure = WindowProcedure;

        var module = PInvoke.GetModuleHandle(default(PCWSTR));
        fixed (char* className = ClassName)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = _windowProcedure,
                hInstance = new HINSTANCE(module.Value),
                lpszClassName = className,
            };

            if (PInvoke.RegisterClassEx(windowClass) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        Handle = PInvoke.CreateWindowEx(
            0,
            ClassName,
            ClassName,
            0,
            0,
            0,
            0,
            0,
            HWND.HWND_MESSAGE,
            null,
            null,
            null);

        if (Handle.IsNull)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    internal HWND Handle { get; }

    internal void AddHandler(Func<uint, WPARAM, LPARAM, bool> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers.Add(handler);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ = PInvoke.DestroyWindow(Handle);
    }

    private LRESULT WindowProcedure(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        foreach (var handler in _handlers)
        {
            if (handler(message, wParam, lParam))
            {
                return new LRESULT(0);
            }
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
