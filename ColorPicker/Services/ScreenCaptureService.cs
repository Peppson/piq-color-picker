using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
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
    public static BitmapSource GetFullScreenImage(int targetX, int targetY)
    {
        var targetPoint = new POINT { X = targetX, Y = targetY };

        if (TryGetMonitorBoundsFromPoint(targetPoint, out int left, out int top, out int width, out int height))
        {
            _fullscreenImageLeft = left;
            _fullscreenImageTop = top;
            int centerX = left + (width / 2);
            int centerY = top + (height / 2);

            return GetImage(centerX, centerY, width, height);
        }

        // Fallback to primary monitor if monitor lookup fails
        _fullscreenImageLeft = 0;
        _fullscreenImageTop = 0;
        int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

        return GetImage(screenWidth / 2, screenHeight / 2, screenWidth, screenHeight);
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
    public static BitmapSource GetImage(int x, int y, int width, int height)
    {
        if (ScreenCaptureGPUService.TryCaptureRegion(x, y, width, height, out BitmapSource duplicated))
        {
            Console.WriteLine("Captured using GPU duplication.");
            return duplicated;
        }

        Console.WriteLine("Captured using GDI BitBlt fallback.");

        // Reuse WriteableBitmap
        if (_reusableBitmap == null || _reusableBitmap.PixelWidth != width || _reusableBitmap.PixelHeight != height)
        {
            _reusableBitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        }

        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            IntPtr hdcDest = g.GetHdc();
            IntPtr hdcSrc = Win32Api.GetDC(IntPtr.Zero);

            try
            {
                int srcX = x - (width / 2);
                int srcY = y - (height / 2);
                Win32Api.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, srcX, srcY, SRCCOPY);
            }
            finally
            {
                _ = Win32Api.ReleaseDC(IntPtr.Zero, hdcSrc);
                g.ReleaseHdc(hdcDest);
            }
        }

        // Copy
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

        try
        {
            _reusableBitmap.Lock();
            _reusableBitmap.WritePixels(new Int32Rect(0, 0, width, height), bmpData.Scan0, bmpData.Stride * height, bmpData.Stride);
            _reusableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _reusableBitmap.Unlock();
        }
        finally
        {
            bmp.UnlockBits(bmpData);
        }

        return _reusableBitmap;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (BitmapSource Bitmap, byte R, byte G, byte B) GetImageWithCenterColor(int x, int y, int width, int height)
    {
        if (ScreenCaptureGPUService.TryCaptureRegionWithCenterColor(x, y, width, height, out BitmapSource duplicated, out byte dr, out byte dg, out byte db))
        {
            Console.WriteLine("Captured using GPU duplication.");
            return (duplicated, dr, dg, db);
        }

        Console.WriteLine("Captured using GDI BitBlt fallback.");

        // Reuse WriteableBitmap
        if (_reusableBitmap == null || _reusableBitmap.PixelWidth != width || _reusableBitmap.PixelHeight != height)
        {
            _reusableBitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        }

        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            IntPtr hdcDest = g.GetHdc();
            IntPtr hdcSrc = Win32Api.GetDC(IntPtr.Zero);

            try
            {
                int srcX = x - (width / 2);
                int srcY = y - (height / 2);
                Win32Api.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, srcX, srcY, SRCCOPY);
            }
            finally
            {
                _ = Win32Api.ReleaseDC(IntPtr.Zero, hdcSrc);
                g.ReleaseHdc(hdcDest);
            }
        }

        byte centerR = 0;
        byte centerG = 0;
        byte centerB = 0;

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

        try
        {
            int centerX = width / 2;
            int centerY = height / 2;
            int pixelOffset = (centerY * bmpData.Stride) + (centerX * 4);

            centerB = Marshal.ReadByte(bmpData.Scan0, pixelOffset + 0);
            centerG = Marshal.ReadByte(bmpData.Scan0, pixelOffset + 1);
            centerR = Marshal.ReadByte(bmpData.Scan0, pixelOffset + 2);

            _reusableBitmap.Lock();
            _reusableBitmap.WritePixels(new Int32Rect(0, 0, width, height), bmpData.Scan0, bmpData.Stride * height, bmpData.Stride);
            _reusableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _reusableBitmap.Unlock();
        }
        finally
        {
            bmp.UnlockBits(bmpData);
        }

        return (_reusableBitmap, centerR, centerG, centerB);
    }

    internal static bool TryGetMonitorBoundsFromPoint(POINT point, out int left, out int top, out int width, out int height)
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
