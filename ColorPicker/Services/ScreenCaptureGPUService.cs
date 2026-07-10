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

    public static bool TryCaptureRegionWithCenterColor(int centerX, int centerY, int width, int height, out BitmapSource bitmap, out byte r, out byte g, out byte b)
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
                    return false;
                if (acquireResult.Failure)
                {
                    RecreateDuplication();
                    return false;
                }

                frameAcquired = true;

                using (desktopResource)
                using (var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>())
                {
                    int left = centerX - (width / 2);
                    int top = centerY - (height / 2);

                    int srcLeft = Math.Clamp(left - _activeOutputLeft, 0, (_activeOutputRight - _activeOutputLeft) - width);
                    int srcTop = Math.Clamp(top - _activeOutputTop, 0, (_activeOutputBottom - _activeOutputTop) - height);

                    var srcBox = new Box(srcLeft, srcTop, 0, srcLeft + width, srcTop + height, 1);
                    _context!.CopySubresourceRegion(_stagingTexture!, 0, 0, 0, 0, desktopTexture, 0, srcBox);
                }

                var mapped = _context!.Map(_stagingTexture!, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
                try
                {
                    EnsureWriteableBitmap(width, height);
                    int rowPitch = (int)mapped.RowPitch;
                    _reusableBitmap!.WritePixels(new Int32Rect(0, 0, width, height), mapped.DataPointer, rowPitch * height, rowPitch);

                    int centerOffset = ((height / 2) * rowPitch) + ((width / 2) * 4);
                    b = System.Runtime.InteropServices.Marshal.ReadByte(mapped.DataPointer, centerOffset + 0);
                    g = System.Runtime.InteropServices.Marshal.ReadByte(mapped.DataPointer, centerOffset + 1);
                    r = System.Runtime.InteropServices.Marshal.ReadByte(mapped.DataPointer, centerOffset + 2);
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

    public static bool TryCaptureRegion(int centerX, int centerY, int width, int height, out BitmapSource bitmap)
    {
        return TryCaptureRegionWithCenterColor(centerX, centerY, width, height, out bitmap, out _, out _, out _);
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
        }
    }

    private static void RecreateDuplication()
    {
        _duplication?.Dispose();
        _duplication = null;
        _activeOutputLeft = _activeOutputTop = _activeOutputRight = _activeOutputBottom = 0;
    }
}
