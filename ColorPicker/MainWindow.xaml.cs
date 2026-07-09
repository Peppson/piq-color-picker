using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ColorPicker.Services;
using ColorPicker.Settings;

namespace ColorPicker;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _dragTimer = new();
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
        SetWindowPosition();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Prevent maximize from doubleclick on titlebar
        var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        hwndSource.AddHook(PreventMaximize);

        if (!GlobalHotkeyManager.Register(this, State.GlobalHotkey!)) State.GlobalHotkey = "";
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetupWindowDragTimer();

#pragma warning disable CS0162
        if (Config.IsWelcomeWindowEnabled)
        {
            IsFirstBootWindow();
        }
#pragma warning restore CS0162

        State.IsFirstBoot = false;
        State.UpdateMainWindowPos();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        State.IsMinimized = WindowState == WindowState.Minimized;
    }

    private void OnWindowSizeOrLocationChanged(object? sender, EventArgs e)
    {
        State.IsDraggingOrResizing = true;
        _dragTimer.Stop();
        _dragTimer.Start();
        State.UpdateMainWindowPos();
    }

    private void OnWindowClose(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        State.Save();
        GlobalHotkeyManager.UnRegister(this);
    }

    private void SetWindowPosition()
    {
        if (!State.SetWindowPosOnStartup || State.IsFirstBoot)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        this.Top = State.WindowTop;
        this.Left = State.WindowLeft;
    }

    private void IsFirstBootWindow()
    {
        if (!Config.BootWelcomeWindow && !State.IsFirstBoot) return;

        State.IsEnabled = false;
        var welcomeWindow = new Windows.WelcomeWindow
        {
            Owner = this
        };

        welcomeWindow.ShowDialog();
        State.IsEnabled = true;
    }

    private void SetupWindowDragTimer()
    {
        _dragTimer.Interval = TimeSpan.FromMilliseconds(25);
        _dragTimer.Tick += (s, e) =>
        {
            _dragTimer.Stop();
            State.IsDraggingOrResizing = false;
        };
    }

    private IntPtr PreventMaximize(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        handled = (msg == 0x00A3);
        return IntPtr.Zero;
    }
}
