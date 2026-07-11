using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace ColorPicker.Services;

public static class ScreenCaptureGPUService
{
    private static readonly Lock SyncRoot = new();

    private static ID3D11Device? _device;
    private static ID3D11DeviceContext? _context;
    private static IDXGIOutputDuplication? _duplication;
    private static ID3D11Texture2D? _stagingTexture;
    private static WriteableBitmap? _reusableBitmap;

    private static int _activeOutputLeft;
    private static int _activeOutputTop;
    private static int _activeOutputRight;
    private static int _activeOutputBottom;
    private static int _stagingWidth;
    private static int _stagingHeight;
    private static byte[]? _reusablePixels;
    private static int _reusableStride;
    private static bool _hasCachedFrame;
    private static byte _cachedR;
    private static byte _cachedG;
    private static byte _cachedB;

    public static bool GPU_GetImageWithCenterColor(int centerX, int centerY, int width, int height, out BitmapSource bitmap, out byte r, out byte g, out byte b)
    {
        lock (SyncRoot)
        {
            bitmap = null!;
            r = g = b = 0;

            if (!EnsureDevice())
                return false;

            if (!EnsureDuplicationForPoint(centerX, centerY))
                return false;

            if (!EnsureStagingTexture(width, height))
                return false;

            bool frameAcquired = false;
            try
            {
                var acquireResult = _duplication!.AcquireNextFrame(0, out _, out IDXGIResource desktopResource);
                if (acquireResult == Vortice.DXGI.ResultCode.WaitTimeout)
                {
                    if (_hasCachedFrame &&
                        _reusableBitmap != null &&
                        _reusableBitmap.PixelWidth == width &&
                        _reusableBitmap.PixelHeight == height)
                    {
                        bitmap = _reusableBitmap;
                        r = _cachedR;
                        g = _cachedG;
                        b = _cachedB;
                        return true;
                    }

                    return false;
                }
                if (acquireResult.Failure)
                {
                    RecreateDuplication();
                    return false;
                }

                frameAcquired = true;

                int outputWidth = _activeOutputRight - _activeOutputLeft;
                int outputHeight = _activeOutputBottom - _activeOutputTop;
                int requestedLeft = centerX - (width / 2);
                int requestedTop = centerY - (height / 2);
                int requestedLeftRel = requestedLeft - _activeOutputLeft;
                int requestedTopRel = requestedTop - _activeOutputTop;
                int maxSrcLeft = Math.Max(0, outputWidth - width);
                int maxSrcTop = Math.Max(0, outputHeight - height);
                int srcLeft = Math.Clamp(requestedLeftRel, 0, maxSrcLeft);
                int srcTop = Math.Clamp(requestedTopRel, 0, maxSrcTop);

                using (desktopResource)
                using (var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>())
                {
                    var srcBox = new Box(srcLeft, srcTop, 0, srcLeft + width, srcTop + height, 1);
                    _context!.CopySubresourceRegion(_stagingTexture!, 0, 0, 0, 0, desktopTexture, 0, srcBox);
                }

                var mapped = _context!.Map(_stagingTexture!, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                try
                {
                    EnsureWriteableBitmap(width, height);
                    EnsureReusablePixelBuffer(width, height);

                    int rowPitch = (int)mapped.RowPitch;
                    Array.Clear(_reusablePixels!, 0, _reusablePixels!.Length);

                    int srcX = Math.Max(0, requestedLeftRel - srcLeft);
                    int srcY = Math.Max(0, requestedTopRel - srcTop);
                    int destX = Math.Max(0, srcLeft - requestedLeftRel);
                    int destY = Math.Max(0, srcTop - requestedTopRel);
                    int copyWidth = Math.Min(width - srcX, width - destX);
                    int copyHeight = Math.Min(height - srcY, height - destY);

                    if (copyWidth > 0 && copyHeight > 0)
                    {
                        int copyBytesPerRow = copyWidth * 4;
                        for (int row = 0; row < copyHeight; row++)
                        {
                            IntPtr sourceRow = IntPtr.Add(mapped.DataPointer, ((srcY + row) * rowPitch) + (srcX * 4));
                            int destOffset = ((destY + row) * _reusableStride) + (destX * 4);
                            System.Runtime.InteropServices.Marshal.Copy(sourceRow, _reusablePixels, destOffset, copyBytesPerRow);
                        }
                    }

                    _reusableBitmap!.WritePixels(new Int32Rect(0, 0, width, height), _reusablePixels, _reusableStride, 0);

                    int centerOffset = ((height / 2) * _reusableStride) + ((width / 2) * 4);
                    b = _reusablePixels[centerOffset + 0];
                    g = _reusablePixels[centerOffset + 1];
                    r = _reusablePixels[centerOffset + 2];

                    _cachedR = r;
                    _cachedG = g;
                    _cachedB = b;
                    _hasCachedFrame = true;
                }
                finally
                {
                    _context.Unmap(_stagingTexture!, 0);
                }

                bitmap = _reusableBitmap!;
                return true;
            }
            catch
            {
                RecreateDuplication();
                return false;
            }
            finally
            {
                if (frameAcquired)
                    _duplication?.ReleaseFrame();
            }
        }
    }

    private static bool EnsureDevice()
    {
        if (_device != null && _context != null)
            return true;

        var result = D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            [],
            out _device,
            out _context);

        return result.Success;
    }

    private static bool EnsureDuplicationForPoint(int x, int y)
    {
        if (_duplication != null &&
            x >= _activeOutputLeft && x < _activeOutputRight &&
            y >= _activeOutputTop && y < _activeOutputBottom)
        {
            return true;
        }

        RecreateDuplication();

        using var factory = CreateDXGIFactory1<IDXGIFactory1>();
        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            if (factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1 adapter).Failure)
                break;

            using (adapter)
            {
                for (uint outputIndex = 0; ; outputIndex++)
                {
                    if (adapter.EnumOutputs(outputIndex, out IDXGIOutput output).Failure)
                        break;

                    using (output)
                    {
                        var desc = output.Description;
                        var bounds = desc.DesktopCoordinates;
                        if (x < bounds.Left || x >= bounds.Right || y < bounds.Top || y >= bounds.Bottom)
                            continue;

                        using var output1 = output.QueryInterface<IDXGIOutput1>();
                        _duplication = output1.DuplicateOutput(_device!);
                        if (_duplication == null)
                            return false;

                        _activeOutputLeft = bounds.Left;
                        _activeOutputTop = bounds.Top;
                        _activeOutputRight = bounds.Right;
                        _activeOutputBottom = bounds.Bottom;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool EnsureStagingTexture(int width, int height)
    {
        if (_stagingTexture != null && _stagingWidth == width && _stagingHeight == height)
            return true;

        _stagingTexture?.Dispose();

        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        _stagingTexture = _device!.CreateTexture2D(desc);
        _stagingWidth = width;
        _stagingHeight = height;

        return _stagingTexture != null;
    }

    private static void EnsureWriteableBitmap(int width, int height)
    {
        if (_reusableBitmap == null || _reusableBitmap.PixelWidth != width || _reusableBitmap.PixelHeight != height)
        {
            _reusableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            _hasCachedFrame = false;
        }
    }

    private static void EnsureReusablePixelBuffer(int width, int height)
    {
        int stride = width * 4;
        int size = stride * height;

        if (_reusablePixels == null || _reusablePixels.Length != size)
        {
            _reusablePixels = new byte[size];
            _reusableStride = stride;
            _hasCachedFrame = false;
        }
    }

    private static void RecreateDuplication()
    {
        _duplication?.Dispose();
        _duplication = null;
        _activeOutputLeft = _activeOutputTop = _activeOutputRight = _activeOutputBottom = 0;
        _hasCachedFrame = false;
    }
}
