using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ColorPicker.Models;
using ColorPicker.Services;
using ColorPicker.Settings;

namespace ColorPicker.Components;

public partial class ColorPicker : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static bool IsEnabledProxy => State.IsEnabled;
    public static string GlobalHotkeyHint =>
        State.GlobalHotkeyEnabled && !string.IsNullOrWhiteSpace(State.GlobalHotkey)
            ? $"Custom hotkey ({State.GlobalHotkey})"
            : "Custom hotkey";

    private ColorTypes _colorType;
    public ColorTypes CurrentColorType // HEX, RGB...
    {
        get => _colorType;
        set
        {
            if (_colorType != value)
            {
                _colorType = value;
                State.CurrentColorType = value;
                OnPropertyChanged(nameof(CurrentColorType));
            }
        }
    }

    private int _zoomLevel = Config.InitialZoomLevel;
    public int ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (_zoomLevel != value)
            {
                _zoomLevel = value;
                State.ZoomLevel = value;
                OnPropertyChanged(nameof(ZoomLevel));
                OnPropertyChanged(nameof(ZoomPercent));
                UpdateZoomView();
            }
        }
    }

    public int ZoomPercent =>
        (_zoomLevel - (int)Config.MinZoomLevel) * 100 / ((int)Config.MaxZoomLevel - (int)Config.MinZoomLevel); // Ugly af
    public void RefreshHotkeyHint() => OnPropertyChanged(nameof(GlobalHotkeyHint));
    public Border? Slider_1 { get; set; }
    public RepeatButton? Slider_2 { get; set; }
    public RepeatButton? Slider_3 { get; set; }

    private bool _isDragging = false;
    private POINT _dragStartMouse;
    private POINT _dragStartPos;
    private POINT _lastMousePos;
    private DateTime _lastUpdate = DateTime.UtcNow;
    private SolidColorBrush _currentBrush = new(Colors.White);
    private SolidColorBrush _invertedBrush = new(Colors.Black);
    private System.Windows.Media.Imaging.BitmapSource? _fullscreenZoomViewSource = null;
}
