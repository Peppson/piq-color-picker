using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ColorPicker.Services;

public static class ScreenCaptureService
{
    private const int SRCCOPY = 0x00CC0020;
    private static WriteableBitmap? _reusableBitmap;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitmapSource GetCapturedImageFullScreen()
    {
        var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

        Console.WriteLine($"Screen width: {screenWidth}, height: {screenHeight}");

        return GetCapturedImage(screenWidth / 2, screenHeight / 2, screenWidth, screenHeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BitmapSource GetCapturedImage(int x, int y, int width, int height)
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
    public static (BitmapSource Bitmap, byte R, byte G, byte B) GetCapturedImageWithCenterColor(int x, int y, int width, int height)
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
