using System.Windows;
using ColorPicker.Models;
using ColorPicker.Settings;

namespace ColorPicker.Services;

public static class State
{
    // Persistent
    public static bool IsFirstBoot { get; set; }
    public static string? GlobalHotkey { get; set; }
    public static bool GlobalHotkeyEnabled { get; set; }
    public static bool SetWindowPosOnStartup { get; set; }
    public static bool SetZoomLevelOnStartup { get; set; }
    public static bool AutoCopyToClipboard { get; set; }
    public static bool CaptureOnSelf { get; set; }
    public static ColorTypes CurrentColorType { get; set; }
    public static double WindowTop { get; set; }
    public static double WindowLeft { get; set; }

    // Runtime
    public static bool IsEnabled { get; set; }
    public static int ZoomLevel { get; set; }
    public static bool IsMinimized { get; set; } = false;
    public static bool IsSettingsOpen { get; set; } = false;
    public static bool IsDraggingOrResizing { get; set; } = false;
    public static MainWindow MainWindow { get; private set; } = null!;
    public static System.Drawing.Rectangle MainWindowPos { get; private set; }

    private static bool _isResetting = false;

    public static void Init(MainWindow window)
    {
        MainWindow = window;
        LoadFromMemory();

#if !RELEASE
        if (Config.IsEnabledOverride != null)
            IsEnabled = Config.IsEnabledOverride.Value;

        Log.Startup();
#endif
    }

    public static void LoadFromMemory()
    {
        IsFirstBoot = Properties.Settings.Default.IsFirstBoot;
        GlobalHotkey = Properties.Settings.Default.GlobalHotkey;
        GlobalHotkeyEnabled = Properties.Settings.Default.GlobalHotkeyEnabled;
        WindowTop = Properties.Settings.Default.WindowTop;
        WindowLeft = Properties.Settings.Default.WindowLeft;
        AutoCopyToClipboard = Properties.Settings.Default.AutoCopyToClipboard;
        CaptureOnSelf = Properties.Settings.Default.CaptureColorOnSelf;
        SetWindowPosOnStartup = Properties.Settings.Default.SetWindowPosOnStartup;
        SetZoomLevelOnStartup = Properties.Settings.Default.SetZoomLevelOnStartup;
        ZoomLevel = Properties.Settings.Default.ZoomLevel;
        CurrentColorType = ColorService.StringToColorType(Properties.Settings.Default.ColorType);

        IsEnabled = true;
    }

    public static void Save()
    {
        if (_isResetting) return; // Don't save if reseting

        Properties.Settings.Default.IsFirstBoot = IsFirstBoot;
        Properties.Settings.Default.GlobalHotkey = GlobalHotkey;
        Properties.Settings.Default.GlobalHotkeyEnabled = GlobalHotkeyEnabled;
        Properties.Settings.Default.WindowTop = MainWindow.Top;
        Properties.Settings.Default.WindowLeft = MainWindow.Left;
        Properties.Settings.Default.AutoCopyToClipboard = AutoCopyToClipboard;
        Properties.Settings.Default.CaptureColorOnSelf = CaptureOnSelf;
        Properties.Settings.Default.SetWindowPosOnStartup = SetWindowPosOnStartup;
        Properties.Settings.Default.SetZoomLevelOnStartup = SetZoomLevelOnStartup;
        Properties.Settings.Default.ZoomLevel = ZoomLevel;
        Properties.Settings.Default.ColorType = CurrentColorType.ToString();

        Properties.Settings.Default.Save();
    }

    public static void UpdateMainWindowPos()
    {
        if (!MainWindow.IsLoaded) return;

        // DPI aware position
        var topLeft = MainWindow.PointToScreen(new Point(0, 0));
        var bottomRight = MainWindow.PointToScreen(new Point(MainWindow.ActualWidth, MainWindow.ActualHeight));

        MainWindowPos = new System.Drawing.Rectangle(
            (int)topLeft.X,
            (int)topLeft.Y,
            (int)(bottomRight.X - topLeft.X),
            (int)(bottomRight.Y - topLeft.Y)
        );
    }

    public static void ResetDebug()
    {
        _isResetting = true;
        Properties.Settings.Default.Reset();
        Properties.Settings.Default.Save();

        // Force restart
        var currentExe = Environment.ProcessPath ??
            throw new InvalidOperationException("Could not get process path");
        System.Diagnostics.Process.Start(currentExe);
        Application.Current.Shutdown();
    }
}
