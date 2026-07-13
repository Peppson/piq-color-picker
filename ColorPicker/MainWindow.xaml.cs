using System.Windows;
using System.Windows.Interop;
using ColorPicker.Services;

namespace ColorPicker;

public partial class MainWindow : Window
{
    internal Components.Settings? _settingsWindow;
    public Components.Settings SettingsWindow
    {
        get
        {
            if (_settingsWindow == null)
            {
                _settingsWindow = new Components.Settings();
                Settings.Content = _settingsWindow;
            }

            return _settingsWindow;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        this.Topmost = true;

        SourceInitialized += OnSourceInitialized;
        StateChanged += OnWindowStateChanged;
        SizeChanged += OnWindowSizeOrLocationChanged;
        LocationChanged += OnWindowSizeOrLocationChanged;
        Loaded += OnLoaded;
        Closing += OnWindowClose;

        State.Init(this);
        SetStartupWindowPosition();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Handle native move/resize lifecycle and prevent titlebar double-click maximize.
        if (PresentationSource.FromVisual(this) is not HwndSource hwndSource) return;
        hwndSource.AddHook(Win32Api.OnWindowProc);

        // Register global hotkey if enabled
        if (State.GlobalHotkeyEnabled && !string.IsNullOrEmpty(State.GlobalHotkey))
        {
            if (!GlobalHotkeyManager.Register(this, State.GlobalHotkey!))
                State.GlobalHotkey = "";
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        State.IsFirstBoot = false;
        State.UpdateMainWindowPos();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        State.IsMinimized = WindowState == WindowState.Minimized;
    }

    private void OnWindowSizeOrLocationChanged(object? sender, EventArgs e)
    {
        if (!State.IsDraggingOrResizing) State.UpdateMainWindowPos();
    }

    private void OnWindowClose(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        State.Save();
        GlobalHotkeyManager.UnRegister(this);
        ScreenCaptureGPUService.Cleanup();
    }

    private void SetStartupWindowPosition()
    {
        if (!State.SetWindowPosOnStartup || State.IsFirstBoot)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        this.Top = State.WindowTop;
        this.Left = State.WindowLeft;
    }
}
