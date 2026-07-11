using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ColorPicker.Models;

namespace ColorPicker.Services;

public static class ScreenCaptureService
{
    private const int SRCCOPY = 0x00CC0020;
    private static WriteableBitmap? _reusableBitmap;
    private static int _fullscreenImageLeft;
    private static int _fullscreenImageTop;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (BitmapSource Bitmap, byte R, byte G, byte B) GetImageWithCenterColor(int x, int y, int width, int height)
    {
        // GPU accelerated screen capture using Desktop Duplication API
        if (ScreenCaptureGPUService.GPU_GetImageWithCenterColor(x, y, width, height, out BitmapSource duplicated, out byte dr, out byte dg, out byte db))
            return (duplicated, dr, dg, db);

        Console.WriteLine("Fallback!!! "); // todo

        // Fallback to GDI-based screen capture if GPU capture fails
        return GDI_GetImageWithCenterColor(x, y, width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitmapSource GetFullScreenImage(POINT targetPoint)
    {
        if (TryGetMonitorBounds(targetPoint, out int left, out int top, out int width, out int height))
        {
            _fullscreenImageLeft = left;
            _fullscreenImageTop = top;
            int centerX = left + (width / 2);
            int centerY = top + (height / 2);

            return GetImageWithCenterColor(centerX, centerY, width, height).Bitmap;
        }

        // Fallback to primary monitor if monitor lookup fails
        _fullscreenImageLeft = 0;
        _fullscreenImageTop = 0;
        int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

        return GetImageWithCenterColor(screenWidth / 2, screenHeight / 2, screenWidth, screenHeight).Bitmap;
    }

    public static (BitmapSource croppedImage, byte R, byte G, byte B) GetPausedImageWithCenterColor(
        BitmapSource fullscreenCapture,
        POINT point,
        int width,
        int height)
    {
        int sourceX = point.X - _fullscreenImageLeft - (width / 2);
        int sourceY = point.Y - _fullscreenImageTop - (height / 2);

        var output = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

        int destX = Math.Max(0, -sourceX);
        int destY = Math.Max(0, -sourceY);
        int copySourceX = Math.Max(0, sourceX);
        int copySourceY = Math.Max(0, sourceY);

        int copyWidth = Math.Min(fullscreenCapture.PixelWidth - copySourceX, width - destX);
        int copyHeight = Math.Min(fullscreenCapture.PixelHeight - copySourceY, height - destY);

        if (copyWidth > 0 && copyHeight > 0)
        {
            int stride = copyWidth * 4;
            var pixels = new byte[stride * copyHeight];

            fullscreenCapture.CopyPixels(
                new Int32Rect(copySourceX, copySourceY, copyWidth, copyHeight),
                pixels,
                stride,
                0);

            output.WritePixels(new Int32Rect(destX, destY, copyWidth, copyHeight), pixels, stride, 0);
        }

        var centerPixel = new byte[4];
        int centerSourceX = point.X - _fullscreenImageLeft;
        int centerSourceY = point.Y - _fullscreenImageTop;

        if (centerSourceX >= 0 && centerSourceX < fullscreenCapture.PixelWidth &&
            centerSourceY >= 0 && centerSourceY < fullscreenCapture.PixelHeight)
        {
            fullscreenCapture.CopyPixels(new Int32Rect(centerSourceX, centerSourceY, 1, 1), centerPixel, 4, 0);
        }

        byte b = centerPixel[0];
        byte g = centerPixel[1];
        byte r = centerPixel[2];

        return (output, r, g, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (BitmapSource Bitmap, byte R, byte G, byte B) GDI_GetImageWithCenterColor(int x, int y, int width, int height)
    {
        // Reuse WriteableBitmap
        if (_reusableBitmap == null || _reusableBitmap.PixelWidth != width || _reusableBitmap.PixelHeight != height)
        {
            _reusableBitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        }

        IntPtr hdcScreen = Win32Api.GetDC(IntPtr.Zero);
        IntPtr hdcMem = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr hOld = IntPtr.Zero;

        try
        {
            hdcMem = Win32Api.CreateCompatibleDC(hdcScreen);
            hBitmap = Win32Api.CreateCompatibleBitmap(hdcScreen, width, height);
            hOld = Win32Api.SelectObject(hdcMem, hBitmap);

            int srcX = x - (width / 2);
            int srcY = y - (height / 2);
            Win32Api.BitBlt(hdcMem, 0, 0, width, height, hdcScreen, srcX, srcY, SRCCOPY);

            var bmpSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmpSource.Freeze();

            int centerX = width / 2;
            int centerY = height / 2;
            var cropped = new CroppedBitmap(bmpSource, new Int32Rect(centerX, centerY, 1, 1));
            var pixel = new byte[4];
            cropped.CopyPixels(pixel, 4, 0);

            byte centerB = pixel[0];
            byte centerG = pixel[1];
            byte centerR = pixel[2];

            int stride = width * 4;
            var pixels = new byte[stride * height];
            bmpSource.CopyPixels(pixels, stride, 0);

            _reusableBitmap.Lock();
            _reusableBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            _reusableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _reusableBitmap.Unlock();

            return (_reusableBitmap, centerR, centerG, centerB);
        }
        finally
        {
            if (hOld != IntPtr.Zero && hdcMem != IntPtr.Zero)
                _ = Win32Api.SelectObject(hdcMem, hOld);
            if (hBitmap != IntPtr.Zero)
                _ = Win32Api.DeleteObject(hBitmap);
            if (hdcMem != IntPtr.Zero)
                _ = Win32Api.DeleteDC(hdcMem);
            _ = Win32Api.ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    internal static bool TryGetMonitorBounds(POINT point, out int left, out int top, out int width, out int height)
    {
        left = 0;
        top = 0;
        width = 0;
        height = 0;

        IntPtr monitor = Win32Api.MonitorFromPoint(point, Win32Api.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
            return false;

        var monitorInfo = new Win32Api.MONITORINFO { cbSize = Marshal.SizeOf<Win32Api.MONITORINFO>() };
        if (!Win32Api.GetMonitorInfo(monitor, ref monitorInfo))
            return false;

        left = monitorInfo.rcMonitor.Left;
        top = monitorInfo.rcMonitor.Top;
        width = monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left;
        height = monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top;

        return width > 0 && height > 0;
    }
}
