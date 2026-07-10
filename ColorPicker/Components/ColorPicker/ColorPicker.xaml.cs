using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorPicker.Models;
using ColorPicker.Services;
using ColorPicker.Settings;
using FontAwesome.WPF;

namespace ColorPicker.Components;

public partial class ColorPicker : UserControl, INotifyPropertyChanged
{
    public ColorPicker()
    {
        InitializeComponent();
        DataContext = this;
        ColorService.Init(this);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Spaghetti this whole thing :)
        CurrentColorType = State.CurrentColorType;

        ZoomLevel = State.SetZoomLevelOnStartup
            ? State.ZoomLevel
            : Config.InitialZoomLevel;

        SetupInputCallbacks();
        RegisterSliderParts();
        SetIsEnabledIcon(State.IsEnabled);
        UpdateColorsStatic();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        DisableInputCallbacks();
    }

    private void DropdownButton_Click(object sender, MouseButtonEventArgs e)
    {
        DropdownButton.ContextMenu.PlacementTarget = DropdownButton;
        DropdownButton.ContextMenu.Placement = PlacementMode.Bottom;
        DropdownButton.ContextMenu.HorizontalOffset = 0;
        DropdownButton.ContextMenu.VerticalOffset = 0;
        DropdownButton.ContextMenu.IsOpen = true;
    }

    private void DropdownMouse_Click(object sender, MouseButtonEventArgs e)
    {
        var mousePos = e.GetPosition((IInputElement)sender);

        DropdownButton.ContextMenu.PlacementTarget = (UIElement)sender;
        DropdownButton.ContextMenu.Placement = PlacementMode.Relative;
        DropdownButton.ContextMenu.HorizontalOffset = mousePos.X;
        DropdownButton.ContextMenu.VerticalOffset = mousePos.Y;
        DropdownButton.ContextMenu.IsOpen = true;
    }

    private void DropdownMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ColorTypes colorType)
        {
            CurrentColorType = colorType;
            UpdateColorsStatic();
        }
        e.Handled = true;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ColorService.CopyColorToClipboard();
        ColorService.UpdateMessageColor(_invertedBrush);
        e.Handled = true;
    }

    private void ToggleEnabled_Click(object sender, MouseButtonEventArgs e)
    {
        // Ignore clicks that originated from child buttons
        if (ReferenceEquals(sender, ColorView) && e.OriginalSource is DependencyObject source)
        {
            if (FindAncestor<Border>(source, DropdownButton) || FindAncestor<Border>(source, CopyButton))
                return;
        }

        ToggleIsEnabled();
        e.Handled = true;
    }

    public void ToggleIsEnabled()
    {
        State.IsEnabled = !State.IsEnabled;
        SetIsEnabledIcon(State.IsEnabled);
        OnPropertyChanged(nameof(IsEnabledProxy));

        if (State.IsEnabled) return;

        // Wait 1 frame to let WPF + DWM present updated UI before capturing fullscreen image
        ScheduleFullscreenCaptureOnNextRender();

        // Auto copy selected colorcode to clipboard when capture is paused if enabled in settings
        if (State.AutoCopyToClipboard)
        {
            _ = ColorService.CopyColorToClipboard();
            ColorService.UpdateMessageColor(_invertedBrush);
        }
    }

    private void ColorPicker_Keyboard_Click(object sender, KeyEventArgs e)
    {
        // CTRL + C
        if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _ = ColorService.CopyColorToClipboard();
            ColorService.UpdateMessageColor(_invertedBrush);
            e.Handled = true;
            return;
        }

        // Spacebar 
        if (e.Key == Key.Space)
        {
            ToggleIsEnabled();
            e.Handled = true;
            return;
        }

        HandleArrowKeyMovement(sender, e);
        UpdateUI(_lastMousePos);
        e.Handled = true;
    }

    private void HandleArrowKeyMovement(object sender, KeyEventArgs e)
    {
        // Only allow arrowkeys after capture
        if (State.IsEnabled) return;

        int nextX = _lastMousePos.X;
        int nextY = _lastMousePos.Y;

        if (e.Key == Key.Left)
            nextX--;
        else if (e.Key == Key.Right)
            nextX++;
        else if (e.Key == Key.Up)
            nextY--;
        else if (e.Key == Key.Down)
            nextY++;

        if (!State.CaptureOnSelf && State.MainWindowPos.Contains(nextX, nextY))
            return;

        _lastMousePos.X = nextX;
        _lastMousePos.Y = nextY;
    }

    private void ZoomView_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        const int step = 2;

        if (e.Delta > 0)
            ZoomLevel = Math.Min(ZoomLevel + step, (int)Config.MaxZoomLevel);
        else
            ZoomLevel = Math.Max(ZoomLevel - step, (int)Config.MinZoomLevel);

        e.Handled = true;
    }

    private void ZoomView_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isDragging = true;
            Mouse.OverrideCursor = Cursors.None;

            if (Win32Api.GetCursorPos(out _dragStartMouse))
                _dragStartPos = _lastMousePos;

            ZoomView.CaptureMouse();
        }
    }

    private void ZoomView_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || !Win32Api.GetCursorPos(out POINT currentMouse))
            return;

        int dx = currentMouse.X - _dragStartMouse.X;
        int dy = currentMouse.Y - _dragStartMouse.Y;
        int targetX = _dragStartPos.X - dx;
        int targetY = _dragStartPos.Y - dy;

        if (!State.CaptureOnSelf && State.MainWindowPos.Contains(targetX, targetY))
            return;

        _lastMousePos.X = targetX;
        _lastMousePos.Y = targetY;

        UpdateUI(_lastMousePos);
    }

    private void ZoomView_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ZoomView.ReleaseMouseCapture();

            // Set mouse pos back where we started
            Win32Api.SetCursorPos(_dragStartMouse.X, _dragStartMouse.Y);
            Mouse.OverrideCursor = null;
        }
    }

    public void MoveZoomTargetOutsideApp()
    {
        if (State.CaptureOnSelf || !State.MainWindowPos.Contains(_lastMousePos.X, _lastMousePos.Y))
            return;

        var window = State.MainWindowPos;
        _lastMousePos.X = window.Left - 1;
        _lastMousePos.Y = window.Bottom;

        UpdateUI(_lastMousePos);
    }









    private void OnNewFrame(object sender, EventArgs e)
    {
        // Clamp max fps, WPF framerates is wonky sometimes...
        const double sampleInterval = 1000.0 / Config.MaxSamplesPerSecond;

        if (!State.IsEnabled || State.IsMinimized || State.IsDraggingOrResizing)
            return;

        /* if (DateTime.UtcNow < _lastUpdate.AddMilliseconds(sampleInterval))
            return;
        _lastUpdate = DateTime.UtcNow; */ //todo

#if !RELEASE
#pragma warning disable CS0162
        if (Config.LogCaptureCount) StopwatchService.TrackFunctionCallRate();
#pragma warning restore CS0162
#endif
        if (!Win32Api.GetCursorPos(out POINT point))
            return;

        if (!State.CaptureOnSelf && State.MainWindowPos.Contains(point.X, point.Y))
            return;

        // todo add if mouse changed or color changed
        /* if (_lastMousePos.X == point.X && _lastMousePos.Y == point.Y)
            return; */
        _lastMousePos = point;

        StopwatchService.TrackFunctionCallRate(); //todo

        //StopwatchService.Start(100, "UpdateUI");
        UpdateUI(point);
        //StopwatchService.Stop();
    }

    private void UpdateUI(POINT point)
    {
        var zoomLevel = Math.Clamp(100 - _zoomLevel, 1, 100);
        var height = zoomLevel;
        var width = zoomLevel;

        if (State.IsEnabled)
            UpdateUI_CaptureEnabled(point, height, width);
        else
            UpdateUI_CaptureDisabled(point, height, width);
    }

    private void UpdateUI_CaptureEnabled(POINT point, int height, int width)
    {
        StopwatchService.Start(50); // todo

        // Grab a small image around the cursor and update zoomview and color preview live
        var (capturedImage, r, g, b) = ScreenCaptureService.GetImageWithCenterColor(point.X, point.Y, width, height);

        ZoomView.Source = capturedImage;
        UpdateColors(r, g, b);

        //ZoomView.Source = ScreenCaptureService.GetImage(point.X, point.Y, width, height);
        StopwatchService.Stop();
    }

    private void UpdateUI_CaptureDisabled(POINT point, int height, int width)
    {
        if (_fullscreenImage == null) return;

        // Grab a small image around the cursor from the saved fullscreen image and update zoomview and color preview
        var (croppedImage, r, g, b) = ScreenCaptureService.GetPausedImageWithCenterColor(_fullscreenImage, point, width, height);

        ZoomView.Source = croppedImage;
        UpdateColors(r, g, b);
    }

    private void UpdateZoomView()
    {
        var zoom = Math.Clamp(100 - _zoomLevel, 1, 100);
        var height = zoom;
        var width = zoom;
        var point = _lastMousePos;

        // Running capture, grab small image around the cursor and update zoomview
        if (State.IsEnabled)
        {
            ZoomView.Source = ScreenCaptureService.GetImage(point.X, point.Y, width, height);
        }

        // Paused capture, use the saved fullscreen image to update zoomview
        else if (_fullscreenImage != null)
        {
            var (croppedImage, _, _, _) = ScreenCaptureService.GetPausedImageWithCenterColor(_fullscreenImage, point, width, height);
            ZoomView.Source = croppedImage;
        }
    }

    private void UpdateColors(byte r, byte g, byte b)
    {
        if (ColorService.IsSameColor(_currentBrush, r, g, b))
            return;

        // Colors 
        _currentBrush.Color = Color.FromRgb(r, g, b);
        _invertedBrush.Color = ColorService.GetInvertedColor(r, g, b);

        // UI
        ColorService.UpdatePreviewView(_currentBrush);
        ColorService.UpdateTextContent(r, g, b, CurrentColorType);
        ColorService.UpdateThemeColors(_invertedBrush);
    }

    private void UpdateColorsStatic()
    {
        byte r = _currentBrush.Color.R;
        byte g = _currentBrush.Color.G;
        byte b = _currentBrush.Color.B;

        ColorService.UpdatePreviewView(_currentBrush);
        ColorService.UpdateTextContent(r, g, b, CurrentColorType);
        ColorService.UpdateThemeColors(_invertedBrush);
    }

    private void ScheduleFullscreenCaptureOnNextRender()
    {
        // Capture after two rendered frames to let WPF + DWM present updated UI. 
        int renderTicks = 0;
        void captureOnNextRender(object? s, EventArgs e)
        {
            if (renderTicks++ < 2) return;

            CompositionTarget.Rendering -= captureOnNextRender;

            if (State.IsEnabled) return;

            _fullscreenImage = ScreenCaptureService.GetFullScreenImage(_lastMousePos.X, _lastMousePos.Y);
            UpdateUI(_lastMousePos);
        }

        CompositionTarget.Rendering += captureOnNextRender;
    }

    public void SetIsEnabled(bool enabled)
    {
        State.IsEnabled = enabled;
        SetIsEnabledIcon(State.IsEnabled);
        OnPropertyChanged(nameof(IsEnabledProxy));
    }

    private void SetIsEnabledIcon(bool enabled)
    {
        IsEnabledIcon.Icon = enabled ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
    }

    public string GetColorType()
    {
        return CurrentColorType.ToString();
    }

    public void SetupInputCallbacks()
    {
        State.MainWindow.PreviewKeyDown += ColorPicker_Keyboard_Click;
        CompositionTarget.Rendering += OnNewFrame!;

        ZoomView.MouseWheel += ZoomView_MouseWheel;
        ZoomView.MouseDown += ZoomView_MouseDown;
        ZoomView.MouseMove += ZoomView_MouseMove;
        ZoomView.MouseUp += ZoomView_MouseUp;

#if !RELEASE
        CompositionTarget.Rendering += OnRenderingDebug;
#endif
    }

    public void DisableInputCallbacks()
    {
        State.MainWindow.PreviewKeyDown -= ColorPicker_Keyboard_Click;
        CompositionTarget.Rendering -= OnNewFrame!;

        ZoomView.MouseWheel -= ZoomView_MouseWheel;
        ZoomView.MouseDown -= ZoomView_MouseDown;
        ZoomView.MouseMove -= ZoomView_MouseMove;
        ZoomView.MouseUp -= ZoomView_MouseUp;

#if !RELEASE
        CompositionTarget.Rendering -= OnRenderingDebug;
#endif
    }

    private void RegisterSliderParts()
    {
        ZoomSlider.ApplyTemplate();

        if (ZoomSlider.Template.FindName("PART_Track", ZoomSlider) is Track track)
        {
            track.DecreaseRepeatButton.ApplyTemplate();
            track.IncreaseRepeatButton.ApplyTemplate();
            track.Thumb.ApplyTemplate();

            Slider_2 = track.DecreaseRepeatButton as RepeatButton;
            Slider_3 = track.IncreaseRepeatButton as RepeatButton;

            if (track.Thumb.Template.FindName("PART_ThumbBorder", track.Thumb) is Border thumbBorder)
            {
                Slider_1 = thumbBorder;
            }
        }
    }

    private static bool FindAncestor<T>(DependencyObject source, T target) where T : DependencyObject
    {
        DependencyObject? current = source;
        while (current != null)
        {
            if (ReferenceEquals(current, target))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

#if !RELEASE
    private void OnRenderingDebug(object? sender, EventArgs e)
    {
        if (Config.LogFPS && e is RenderingEventArgs renderingArgs)
        {
            StopwatchService.TrackRenderFps(renderingArgs.RenderingTime);
        }
    }
#endif
}
