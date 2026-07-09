using System.ComponentModel;
using ColorPicker.Services;

namespace ColorPicker.Components;

public class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    public bool SetWindowPosOnStartup
    {
        get => State.SetWindowPosOnStartup;
        set
        {
            if (State.SetWindowPosOnStartup != value)
            {
                State.SetWindowPosOnStartup = value;
                OnPropertyChanged(nameof(SetWindowPosOnStartup));
            }
        }
    }

    public bool SetZoomLevelOnStartup
    {
        get => State.SetZoomLevelOnStartup;
        set
        {
            if (State.SetZoomLevelOnStartup != value)
            {
                State.SetZoomLevelOnStartup = value;
                OnPropertyChanged(nameof(SetZoomLevelOnStartup));
            }
        }
    }

    public bool CaptureOnSelf
    {
        get => State.CaptureOnSelf;
        set
        {
            if (State.CaptureOnSelf != value)
            {
                State.CaptureOnSelf = value;
                OnPropertyChanged(nameof(CaptureOnSelf));
            }
        }
    }

    public bool BootWithCaptureEnabled
    {
        get => State.BootWithCaptureEnabled;
        set
        { 
            if (State.BootWithCaptureEnabled != value)
            {
                State.BootWithCaptureEnabled = value;
                OnPropertyChanged(nameof(BootWithCaptureEnabled));
            }
        }
    }

    public bool GlobalHotkeyEnabled
    {
        get => State.GlobalHotkeyEnabled;
        set
        { 
            if (State.GlobalHotkeyEnabled != value)
            {
                State.GlobalHotkeyEnabled = value;
                OnPropertyChanged(nameof(GlobalHotkeyEnabled));
            }
        }
    }
}
