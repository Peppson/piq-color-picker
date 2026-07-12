using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ColorPicker.Services;

public static partial class GlobalHotkeyManager
{
    private const int HOTKEY_ID = 9000;
    private const int WM_HOTKEY = 0x0312;
    private static HwndSource? _source;

    public static bool Register(Window window, string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return false;

        string[] parts = hotkey
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToLower())
            .ToArray()!;

        uint modifiers = GetMappedModifiers(parts);
        var key = GetMappedKey(parts[^1]);

        if (modifiers == 0 || key == 0)
            return false;

        // Try to set hotkey
        var helper = new WindowInteropHelper(window);
        if (!Win32Api.RegisterHotKey(helper.Handle, HOTKEY_ID, modifiers, key))
            return false;

        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(HandleHotkey);
        return true;
    }

    public static void UnRegister(Window window)
    {
        if (_source == null) return;

        var helper = new WindowInteropHelper(window);
        Win32Api.UnregisterHotKey(helper.Handle, HOTKEY_ID);
        _source.RemoveHook(HandleHotkey);
        _source = null;
    }

    private static IntPtr HandleHotkey(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!State.GlobalHotkeyEnabled || State.IsSettingsOpen)
            return IntPtr.Zero;

        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            State.MainWindow.ColorPicker.ToggleIsEnabled();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static uint GetMappedModifiers(string[] parts)
    {
        uint modifier = 0;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            modifier |= MapModifier(parts[i]);
        }

        return modifier;
    }

    private static uint MapModifier(string modifier)
    {
        return modifier switch
        {
            "ctrl" or "control" => 0x0002,
            "alt" => 0x0001,
            "shift" => 0x0004,
            _ => 0
        };
    }

    private static uint GetMappedKey(string key)
    {
        if (Enum.TryParse<Key>(key, true, out var keyEnum))
            return (uint)KeyInterop.VirtualKeyFromKey(keyEnum);

        return 0;
    }

    public static bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LeftAlt || key == Key.RightAlt;
    }

    public static string BuildModifiersString(ModifierKeys modifiers)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");

        return string.Join("+", parts);
    }

    public static string BuildHotkeyString(ModifierKeys modifiers, Key key)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");

        if (key != Key.None && !IsModifierKey(key))
            parts.Add(key.ToString());

        return string.Join("+", parts);
    }
}
