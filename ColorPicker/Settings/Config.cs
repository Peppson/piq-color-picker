using System.Reflection;

namespace ColorPicker.Settings;

public static class Config
{
    public const bool IsWelcomeWindowEnabled = false;

    // Debug
    public static readonly bool? IsEnabledOverride = null; // null = disabled
    public const bool ShowDebugbutton = false;
    public const bool BootSettingsWindow = false;
    public const bool BootWelcomeWindow = false;

    public const bool Log_RendererFPS = false;
    public const bool Log_UpdateUI_FunctionCallRate = false;
    public const bool Log_UpdateUI_FPS = false;
    public const bool Log_UpdateZoomView_FPS = true;

    // Constants
    public const string GithubLink = "https://github.com/Peppson/color-grab";
    public const int MaxSamplesPerSecond = 1120; // todo
    public const int InitialZoomLevel = 19;
    public const double MinZoomLevel = 11; // Uneven needed for px centering
    public const double MaxZoomLevel = 91;
    public const int StatusMessageDuration_ms = 2000;
    public static readonly string RawVersionNumber =
        Assembly.GetExecutingAssembly().GetName().Version!.ToString(3) ??
        throw new InvalidOperationException("Failed to get version number");
    public static readonly string VersionNumber = "v" + RawVersionNumber;
}
