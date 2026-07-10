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
    private static int _fullscreenZoomViewSourceLeft;
    private static int _fullscreenZoomViewSourceTop;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitmapSource GetFullScreenImage(int targetX, int targetY)
    {
        var targetPoint = new POINT { X = targetX, Y = targetY };

        if (Win32Api.TryGetMonitorBoundsFromPoint(targetPoint, out int left, out int top, out int width, out int height))
        {
            int centerX = left + (width / 2);
            int centerY = top + (height / 2);

            StopwatchService.Start(20);
            var image = GetImage(centerX, centerY, width, height);
            StopwatchService.Stop();

            return image;
        }

        // Fallback to primary monitor when monitor lookup fails. todo
        int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        int screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        return GetImage(screenWidth / 2, screenHeight / 2, screenWidth, screenHeight);
    }

    public static (BitmapSource croppedImage, byte R, byte G, byte B) GetCroppedImageWithCenterColor(
        BitmapSource fullscreenCapture,
        POINT point,
        int width,
        int height)
    {
        int maxX = Math.Max(0, fullscreenCapture.PixelWidth - width);
        int maxY = Math.Max(0, fullscreenCapture.PixelHeight - height);

        int sourceX = point.X - _fullscreenZoomViewSourceLeft - (width / 2);
        int sourceY = point.Y - _fullscreenZoomViewSourceTop - (height / 2);

        sourceX = Math.Clamp(sourceX, 0, maxX);
        sourceY = Math.Clamp(sourceY, 0, maxY);

        var crop = new CroppedBitmap(fullscreenCapture, new Int32Rect(sourceX, sourceY, width, height));

        var centerPixel = new byte[4];
        crop.CopyPixels(new Int32Rect(width / 2, height / 2, 1, 1), centerPixel, 4, 0);

        byte b = centerPixel[0];
        byte g = centerPixel[1];
        byte r = centerPixel[2];

        return (crop, r, g, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitmapSource GetImage(int x, int y, int width, int height)
    {
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
}
