using System.ComponentModel;
using System.Runtime.InteropServices;
using NeatShot.Core.Capture;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace NeatShot.Platform.Capture;

public sealed class GdiScreenCapture : IScreenCapture
{
    private const ushort BitsPerPixel = 32;

    public unsafe CapturedImage Capture(PixelRect bounds)
    {
        if (bounds.IsEmpty)
        {
            throw new ArgumentException("Capture bounds must not be empty.", nameof(bounds));
        }

        var screenDc = PInvoke.GetDC(HWND.Null);
        var memoryDc = PInvoke.CreateCompatibleDC(screenDc);
        try
        {
            return CopyFromScreen(screenDc, memoryDc, bounds);
        }
        finally
        {
            _ = PInvoke.DeleteDC(memoryDc);
            _ = PInvoke.ReleaseDC(HWND.Null, screenDc);
        }
    }

    private static unsafe CapturedImage CopyFromScreen(HDC screenDc, HDC memoryDc, PixelRect bounds)
    {
        var header = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = bounds.Width,
                biHeight = -bounds.Height,
                biPlanes = 1,
                biBitCount = BitsPerPixel,
                biCompression = (uint)BI_COMPRESSION.BI_RGB,
            },
        };

        using var bitmap = PInvoke.CreateDIBSection(memoryDc, &header, DIB_USAGE.DIB_RGB_COLORS, out var bits, null, 0);
        if (bitmap.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var previous = PInvoke.SelectObject(memoryDc, (HGDIOBJ)bitmap.DangerousGetHandle());
        try
        {
            var copied = PInvoke.BitBlt(
                memoryDc,
                0,
                0,
                bounds.Width,
                bounds.Height,
                screenDc,
                bounds.X,
                bounds.Y,
                ROP_CODE.SRCCOPY | ROP_CODE.CAPTUREBLT);

            if (!copied)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var pixels = new byte[bounds.Width * bounds.Height * CapturedImage.BytesPerPixel];
            Marshal.Copy((nint)bits, pixels, 0, pixels.Length);
            return new CapturedImage(bounds.Width, bounds.Height, pixels);
        }
        finally
        {
            _ = PInvoke.SelectObject(memoryDc, previous);
        }
    }
}
