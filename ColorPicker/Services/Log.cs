using System.Diagnostics;
using ColorPicker.Settings;

namespace ColorPicker.Services;

public static class Log
{
    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        Console.WriteLine(message);
    }

    [Conditional("DEBUG")]
    public static void Startup()
    {
        Log.Debug($"\n--- {Config.VersionNumber} ---");
        Log.Debug($"- IsFirstBoot: {State.IsFirstBoot}");
        Log.Debug($"- IsEnabled: {State.IsEnabled} (override = {Config.IsEnabledOverride.HasValue})");
        Log.Debug($"- AutoCopyToClipboard: {State.AutoCopyToClipboard}");
        Log.Debug($"- SetWindowPosOnStartup: {State.SetWindowPosOnStartup}");
        Log.Debug($"- SetZoomLevelOnStartup: {State.SetZoomLevelOnStartup}");
        Log.Debug($"- ZoomLevel: {State.ZoomLevel}");
        Log.Debug($"- CaptureOnSelf: {State.CaptureOnSelf}");
        Log.Debug($"- WindowTop: {State.WindowTop}");
        Log.Debug($"- WindowLeft: {State.WindowLeft}");
        Log.Debug($"- CurrentColorType: {State.CurrentColorType}");
        Log.Debug($"- GlobalHotkey: {State.GlobalHotkey}");
        Log.Debug($"- GlobalHotkeyEnabled: {State.GlobalHotkeyEnabled}");
        Log.Debug($"- IsWindows11 OrGreater: {Config.IsWindows11OrGreater}");
        Log.Debug("");
    }
}
