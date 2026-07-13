using System.Reflection;
using System.Windows;

namespace ColorPicker.Settings;

public static class Config
{
    // Debug
    public static readonly bool? IsEnabledOverride = null; // null = disabled
    public const bool ShowDebugbutton = false;
    public const bool BootSettingsWindow = false;

    public const bool Log_RendererFPS = false;
    public const bool Log_UpdateUI_Frametimes = false;
    public const bool Log_UpdateUI_FunctionCallRate = false;
    public const bool Log_UpdateZoomView_Frametimes = false;
    public const bool Log_UpdateZoomView_FunctionCallRate = false;

    // Constants 
    public const string AppName = "Piq";
    public const string AppNameFull = $"{AppName} - Color Picker";
    public const string GithubLink = "https://github.com/Peppson/piq-color-picker";
    public const double BottomCornerRadius = 5;
    public const int InitialZoomLevel = 19;
    public const double MinZoomLevel = 11; // Uneven needed for px centering
    public const double MaxZoomLevel = 91;
    public const int StatusMessageDuration_ms = 2000;

    public static readonly string VersionNumber =
        Assembly.GetExecutingAssembly().GetName().Version!.ToString(3) ?? throw new InvalidOperationException("Failed to get version number");
    public static bool IsWindows11OrGreater => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
    public static double EffectiveWindowCornerRadius => IsWindows11OrGreater ? BottomCornerRadius : 0;
    public static CornerRadius WindowBottomCornerRadius => new(0, 0, EffectiveWindowCornerRadius, EffectiveWindowCornerRadius);
}
