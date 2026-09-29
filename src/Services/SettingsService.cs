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

    private bool _isSoundMuted = false;
    /// <summary>Silencia los sonidos que no son voces (por ejemplo, la alarma al terminar el descanso).</summary>
    public bool IsSoundMuted
    {
        get => _isSoundMuted;
        set
        {
            if (_isSoundMuted != value)
            {
                _isSoundMuted = value;
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

    private bool _showState = true;
    public bool ShowState
    {
        get => _showState;
        set
        {
            if (_showState != value)
            {
                _showState = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _showTimer = true;
    public bool ShowTimer
    {
        get => _showTimer;
        set
        {
            if (_showTimer != value)
            {
                _showTimer = value;
                OnPropertyChanged();
            }
        }
    }

    // No es una preferencia del usuario: indica si el bloqueo de teclado funciona en este equipo.
    // Se expone aquí para que la configuración pueda avisar cuando no está disponible.
    private bool _isKeyboardBlockAvailable = true;
    public bool IsKeyboardBlockAvailable
    {
        get => _isKeyboardBlockAvailable;
        set
        {
            if (_isKeyboardBlockAvailable != value)
            {
                _isKeyboardBlockAvailable = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isDebugConsoleEnabled = false;
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