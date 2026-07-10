using System.Reflection;

namespace ColorPicker.Settings;

public static class Config
{
    // Flags
    public const bool IsWelcomeWindowEnabled = false;

    // Debug
    public static readonly bool? IsEnabledOverride = null; // null = disabled
    public const bool ShowDebugbutton = false;
    public const bool BootSettingsWindow = false;
    public const bool BootWelcomeWindow = false;
    public const bool LogCaptureCount = true;

    // Constants
    public const int MaxSamplesPerSecond = 120;
    public const int InitialZoomLevel = 19;
    public const double MinZoomLevel = 11; // Uneven needed for px centering
    public const double MaxZoomLevel = 91;
    public const int StatusMessageDuration_ms = 2000;
    public const string GithubLink = "https://github.com/Peppson/color-grab";
    public static readonly string RawVersionNumber =
        Assembly.GetExecutingAssembly().GetName().Version!.ToString(3) ??
        throw new InvalidOperationException("Failed to get version number");
    public static readonly string VersionNumber = "v" + RawVersionNumber;
}
