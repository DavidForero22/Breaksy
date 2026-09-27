using System.ComponentModel;
using System.Runtime.CompilerServices;
using Breaksy.Models;

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

    private InterruptionLevel _interruptionLevel = InterruptionLevel.Intermediate;
    public InterruptionLevel InterruptionLevel
    {
        get => _interruptionLevel;
        set
        {
            if (_interruptionLevel != value)
            {
                _interruptionLevel = value;
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