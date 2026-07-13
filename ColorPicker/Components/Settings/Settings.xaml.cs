using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ColorPicker.Services;

namespace ColorPicker.Components;

public partial class Settings : UserControl
{
    public Settings()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    public void RefreshHotkeyInput() => KeybindInput.Text = State.GlobalHotkey;
    public static void RefreshHotkeyHint() => State.MainWindow.ColorPicker.RefreshHotkeyHint();
    public void Reset() => ClearFocus();

    private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (KeybindInput.IsFocused) ClearFocus(true);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Same as closing settings from titlebar
        State.MainWindow.TopTitleBar.SettingsButton_Click(sender, e);
    }

    private void KeybindInput_GotFocus(object sender, RoutedEventArgs e)
    {
        KeybindInput.BorderBrush = (System.Windows.Media.Brush)FindResource("LogoBlue");

        if (!string.IsNullOrWhiteSpace(State.GlobalHotkey))
        {
            GlobalHotkeyManager.UnRegister(State.MainWindow);
            RefreshHotkeyInput();
        }
    }

    private void KeybindInput_LostFocus(object sender, RoutedEventArgs e)
    {
        KeybindInput.BorderBrush = System.Windows.Media.Brushes.Black;
        RefreshHotkeyInput();
    }

    private void KeybindInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var modifierKey = Keyboard.Modifiers;
        var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
        e.Handled = true;

        // Back, Delete, Escape
        if (HandleCancelKeys(key))
            return;

        // Require at least one modifier
        if (modifierKey == ModifierKeys.None)
        {
            KeybindInput.Text = "Missing modifier";
            return;
        }

        // Ctrl + Alt + Shift
        if (GlobalHotkeyManager.IsModifierKey(key))
        {
            KeybindInput.Text = $"{GlobalHotkeyManager.BuildModifiersString(modifierKey)}+";
            return;
        }

        var hotkey = GlobalHotkeyManager.BuildHotkeyString(modifierKey, key);
        RegisterHotkey(hotkey);
    }

    private void RegisterHotkey(string hotkey)
    {
        var previousHotkey = State.GlobalHotkey;

        if (!GlobalHotkeyManager.Register(State.MainWindow, hotkey))
        {
            MessageService.ShowMessageBox("Failed to register hotkey. It might already be in use by another application.");
            RefreshHotkeyInput();
            return;
        }

        State.GlobalHotkey = hotkey;
        RefreshHotkeyInput();

        if (!string.Equals(previousHotkey, State.GlobalHotkey, StringComparison.Ordinal))
            RefreshHotkeyHint();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HandleCancelKeys(Key key)
    {
        // Clear hotkey
        if (key == Key.Back || key == Key.Delete || key == Key.Escape)
        {
            GlobalHotkeyManager.UnRegister(State.MainWindow);
            RefreshHotkeyInput();
            return true;
        }

        return false;
    }

    private void ClearFocus(bool clearKeyboardFocus = false)
    {
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(KeybindInput), null);
        if (clearKeyboardFocus) Keyboard.ClearFocus();
    }
}
