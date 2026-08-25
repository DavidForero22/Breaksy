using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Breaksy.Services;

/// <summary>
/// Contiene la configuración global de la aplicación.
/// </summary>
public class SettingsService : INotifyPropertyChanged
{
    private bool _isMuted = false;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isDebugConsoleEnabled = true;
    public bool IsDebugConsoleEnabled
    {
        get => _isDebugConsoleEnabled;
        set
        {
            if (_isDebugConsoleEnabled != value)
            {
                _isDebugConsoleEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}