using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using ColorPicker.Models;
using ColorPicker.Settings;

namespace ColorPicker.Services;

public static class ColorService
{
    private static string CurrentColorCode { get; set; } = "#FFFFFF";
    private static Components.ColorPicker _picker = null!;

    public static void Init(Components.ColorPicker pickerInstance)
    {
        _picker = pickerInstance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (byte, byte, byte) GetColorAtPos(POINT p)
    {
        IntPtr hdc = Win32Api.GetDC(IntPtr.Zero);
        uint color = Win32Api.GetPixel(hdc, p.X, p.Y);

        _ = Win32Api.ReleaseDC(IntPtr.Zero, hdc);

        byte r = (byte)(color & 0x000000FF);
        byte g = (byte)((color & 0x0000FF00) >> 8);
        byte b = (byte)((color & 0x00FF0000) >> 16);

        return (r, g, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetInvertedColor(byte r, byte g, byte b)
    {
        byte invR = (byte)(255 - r);
        byte invG = (byte)(255 - g);
        byte invB = (byte)(255 - b);

        return Color.FromRgb(invR, invG, invB);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Brush GetIconColor(bool isActive)
    {
        return isActive
            ? (Brush)Application.Current.Resources["PrimaryText"]
            : (Brush)Application.Current.Resources["OnTopDisabled"];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSameColor(SolidColorBrush brush, byte r, byte g, byte b)
    {
        return brush.Color.R == r &&
                brush.Color.G == g &&
                brush.Color.B == b;
    }

    public static async Task CopyColorToClipboard()
    {
        if (string.IsNullOrEmpty(CurrentColorCode)) return;

        string success = "Copied";
        try
        {
            Clipboard.SetText(CurrentColorCode);
        }
        catch
        {
            success = "Copy failed!";
        }

        await MessageService.ShowAsync(_picker, success, Config.MessageDuration);
    }

    public static ColorTypes StringToColorType(string colorType)
    {
        return colorType switch
        {
            "RGB" => ColorTypes.RGB,
            "HEX" => ColorTypes.HEX,
            "HSL" => ColorTypes.HSL,
            "HSV" => ColorTypes.HSV,
            "CMYK" => ColorTypes.CMYK,
            _ => ColorTypes.HEX,
        };
    }

    public static void UpdateTextContent(byte r, byte g, byte b, ColorTypes currentColorType)
    {
        string content, type;
        switch (currentColorType)
        {
            case ColorTypes.RGB:
                content = RGB(r, g, b); type = "RGB"; break;
            case ColorTypes.HEX:
                content = HEX(r, g, b); type = "HEX"; break;
            case ColorTypes.HSL:
                content = HSL(r, g, b); type = "HSL"; break;
            case ColorTypes.HSV:
                content = HSV(r, g, b); type = "HSV"; break;
            case ColorTypes.CMYK:
                content = CMYK(r, g, b); type = "CMYK"; break;
            default:
                content = "";
                type = "";
                break;
        }

        _picker.ColorTextType.Text = type;
        _picker.ColorText.Text = content;
        CurrentColorCode = content;
    }

    public static void UpdateThemeColors(SolidColorBrush brush)
    {
        // Text
        _picker.ColorTextType.Foreground = brush;
        _picker.ColorText.Foreground = brush;

        // Icons
        _picker.DropdownButtonIcon.Foreground = brush;
        _picker.CopyButtonIcon.Foreground = brush;
        _picker.IsEnabledIcon.Foreground = brush;
        _picker.InfoButtonIcon.Foreground = brush;

        // Message
        if (MessageService.IsMessageOpen)
            _picker.Message.Foreground = brush;

        // Crosshair
        _picker.CrosshairHorizontal.Stroke = brush;
        _picker.CrosshairVertical.Stroke = brush;

        // Slider
        _picker.Slider_1?.Background = brush;
        _picker.Slider_2?.Background = brush;
        _picker.Slider_3?.Background = brush;

        // Slider text %
        _picker.ZoomLevelText.Foreground = brush;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdatePreviewView(SolidColorBrush brush)
    {
        _picker.ColorPreview.Fill = brush;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateMessageColor(SolidColorBrush brush)
    {
        if (MessageService.IsMessageOpen)
            _picker.Message.Foreground = brush;
    }

    private static string HEX(byte r, byte g, byte b)
    {
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string RGB(byte r, byte g, byte b)
    {
        return $"{r},{g},{b}";
    }

    private static string HSV(byte r, byte g, byte b)
    {
        var (h, s, v) = ConvertToHSV(r, g, b);
        return $"{h:F0}°,{s * 100:F0}%,{v * 100:F0}%";
    }

    private static string HSL(byte r, byte g, byte b)
    {
        var (h, s, l) = ConvertToHSL(r, g, b);
        return $"{h:F0}°,{s * 100:F0}%,{l * 100:F0}%";
    }

    private static string CMYK(byte r, byte g, byte b)
    {
        var (c, m, y, k) = ConvertToCMYK(r, g, b);
        return $"{c * 100:F0}%,{m * 100:F0}%,{y * 100:F0}%,{k * 100:F0}%";
    }

    private static (double H, double S, double L) ConvertToHSL(byte r, byte g, byte b)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        double h = 0;
        double s = 0;
        double l = (max + min) / 2.0;

        if (delta != 0)
        {
            s = delta / (1 - Math.Abs(2 * l - 1));

            if (max == rNorm)
                h = 60 * (((gNorm - bNorm) / delta) % 6);
            else if (max == gNorm)
                h = 60 * (((bNorm - rNorm) / delta) + 2);
            else // max == bNorm
                h = 60 * (((rNorm - gNorm) / delta) + 4);
        }

        if (h < 0) h += 360;

        return (h, s, l);
    }

    private static (double H, double S, double V) ConvertToHSV(byte r, byte g, byte b)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        double h = 0;
        if (delta != 0)
        {
            if (max == rNorm)
                h = 60 * (((gNorm - bNorm) / delta) % 6);
            else if (max == gNorm)
                h = 60 * (((bNorm - rNorm) / delta) + 2);
            else // max == bNorm
                h = 60 * (((rNorm - gNorm) / delta) + 4);
        }

        if (h < 0) h += 360;

        double s = (max == 0) ? 0 : delta / max;
        double v = max;

        return (h, s, v);
    }

    private static (double C, double M, double Y, double K) ConvertToCMYK(byte r, byte g, byte b)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double k = 1 - Math.Max(rNorm, Math.Max(gNorm, bNorm));
        if (k >= 1.0 - 1e-6) // Black
            return (0, 0, 0, 1);

        double c = (1 - rNorm - k) / (1 - k);
        double m = (1 - gNorm - k) / (1 - k);
        double y = (1 - bNorm - k) / (1 - k);

        return (c, m, y, k);
    }
}
