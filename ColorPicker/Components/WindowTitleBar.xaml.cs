using System.Windows;
using System.Windows.Controls;
using ColorPicker.Services;
using ColorPicker.Settings;

namespace ColorPicker.Components;

public partial class WindowTitleBar : UserControl
{
    public WindowTitleBar()
    {
        InitializeComponent();

#if !RELEASE
#pragma warning disable CS0162
        if (Config.ShowDebugbutton)
            DebugButton.Visibility = Visibility.Visible;

        if (Config.BootSettingsWindow)
            Loaded += SettingsButton_Click;
#pragma warning restore CS0162
#endif
    }

    private void OnTopButton_Click(object sender, RoutedEventArgs e)
    {
        State.MainWindow.Topmost = !State.MainWindow.Topmost;
        OnTopButtonIcon.Foreground = ColorService.GetIconColor(State.MainWindow.Topmost);
    }

    public void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        State.IsSettingsOpen = !State.IsSettingsOpen;

        if (State.IsSettingsOpen)
        {
            State.MainWindow.ColorPicker.DisableInput();
            State.MainWindow.SettingsWindow.RefreshHotkeyInput();
            State.MainWindow.ColorPicker.Visibility = Visibility.Collapsed;
            State.MainWindow.SettingsWindow.Visibility = Visibility.Visible;
        }
        else
        {
            State.MainWindow.ColorPicker.EnableInput();
            State.MainWindow.SettingsWindow.Reset();
            State.MainWindow.ColorPicker.Visibility = Visibility.Visible;
            State.MainWindow.SettingsWindow.Visibility = Visibility.Collapsed;
        }
    }

    private void OnMinimizeButton_Click(object sender, RoutedEventArgs e) =>
        State.MainWindow.WindowState = WindowState.Minimized;

    private void OnCloseButton_Click(object sender, RoutedEventArgs e) =>
        State.MainWindow.Close();

    private void DebugButton_Click(object sender, RoutedEventArgs e) =>
        State.ResetDebug();
}
